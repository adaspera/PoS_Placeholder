using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Services;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/products")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IImageService _imageService;

    public ProductsController(ApplicationDbContext db, UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager, IImageService imageService, IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _imageService = imageService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllProducts()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var businessProducts = await _db.Products.Where(product => product.BusinessId == userBusinessId).ToListAsync();

        
        return Ok(businessProducts);
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [Authorize]
    public async Task<IActionResult> GetProductById(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product ID.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == userBusinessId);
        if (product == null)
        {
            return NotFound("Product not found.");
        }

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto createProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var businessId = user.BusinessId;

            var newProduct = new Product
            {
                Name = createProductDto.Name,
                ItemGroup = createProductDto.ItemGroup,
                BusinessId = businessId,
            };

            _db.Products.Add(newProduct);
            await _db.SaveChangesAsync();
            
            return CreatedAtRoute("GetProductById", new { id = newProduct.Id }, newProduct);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductDto updateProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;

            var product = await _db.Products.FindAsync(updateProductDto);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this product.");
            }

            product.Name = updateProductDto.Name;
            product.ItemGroup = updateProductDto.ItemGroup;

            

            _db.Products.Update(product);
            await _db.SaveChangesAsync();

            return Ok(product);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;

            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this product.");
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}