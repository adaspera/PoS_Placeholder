using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;


namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/orders")]
public class OrderController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly OrderRepository _orderRepository;

    public OrderController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        OrderRepository orderRepository, ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _orderRepository = orderRepository;
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllOrders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var businessOrders = await _orderRepository.GetOrdersByBusinessIdAsync(user.BusinessId);
        if (businessOrders == null)
            return NotFound("Orders not found.");

        var orderResponseDtos = businessOrders.Select(order =>
        {
            var totalPrice = order.Products.Sum(p => p.Price * p.Quantity);
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

        var businessOrder = await _orderRepository.GetOrderByOrderIdAndBusinessIdAsync(id, user.BusinessId);
        if (businessOrder == null)
            return NotFound("Order not found.");

        var totalPrice = businessOrder.Products.Sum(p => p.Price * p.Quantity);

        var orderResponseDto = new OrderResponseDto
        {
            Id = businessOrder.Id,
            Tip = businessOrder.Tip,
            Date = businessOrder.Date,
            Status = businessOrder.Status.ToString(),
            TotalPrice = totalPrice,
            Products = businessOrder.Products.Select(pa => new OrderProductDto
            {
                FullName = pa.FullName,
                Price = pa.Price,
                Quantity = pa.Quantity
            }).ToList()
        };

        return Ok(orderResponseDto);
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

        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                var order = new Order
                {
                    Tip = createOrderDto.Tip ?? 0.00m,
                    Date = DateTime.Now,
                    Status = OrderStatus.Closed,
                    UserId = user.Id,
                    BusinessId = user.BusinessId
                };

                _orderRepository.Add(order);
                await _orderRepository.SaveChangesAsync();

                foreach (var item in createOrderDto.OrderItems)
                {
                    var productVariation = await _db.ProductVariations
                        .Include(pv => pv.Product)
                        .FirstOrDefaultAsync(pv => pv.Id == item.ProductVariationId);

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
                        Quantity = item.Quantity,
                        OrderId = order.Id,
                    };

                    _db.ProductsArchive.Add(productArchive);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var productArchives = await _db.ProductsArchive.Where(pa => pa.OrderId == order.Id).ToListAsync();
                var totalPrice = productArchives.Sum(pa => pa.Price * pa.Quantity);

                var orderResponseDto = new OrderResponseDto
                {
                    Id = order.Id,
                    Tip = order.Tip,
                    Date = order.Date,
                    Status = order.Status.ToString(),
                    TotalPrice = totalPrice,
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