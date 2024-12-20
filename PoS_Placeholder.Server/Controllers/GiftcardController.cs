using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/giftcards")]
public class GiftcardController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly GiftcardRepository _giftcardRepository;
    private readonly ILogger<GiftcardController> _logger;

    public GiftcardController(UserManager<User> userManager, GiftcardRepository giftcardRepository,
        ILogger<GiftcardController> logger)
    {
        _userManager = userManager;
        _giftcardRepository = giftcardRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllGiftcards()
    {
        _logger.LogInformation("GetAllGiftcards: User {UserId} is requesting all giftcards.", User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllGiftcards: Unauthorized access attempt. User not found or not logged in.");
            return Unauthorized("User not found.");
        }

        var businessGiftcards = await _giftcardRepository.GetWhereAsync(g => g.BusinessId == user.BusinessId);

        _logger.LogInformation("GetAllGiftcards: Found {Count} giftcards for user {UserId}", businessGiftcards.Count(),
            user.Id);
        return Ok(businessGiftcards);
    }

    [HttpGet("{id}", Name = "GetGiftcardById")]
    [Authorize]
    public async Task<IActionResult> GetGiftcardById(string id)
    {
        _logger.LogInformation("GetGiftcardById: User {UserId} requests giftcard {GiftcardId}", User?.Claims.FirstOrDefault()?.Value,
            id);

        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("GetGiftcardById: Invalid giftcard ID requested by user {UserId}", User?.Claims.FirstOrDefault()?.Value);
            return BadRequest("Invalid giftcard ID.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetGiftcardById: Unauthorized access attempt for giftcard {GiftcardId}", id);
            return Unauthorized("User not found.");
        }

        var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(id.Trim(), user.BusinessId);
        if (giftcard == null)
        {
            _logger.LogWarning("GetGiftcardById: Giftcard {GiftcardId} not found for user {UserId}", id, user.Id);
            return NotFound("Giftcard not found.");
        }

        _logger.LogInformation("GetGiftcardById: Giftcard {GiftcardId} found for user {UserId}", id, user.Id);
        return Ok(giftcard);
    }

    [HttpGet("{giftcardId}/canPay/{amount:decimal}")]
    [Authorize]
    public async Task<IActionResult> CanGiftcardPay(string giftcardId, decimal amount)
    {
        _logger.LogInformation("CanGiftcardPay: User {UserId} checks if giftcard {GiftcardId} can pay {Amount}",
            User?.Claims.FirstOrDefault()?.Value, giftcardId, amount);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("CanGiftcardPay: Unauthorized access attempt by unknown user.");
            return Unauthorized("User not found.");
        }

        var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(giftcardId, user.BusinessId);
        if (giftcard == null)
        {
            _logger.LogWarning("CanGiftcardPay: Giftcard {GiftcardId} not found for user {UserId}", giftcardId,
                user.Id);
            return NotFound("Giftcard not found.");
        }

        if (giftcard.Balance >= amount)
        {
            _logger.LogInformation(
                "CanGiftcardPay: Giftcard {GiftcardId} can pay {Amount}, balance={Balance}, user={UserId}",
                giftcardId, amount, giftcard.Balance, user.Id);
            return Ok();
        }
        else
        {
            _logger.LogInformation(
                "CanGiftcardPay: Giftcard {GiftcardId} cannot pay {Amount}, insufficient balance. user={UserId}",
                giftcardId, amount, user.Id);
            return BadRequest("Insufficient giftcard balance.");
        }
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> CreateGiftcard([FromBody] CreateGiftcardDto createGiftcardDto)
    {
        _logger.LogInformation("CreateGiftcard: User {UserId} attempts to create a new giftcard with balance {Balance}",
            User?.Claims.FirstOrDefault()?.Value, createGiftcardDto.BalanceAmount);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("CreateGiftcard: Invalid model state for user {UserId}. Errors: {Errors}",
                User?.Claims.FirstOrDefault()?.Value,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("CreateGiftcard: Unauthorized access attempt by unknown user.");
            return Unauthorized("User not found.");
        }

        try
        {
            var newGiftcard = new Giftcard
            {
                Balance = Math.Round(createGiftcardDto.BalanceAmount, 2),
                BusinessId = user.BusinessId,
            };

            _giftcardRepository.Add(newGiftcard);
            await _giftcardRepository.SaveChangesAsync();

            _logger.LogInformation(
                "CreateGiftcard: Giftcard {GiftcardId} created with balance={Balance} by user {UserId}",
                newGiftcard.Id, newGiftcard.Balance, user.Id);
            return Ok(newGiftcard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateGiftcard: Error creating giftcard for user {UserId}. Message: {Message}",
                user.Id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateGiftcard([FromBody] UpdateGiftcardDto updateGiftcardDto)
    {
        _logger.LogInformation(
            "UpdateGiftcard: User {UserId} attempts to update giftcard {GiftcardId} with balance={Balance}",
            User?.Claims.FirstOrDefault()?.Value, updateGiftcardDto.Id, updateGiftcardDto.BalanceAmount);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateGiftcard: Invalid model state for user {UserId}. Errors: {Errors}",
                User?.Claims.FirstOrDefault()?.Value,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("UpdateGiftcard: Unauthorized access attempt by unknown user.");
            return Unauthorized("User not found.");
        }

        try
        {
            var giftcard = await _giftcardRepository.GetByStringIdAndBidAsync(updateGiftcardDto.Id, user.BusinessId);
            if (giftcard == null)
            {
                _logger.LogWarning("UpdateGiftcard: Giftcard {GiftcardId} not found for user {UserId}",
                    updateGiftcardDto.Id, user.Id);
                return NotFound("Giftcard not found.");
            }

            giftcard.Balance = Math.Round(updateGiftcardDto.BalanceAmount ?? 0m, 2);

            _giftcardRepository.Update(giftcard);
            await _giftcardRepository.SaveChangesAsync();

            _logger.LogInformation(
                "UpdateGiftcard: Giftcard {GiftcardId} updated with new balance={Balance} by user {UserId}",
                giftcard.Id, giftcard.Balance, user.Id);
            return Ok(giftcard);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateGiftcard: Error updating giftcard {GiftcardId} for user {UserId}",
                updateGiftcardDto.Id, user?.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteGiftcard([FromRoute] string id)
    {
        _logger.LogInformation("DeleteGiftcard: User {UserId} attempts to delete giftcard {GiftcardId}",
            User?.Claims.FirstOrDefault()?.Value, id);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("DeleteGiftcard: Unauthorized access attempt by unknown user for giftcard {GiftcardId}",
                id);
            return Unauthorized("User not found.");
        }

        try
        {
            var giftcard = await _giftcardRepository.GetByStringIdAndBidAsync(id, user.BusinessId);
            if (giftcard == null)
            {
                _logger.LogWarning("DeleteGiftcard: Giftcard {GiftcardId} not found for user {UserId}", id, user.Id);
                return NotFound("Giftcard not found.");
            }

            _giftcardRepository.Remove(giftcard);
            await _giftcardRepository.SaveChangesAsync();

            _logger.LogInformation("DeleteGiftcard: Giftcard {GiftcardId} deleted by user {UserId}", id, user.Id);
            return NoContent();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DeleteGiftcard: Error deleting giftcard {GiftcardId} for user {UserId}", id, user?.Id);
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}