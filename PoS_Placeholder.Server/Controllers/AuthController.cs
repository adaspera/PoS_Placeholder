using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private ApiResponse _apiResponse;
    private string _secretKey;

    public AuthController(ApplicationDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _secretKey = configuration.GetValue<string>("JwtSettings:Secret");
        _apiResponse = new ApiResponse();
    }

    [Authorize(Roles = nameof(UserRole.Owner))]
    [HttpPost("register-employee")]
    public async Task<IActionResult> RegisterEmployee([FromBody] RegisterEmployeeDto registerEmployeeDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get owner user for linking businessId later
        User ownerUser = await _userManager.GetUserAsync(User);

        if (ownerUser == null)
        {
            _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Business Owner user not found");
            return Unauthorized(_apiResponse);
        }

        // Check if user with the email already exists
        User employeeUserFromDb =
            _db.Users.FirstOrDefault(u => u.Email.ToLower() == registerEmployeeDto.Email.ToLower());

        if (employeeUserFromDb != null)
        {
            _apiResponse.StatusCode = HttpStatusCode.BadRequest;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Employee already exists");
            return BadRequest(_apiResponse);
        }

        // Create new employee user
        User newEmployee = new User
        {
            UserName = registerEmployeeDto.Email,
            Email = registerEmployeeDto.Email,
            FirstName = registerEmployeeDto.FirstName,
            LastName = registerEmployeeDto.LastName,
            PhoneNumber = registerEmployeeDto.PhoneNumber,
            AvailabilityStatus = AvailabilityStatus.Available,
            BusinessId = ownerUser.BusinessId,
        };

        try
        {
            var result = await _userManager.CreateAsync(newEmployee, registerEmployeeDto.Password);
            if (result.Succeeded)
            {
                // Check if Roles exist if not create them
                if (!await _roleManager.RoleExistsAsync(UserRole.Employee.ToString()))
                {
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.SuperAdmin.ToString()));
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.Owner.ToString()));
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.Employee.ToString()));
                }

                // Set newUser role to employee
                await _userManager.AddToRoleAsync(newEmployee, UserRole.Employee.ToString());

                _apiResponse.StatusCode = HttpStatusCode.OK;
                return Ok(_apiResponse);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        _apiResponse.StatusCode = HttpStatusCode.BadRequest;
        _apiResponse.IsSuccess = false;
        _apiResponse.ErrorMessages.Add("Error while registering employee");
        return BadRequest(_apiResponse);
    }

    [HttpPost("register-business")]
    public async Task<IActionResult> RegisterBusiness([FromBody] RegisterBusinessDto registerBusinessDto)
    {
        _apiResponse = new ApiResponse();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if the owner user or business already exists
        User ownerUserFromDb = await _userManager.FindByEmailAsync(registerBusinessDto.Email);
        Business businessFromDb =
            _db.Businesses.FirstOrDefault(b => b.Email.ToLower() == registerBusinessDto.BusinessEmail.ToLower());

        if (ownerUserFromDb != null || businessFromDb != null)
        {
            _apiResponse.StatusCode = HttpStatusCode.BadRequest;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Business or owner already exists");
            return BadRequest(_apiResponse);
        }

        // Begin a transaction
        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                // Create new business
                Business newBusiness = new Business
                {
                    Name = registerBusinessDto.BusinessName,
                    Phone = registerBusinessDto.BusinessPhone,
                    Email = registerBusinessDto.BusinessEmail,
                    Street = registerBusinessDto.BusinessStreet,
                    City = registerBusinessDto.BusinessCity,
                    Region = registerBusinessDto.BusinessRegion,
                    Country = registerBusinessDto.BusinessCountry,
                };

                _db.Businesses.Add(newBusiness);
                await _db.SaveChangesAsync();

                // Create new owner user
                User newOwner = new User
                {
                    UserName = registerBusinessDto.Email,
                    Email = registerBusinessDto.Email,
                    FirstName = registerBusinessDto.FirstName,
                    LastName = registerBusinessDto.LastName,
                    PhoneNumber = registerBusinessDto.PhoneNumber,
                    BusinessId = newBusiness.Id,
                    AvailabilityStatus = AvailabilityStatus.Available,
                };

                var result = await _userManager.CreateAsync(newOwner, registerBusinessDto.Password);
                if (result.Succeeded)
                {
                    // Check if roles exist, if not create them in db
                    if (!await _roleManager.RoleExistsAsync(UserRole.Owner.ToString()))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(UserRole.SuperAdmin.ToString()));
                        await _roleManager.CreateAsync(new IdentityRole(UserRole.Owner.ToString()));
                        await _roleManager.CreateAsync(new IdentityRole(UserRole.Employee.ToString()));
                    }
                    
                    // Assign the Owner role
                    await _userManager.AddToRoleAsync(newOwner, UserRole.Owner.ToString());

                    // Commit the transaction
                    await transaction.CommitAsync();

                    _apiResponse.StatusCode = HttpStatusCode.OK;
                    _apiResponse.IsSuccess = true;
                    return Ok(_apiResponse);
                }
                else
                {
                    // Rollback the transaction
                    await transaction.RollbackAsync();

                    // Collect errors from IdentityResult
                    foreach (var error in result.Errors)
                    {
                        _apiResponse.ErrorMessages.Add(error.Description);
                    }

                    _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                    _apiResponse.IsSuccess = false;
                    return BadRequest(_apiResponse);
                }
            }
            catch (Exception ex)
            {
                // Rollback the transaction in case of an exception
                await transaction.RollbackAsync();

                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("An unexpected error occurred while registering the business.");
                return StatusCode(StatusCodes.Status500InternalServerError, _apiResponse);
            }
        }
    }
}