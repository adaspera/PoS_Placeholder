using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using Stripe;

namespace PoS_Placeholder.Server.Controllers;

[Route("api/business")]
[ApiController]
public class BusinessController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly ProductRepository _productRepository;
    public BusinessController(ApplicationDbContext context, UserManager<User> userManager, ProductRepository productRepository)
    {
        _db = context;
        _userManager = userManager;
        _productRepository = productRepository;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SuperAdmin))] // Restrict to Super Admin
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessDto dto)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState); // 422 Validation exception
        }

        // Check for uniqueness
        if (_db.Businesses.Any(b => b.Phone == dto.Phone || b.Email == dto.Email))
        {
            return Conflict("Phone/email already in use."); // 409 Conflict
        }

        var business = new Business
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Street = dto.Street,
            City = dto.City,
            Region = dto.Region,
            Country = dto.Country
        };

        try
        {
            _db.Businesses.Add(business);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateBusiness), new { id = business.Id }, business); // 201 Created
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the business.");
        }
    }

    [HttpPut("{business_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> UpdateBusiness(int business_id, [FromBody] UpdateBusinessDto dto)
    {
        // Find the business by ID
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Update fields only if provided
        if (!string.IsNullOrWhiteSpace(dto.Name)) business.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) business.Phone = dto.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Email)) business.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Street)) business.Street = dto.Street;
        if (!string.IsNullOrWhiteSpace(dto.City)) business.City = dto.City;
        if (!string.IsNullOrWhiteSpace(dto.Region)) business.Region = dto.Region;
        if (!string.IsNullOrWhiteSpace(dto.Country)) business.Country = dto.Country;

        // Check for uniqueness constraints
        if (_db.Businesses.Any(b => (b.Phone == business.Phone || b.Email == business.Email) && b.Id != business_id))
        {
            return Conflict("Phone/email already exists.");
        }

        // Save changes
        try
        {
            await _db.SaveChangesAsync();
            return Ok(business);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while updating the business.");
        }
    }
    [HttpGet("{business_id:int}/product")]
    [Authorize(Roles = nameof(UserRole.Owner) + "," + nameof(UserRole.Employee))] // Access restricted to business owner or employee
    public async Task<IActionResult> GetProducts(int business_id, int page, int get_amount)
    {
        // Check if the business exists
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Apply pagination
        var products = await _db.Products
            .Where(p => p.BusinessId == business_id)
            .Skip((page - 1) * get_amount)
            .Take(get_amount)
            .ToListAsync();

        return Ok(products);
    }

    [HttpPost("{business_id}/product")]
    [Authorize(Roles = nameof(UserRole.Owner) + "," + nameof(UserRole.Employee))] // Business owner or employee
    public async Task<IActionResult> CreateProduct(int business_id, [FromForm] CreateProductDto createProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the business exists
            var business = await _db.Businesses.FindAsync(business_id);
            if (business == null)
            {
                return NotFound("Business not found.");
            }

            // Create the product and associate it with the business
            var newProduct = new Models.Product
            {
                Name = createProductDto.Name,
                ItemGroup = createProductDto.ItemGroup,
                BusinessId = business_id,
            };

            _productRepository.Add(newProduct);
            await _productRepository.SaveChangesAsync();

            // Return the created product
            return CreatedAtAction("GetProductById", "Product", new { id = newProduct.Id }, newProduct);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }


}





/*
    [HttpGet("{business_id:int}/product/productVariation")]
    [Authorize(Roles = nameof(UserRole.Owner) + "," + nameof(UserRole.Employee))] // Only owners and employees can access this
    public async Task<IActionResult> GetProductVariations(int business_id, int page, int get_amount)
    {
        // Check if the business exists
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Fetch product variations related to the business, applying pagination
        var variations = await _db.ProductVariations
            .Where(v => v.Product.BusinessId == business_id)  // Assuming there's a relationship between products and variations
            .Skip((page - 1) * get_amount)
            .Take(get_amount)
            .ToListAsync();

        return Ok(variations);
    }

    [HttpPost("{business_id:int}/discount")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Only business owner can create discounts
    public async Task<IActionResult> CreateDiscount(int business_id, [FromBody] CreateDiscount discountDto)
    {
        // Check if the business exists
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Validate discount data (e.g., check if start date is before end date, amount is within range)
        if (discountDto.StartDate >= discountDto.EndDate)
        {
            return UnprocessableEntity("Starting date is later than the end date.");
        }
        if (discountDto.Amount < 0 || discountDto.Amount > 100)
        {
            return UnprocessableEntity("The amount doesn't fall into the valid range (0 to 100).");
        }

        // Create the discount entity
        var discount = new Discount
        {
            BusinessId = business_id,
            Amount = discountDto.Amount,
            StartDate = discountDto.StartDate,
            EndDate = discountDto.EndDate,
            Description = discountDto.Description
        };

        // Save the discount in the database
        _db.Discounts.Add(discount);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDiscount), new { business_id = business_id, discount_id = discount.Id }, discount);
    }
    [HttpGet("{business_id:int}/discount")]
    [Authorize(Roles = nameof(UserRole.Owner) + "," + nameof(UserRole.Employee))] // Owners and employees can access
    public async Task<IActionResult> GetDiscounts(int business_id, int page, int get_amount)
    {
        // Check if the business exists
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Fetch discounts with pagination
        var discounts = await _db.Discounts
            .Where(d => d.BusinessId == business_id)
            .Skip((page - 1) * get_amount)
            .Take(get_amount)
            .ToListAsync();

        return Ok(discounts);
    }
    [HttpPost("{business_id:int}/discount/discountForItem")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Only the business owner can create discounts
    public async Task<IActionResult> CreateDiscountForItems(int business_id, [FromBody] CreateDiscountForItems discountDto)
    {
        // Check if the business exists
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Validate the discount data
        if (discountDto.StartDate >= discountDto.EndDate)
        {
            return UnprocessableEntity("Starting date is later than the end date.");
        }

        if (discountDto.Amount < 0 || discountDto.Amount > 100)
        {
            return UnprocessableEntity("The discount amount should be between 0 and 100.");
        }

        // Create the discount entity
        var discount = new Discount
        {
            BusinessId = business_id,
            Amount = discountDto.Amount,
            StartDate = discountDto.StartDate,
            EndDate = discountDto.EndDate,
            Description = discountDto.Description
        };

        // Save the discount in the database
        _db.Discounts.Add(discount);
        await _db.SaveChangesAsync();

        // Validate item IDs based on the type
        if (discountDto.Type == "itemGroup")
        {
            var products = await _db.Products
                .Where(p => discountDto.ItemIds.Contains(p.Id) && p.ItemGroup == "Drink") // Example: Filter by itemGroup
                .ToListAsync();

            if (products.Count != discountDto.ItemIds.Count)
            {
                return NotFound("Some item ids were not found.");
            }

            // Assign the discount to the products and their variations
            // (Here, you'll apply the discount logic to the product variations as well)
        }
        else if (discountDto.Type == "product")
        {
            var products = await _db.Products
                .Where(p => discountDto.ItemIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != discountDto.ItemIds.Count)
            {
                return NotFound("Some product ids were not found.");
            }

            // Assign the discount to the products and their variations
        }
        else if (discountDto.Type == "variation")
        {
            var variations = await _db.ProductVariations
                .Where(v => discountDto.ItemIds.Contains(v.Id))
                .ToListAsync();

            if (variations.Count != discountDto.ItemIds.Count)
            {
                return NotFound("Some variation ids were not found.");
            }

            // Assign the discount to the product variations
        }
        else
        {
            return BadRequest("Invalid type. Must be 'itemGroup', 'product', or 'variation'.");
        }

        // Create a DiscountForItems entity to associate the discount with items
        var discountForItems = new DiscountForItems
        {
            DiscountId = discount.Id,
            ItemIds = discountDto.ItemIds,
            Type = discountDto.Type
        };

        // Save the DiscountForItems entity
        _db.DiscountForItems.Add(discountForItems);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDiscountForItems), new { business_id = business_id, discount_for_item_id = discountForItems.DiscountId }, discountForItems);
    }
    [HttpGet("{business_id:int}/service/{name_of_service}/vacant-dates")]
    [Authorize(Roles = "Owner,Employee")]
    public async Task<IActionResult> GetVacantDates(int business_id, string name_of_service, [FromQuery] string date_from, [FromQuery] string date_to, [FromQuery] int page, [FromQuery] int get_amount)
    {
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        var service = await _db.Services
            .Where(s => s.BusinessId == business_id && s.Name == name_of_service)
            .FirstOrDefaultAsync();
        if (service == null)
        {
            return NotFound("Service not found.");
        }

        DateTime startDate = DateTime.TryParse(date_from, out var from) ? from : DateTime.Now;
        DateTime endDate = DateTime.TryParse(date_to, out var to) ? to : DateTime.Now.AddDays(30); // Default 30 days range

        // Retrieve vacant slots from appointments
        var vacantSlots = await _db.Appointments
            .Where(a => a.ServiceId == service.Id && a.StartTime >= startDate && a.EndTime <= endDate && a.Status == "Booked")
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        // Determine available timeslots from the service working hours and existing appointments
        var availableSlots = GetAvailableSlots(service, vacantSlots, startDate, endDate, page, get_amount);

        return Ok(availableSlots);
    }
    [HttpGet("{business_id:int}/service")]
    [Authorize(Roles = "Owner,Employee")]
    public async Task<IActionResult> GetServices(int business_id, [FromQuery] decimal? price_min, [FromQuery] decimal? price_max, [FromQuery] int page, [FromQuery] int get_amount)
    {
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        if (price_min.HasValue && price_max.HasValue && price_min.Value > price_max.Value)
        {
            return UnprocessableEntity("price_min cannot be greater than price_max.");
        }

        var servicesQuery = _db.Services.Where(s => s.BusinessId == business_id);

        if (price_min.HasValue)
        {
            servicesQuery = servicesQuery.Where(s => s.Price >= price_min.Value);
        }

        if (price_max.HasValue)
        {
            servicesQuery = servicesQuery.Where(s => s.Price <= price_max.Value);
        }

        var services = await servicesQuery
            .Skip((page - 1) * get_amount)
            .Take(get_amount)
            .ToListAsync();

        return Ok(services);
    }
    //[HttpPost("{business_id:int}/service")]
    //[Authorize(Roles = "Owner")]
    //public async Task<IActionResult> AddService(int business_id, [FromBody] CreateService serviceDto)
    //{
    //    var business = await _db.Businesses.FindAsync(business_id);
    //    if (business == null)
    //    {
    //        return NotFound("Business not found.");
    //    }

    //    var existingService = await _db.Services
    //        .Where(s => s.BusinessId == business_id && s.Name == serviceDto.Name)
    //        .FirstOrDefaultAsync();

    //    if (existingService != null)
    //    {
    //        return Conflict("The service already exists within the business.");
    //    }

    //    var newService = new Service
    //    {
    //        BusinessId = business_id,
    //        Name = serviceDto.Name,
    //        Price = serviceDto.Price,
    //        Description = serviceDto.Description
    //    };

    //    _db.Services.Add(newService);
    //    await _db.SaveChangesAsync();

    //    return CreatedAtAction(nameof(GetServices), new { business_id = business_id, service_id = newService.Id }, newService);
    //}


}
*/