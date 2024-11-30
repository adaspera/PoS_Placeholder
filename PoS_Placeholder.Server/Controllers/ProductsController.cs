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
    private string _containerName;
    private ApiResponse _apiResponse;

    public ProductsController(ApplicationDbContext db, UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager, IImageService imageService, IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _imageService = imageService;
        _containerName = configuration.GetValue<string>("IMG_STORAGE_CONTAINER:ContainerName");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllProducts()
    {
        _apiResponse = new ApiResponse();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("User not found.");
            return Unauthorized(_apiResponse);
        }

        var userBusinessId = user.BusinessId;
        var businessProducts = await _db.Products.Where(product => product.BusinessId == userBusinessId).ToListAsync();


        _apiResponse.StatusCode = HttpStatusCode.OK;
        _apiResponse.Data = businessProducts;
        return Ok(_apiResponse);
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [Authorize]
    public async Task<IActionResult> GetProductById(int id)
    {
        _apiResponse = new ApiResponse();
        if (id <= 0)
        {
            _apiResponse.StatusCode = HttpStatusCode.BadRequest;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Invalid product ID.");
            return BadRequest(_apiResponse);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("User not found.");
            return Unauthorized(_apiResponse);
        }

        var userBusinessId = user.BusinessId;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == userBusinessId);
        if (product == null)
        {
            _apiResponse.StatusCode = HttpStatusCode.NotFound;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Product not found.");
            return NotFound(_apiResponse);
        }

        _apiResponse.StatusCode = HttpStatusCode.OK;
        _apiResponse.Data = product;
        return Ok(_apiResponse);
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

            _apiResponse = new ApiResponse();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("User not found.");
                return Unauthorized(_apiResponse);
            }

            var businessId = user.BusinessId;

            if (createProductDto.PictureFile == null || createProductDto.PictureFile.Length == 0)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("Picture file is required.");
                return BadRequest(_apiResponse);
            }

            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(createProductDto.PictureFile.FileName)}";
            string pictureUrl =
                await _imageService.UploadFileBlobAsync(fileName, _containerName, createProductDto.PictureFile);

            var newProduct = new Product
            {
                ProductName = createProductDto.ProductName,
                VariationName = createProductDto.VariationName,
                ItemGroup = createProductDto.ItemGroup,
                Price = createProductDto.Price,
                PictureUrl = pictureUrl,
                BusinessId = businessId,
            };

            _db.Products.Add(newProduct);
            await _db.SaveChangesAsync();

            _apiResponse.StatusCode = HttpStatusCode.Created;
            _apiResponse.Data = newProduct;
            return CreatedAtRoute("GetProductById", new { id = newProduct.Id }, _apiResponse);
        }
        catch (Exception ex)
        {
            _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages = new List<string>() { ex.Message };
            return StatusCode(StatusCodes.Status500InternalServerError, _apiResponse);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto updateProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _apiResponse = new ApiResponse();

            if (updateProductDto.Id != id)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("Product ID mismatch.");
                return BadRequest(_apiResponse);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("User not found.");
                return Unauthorized(_apiResponse);
            }

            var userBusinessId = user.BusinessId;

            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                _apiResponse.StatusCode = HttpStatusCode.NotFound;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("Product not found.");
                return NotFound(_apiResponse);
            }

            if (userBusinessId != product.BusinessId)
            {
                _apiResponse.StatusCode = HttpStatusCode.Forbidden;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("You do not have permission to update this product.");
                return StatusCode(StatusCodes.Status403Forbidden, _apiResponse);
            }

            product.ProductName = updateProductDto.ProductName;
            product.VariationName = updateProductDto.VariationName;
            product.ItemGroup = updateProductDto.ItemGroup;
            product.Price = updateProductDto.Price;

            // If user passed the new image file, we must delete old one and upload new image to image storage container
            if (updateProductDto.PictureFile != null || updateProductDto.PictureFile.Length > 0)
            {
                string oldFileName = product.PictureUrl.Split('/').Last();
                await _imageService.DeleteFileBlobAsync(oldFileName, _containerName);

                string newFileName = $"{Guid.NewGuid()}{Path.GetExtension(updateProductDto.PictureFile.FileName)}";
                string pictureUrl =
                    await _imageService.UploadFileBlobAsync(newFileName, _containerName, updateProductDto.PictureFile);

                product.PictureUrl = pictureUrl;
            }

            _db.Products.Update(product);
            await _db.SaveChangesAsync();

            _apiResponse.StatusCode = HttpStatusCode.NoContent;
            return Ok(_apiResponse);
        }
        catch (Exception ex)
        {
            _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages = new List<string>() { ex.Message };
            return StatusCode(StatusCodes.Status500InternalServerError, _apiResponse);
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

            _apiResponse = new ApiResponse();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("User not found.");
                return Unauthorized(_apiResponse);
            }

            var userBusinessId = user.BusinessId;

            var product = await _db.Products.FindAsync(id);
            if (product == null)
            {
                _apiResponse.StatusCode = HttpStatusCode.NotFound;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("Product not found.");
                return NotFound(_apiResponse);
            }

            if (userBusinessId != product.BusinessId)
            {
                _apiResponse.StatusCode = HttpStatusCode.Forbidden;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("You do not have permission to delete this product.");
                return StatusCode(StatusCodes.Status403Forbidden, _apiResponse);
            }

            // Delete the image
            if (!string.IsNullOrEmpty(product.PictureUrl))
            {
                string fileName = product.PictureUrl.Split('/').Last();
                await _imageService.DeleteFileBlobAsync(fileName, _containerName);
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            _apiResponse.StatusCode = HttpStatusCode.NoContent;
            return Ok(_apiResponse);
        }
        catch (Exception ex)
        {
            _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages = new List<string>() { ex.Message };
            return StatusCode(StatusCodes.Status500InternalServerError, _apiResponse);
        }
    }
}