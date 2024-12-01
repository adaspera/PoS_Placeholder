using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("api/authtest")]
public class AuthTestController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public AuthTestController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("getSomething")]
    [Authorize]
    public async Task<ActionResult> GetSomething()
    {
        // var user = await _userManager.GetUserAsync(User);
        // var userRoles = await _userManager.GetRolesAsync(user);
        // var userRole = userRoles.FirstOrDefault();
        // Console.WriteLine(user.BusinessId + ", " + user.Email + ", " + userRole);
        
        return Ok("You are authenticated (logged in)");
    }

    [HttpGet("getSomethingOwner")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<ActionResult> GetSomethingOwner()
    {
        // var user = await _userManager.GetUserAsync(User);
        // var userRoles = await _userManager.GetRolesAsync(user);
        // var userRole = userRoles.FirstOrDefault();
        // Console.WriteLine(user.BusinessId + ", " + user.Email + ", " + userRole);
        
        return Ok("You are authorized with Owner role");
    }
}