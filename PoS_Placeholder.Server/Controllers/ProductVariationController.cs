using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using PoS_Placeholder.Server.Services;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/productVariations")]
public class ProductVariationController : ControllerBase
{
    private readonly ProductVariationRepository _variationRepository;
    private readonly ProductRepository _productRepository;
    private readonly UserManager<User> _userManager;
    private readonly IImageService _imageService;
    private readonly ILogger<ProductVariationController> _logger;

    public ProductVariationController(
        ProductVariationRepository productVariationRepository,
        ProductRepository productRepository,
        UserManager<User> userManager,
        IImageService imageService,
        ILogger<ProductVariationController> logger)
    {
        _variationRepository = productVariationRepository;
        _productRepository = productRepository;
        _userManager = userManager;
        _imageService = imageService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllProductVariations()
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("GetAllProductVariations: Received request from user {UserId}", userIdentifier);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllProductVariations: Unauthorized access attempt (User is null).");
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var productVariations =
            await _variationRepository.GetWhereAsync(variation => variation.Product.BusinessId == userBusinessId);

        _logger.LogInformation("GetAllProductVariations: Returning {Count} product variations for user {UserId}",
            productVariations.Count(), userIdentifier);
        return Ok(productVariations);
    }

    [HttpGet("{id:int}", Name = "GetAllProductVariationsById")]
    [Authorize]
    public async Task<IActionResult> GetAllProductVariationsById(int id)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("GetAllProductVariationsById: Request for productId {ProductId} by user {UserId}", id,
            userIdentifier);

        if (id <= 0)
        {
            _logger.LogWarning("GetAllProductVariationsById: Invalid product ID {ProductId} requested by user {UserId}",
                id, userIdentifier);
            return BadRequest("Invalid product ID.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning(
                "GetAllProductVariationsById: Unauthorized access. User is null for productId {ProductId}", id);
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var productVariations = await _variationRepository.GetByProductAndBusinessId(id, userBusinessId);

        if (productVariations == null || !productVariations.Any())
        {
            _logger.LogWarning(
                "GetAllProductVariationsById: No product variations found for productId {ProductId} and user {UserId}",
                id, userIdentifier);
            return NotFound("Product variations not found.");
        }

        _logger.LogInformation(
            "GetAllProductVariationsById: Found {Count} variations for productId {ProductId} for user {UserId}",
            productVariations.Count(), id, userIdentifier);
        return Ok(productVariations);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> CreateProductVariation(
        [FromForm] CreateProductVariationDto createProductVariationDto)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation("CreateProductVariation: Received create request from user {UserId}", userIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateProductVariation: Invalid model state from user {UserId}. Errors: {Errors}",
                userIdentifier,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("CreateProductVariation: Unauthorized access attempt. User is null.");
            return Unauthorized("User not found.");
        }

        if (createProductVariationDto.PictureFile == null || createProductVariationDto.PictureFile.Length == 0)
        {
            _logger.LogWarning("CreateProductVariation: No picture file provided by user {UserId}.", userIdentifier);
            return BadRequest("Picture file is required.");
        }

        try
        {
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

            _variationRepository.Add(newProductVariation);
            await _variationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "CreateProductVariation: ProductVariation created successfully by user {UserId}. Variation ID: {VariationId}",
                userIdentifier, newProductVariation.Id);

            return CreatedAtRoute("GetAllProductVariationsById", new { id = newProductVariation.Id },
                newProductVariation);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateProductVariation: Error creating product variation for user {UserId}",
                userIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Error: {e.Message}, StackTrace: {e.StackTrace}");
        }
    }

    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateProductVariation(
        [FromForm] UpdateProductVariationDto updateProductVariationDto)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation(
            "UpdateProductVariation: Received update request for variation {VariationId} from user {UserId}",
            updateProductVariationDto.Id, userIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateProductVariation: Invalid model state from user {UserId}. Errors: {Errors}",
                userIdentifier,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning(
                "UpdateProductVariation: Unauthorized access attempt. User is null for variation {VariationId}",
                updateProductVariationDto.Id);
            return Unauthorized("User not found.");
        }

        try
        {
            var userBusinessId = user.BusinessId;
            var productVariation = await _variationRepository.GetByIdAsync(updateProductVariationDto.Id);

            if (productVariation == null)
            {
                _logger.LogWarning("UpdateProductVariation: Variation {VariationId} not found for user {UserId}",
                    updateProductVariationDto.Id, userIdentifier);
                return NotFound("ProductVariation not found.");
            }

            var product = await _productRepository.GetByIdAsync(updateProductVariationDto.ProductId);
            if (product == null)
            {
                _logger.LogWarning(
                    "UpdateProductVariation: Product not found for variation {VariationId}, user {UserId}",
                    updateProductVariationDto.Id, userIdentifier);
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                _logger.LogWarning(
                    "UpdateProductVariation: User {UserId} tried to update variation {VariationId} from another business",
                    userIdentifier, updateProductVariationDto.Id);
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this product.");
            }

            // If user passed the new image file
            if (updateProductVariationDto.PictureFile != null && updateProductVariationDto.PictureFile.Length > 0)
            {
                string oldFileName = productVariation.PictureUrl.Split('/').Last();
                await _imageService.DeleteFileBlobAsync(oldFileName);

                string newFileName =
                    $"{Guid.NewGuid()}{Path.GetExtension(updateProductVariationDto.PictureFile.FileName)}";
                string pictureUrl =
                    await _imageService.UploadFileBlobAsync(newFileName, updateProductVariationDto.PictureFile);

                productVariation.PictureUrl = pictureUrl;
            }

            if (updateProductVariationDto.Name != null)
                productVariation.Name = updateProductVariationDto.Name;

            if (updateProductVariationDto.Price != null)
                productVariation.Price = updateProductVariationDto.Price;

            _variationRepository.Update(productVariation);
            await _variationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "UpdateProductVariation: Variation {VariationId} updated successfully by user {UserId}",
                productVariation.Id, userIdentifier);
            return Ok(productVariation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateProductVariation: Error updating variation {VariationId} for user {UserId}",
                updateProductVariationDto.Id, userIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteProductVariation(int id)
    {
        var userIdentifier = User?.Claims.FirstOrDefault()?.Value;
        _logger.LogInformation(
            "DeleteProductVariation: Received delete request for variation {VariationId} from user {UserId}",
            id, userIdentifier);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning(
                "DeleteProductVariation: Invalid model state from user {UserId} for variation {VariationId}. Errors: {Errors}",
                userIdentifier, id,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning(
                "DeleteProductVariation: Unauthorized access attempt by unknown user for variation {VariationId}", id);
            return Unauthorized("User not found.");
        }

        try
        {
            var userBusinessId = user.BusinessId;
            var productVariation = await _variationRepository.GetByIdAsync(id);
            if (productVariation == null)
            {
                _logger.LogWarning("DeleteProductVariation: Variation {VariationId} not found for user {UserId}", id,
                    userIdentifier);
                return NotFound("ProductVariation not found.");
            }

            var product = await _productRepository.GetByIdAsync(productVariation.ProductId);
            if (product == null)
            {
                _logger.LogWarning(
                    "DeleteProductVariation: Product not found for variation {VariationId}, user {UserId}", id,
                    userIdentifier);
                return NotFound("Product not found.");
            }

            if (userBusinessId != product.BusinessId)
            {
                _logger.LogWarning(
                    "DeleteProductVariation: User {UserId} tried to delete variation {VariationId} from another business",
                    userIdentifier, id);
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this product.");
            }

            _variationRepository.Remove(productVariation);
            await _variationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "DeleteProductVariation: Variation {VariationId} deleted successfully by user {UserId}",
                productVariation.Id, userIdentifier);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteProductVariation: Error deleting variation {VariationId} for user {UserId}", id,
                userIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}