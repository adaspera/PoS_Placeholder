﻿using Microsoft.AspNetCore.Authorization;
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

    public GiftcardController(UserManager<User> userManager, GiftcardRepository giftcardRepository)
    {
        _userManager = userManager;
        _giftcardRepository = giftcardRepository;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllGiftcards()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var businessGiftcards = await _giftcardRepository.GetWhereAsync(g => g.BusinessId == user.BusinessId);

        return Ok(businessGiftcards);
    }

    [HttpGet("{id}", Name = "GetGiftcardById")]
    [Authorize]
    public async Task<IActionResult> GetGiftcardById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Invalid giftcard ID.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(id.Trim(), user.BusinessId);
        if (giftcard == null)
            return NotFound("Giftcard not found.");

        return Ok(giftcard);
    }
    
    [HttpGet("{giftcardId}/canPay/{amount:decimal}")]
    [Authorize]
    public async Task<IActionResult> CanGiftcardPay(string giftcardId, decimal amount)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("User not found.");

        var giftcard = await _giftcardRepository.GetGiftcardByIdAndBusinessIdAsync(giftcardId, user.BusinessId);
        if (giftcard == null) 
            return NotFound("Giftcard not found.");

        if (giftcard.Balance >= amount)
            return Ok();
        else
            return BadRequest("Insufficient giftcard balance.");
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> CreateGiftcard([FromBody] CreateGiftcardDto createGiftcardDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not found.");


            var newGiftcard = new Giftcard
            {
                Balance = Math.Round(createGiftcardDto.BalanceAmount, 2),
                BusinessId = user.BusinessId,
            };

            _giftcardRepository.Add(newGiftcard);
            await _giftcardRepository.SaveChangesAsync();

            return Ok(newGiftcard);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateGiftcard([FromBody] UpdateGiftcardDto updateGiftcardDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not found.");

            var giftcard = await _giftcardRepository.GetByStringIdAndBidAsync(updateGiftcardDto.Id, user.BusinessId);
            if (giftcard == null)
                return NotFound("Giftcard not found.");

            giftcard.Balance = Math.Round(updateGiftcardDto.BalanceAmount ?? 0m, 2);

            _giftcardRepository.Update(giftcard);
            await _giftcardRepository.SaveChangesAsync();
            return Ok(giftcard);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteGiftcard([FromRoute] string id)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized("User not found.");

            var giftcard = await _giftcardRepository.GetByStringIdAndBidAsync(id, user.BusinessId);
            if (giftcard == null)
                return NotFound("Giftcard not found.");

            _giftcardRepository.Remove(giftcard);
            await _giftcardRepository.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}