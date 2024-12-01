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

        if (productVariations == null || !productVariations.Any())
        {
            return NotFound("Product variations not found.");
        }

        return Ok(productVariations);
    }
    //
    // [HttpPost]
    // [Authorize(Roles = nameof(UserRole.Owner))]
    // public async Task<IActionResult> CreateProductVariation([FromForm] CreateProductVariationDto createProductVariationDto)
    // {
    //     if (createProductVariationDto.PictureFile == null || createProductVariationDto.PictureFile.Length == 0)
    //     {
    //         return BadRequest("Picture file is required.");
    //     }
    //     
    //     string fileName = $"{Guid.NewGuid()}{Path.GetExtension(createProductVariationDto.PictureFile.FileName)}";
    //     string pictureUrl =
    //         await _imageService.UploadFileBlobAsync(fileName, createProductVariationDto.PictureFile);
    //
    // }
    //
    //
    // [HttpPut("{id:int}")]
    // [Authorize(Roles = nameof(UserRole.Owner))]
    // public async Task<IActionResult> UpdateProductVariation(int id, [FromForm] UpdateProductVariationDto updateProductVariationDto)
    // {
    //     // If user passed the new image file, we must delete old one and upload new image to image storage container
    //     if (updateProductVariationDto.PictureFile != null || updateProductVariationDto.PictureFile.Length > 0)
    //     {
    //         string oldFileName = product.PictureUrl.Split('/').Last();
    //         await _imageService.DeleteFileBlobAsync(oldFileName);
    //
    //         string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(updateProductVariationDto.PictureFile.FileName)}";
    //         string pictureUrl =
    //             await _imageService.UploadFileBlobAsync(newFileName, updateProductVariationDto.PictureFile);
    //
    //         product.PictureUrl = pictureUrl;
    //     }
    // }
    //
    // [HttpDelete("{id:int}")]
    // [Authorize(Roles = nameof(UserRole.Owner))]
    // public async Task<IActionResult> DeleteProductVariation(int id)
    // {
    //     // Delete the image
    //     if (!string.IsNullOrEmpty(product.PictureUrl))
    //     {
    //         string fileName = product.PictureUrl.Split('/').Last();
    //         await _imageService.DeleteFileBlobAsync(fileName);
    //     }
    // }

}