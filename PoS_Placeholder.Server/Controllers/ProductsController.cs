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
    private readonly UserManager<User> _userManager;
    private readonly ProductRepository _productRepository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(UserManager<User> userManager, ProductRepository productRepository,
        ILogger<ProductsController> logger)
    {
        _userManager = userManager;
        _productRepository = productRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllProducts()
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("GetAllProducts: Received request from user {UserId}", userIdentifier);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllProducts: Unauthorized access attempt (User is null).");
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var businessProducts = await _productRepository.GetWhereAsync(product => product.BusinessId == userBusinessId);

        _logger.LogInformation("GetAllProducts: Returning {Count} products for user {UserId}", businessProducts.Count(),
            userIdentifier);
        return Ok(businessProducts);
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [Authorize]
    public async Task<IActionResult> GetProductById(int id)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("GetProductById: Received request for product {ProductId} from user {UserId}", id,
            userIdentifier);

        if (id <= 0)
        {
            _logger.LogWarning("GetProductById: Invalid product ID {ProductId} requested by user {UserId}", id,
                userIdentifier);
            return BadRequest("Invalid product ID.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetProductById: Unauthorized access attempt (User is null). ProductId: {ProductId}",
                id);
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var product = await _productRepository.GetByIdAndBusinessAsync(id, userBusinessId);

        if (product == null)
        {
            _logger.LogWarning("GetProductById: Product {ProductId} not found for user {UserId}", id, userIdentifier);
            return NotFound("Product not found.");
        }

        _logger.LogInformation("GetProductById: Product {ProductId} found and returned to user {UserId}", id,
            userIdentifier);
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto createProductDto)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("CreateProduct: Received create request from user {UserId}", userIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateProduct: Invalid model state from user {UserId}. Errors: {Errors}",
                userIdentifier,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("CreateProduct: Unauthorized access attempt by unknown user.");
            return Unauthorized("User not found.");
        }

        try
        {
            var businessId = user.BusinessId;

            var newProduct = new Product
            {
                Name = createProductDto.Name,
                ItemGroup = createProductDto.ItemGroup,
                BusinessId = businessId,
            };

            _productRepository.Add(newProduct);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation(
                "CreateProduct: Product {ProductName} created successfully for user {UserId}. Product ID: {ProductId}",
                createProductDto.Name, userIdentifier, newProduct.Id);

            return CreatedAtRoute("GetProductById", new { id = newProduct.Id }, newProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateProduct: Error creating product for user {UserId}", userIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductDto updateProductDto)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("UpdateProduct: Received update request for product {ProductId} from user {UserId}",
            updateProductDto.Id, userIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateProduct: Invalid model state from user {UserId}. Errors: {Errors}",
                userIdentifier,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("UpdateProduct: Unauthorized access attempt by unknown user for product {ProductId}",
                updateProductDto.Id);
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;

        try
        {
            var product = await _productRepository.GetByIdAsync(updateProductDto.Id);
            if (product == null)
            {
                _logger.LogWarning("UpdateProduct: Product {ProductId} not found for user {UserId}",
                    updateProductDto.Id, userIdentifier);
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                _logger.LogWarning(
                    "UpdateProduct: User {UserId} attempted to update a product {ProductId} from another business",
                    userIdentifier, updateProductDto.Id);
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this product.");
            }

            if (updateProductDto.Name != null)
                product.Name = updateProductDto.Name;

            if (updateProductDto.ItemGroup != null)
                product.ItemGroup = updateProductDto.ItemGroup;

            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("UpdateProduct: Product {ProductId} updated successfully by user {UserId}",
                product.Id, userIdentifier);
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateProduct: Error updating product {ProductId} for user {UserId}",
                updateProductDto.Id, userIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("DeleteProduct: Received delete request for product {ProductId} from user {UserId}", id,
            userIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "DeleteProduct: Invalid model state from user {UserId} for product {ProductId}. Errors: {Errors}",
                userIdentifier, id,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("DeleteProduct: Unauthorized access attempt by unknown user for product {ProductId}",
                id);
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;

        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("DeleteProduct: Product {ProductId} not found for user {UserId}", id,
                    userIdentifier);
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                _logger.LogWarning(
                    "DeleteProduct: User {UserId} attempted to delete product {ProductId} from another business",
                    userIdentifier, id);
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this product.");
            }

            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("DeleteProduct: Product {ProductId} deleted successfully by user {UserId}",
                product.Id, userIdentifier);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteProduct: Error deleting product {ProductId} for user {UserId}", id,
                userIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}