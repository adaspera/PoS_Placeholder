using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Builders;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/discounts")]
public class DiscountController : ControllerBase
{
    private readonly DiscountRepository _discountRepository;
    private readonly ProductVariationRepository _variationRepository;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DiscountController> _logger;
    
    public DiscountController(DiscountRepository repository, UserManager<User> userManager, 
        ProductVariationRepository variationRepository, ILogger<DiscountController> logger)
    {
        _discountRepository = repository;
        _variationRepository = variationRepository;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllDiscounts()
    {
        _logger.LogInformation("Received GetAllDiscounts request from user {UserId}", User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllDiscounts: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        _logger.LogInformation("Fetching discounts for businessId={BusinessId}, userId={UserId}", userBusinessId, user.Id);
        
        var businessDiscounts = await _discountRepository.GetWhereAsync(discount => discount.BusinessId == userBusinessId);

        businessDiscounts = businessDiscounts.Where(d => d.EndDate >= DateTime.Now);
        
        _logger.LogInformation("Returning {Count} active discounts for businessId={BusinessId}, userId={UserId}", businessDiscounts.Count(), userBusinessId, user.Id);
        return Ok(businessDiscounts);
    }
    
    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetDiscount(int id)
    {
        _logger.LogInformation("Received GetDiscount request for DiscountId={DiscountId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetDiscount: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Fetching DiscountId={DiscountId} for businessId={BusinessId}, userId={UserId}", id, user.BusinessId, user.Id);
        
        var variationDiscount = await _discountRepository.GetByIdAsync(id);
        if (variationDiscount == null || variationDiscount.EndDate <= DateTime.Now)
        {
            _logger.LogWarning("GetDiscount: DiscountId={DiscountId} not found or expired for user {UserId}", id, user.Id);
            return NotFound("Discount not valid");
        }
        
        _logger.LogInformation("Returning DiscountId={DiscountId} for user {UserId}", id, user.Id);
        return Ok(variationDiscount);
    }
    
    [HttpGet("productVariations/{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetAllProductVariationsByDiscountId(int id)
    {
        _logger.LogInformation("Received GetAllProductVariationsByDiscountId request for DiscountId={DiscountId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);
        
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllProductVariationsByDiscountId: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        _logger.LogInformation("Fetching discount details for DiscountId={DiscountId}, businessId={BusinessId}, userId={UserId}", id, userBusinessId, user.Id);

        var discount = await _discountRepository.GetDiscountByDiscountAndBusinessId(id, userBusinessId);
        if (discount == null)
        {
            _logger.LogWarning("GetAllProductVariationsByDiscountId: DiscountId={DiscountId} not found for businessId={BusinessId}", id, userBusinessId);
            return BadRequest("Discount not found.");
        }

        _logger.LogInformation("Fetching product variations for DiscountId={DiscountId}, businessId={BusinessId}", id, userBusinessId);
        var discountVariations = await _variationRepository.GetWhereAsync(variation => variation.DiscountId == discount.Id);
        
        _logger.LogInformation("Returning {Count} product variations for DiscountId={DiscountId}, userId={UserId}", discountVariations.Count(), id, user.Id);
        return Ok(discountVariations);
    }
    
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountDto createDiscountDto)
    {
        _logger.LogInformation("Received CreateDiscount request from user {UserId} with dto={Dto}", User?.Claims.FirstOrDefault()?.Value, createDiscountDto);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateDiscount: Invalid model state from user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("CreateDiscount: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized("User not found.");
            }

            var businessId = user.BusinessId;
            _logger.LogInformation("Creating discount for businessId={BusinessId}, userId={UserId}", businessId, user.Id);

            var newDiscount = new DiscountBuilder().FromCreateDto(createDiscountDto, businessId).Build();

            _discountRepository.Add(newDiscount);
            await _discountRepository.SaveChangesAsync();

            _logger.LogInformation("Created DiscountId={DiscountId} successfully for businessId={BusinessId}, userId={UserId}", newDiscount.Id, businessId, user.Id);
            return Ok(newDiscount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discount. UserId={UserId}. Message: {Message}", User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
    
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> AddProductVariationsToDiscount(int id, [FromBody] IEnumerable<UpdateProductVariationDiscountDto> dto)
    {
        _logger.LogInformation("Received AddProductVariationsToDiscount request for DiscountId={DiscountId} from user {UserId} with dto={Dto}",
            id, User?.Claims.FirstOrDefault()?.Value, dto);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddProductVariationsToDiscount: Invalid model state from user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("AddProductVariationsToDiscount: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized("User not found.");
            }
            
            var userBusinessId = user.BusinessId;
            _logger.LogInformation("Fetching discountId={DiscountId} for businessId={BusinessId}, userId={UserId}", id, userBusinessId, user.Id);
            
            var discount = await _discountRepository.GetByIdAsync(id);
            if (discount == null)
            {
                _logger.LogWarning("AddProductVariationsToDiscount: DiscountId={DiscountId} not found for user {UserId}", id, user.Id);
                return NotFound("Discount not found.");
            }
            
            if (userBusinessId != discount.BusinessId)
            {
                _logger.LogWarning("AddProductVariationsToDiscount: Forbidden access to DiscountId={DiscountId} for user {UserId}", id, user.Id);
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this discount.");
            }
            
            _logger.LogInformation("Processing product variations for DiscountId={DiscountId}, userId={UserId}", id, user.Id);
            
            List<int> variationIdsForAdding = dto.Where(d => d.IsAdd).Select(d => d.ProductVariationId).ToList();
            List<int> variationIdsForRemoving = dto.Where(d => !d.IsAdd).Select(d => d.ProductVariationId).ToList();
            
            var variationsForAdding =
                await _variationRepository.GetByVariationIdsAndBusinessIdAsync(variationIdsForAdding, userBusinessId);
            
            var variationsForRemoving =
                await _variationRepository.GetByVariationIdsAndBusinessIdAsync(variationIdsForRemoving, userBusinessId);
            
            foreach (var variation in variationsForAdding)
            {
                variation.DiscountId = discount.Id;
            }
            
            foreach (var variation in variationsForRemoving)
            {
                variation.DiscountId = null;
            }
            
            _variationRepository.BulkUpdate(variationsForAdding);
            _variationRepository.BulkUpdate(variationsForRemoving);
            await _variationRepository.SaveChangesAsync();
            
            _logger.LogInformation("Updated product variations for DiscountId={DiscountId} successfully for user {UserId}", id, user.Id);
            return Ok(variationsForAdding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product variations for DiscountId={DiscountId}, UserId={UserId}. Message: {Message}", id, User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
    
    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateDiscount([FromBody] UpdateDiscountDto updateDiscountDto)
    {
        _logger.LogInformation("Received UpdateDiscount request from user {UserId} with dto={Dto}", User?.Claims.FirstOrDefault()?.Value, updateDiscountDto);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateDiscount: Invalid model state from user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("UpdateDiscount: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;
            _logger.LogInformation("Fetching discountId={DiscountId} for update, businessId={BusinessId}, userId={UserId}", updateDiscountDto.Id, userBusinessId, user.Id);

            var discount = await _discountRepository.GetByIdAsync(updateDiscountDto.Id);
            if (discount == null)
            {
                _logger.LogWarning("UpdateDiscount: DiscountId={DiscountId} not found for user {UserId}", updateDiscountDto.Id, user.Id);
                return NotFound("Discount not found.");
            }

            if (userBusinessId != discount.BusinessId)
            {
                _logger.LogWarning("UpdateDiscount: Forbidden access to DiscountId={DiscountId} for user {UserId}", updateDiscountDto.Id, user.Id);
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this discount.");
            }

            _logger.LogInformation("Updating DiscountId={DiscountId} for user {UserId}", updateDiscountDto.Id, user.Id);
            var updatedDiscount = new DiscountBuilder().FromUpdateDto(updateDiscountDto, discount).Build();

            _discountRepository.Update(updatedDiscount);
            await _discountRepository.SaveChangesAsync();

            _logger.LogInformation("DiscountId={DiscountId} updated successfully for user {UserId}", updatedDiscount.Id, user.Id);
            return Ok(updatedDiscount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DiscountId={DiscountId} for user {UserId}. Message: {Message}", updateDiscountDto.Id, User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
    
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteDiscount(int id)
    {
        _logger.LogInformation("Received DeleteDiscount request for DiscountId={DiscountId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("DeleteDiscount: Invalid model state from user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("DeleteDiscount: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;
            _logger.LogInformation("Fetching DiscountId={DiscountId} for deletion, businessId={BusinessId}, userId={UserId}", id, userBusinessId, user.Id);

            var discount = await _discountRepository.GetByIdAsync(id);
            if (discount == null)
            {
                _logger.LogWarning("DeleteDiscount: DiscountId={DiscountId} not found for user {UserId}", id, user.Id);
                return NotFound("Discount not found.");
            }

            if (userBusinessId != discount.BusinessId)
            {
                _logger.LogWarning("DeleteDiscount: Forbidden access to DiscountId={DiscountId} for user {UserId}", id, user.Id);
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this discount.");
            }

            _logger.LogInformation("Deleting DiscountId={DiscountId} for user {UserId}", id, user.Id);
            _discountRepository.Remove(discount);
            await _discountRepository.SaveChangesAsync();

            _logger.LogInformation("DiscountId={DiscountId} deleted successfully for user {UserId}", id, user.Id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting DiscountId={DiscountId} for user {UserId}. Message: {Message}", id, User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}