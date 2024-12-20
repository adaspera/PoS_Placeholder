﻿using Microsoft.AspNetCore.Authorization;
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
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var orders = await _orderRepository.GetOrdersByBusinessIdAsync(user.BusinessId);
        if (orders == null)
            return NotFound("Orders not found.");

        var orderResponseDtos = orders.Select(order =>
        {
            var discountsTotal = order.Discounts.Sum(d =>
                d.IsPercentage
                    ? order.Products.Where(p => p.FullName == d.ProductFullName)
                        .Sum(p => p.Price * p.Quantity) * d.Amount / 100m
                    : d.Amount);
            discountsTotal = Math.Round(discountsTotal, 2);

            var subTotal = order.Products.Sum(p => p.Price * p.Quantity);
            
            if (order.Services != null) 
                subTotal += order.Services.Sum(s => s.IsPercentage ? 0m : s.Price);

            var serviceChargeTotal = 0m;
            if (order.Services != null) 
                serviceChargeTotal += order.Services.Sum(s => s.IsPercentage ? subTotal * s.Price / 100m : 0m);
            
            subTotal = Math.Round(subTotal, 2);
            serviceChargeTotal = Math.Round((decimal)serviceChargeTotal, 2);

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

        var discountsTotal = order.Discounts.Sum(d =>
            d.IsPercentage
                ? order.Products.Where(p => p.FullName == d.ProductFullName)
                    .Sum(p => p.Price * p.Quantity) * d.Amount / 100m
                : d.Amount);
        discountsTotal = Math.Round(discountsTotal, 2);

        var subTotal = order.Products.Sum(p => p.Price * p.Quantity);

        var serviceChargeTotal = 0m;
        if (order.Services != null) 
            serviceChargeTotal += order.Services.Sum(s => s.IsPercentage ? subTotal * s.Price / 100m : 0m);

        if (order.Services != null) 
            subTotal += order.Services.Sum(s => s.IsPercentage ? 0m : s.Price);
            
        subTotal = Math.Round(subTotal, 2);
        serviceChargeTotal = Math.Round((decimal)serviceChargeTotal, 2);

        var taxesTotal = order.Taxes.Sum(t => t.TaxAmount);
        taxesTotal = Math.Round(taxesTotal, 2);

        var totalPrice = subTotal + taxesTotal + serviceChargeTotal + (order.Tip ?? 0m) - discountsTotal;
        totalPrice = Math.Round(totalPrice, 2);

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
                    return BadRequest($"Service with Id {orderServiceId} not found.");

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

                // Order entry saved to db, so we can use them instantly
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
                    foreach (int serviceId in createOrderDto.OrderServiceIds)
                    {
                        var service = await _serviceRepository.GetByIdAsync(serviceId);

                        if (service == null)
                        {
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

                // productArchive, discountArchive and serviceArchive entries saved to db, so we can get use them instantly
                await _db.SaveChangesAsync();

                var productArchives = await _db.ProductsArchive.Where(pa => pa.OrderId == order.Id).ToListAsync();
                var subtotal = productArchives.Sum(pa => pa.Price * pa.Quantity);
                
                var serviceArchives = await _db.ServicesArchive.Where(sa => sa.OrderId == order.Id).ToListAsync();
                subtotal += serviceArchives.Sum(sa => sa.IsPercentage ? 0 : sa.Price);
                
                // For the percentage from full order price charge
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

                // tax entries saved to db, so we can use them instantly
                await _db.SaveChangesAsync();

                serviceChargeTotal = Math.Round(serviceChargeTotal, 2);
                discountsTotal = Math.Round(discountsTotal, 2);
                taxesTotal = Math.Round(taxesTotal, 2);
                subtotal = Math.Round(subtotal, 2);
                var grandTotal = subtotal + taxesTotal + serviceChargeTotal + tip - discountsTotal;

                // Creating PaymentArchive entry based on the Method (payment method) received in createOrderDto
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
                            await transaction.RollbackAsync();
                            return BadRequest("Payment Intent Id not provided.");
                        }

                        newPaymentArchive.Method = PaymentMethod.Card;
                        newPaymentArchive.PaymentIntentId = createOrderDto.PaymentIntentId;
                        newPaymentArchive.GiftCardId = null;
                        break;

                    case PaymentMethod.GiftCard:
                        if (createOrderDto.GiftCardId == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest("Giftcard Id not provided.");
                        }

                        var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(
                            createOrderDto.GiftCardId.Trim(), user.BusinessId);
                        if (giftcard == null)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest("Incorrect Giftcard Id.");
                        }

                        if (giftcard.Balance < grandTotal)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest("Insufficient giftcard balance.");
                        }

                        giftcard.Balance -= grandTotal;
                        _giftcardRepository.Update(giftcard);

                        newPaymentArchive.Method = PaymentMethod.GiftCard;
                        newPaymentArchive.GiftCardId = createOrderDto.GiftCardId;
                        newPaymentArchive.PaymentIntentId = null;
                        break;

                    case PaymentMethod.Cash:
                        newPaymentArchive.Method = PaymentMethod.Cash;
                        newPaymentArchive.PaymentIntentId = null;
                        newPaymentArchive.GiftCardId = null;
                        break;

                    default:
                        await transaction.RollbackAsync();
                        return BadRequest("Unknown payment method.");
                }

                // paymentArchive (and giftcard changes if giftcard payment method) saved to db
                _db.PaymentsArchive.Add(newPaymentArchive);
                await _db.SaveChangesAsync();

                // transaction commited -> everything commited to db after this point
                await transaction.CommitAsync();

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
                var tip = Math.Round(createSplitOrderDto.Tip ?? 0.00m, 2);
                var order = new Order
                {
                    Tip = tip,
                    Date = DateTime.Now,
                    Status = OrderStatus.Closed,
                    UserId = user.Id,
                    BusinessId = user.BusinessId
                };
                // Order entry saved to db, so we can use them instantly
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
                
                foreach (int serviceId in createSplitOrderDto.OrderServiceIds)
                {
                    var service = await _serviceRepository.GetByIdAsync(serviceId);

                    if (service == null)
                    {
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

                // productArchive and discountArchive entries saved to db, so we can get use them instantly
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

                // tax entries saved to db, so we can use them instantly
                await _db.SaveChangesAsync();

                serviceChargeTotal = Math.Round(serviceChargeTotal, 2);
                discountsTotal = Math.Round(discountsTotal, 2);
                taxesTotal = Math.Round(taxesTotal, 2);
                subtotal = Math.Round(subtotal, 2);
                var grandTotal = subtotal + taxesTotal + serviceChargeTotal + tip - discountsTotal;
                
                // Check if Partial Payments are not empty
                if (createSplitOrderDto.Payments.IsNullOrEmpty())
                {
                    return BadRequest("Payments are required.");
                }
                
                // Check if partial Payments equal to the grandTotal of the order
                var sumOfPayments = createSplitOrderDto.Payments.Sum(p => p.PaidPrice);
                if (sumOfPayments != grandTotal)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("Sum of partial payments does not match the total order amount.");
                }

                foreach (var payment in createSplitOrderDto.Payments)
                {
                    // Creating PaymentArchive entry based on the Method (payment method) received in createSplitOrderDto.Payments
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
                                await transaction.RollbackAsync();
                                return BadRequest("Payment Intent Id not provided.");
                            }

                            newPaymentArchive.Method = PaymentMethod.Card;
                            newPaymentArchive.PaymentIntentId = payment.PaymentIntentId;
                            newPaymentArchive.GiftCardId = null;
                            break;

                        case PaymentMethod.GiftCard:
                            if (payment.GiftCardId == null)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest("Giftcard Id not provided.");
                            }

                            var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(
                                payment.GiftCardId.Trim(), user.BusinessId);
                            if (giftcard == null)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest("Incorrect Giftcard Id.");
                            }

                            if (giftcard.Balance < payment.PaidPrice)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest("Insufficient giftcard balance.");
                            }

                            giftcard.Balance -= payment.PaidPrice;
                            _giftcardRepository.Update(giftcard);

                            newPaymentArchive.Method = PaymentMethod.GiftCard;
                            newPaymentArchive.GiftCardId = payment.GiftCardId;
                            newPaymentArchive.PaymentIntentId = null;
                            break;

                        case PaymentMethod.Cash:
                            newPaymentArchive.Method = PaymentMethod.Cash;
                            newPaymentArchive.PaymentIntentId = null;
                            newPaymentArchive.GiftCardId = null;
                            break;

                        default:
                            await transaction.RollbackAsync();
                            return BadRequest("Unknown payment method.");
                    }
                    // paymentArchive (and giftcard changes if giftcard payment method) saved to db
                    _db.PaymentsArchive.Add(newPaymentArchive);
                }
                
                await _db.SaveChangesAsync();

                // transaction commited -> everything commited to db after this point
                await transaction.CommitAsync();

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
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error: {e.Message}, StackTrace: {e.StackTrace}");
            }
        }
    }
}