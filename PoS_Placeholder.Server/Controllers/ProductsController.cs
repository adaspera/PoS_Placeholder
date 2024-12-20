using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductRepository _productRepository;
    private readonly UserManager<User> _userManager;

    public ProductsController(ProductRepository productRepository, UserManager<User> userManager)
    {
        _productRepository = productRepository;
        _userManager = userManager;
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
        var businessProducts = await _productRepository.GetWhereAsync(product => product.BusinessId == userBusinessId);
        
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

        var product = await _productRepository.GetByIdAndBusinessAsync(id, userBusinessId);
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

            _productRepository.Add(newProduct);
            await _productRepository.SaveChangesAsync();

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

            var product = await _productRepository.GetByIdAsync(updateProductDto.Id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this product.");
            }

            if (updateProductDto.Name != null)
                product.Name = updateProductDto.Name;

            if (updateProductDto.ItemGroup != null)
                product.ItemGroup = updateProductDto.ItemGroup;


            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();

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

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this product.");
            }

            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}