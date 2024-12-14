using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using PoS_Placeholder.Server.Services;


namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/orders")]
public class OrderController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly OrderRepository _orderRepository;
    private readonly DiscountRepository _discountRepository;
    private readonly ITaxService _taxService;
    private readonly ApplicationDbContext _db;

    public OrderController(UserManager<User> userManager, OrderRepository orderRepository, ITaxService taxService,
        ApplicationDbContext db, DiscountRepository discountRepository)
    {
        _userManager = userManager;
        _orderRepository = orderRepository;
        _discountRepository = discountRepository;
        _taxService = taxService;
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllOrders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var orders = await _orderRepository.GetOrdersByBusinessIdAsync(user.BusinessId);
        if (orders == null)
            return NotFound("Orders not found.");

        var orderResponseDtos = orders.Select(order =>
        {
            var totalPrice = order.Products.Sum(p => p.Price * p.Quantity)
                             + order.Taxes.Sum(t => t.TaxAmount)
                             + (order.Tip ?? 0m);
            return new OrderResponseDto
            {
                Id = order.Id,
                Tip = order.Tip,
                Date = order.Date,
                Status = order.Status.ToString(),
                TotalPrice = totalPrice,
                Products = order.Products.Select(pa => new OrderProductDto
                {
                    FullName = pa.FullName,
                    Price = pa.Price,
                    Quantity = pa.Quantity
                }).ToList()
            };
        }).ToList();

        return Ok(orderResponseDtos);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var order = await _orderRepository.GetOrderByOrderIdAndBusinessIdAsync(id, user.BusinessId);
        if (order == null)
            return NotFound("Order not found.");

        var totalPrice = order.Products.Sum(p => p.Price * p.Quantity)
                         + order.Taxes.Sum(t => t.TaxAmount)
                         + (order.Tip ?? 0m);

        var orderResponseDto = new OrderResponseDto
        {
            Id = order.Id,
            Tip = order.Tip,
            Date = order.Date,
            Status = order.Status.ToString(),
            TotalPrice = totalPrice,
            Products = order.Products.Select(pa => new OrderProductDto
            {
                FullName = pa.FullName,
                Price = pa.Price,
                Quantity = pa.Quantity
            }).ToList()
        };

        return Ok(orderResponseDto);
    }

    [HttpPost("preview")]
    [Authorize]
    public async Task<IActionResult> GetOrderPaymentPreview([FromBody] CreateOrderDto createOrderDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var taxes = _taxService.GetTaxesByCountry("LIT");
        if (taxes == null)
            return NotFound("No Taxes found for LIT.");

        decimal subTotal = 0m;
        decimal discountsTotal = 0m;

        foreach (var orderItem in createOrderDto.OrderItems)
        {
            var productVariation = await _db.ProductVariations
                .Include(pv => pv.Product)
                .Include(pv => pv.Discount)
                .FirstOrDefaultAsync(pv => pv.Id == orderItem.ProductVariationId);

            if (productVariation == null)
                return BadRequest($"Product variation with Id {orderItem.ProductVariationId} not found.");

            var discount = productVariation.Discount;
            if (discount != null)
            {
                discountsTotal += discount.IsPercentage
                    ? productVariation.Price * discount.Amount / 100
                    : discount.Amount;
            }
            
            subTotal += productVariation.Price * orderItem.Quantity;
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

        taxesTotal = Math.Round(taxesTotal, 2);
        discountsTotal = Math.Round(discountsTotal, 2);
        var tip = createOrderDto.Tip ?? 0.00m;
        tip = Math.Round(tip, 2);
        decimal total = subTotal + taxesTotal + tip - discountsTotal;

        var orderPreviewDto = new OrderPreviewDto
        {
            Tip = tip,
            SubTotal = subTotal,
            TaxesTotal = taxesTotal,
            DiscountsTotal = discountsTotal,
            Total = total,
            Taxes = taxDtos
        };

        return Ok(orderPreviewDto);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var taxes = _taxService.GetTaxesByCountry("LIT");
        if (taxes == null)
            return NotFound("No Taxes found for LIT.");
        
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

                _orderRepository.Add(order);
                await _orderRepository.SaveChangesAsync();

                foreach (var orderItem in createOrderDto.OrderItems)
                {
                    var productVariation = await _db.ProductVariations
                        .Include(pv => pv.Product)
                        .Include(pv => pv.Discount)
                        .FirstOrDefaultAsync(pv => pv.Id == orderItem.ProductVariationId);

                    if (productVariation == null)
                    {
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
                    
                    if (productVariation.Discount != null)
                    {
                        var discountArchive = new DiscountArchive()
                        {
                            Amount = productVariation.Discount.Amount,
                            IsPercentage = productVariation.Discount.IsPercentage,
                            ProductFullName = fullName,
                            OrderId = order.Id
                        };

                        _db.DiscountsArchives.Add(discountArchive);
                    }
                }
                // for saving the productArchive items, so we can get them instantly afterwards
                await _db.SaveChangesAsync();
                
                var productArchives = await _db.ProductsArchive.Where(pa => pa.OrderId == order.Id).ToListAsync();
                var subtotal = productArchives.Sum(pa => pa.Price * pa.Quantity);
                
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
                // final tax changes saved and transaction commited, everything saved successfully after this point
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var orderResponseDto = new OrderResponseDto
                {
                    Id = order.Id,
                    Tip = tip,
                    Date = order.Date,
                    Status = order.Status.ToString(),
                    TotalPrice = subtotal + taxesTotal + tip,
                    Products = productArchives.Select(pa => new OrderProductDto
                    {
                        FullName = pa.FullName,
                        Price = pa.Price,
                        Quantity = pa.Quantity
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, orderResponseDto);
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error: {e.Message}, StackTrace: {e.StackTrace}");
            }
        }
    }
}