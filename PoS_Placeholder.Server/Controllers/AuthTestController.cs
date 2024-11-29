using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("api/authtest")]
public class AuthTestController : ControllerBase
{
    [HttpGet("getSomething")]
    [Authorize]
    public async Task<ActionResult> GetSomething()
    {
        return Ok("You are authenticated (logged in)");
    }

    [HttpGet("getSomethingOwner")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<ActionResult> GetSomethingOwner()
    {
        return Ok("You are authorized with Owner role");
    }
}