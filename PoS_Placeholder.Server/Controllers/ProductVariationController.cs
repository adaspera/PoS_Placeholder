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
[Route("/api/productVariations")]
public class ProductVariationController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IImageService _imageService;

    public ProductVariationController(ApplicationDbContext db, UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager, IImageService imageService, IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _imageService = imageService;
    }
    
    [HttpGet("{id:int}", Name = "GetAllProductVariationsById")]
    [Authorize]
    public async Task<IActionResult> GetAllProductVariationsById(int id)
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

        var productVariations = await _db.ProductVariations
            .Where(pv => pv.ProductId == id && pv.Product.BusinessId == userBusinessId)
            .ToListAsync();

         if (productVariations == null)
         {
             return NotFound("Product not found.");
         }
         
        return Ok(productVariations);
    }
    
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> CreateProductVariation([FromForm] CreateProductVariationDto createProductVariationDto)
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
            
            if (createProductVariationDto.PictureFile == null || createProductVariationDto.PictureFile.Length == 0)
            {
                return BadRequest("Picture file is required.");
            }
        
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(createProductVariationDto.PictureFile.FileName)}";
            string pictureUrl =
                await _imageService.UploadFileBlobAsync(fileName, createProductVariationDto.PictureFile);

            var newProductVariation = new ProductVariation()
            {
                Name = createProductVariationDto.Name,
                PictureUrl = pictureUrl,
                Price = createProductVariationDto.Price,
                ProductId = createProductVariationDto.ProductId
            };
            
            _db.ProductVariations.Add(newProductVariation);
            await _db.SaveChangesAsync();
            
            return CreatedAtRoute("GetAllProductVariationsById", new { id = newProductVariation.Id }, newProductVariation);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {e.Message}, StackTrace: {e.StackTrace}");
        }
        
    }
    
    //Updates but doesnt save old entry TODO
    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateProductVariation([FromForm] UpdateProductVariationDto updateProductVariationDto)
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

            var productVariation = await _db.ProductVariations.FindAsync(updateProductVariationDto.Id);
            if (productVariation == null)
            {
                return NotFound("ProductVariation not found.");
            }

            var product = await _db.Products.FindAsync(updateProductVariationDto.ProductId);
            if (productVariation == null)
            {
                return NotFound("Product not found.");
            }
            
            if (userBusinessId != product.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this product.");
            }
            
            // If user passed the new image file, we must delete old one and upload new image to image storage container
            if (updateProductVariationDto.PictureFile != null || updateProductVariationDto.PictureFile.Length > 0)
            {
                string oldFileName = productVariation.PictureUrl.Split('/').Last();
                await _imageService.DeleteFileBlobAsync(oldFileName);
            
                string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(updateProductVariationDto.PictureFile.FileName)}";
                string pictureUrl =
                    await _imageService.UploadFileBlobAsync(newFileName, updateProductVariationDto.PictureFile);
            
                productVariation.PictureUrl = pictureUrl;
            }

            productVariation.Name = updateProductVariationDto.Name;
            productVariation.Price = updateProductVariationDto.Price;
            
            _db.Products.Update(product);
            await _db.SaveChangesAsync();

            return Ok(productVariation);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteProductVariation(int id)
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

            var productVariation = await _db.ProductVariations.FindAsync(id);
            if (productVariation == null)
            {
                return NotFound("ProductVariation not found.");
            }

            var product = await _db.Products.FindAsync(productVariation.ProductId);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this product.");
            }

            _db.ProductVariations.Remove(productVariation);
            await _db.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

}