using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using Stripe;
using PaymentMethod = PoS_Placeholder.Server.Models.Enum.PaymentMethod;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly OrderRepository _orderRepository;
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IConfiguration configuration, OrderRepository orderRepository,
        UserManager<User> userManager, ApplicationDbContext db, ILogger<PaymentController> logger)
    {
        _configuration = configuration;
        _orderRepository = orderRepository;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    [HttpPost("makepayment")]
    [Authorize]
    public async Task<IActionResult> MakePayment([FromBody] PaymentRequestDto paymentRequestDto)
    {
        _logger.LogInformation("Received MakePayment request from user {UserId} with amount={Amount}",
            User?.Identity?.Name, paymentRequestDto.TotalAmount);

        StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

        try
        {
            if (paymentRequestDto.TotalAmount <= 0)
            {
                _logger.LogWarning("MakePayment: Invalid amount {Amount} from user {UserId}",
                    paymentRequestDto.TotalAmount, User?.Identity?.Name);
                return BadRequest("Invalid payment amount");
            }

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)Math.Round(paymentRequestDto.TotalAmount * 100m),
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
            };

            var service = new PaymentIntentService();
            var paymentIntentResponse = await service.CreateAsync(options);

            _logger.LogInformation(
                "MakePayment: Created payment intent {PaymentIntentId} for user {UserId}, amount={Amount}",
                paymentIntentResponse.Id, User?.Identity?.Name, paymentRequestDto.TotalAmount);

            var paymentResponseDto = new PaymentResponseDto
            {
                ClientSecret = paymentIntentResponse.ClientSecret,
                PaymentIntentId = paymentIntentResponse.Id
            };

            return Ok(paymentResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MakePayment for user {UserId}: {Message}", User?.Identity?.Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing payment.");
        }
    }

    [HttpPost("refund/{orderId:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> RefundOrder([FromRoute] int orderId)
    {
        _logger.LogInformation("Received RefundOrder request from user {UserId} for OrderId={OrderId}",
            User?.Identity?.Name, orderId);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("RefundOrder: User not found or unauthorized. UserId={UserId}", User?.Identity?.Name);
            return Unauthorized("User not found.");
        }

        var order = await _orderRepository.GetOrderAndPaymentsByUserIdAndBID(orderId, user.BusinessId);
        if (order == null)
        {
            _logger.LogWarning("RefundOrder: Order {OrderId} not found for user {UserId}", orderId, user.Id);
            return NotFound("Order not found.");
        }

        if (order.Status == OrderStatus.Refunded)
        {
            _logger.LogWarning("RefundOrder: Order {OrderId} is already refunded for user {UserId}", orderId, user.Id);
            return BadRequest("Order is already refunded.");
        }

        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                _logger.LogInformation("Processing refund for OrderId={OrderId}, user {UserId}", orderId, user.Id);

                decimal totalPricePaid = 0m;
                foreach (var payment in order.Payments)
                {
                    _logger.LogInformation("RefundOrder: Processing {Method} payment of {Amount} for OrderId={OrderId}",
                        payment.Method, payment.PaidPrice, orderId);
                    switch (payment.Method)
                    {
                        case PaymentMethod.Cash:
                            order.Status = OrderStatus.Refunded;
                            _orderRepository.Update(order);
                            await _db.SaveChangesAsync();
                            totalPricePaid += payment.PaidPrice;
                            _logger.LogInformation(
                                "RefundOrder: Cash refund completed for OrderId={OrderId}, Amount={Amount}", orderId,
                                payment.PaidPrice);
                            break;

                        case PaymentMethod.GiftCard:
                            if (payment.GiftCardId == null)
                            {
                                _logger.LogWarning(
                                    "RefundOrder: Missing GiftCardId for GiftCard payment in order {OrderId}", orderId);
                                await transaction.RollbackAsync();
                                return NotFound("Gift card to process refund was not found.");
                            }

                            var giftcard = _db.Giftcards.FirstOrDefault(g => g.Id == payment.GiftCardId);
                            if (giftcard == null)
                            {
                                _logger.LogWarning(
                                    "RefundOrder: GiftCard {GiftCardId} no longer exists for OrderId={OrderId}",
                                    payment.GiftCardId, orderId);
                                return NotFound("Gift card no longer exists (Not found).");
                            }

                            giftcard.Balance += payment.PaidPrice;
                            _db.Update(giftcard);
                            order.Status = OrderStatus.Refunded;
                            _orderRepository.Update(order);
                            await _db.SaveChangesAsync();
                            totalPricePaid += payment.PaidPrice;
                            _logger.LogInformation(
                                "RefundOrder: Giftcard refund completed for OrderId={OrderId}, Amount={Amount}, GiftcardId={GiftCardId}, NewBalance={NewBalance}",
                                orderId, payment.PaidPrice, payment.GiftCardId, giftcard.Balance);
                            break;

                        case PaymentMethod.Card:
                            if (payment.PaymentIntentId == null)
                            {
                                _logger.LogWarning(
                                    "RefundOrder: PaymentIntentId missing for card payment in order {OrderId}",
                                    orderId);
                                await transaction.RollbackAsync();
                                return NotFound("Transaction (Payment Intent Id) to process refund was not found.");
                            }

                            StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];

                            var refundService = new RefundService();
                            var refundOptions = new RefundCreateOptions
                            {
                                Amount = (long)Math.Round(payment.PaidPrice * 100m),
                                PaymentIntent = payment.PaymentIntentId
                            };

                            var refundResponse = await refundService.CreateAsync(refundOptions);
                            if (refundResponse.Status == "succeeded")
                            {
                                order.Status = OrderStatus.Refunded;
                                _orderRepository.Update(order);
                                await _db.SaveChangesAsync();
                                totalPricePaid += payment.PaidPrice;
                                _logger.LogInformation(
                                    "RefundOrder: Card refund succeeded for OrderId={OrderId}, Amount={Amount}, PaymentIntentId={PaymentIntentId}",
                                    orderId, payment.PaidPrice, payment.PaymentIntentId);
                            }
                            else
                            {
                                _logger.LogError(
                                    "RefundOrder: Stripe refund failed for OrderId={OrderId}, PaymentIntentId={PaymentIntentId}, Status={RefundStatus}",
                                    orderId, payment.PaymentIntentId, refundResponse.Status);
                                await transaction.RollbackAsync();
                                return StatusCode(StatusCodes.Status500InternalServerError,
                                    "Stripe refund failed. Status: " + refundResponse.Status);
                            }

                            break;

                        default:
                            _logger.LogWarning(
                                "RefundOrder: Unknown payment method {Method} for OrderId={OrderId}. Rolling back.",
                                payment.Method, orderId);
                            await transaction.RollbackAsync();
                            return BadRequest("Unknown payment method for refund.");
                    }
                }

                await transaction.CommitAsync();
                _logger.LogInformation("RefundOrder: Order {OrderId} fully refunded {TotalPricePaid} for user {UserId}",
                    orderId, totalPricePaid, user.Id);

                var orderResponseDtoWithStatusChanged = new OrderResponseDto
                {
                    Id = order.Id,
                    Tip = order.Tip,
                    Date = order.Date,
                    Status = order.Status.ToString(),
                    TotalPrice = totalPricePaid,
                    SubTotal = null,
                    TaxesTotal = null,
                    DiscountsTotal = null,
                    Products = order.Products.Select(pa => new OrderProductDto
                    {
                        FullName = pa.FullName,
                        Price = pa.Price,
                        Quantity = pa.Quantity
                    }).ToList()
                };

                return Ok(orderResponseDtoWithStatusChanged);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "RefundOrder: Error while refunding order {OrderId} for user {UserId}. Rolling back transaction. Message: {Message}",
                    orderId, user.Id, e.Message);
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error: {e.Message}, StackTrace: {e.StackTrace}");
            }
        }
    }
}