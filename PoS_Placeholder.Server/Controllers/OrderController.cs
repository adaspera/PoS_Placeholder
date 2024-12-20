using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using PoS_Placeholder.Server.Services;
using PaymentMethod = PoS_Placeholder.Server.Models.Enum.PaymentMethod;


namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/orders")]
public class OrderController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly OrderRepository _orderRepository;
    private readonly DiscountRepository _discountRepository;
    private readonly GiftcardRepository _giftcardRepository;
    private readonly ServiceRepository _serviceRepository;
    private readonly ITaxService _taxService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OrderController> _logger;

    public OrderController(UserManager<User> userManager, OrderRepository orderRepository, ITaxService taxService,
        ApplicationDbContext db, DiscountRepository discountRepository, GiftcardRepository giftcardRepository,
        ILogger<OrderController> logger, ServiceRepository serviceRepository)
    {
        _userManager = userManager;
        _orderRepository = orderRepository;
        _discountRepository = discountRepository;
        _giftcardRepository = giftcardRepository;
        _serviceRepository = serviceRepository;
        _taxService = taxService;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllOrders()
    {
        _logger.LogInformation("Received GetAllOrders request from user {UserId}", User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllOrders: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        var orders = await _orderRepository.GetOrdersByBusinessIdAsync(user.BusinessId);
        if (orders == null || !orders.Any())
        {
            _logger.LogWarning("GetAllOrders: No orders found for user {UserId} businessId={BusinessId}", user.Id,
                user.BusinessId);
            return NotFound("Orders not found.");
        }

        _logger.LogInformation("Calculating totals for {OrderCount} orders for user {UserId}", orders.Count(), user.Id);

        var orderResponseDtos = orders.Select(order =>
        {
            var discountsTotal = order.Discounts.Sum(d =>
                d.IsPercentage
                    ? order.Products.Where(p => p.FullName == d.ProductFullName).Sum(p => p.Price * p.Quantity) *
                    d.Amount / 100m
                    : d.Amount);
            discountsTotal = Math.Round(discountsTotal, 2);

            var subTotal = order.Products.Sum(p => p.Price * p.Quantity);

            var serviceChargeTotal = 0m;
            if (order.Services != null)
                serviceChargeTotal += order.Services.Sum(s => s.IsPercentage ? subTotal * s.Price / 100m : 0m);

            if (order.Services != null)
                subTotal += order.Services.Sum(s => s.IsPercentage ? 0m : s.Price);

            subTotal = Math.Round(subTotal, 2);
            serviceChargeTotal = Math.Round(serviceChargeTotal, 2);

            var taxesTotal = order.Taxes.Sum(t => t.TaxAmount);
            taxesTotal = Math.Round(taxesTotal, 2);

            var totalPrice = subTotal + taxesTotal + serviceChargeTotal + (order.Tip ?? 0m) - discountsTotal;
            totalPrice = Math.Round(totalPrice, 2);

            return new OrderResponseDto
            {
                Id = order.Id,
                Tip = order.Tip,
                Date = order.Date,
                Status = order.Status.ToString(),
                TotalPrice = totalPrice,
                SubTotal = subTotal,
                TaxesTotal = taxesTotal,
                DiscountsTotal = discountsTotal,
                ServiceChargesTotal = serviceChargeTotal,
                Products = order.Products.Select(pa => new OrderProductDto
                {
                    FullName = pa.FullName,
                    Price = pa.Price,
                    Quantity = pa.Quantity
                }).ToList(),
                Services = order.Services?.Select(sa => new OrderServiceDto
                {
                    FullName = sa.Name,
                    Price = sa.Price,
                    isPercentage = sa.IsPercentage
                }).ToList()
            };
        }).ToList();

        _logger.LogInformation("GetAllOrders: Returning {Count} orders for user {UserId}", orderResponseDtos.Count,
            user.Id);

        return Ok(orderResponseDtos);
    }


    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetOrderById(int id)
    {
        _logger.LogInformation("Received GetOrderById request for OrderId={OrderId} from user {UserId}", id,
            User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetOrderById: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        var order = await _orderRepository.GetOrderByOrderIdAndBusinessIdAsync(id, user.BusinessId);
        if (order == null)
        {
            _logger.LogWarning("GetOrderById: Order {OrderId} not found for user {UserId}", id, user.Id);
            return NotFound("Order not found.");
        }

        _logger.LogInformation("Calculating totals for OrderId={OrderId}, UserId={UserId}", id, user.Id);

        var discountsTotal = order.Discounts.Sum(d =>
            d.IsPercentage
                ? order.Products.Where(p => p.FullName == d.ProductFullName).Sum(p => p.Price * p.Quantity) * d.Amount /
                  100m
                : d.Amount);
        discountsTotal = Math.Round(discountsTotal, 2);

        var subTotal = order.Products.Sum(p => p.Price * p.Quantity);

        var serviceChargeTotal = 0m;
        if (order.Services != null)
            serviceChargeTotal += order.Services.Sum(s => s.IsPercentage ? subTotal * s.Price / 100m : 0m);

        if (order.Services != null)
            subTotal += order.Services.Sum(s => s.IsPercentage ? 0m : s.Price);

        subTotal = Math.Round(subTotal, 2);
        serviceChargeTotal = Math.Round(serviceChargeTotal, 2);

        var taxesTotal = order.Taxes.Sum(t => t.TaxAmount);
        taxesTotal = Math.Round(taxesTotal, 2);

        var totalPrice = subTotal + taxesTotal + serviceChargeTotal + (order.Tip ?? 0m) - discountsTotal;
        totalPrice = Math.Round(totalPrice, 2);

        _logger.LogInformation("GetOrderById: Computed totals for OrderId={OrderId}: TotalPrice={TotalPrice}", id,
            totalPrice);

        var orderResponseDto = new OrderResponseDto
        {
            Id = order.Id,
            Tip = order.Tip,
            Date = order.Date,
            Status = order.Status.ToString(),
            TotalPrice = totalPrice,
            SubTotal = subTotal,
            TaxesTotal = taxesTotal,
            DiscountsTotal = discountsTotal,
            ServiceChargesTotal = serviceChargeTotal,
            Products = order.Products.Select(pa => new OrderProductDto
            {
                FullName = pa.FullName,
                Price = pa.Price,
                Quantity = pa.Quantity
            }).ToList(),
            Services = order.Services?.Select(sa => new OrderServiceDto
            {
                FullName = sa.Name,
                Price = sa.Price,
                isPercentage = sa.IsPercentage
            }).ToList()
        };

        _logger.LogInformation("Returning OrderId={OrderId} details to user {UserId}", id, user.Id);
        return Ok(orderResponseDto);
    }


    [HttpPost("preview")]
    [Authorize]
    public async Task<IActionResult> GetOrderPaymentPreview([FromBody] CreateOrderDto createOrderDto)
    {
        _logger.LogInformation("Received GetOrderPaymentPreview request from user {UserId} with {ItemCount} items",
            User?.Claims.FirstOrDefault()?.Value, createOrderDto.OrderItems.Count);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("GetOrderPaymentPreview: Invalid model for user {UserId}. Errors: {Errors}",
                User?.Claims.FirstOrDefault()?.Value,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetOrderPaymentPreview: User not found or unauthorized. UserId={UserId}",
                User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        var taxes = _taxService.GetTaxesByCountry("LIT");
        if (taxes == null)
        {
            _logger.LogWarning("No Taxes found for LIT in GetOrderPaymentPreview for user {UserId}", user.Id);
            return NotFound("No Taxes found for LIT.");
        }

        decimal subTotal = 0m;
        decimal discountsTotal = 0m;

        foreach (var orderItem in createOrderDto.OrderItems)
        {
            var productVariation = await _db.ProductVariations
                .Include(pv => pv.Product)
                .Include(pv => pv.Discount)
                .FirstOrDefaultAsync(pv => pv.Id == orderItem.ProductVariationId);

            if (productVariation == null)
            {
                _logger.LogWarning(
                    "GetOrderPaymentPreview: Product variation {VariationId} not found for user {UserId}",
                    orderItem.ProductVariationId, user.Id);
                return BadRequest($"Product variation with Id {orderItem.ProductVariationId} not found.");
            }

            var discount = productVariation.Discount;
            if (discount != null && discount.EndDate >= DateTime.Now)
            {
                discountsTotal += discount.IsPercentage
                    ? productVariation.Price * orderItem.Quantity * discount.Amount / 100
                    : discount.Amount * orderItem.Quantity;
            }

            subTotal += productVariation.Price * orderItem.Quantity;
        }

        List<decimal> serviceCharges = new List<decimal>();
        if (createOrderDto.OrderServiceIds != null)
            foreach (int orderServiceId in createOrderDto.OrderServiceIds)
            {
                var service = await _serviceRepository.GetByIdAsync(orderServiceId);

                if (service == null)
                {
                    _logger.LogWarning("GetOrderPaymentPreview: Service {ServiceId} not found for user {UserId}",
                        orderServiceId, user.Id);
                    return BadRequest($"Service with Id {orderServiceId} not found.");
                }

                if (service.IsPercentage)
                {
                    serviceCharges.Add(service.ServiceCharge);
                }
                else
                {
                    subTotal += service.ServiceCharge;
                }
            }

        subTotal = Math.Round(subTotal, 2);
        decimal taxesTotal = 0m;
        var taxDtos = new List<TaxDto>();

        foreach (var tax in taxes)
        {
            decimal taxValue;
            if (tax.IsPercentage)
            {
                taxValue = subTotal * (tax.TaxAmount / 100m);
            }
            else
            {
                taxValue = tax.TaxAmount;
            }

            taxesTotal += taxValue;
            taxDtos.Add(new TaxDto
            {
                Name = tax.NameOfTax,
                Amount = Math.Round(taxValue, 2),
                IsPercentage = tax.IsPercentage
            });
        }

        var totalServiceCharge = 0m;
        totalServiceCharge += serviceCharges.Sum(sc => sc * subTotal / 100m);
        totalServiceCharge = Math.Round(totalServiceCharge, 2);

        taxesTotal = Math.Round(taxesTotal, 2);
        discountsTotal = Math.Round(discountsTotal, 2);

        var tip = createOrderDto.Tip ?? 0.00m;
        tip = Math.Round(tip, 2);

        decimal total = subTotal + taxesTotal + totalServiceCharge + tip - discountsTotal;

        _logger.LogInformation(
            "GetOrderPaymentPreview computed for user {UserId}: SubTotal={SubTotal}, Taxes={Taxes}, Tip={Tip}, Discounts={Discounts}, ServiceCharges={ServiceCharges}, Total={Total}",
            user.Id, subTotal, taxesTotal, tip, discountsTotal, totalServiceCharge, total);

        var orderPreviewDto = new OrderPreviewDto
        {
            Tip = tip,
            SubTotal = subTotal,
            TaxesTotal = taxesTotal,
            DiscountsTotal = discountsTotal,
            Total = total,
            Taxes = taxDtos,
            ServiceChargeTotal = totalServiceCharge
        };

        _logger.LogInformation("Returning preview to user {UserId}", user.Id);
        return Ok(orderPreviewDto);
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        _logger.LogInformation("Received CreateOrder request from user {UserId} with {ItemCount} items.",
            User?.Claims.FirstOrDefault()?.Value, createOrderDto.OrderItems.Count);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateOrder request from user {UserId} is invalid. ModelState: {ModelStateErrors}",
                User?.Claims.FirstOrDefault()?.Value,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("CreateOrder: User not found or unauthorized for user {UserId}.", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        var taxes = _taxService.GetTaxesByCountry("LIT");
        if (taxes == null)
        {
            _logger.LogWarning("No Taxes found for LIT. User {UserId} request cannot compute tax.", user.Id);
            return NotFound("No Taxes found for LIT.");
        }

        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                var tip = Math.Round(createOrderDto.Tip ?? 0.00m, 2);
                var order = new Order
                {
                    Tip = tip,
                    Date = DateTime.Now,
                    Status = OrderStatus.Closed,
                    UserId = user.Id,
                    BusinessId = user.BusinessId
                };

                _logger.LogInformation("Creating order record for user {UserId}", user.Id);
                _orderRepository.Add(order);
                await _orderRepository.SaveChangesAsync();

                decimal discountsTotal = 0m;

                foreach (var orderItem in createOrderDto.OrderItems)
                {
                    var productVariation = await _db.ProductVariations
                        .Include(pv => pv.Product)
                        .Include(pv => pv.Discount)
                        .FirstOrDefaultAsync(pv => pv.Id == orderItem.ProductVariationId);

                    if (productVariation == null)
                    {
                        _logger.LogWarning(
                            "Product variation {VariationId} not found for user {UserId}. Rolling back transaction.",
                            orderItem.ProductVariationId, user.Id);
                        await transaction.RollbackAsync();
                        return BadRequest("Product variation not found.");
                    }

                    string fullName = $"{productVariation.Product.Name} {productVariation.Name}";
                    decimal price = productVariation.Price;

                    var productArchive = new ProductArchive
                    {
                        FullName = fullName,
                        Price = price,
                        Quantity = orderItem.Quantity,
                        OrderId = order.Id,
                    };

                    _db.ProductsArchive.Add(productArchive);

                    var discount = productVariation.Discount;
                    if (discount != null && discount.EndDate >= DateTime.Now)
                    {
                        _logger.LogInformation(
                            "Applying discount {DiscountAmount}{IsPercentage} on {FullName} for user {UserId}.",
                            discount.Amount, discount.IsPercentage ? "%" : "", fullName, user.Id);

                        discountsTotal += discount.IsPercentage
                            ? productVariation.Price * orderItem.Quantity * discount.Amount / 100m
                            : discount.Amount * orderItem.Quantity;

                        var discountArchive = new DiscountArchive()
                        {
                            Amount = discount.Amount,
                            IsPercentage = discount.IsPercentage,
                            ProductFullName = fullName,
                            OrderId = order.Id
                        };

                        _db.DiscountsArchives.Add(discountArchive);
                    }
                }

                if (createOrderDto.OrderServiceIds != null)
                {
                    foreach (int serviceId in createOrderDto.OrderServiceIds)
                    {
                        var service = await _serviceRepository.GetByIdAsync(serviceId);

                        if (service == null)
                        {
                            _logger.LogWarning("Service {ServiceId} not found for user {UserId}. Rolling back.",
                                serviceId, user.Id);
                            await transaction.RollbackAsync();
                            return BadRequest("Bad service Id. Service not found.");
                        }

                        var serviceArchive = new ServiceArchive
                        {
                            IsPercentage = service.IsPercentage,
                            Name = service.Name,
                            OrderId = order.Id,
                            Price = service.ServiceCharge
                        };

                        _db.ServicesArchive.Add(serviceArchive);
                    }
                }

                await _db.SaveChangesAsync();

                var productArchives = await _db.ProductsArchive.Where(pa => pa.OrderId == order.Id).ToListAsync();
                var subtotal = productArchives.Sum(pa => pa.Price * pa.Quantity);

                var serviceArchives = await _db.ServicesArchive.Where(sa => sa.OrderId == order.Id).ToListAsync();
                subtotal += serviceArchives.Sum(sa => sa.IsPercentage ? 0 : sa.Price);

                // Calculate percentage-based service charges
                decimal serviceChargeTotal = 0m;
                foreach (var serviceArchive in serviceArchives)
                {
                    serviceChargeTotal += serviceArchive.IsPercentage ? subtotal * serviceArchive.Price / 100 : 0m;
                }

                var taxesTotal = 0m;

                foreach (var tax in taxes)
                {
                    var taxAmount = tax.IsPercentage ? subtotal * (tax.TaxAmount / 100m) : tax.TaxAmount;
                    taxAmount = Math.Round(taxAmount, 2);
                    taxesTotal += taxAmount;
                    var taxArchiveEntry = new TaxArchive
                    {
                        Name = tax.NameOfTax,
                        TaxAmount = taxAmount,
                        IsPercentage = tax.IsPercentage,
                        OrderId = order.Id,
                    };

                    _db.TaxesArchive.Add(taxArchiveEntry);
                }

                await _db.SaveChangesAsync();

                serviceChargeTotal = Math.Round(serviceChargeTotal, 2);
                discountsTotal = Math.Round(discountsTotal, 2);
                taxesTotal = Math.Round(taxesTotal, 2);
                subtotal = Math.Round(subtotal, 2);
                var grandTotal = subtotal + taxesTotal + serviceChargeTotal + tip - discountsTotal;

                _logger.LogInformation(
                    "Computed totals for order {OrderId} (User {UserId}): Subtotal={Subtotal}, Taxes={Taxes}, ServiceCharges={ServiceChargeTotal}, Tip={Tip}, Discounts={DiscountsTotal}, GrandTotal={GrandTotal}",
                    order.Id, user.Id, subtotal, taxesTotal, serviceChargeTotal, tip, discountsTotal, grandTotal);

                PaymentArchive newPaymentArchive = new PaymentArchive
                {
                    OrderId = order.Id,
                    PaidPrice = grandTotal
                };

                switch (createOrderDto.Method)
                {
                    case PaymentMethod.Card:
                        if (createOrderDto.PaymentIntentId == null)
                        {
                            _logger.LogWarning(
                                "PaymentIntentId not provided for card payment. Rolling back order {OrderId}.",
                                order.Id);
                            await transaction.RollbackAsync();
                            return BadRequest("Payment Intent Id not provided.");
                        }

                        newPaymentArchive.Method = PaymentMethod.Card;
                        newPaymentArchive.PaymentIntentId = createOrderDto.PaymentIntentId;
                        newPaymentArchive.GiftCardId = null;
                        _logger.LogInformation(
                            "Order {OrderId}: Card payment confirmed with PaymentIntentId {PaymentIntentId}", order.Id,
                            createOrderDto.PaymentIntentId);
                        break;

                    case PaymentMethod.GiftCard:
                        if (createOrderDto.GiftCardId == null)
                        {
                            _logger.LogWarning(
                                "GiftcardId not provided for giftcard payment. Rolling back order {OrderId}.",
                                order.Id);
                            await transaction.RollbackAsync();
                            return BadRequest("Giftcard Id not provided.");
                        }

                        var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(
                            createOrderDto.GiftCardId.Trim(), user.BusinessId);
                        if (giftcard == null)
                        {
                            _logger.LogWarning("Giftcard {GiftcardId} not found. Rolling back order {OrderId}.",
                                createOrderDto.GiftCardId, order.Id);
                            await transaction.RollbackAsync();
                            return BadRequest("Incorrect Giftcard Id.");
                        }

                        if (giftcard.Balance < grandTotal)
                        {
                            _logger.LogWarning(
                                "Insufficient balance on Giftcard {GiftcardId} for order {OrderId}. Rolling back.",
                                createOrderDto.GiftCardId, order.Id);
                            await transaction.RollbackAsync();
                            return BadRequest("Insufficient giftcard balance.");
                        }

                        giftcard.Balance -= grandTotal;
                        _giftcardRepository.Update(giftcard);

                        newPaymentArchive.Method = PaymentMethod.GiftCard;
                        newPaymentArchive.GiftCardId = createOrderDto.GiftCardId;
                        newPaymentArchive.PaymentIntentId = null;
                        _logger.LogInformation(
                            "Order {OrderId}: Giftcard {GiftcardId} charged {GrandTotal}. New balance={NewBalance}",
                            order.Id, createOrderDto.GiftCardId, grandTotal, giftcard.Balance);
                        break;

                    case PaymentMethod.Cash:
                        newPaymentArchive.Method = PaymentMethod.Cash;
                        newPaymentArchive.PaymentIntentId = null;
                        newPaymentArchive.GiftCardId = null;
                        _logger.LogInformation("Order {OrderId}: Paid by cash {GrandTotal}", order.Id, grandTotal);
                        break;

                    default:
                        _logger.LogWarning(
                            "Unknown payment method {Method} in createOrder for user {UserId}. Rolling back order {OrderId}.",
                            createOrderDto.Method, user.Id, order.Id);
                        await transaction.RollbackAsync();
                        return BadRequest("Unknown payment method.");
                }

                _db.PaymentsArchive.Add(newPaymentArchive);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, user.Id);

                var orderResponseDto = new OrderResponseDto
                {
                    Id = order.Id,
                    Tip = tip,
                    Date = order.Date,
                    Status = order.Status.ToString(),
                    TotalPrice = grandTotal,
                    SubTotal = subtotal,
                    TaxesTotal = taxesTotal,
                    DiscountsTotal = discountsTotal,
                    Products = productArchives.Select(pa => new OrderProductDto
                    {
                        FullName = pa.FullName,
                        Price = pa.Price,
                        Quantity = pa.Quantity
                    }).ToList(),
                    Services = serviceArchives.Select(sa => new OrderServiceDto
                    {
                        FullName = sa.Name,
                        Price = sa.Price,
                        isPercentage = sa.IsPercentage
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, orderResponseDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Error creating order for user {UserId}. Rolling back transaction. Message: {Message}",
                    user.Id, e.Message);
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error: {e.Message}, StackTrace: {e.StackTrace}");
            }
        }
    }


    [HttpPost("createSplitOrder")]
    [Authorize]
    public async Task<IActionResult> CreateSplitOrder([FromBody] CreateSplitOrderDto createSplitOrderDto)
    {
        _logger.LogInformation(
            "Received CreateSplitOrder request from user {UserId} with {ItemCount} items and {PaymentCount} partial payments.",
            User?.Claims.FirstOrDefault()?.Value, createSplitOrderDto.OrderItems.Count, createSplitOrderDto.Payments?.Count ?? 0);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateSplitOrder request from user {UserId} is invalid. ModelState: {ModelStateErrors}",
                User?.Claims.FirstOrDefault()?.Value,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("CreateSplitOrder: User not found or unauthorized for user {UserId}.",
                User?.Identity?.Name);
            return Unauthorized("User not found.");
        }

        var taxes = _taxService.GetTaxesByCountry("LIT");
        if (taxes == null)
        {
            _logger.LogWarning("No Taxes found for LIT in CreateSplitOrder. User {UserId} request cannot compute tax.",
                user.Id);
            return NotFound("No Taxes found for LIT.");
        }

        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                var tip = Math.Round(createSplitOrderDto.Tip ?? 0.00m, 2);
                var order = new Order
                {
                    Tip = tip,
                    Date = DateTime.Now,
                    Status = OrderStatus.Closed,
                    UserId = user.Id,
                    BusinessId = user.BusinessId
                };

                _logger.LogInformation("Creating order record for user {UserId} in CreateSplitOrder", user.Id);
                _orderRepository.Add(order);
                await _orderRepository.SaveChangesAsync();

                decimal discountsTotal = 0m;

                foreach (var orderItem in createSplitOrderDto.OrderItems)
                {
                    var productVariation = await _db.ProductVariations
                        .Include(pv => pv.Product)
                        .Include(pv => pv.Discount)
                        .FirstOrDefaultAsync(pv => pv.Id == orderItem.ProductVariationId);

                    if (productVariation == null)
                    {
                        _logger.LogWarning(
                            "CreateSplitOrder: Product variation {VariationId} not found for user {UserId}. Rolling back.",
                            orderItem.ProductVariationId, user.Id);
                        await transaction.RollbackAsync();
                        return BadRequest("Product variation not found.");
                    }

                    string fullName = $"{productVariation.Product.Name} {productVariation.Name}";
                    decimal price = productVariation.Price;

                    var productArchive = new ProductArchive
                    {
                        FullName = fullName,
                        Price = price,
                        Quantity = orderItem.Quantity,
                        OrderId = order.Id,
                    };

                    _db.ProductsArchive.Add(productArchive);

                    var discount = productVariation.Discount;
                    if (discount != null && discount.EndDate >= DateTime.Now)
                    {
                        _logger.LogInformation(
                            "Applying discount {DiscountAmount}{IsPercentage} on {FullName} (SplitOrder) for user {UserId}.",
                            discount.Amount, discount.IsPercentage ? "%" : "", fullName, user.Id);

                        discountsTotal += discount.IsPercentage
                            ? productVariation.Price * orderItem.Quantity * discount.Amount / 100m
                            : discount.Amount * orderItem.Quantity;

                        var discountArchive = new DiscountArchive()
                        {
                            Amount = discount.Amount,
                            IsPercentage = discount.IsPercentage,
                            ProductFullName = fullName,
                            OrderId = order.Id
                        };

                        _db.DiscountsArchives.Add(discountArchive);
                    }
                }

                if (createSplitOrderDto.OrderServiceIds != null)
                {
                    foreach (int serviceId in createSplitOrderDto.OrderServiceIds)
                    {
                        var service = await _serviceRepository.GetByIdAsync(serviceId);

                        if (service == null)
                        {
                            _logger.LogWarning(
                                "CreateSplitOrder: Service {ServiceId} not found for user {UserId}. Rolling back.",
                                serviceId, user.Id);
                            await transaction.RollbackAsync();
                            return BadRequest("Bad service Id. Service not found.");
                        }

                        var serviceArchive = new ServiceArchive
                        {
                            IsPercentage = service.IsPercentage,
                            Name = service.Name,
                            OrderId = order.Id,
                            Price = service.ServiceCharge
                        };

                        _db.ServicesArchive.Add(serviceArchive);
                    }
                }

                await _db.SaveChangesAsync();

                var productArchives = await _db.ProductsArchive.Where(pa => pa.OrderId == order.Id).ToListAsync();
                var subtotal = productArchives.Sum(pa => pa.Price * pa.Quantity);

                var serviceArchives = await _db.ServicesArchive.Where(sa => sa.OrderId == order.Id).ToListAsync();
                subtotal += serviceArchives.Sum(sa => sa.IsPercentage ? 0 : sa.Price);

                decimal serviceChargeTotal = 0m;
                foreach (var serviceArchive in serviceArchives)
                {
                    serviceChargeTotal += serviceArchive.IsPercentage ? subtotal * serviceArchive.Price / 100 : 0m;
                }

                var taxesTotal = 0m;
                foreach (var tax in taxes)
                {
                    var taxAmount = tax.IsPercentage ? subtotal * (tax.TaxAmount / 100m) : tax.TaxAmount;
                    taxAmount = Math.Round(taxAmount, 2);
                    taxesTotal += taxAmount;
                    var taxArchiveEntry = new TaxArchive
                    {
                        Name = tax.NameOfTax,
                        TaxAmount = taxAmount,
                        IsPercentage = tax.IsPercentage,
                        OrderId = order.Id,
                    };

                    _db.TaxesArchive.Add(taxArchiveEntry);
                }

                await _db.SaveChangesAsync();

                serviceChargeTotal = Math.Round(serviceChargeTotal, 2);
                discountsTotal = Math.Round(discountsTotal, 2);
                taxesTotal = Math.Round(taxesTotal, 2);
                subtotal = Math.Round(subtotal, 2);
                var grandTotal = subtotal + taxesTotal + serviceChargeTotal + tip - discountsTotal;

                _logger.LogInformation(
                    "Computed totals for split order {OrderId} (User {UserId}): Subtotal={Subtotal}, Taxes={Taxes}, ServiceCharges={ServiceCharges}, Tip={Tip}, Discounts={Discounts}, GrandTotal={GrandTotal}",
                    order.Id, user.Id, subtotal, taxesTotal, serviceChargeTotal, tip, discountsTotal, grandTotal);

                if (createSplitOrderDto.Payments.IsNullOrEmpty())
                {
                    _logger.LogWarning(
                        "No Payments provided in CreateSplitOrder for user {UserId}. Rolling back order {OrderId}.",
                        user.Id, order.Id);
                    await transaction.RollbackAsync();
                    return BadRequest("Payments are required.");
                }

                var sumOfPayments = createSplitOrderDto.Payments.Sum(p => p.PaidPrice);
                if (sumOfPayments != grandTotal)
                {
                    _logger.LogWarning(
                        "Sum of partial payments ({SumOfPayments}) != grandTotal ({GrandTotal}) in CreateSplitOrder for order {OrderId}, user {UserId}. Rolling back.",
                        sumOfPayments, grandTotal, order.Id, user.Id);
                    await transaction.RollbackAsync();
                    return BadRequest("Sum of partial payments does not match the total order amount.");
                }

                foreach (var payment in createSplitOrderDto.Payments)
                {
                    PaymentArchive newPaymentArchive = new PaymentArchive
                    {
                        OrderId = order.Id,
                        PaidPrice = payment.PaidPrice,
                    };

                    switch (payment.Method)
                    {
                        case PaymentMethod.Card:
                            if (payment.PaymentIntentId == null)
                            {
                                _logger.LogWarning(
                                    "PaymentIntentId not provided for card partial payment in CreateSplitOrder. Rolling back order {OrderId}.",
                                    order.Id);
                                await transaction.RollbackAsync();
                                return BadRequest("Payment Intent Id not provided.");
                            }

                            newPaymentArchive.Method = PaymentMethod.Card;
                            newPaymentArchive.PaymentIntentId = payment.PaymentIntentId;
                            newPaymentArchive.GiftCardId = null;
                            _logger.LogInformation(
                                "Order {OrderId} partial card payment: PaymentIntentId {PaymentIntentId}, Amount={Amount}",
                                order.Id, payment.PaymentIntentId, payment.PaidPrice);
                            break;

                        case PaymentMethod.GiftCard:
                            if (payment.GiftCardId == null)
                            {
                                _logger.LogWarning(
                                    "GiftcardId not provided for giftcard partial payment in CreateSplitOrder. Rolling back order {OrderId}.",
                                    order.Id);
                                await transaction.RollbackAsync();
                                return BadRequest("Giftcard Id not provided.");
                            }

                            var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(
                                payment.GiftCardId.Trim(), user.BusinessId);
                            if (giftcard == null)
                            {
                                _logger.LogWarning(
                                    "Giftcard {GiftcardId} not found for partial payment in CreateSplitOrder. Rolling back order {OrderId}.",
                                    payment.GiftCardId, order.Id);
                                await transaction.RollbackAsync();
                                return BadRequest("Incorrect Giftcard Id.");
                            }

                            if (giftcard.Balance < payment.PaidPrice)
                            {
                                _logger.LogWarning(
                                    "Insufficient balance on Giftcard {GiftcardId} for partial payment {Amount} in order {OrderId}. Rolling back.",
                                    payment.GiftCardId, payment.PaidPrice, order.Id);
                                await transaction.RollbackAsync();
                                return BadRequest("Insufficient giftcard balance.");
                            }

                            giftcard.Balance -= payment.PaidPrice;
                            _giftcardRepository.Update(giftcard);

                            newPaymentArchive.Method = PaymentMethod.GiftCard;
                            newPaymentArchive.GiftCardId = payment.GiftCardId;
                            newPaymentArchive.PaymentIntentId = null;
                            _logger.LogInformation(
                                "Order {OrderId} partial giftcard payment: GiftcardId {GiftcardId}, Amount={Amount}, NewBalance={NewBalance}",
                                order.Id, payment.GiftCardId, payment.PaidPrice, giftcard.Balance);
                            break;

                        case PaymentMethod.Cash:
                            newPaymentArchive.Method = PaymentMethod.Cash;
                            newPaymentArchive.PaymentIntentId = null;
                            newPaymentArchive.GiftCardId = null;
                            _logger.LogInformation("Order {OrderId} partial cash payment: Amount={Amount}", order.Id,
                                payment.PaidPrice);
                            break;

                        default:
                            _logger.LogWarning(
                                "Unknown payment method {Method} in CreateSplitOrder for user {UserId}, order {OrderId}. Rolling back.",
                                payment.Method, user.Id, order.Id);
                            await transaction.RollbackAsync();
                            return BadRequest("Unknown payment method.");
                    }

                    _db.PaymentsArchive.Add(newPaymentArchive);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Split order {OrderId} created successfully for user {UserId}", order.Id,
                    user.Id);

                var orderResponseDto = new OrderResponseDto
                {
                    Id = order.Id,
                    Tip = tip,
                    Date = order.Date,
                    Status = order.Status.ToString(),
                    TotalPrice = grandTotal,
                    SubTotal = subtotal,
                    ServiceChargesTotal = serviceChargeTotal,
                    TaxesTotal = taxesTotal,
                    DiscountsTotal = discountsTotal,
                    Products = productArchives.Select(pa => new OrderProductDto
                    {
                        FullName = pa.FullName,
                        Price = pa.Price,
                        Quantity = pa.Quantity
                    }).ToList(),
                    Services = serviceArchives.Select(sa => new OrderServiceDto
                    {
                        FullName = sa.Name,
                        Price = sa.Price,
                        isPercentage = sa.IsPercentage
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, orderResponseDto);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Error creating split order for user {UserId}. Rolling back transaction. Message: {Message}",
                    user.Id, e.Message);
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error: {e.Message}, StackTrace: {e.StackTrace}");
            }
        }
    }
}