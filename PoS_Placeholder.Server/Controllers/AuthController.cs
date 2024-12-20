using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    private readonly ILogger<OrderController> _logger;

    public AuthController(ApplicationDbContext db, UserManager<User> userManager, RoleManager<IdentityRole> roleManager,
        IConfiguration configuration, ILogger<OrderController> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _secretKey = configuration.GetValue<string>("JwtSettings:Secret");
        _apiResponse = new ApiResponse();
        _logger = logger;
    }

    [Authorize(Roles = nameof(UserRole.Owner))]
    [HttpPost("register-employee")]
    public async Task<IActionResult> RegisterEmployee([FromBody] RegisterEmployeeDto registerEmployeeDto)
    {
        _logger.LogInformation(
            "Received RegisterEmployee request from user {UserId}, registering employee with email {EmployeeEmail}",
            User?.Claims.FirstOrDefault()?.Value, registerEmployeeDto.Email);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("RegisterEmployee: Invalid model. Errors: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        User ownerUser = await _userManager.GetUserAsync(User);

        if (ownerUser == null)
        {
            _logger.LogWarning("RegisterEmployee: Owner user not found or unauthorized. UserId={UserId}",
                User?.Claims.FirstOrDefault()?.Value);
            _apiResponse = new ApiResponse
            {
                StatusCode = HttpStatusCode.Unauthorized,
                IsSuccess = false,
                ErrorMessages = { "Business Owner user not found" }
            };
            return Unauthorized(_apiResponse);
        }

        User employeeUserFromDb =
            _db.Users.FirstOrDefault(u => u.Email.ToLower() == registerEmployeeDto.Email.ToLower());
        if (employeeUserFromDb != null)
        {
            _logger.LogWarning(
                "RegisterEmployee: Employee with email {EmployeeEmail} already exists. OwnerId={OwnerId}",
                registerEmployeeDto.Email, ownerUser.Id);
            _apiResponse = new ApiResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                IsSuccess = false,
                ErrorMessages = { "Employee already exists" }
            };
            return BadRequest(_apiResponse);
        }

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
            _logger.LogInformation("Creating new employee user {EmployeeEmail} for owner {OwnerId}",
                registerEmployeeDto.Email, ownerUser.Id);
            var result = await _userManager.CreateAsync(newEmployee, registerEmployeeDto.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("New employee user {EmployeeEmail} created. Checking roles...",
                    registerEmployeeDto.Email);
                if (!await _roleManager.RoleExistsAsync(UserRole.Employee.ToString()))
                {
                    _logger.LogInformation("Roles do not exist. Creating roles for Employee, Owner, SuperAdmin.");
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.SuperAdmin.ToString()));
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.Owner.ToString()));
                    await _roleManager.CreateAsync(new IdentityRole(UserRole.Employee.ToString()));
                }

                await _userManager.AddToRoleAsync(newEmployee, UserRole.Employee.ToString());
                _logger.LogInformation("Employee {EmployeeEmail} assigned to Employee role successfully.",
                    registerEmployeeDto.Email);

                _apiResponse = new ApiResponse
                {
                    Data = newEmployee,
                    StatusCode = HttpStatusCode.OK
                };
                return Ok(_apiResponse);
            }
            else
            {
                _logger.LogWarning("Failed to create employee {EmployeeEmail}. Identity errors: {Errors}",
                    registerEmployeeDto.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while registering employee {EmployeeEmail} for owner {OwnerId}",
                registerEmployeeDto.Email, ownerUser.Id);
            throw;
        }

        _logger.LogWarning("RegisterEmployee: Error while registering employee {EmployeeEmail}.",
            registerEmployeeDto.Email);
        _apiResponse = new ApiResponse
        {
            StatusCode = HttpStatusCode.BadRequest,
            IsSuccess = false,
            ErrorMessages = { "Error while registering employee" }
        };
        return BadRequest(_apiResponse);
    }

    [HttpPost("register-business")]
    public async Task<IActionResult> RegisterBusiness([FromBody] RegisterBusinessDto registerBusinessDto)
    {
        _logger.LogInformation(
            "Received RegisterBusiness request with business email {BusinessEmail} and owner email {OwnerEmail}",
            registerBusinessDto.BusinessEmail, registerBusinessDto.Email);

        _apiResponse = new ApiResponse();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("RegisterBusiness: Invalid model. Errors: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        User ownerUserFromDb = await _userManager.FindByEmailAsync(registerBusinessDto.Email);
        Business businessFromDb =
            _db.Businesses.FirstOrDefault(b => b.Email.ToLower() == registerBusinessDto.BusinessEmail.ToLower());

        if (ownerUserFromDb != null || businessFromDb != null)
        {
            _logger.LogWarning(
                "RegisterBusiness: Business or Owner already exists with this email. OwnerEmail={OwnerEmail}, BusinessEmail={BusinessEmail}",
                registerBusinessDto.Email, registerBusinessDto.BusinessEmail);
            _apiResponse.StatusCode = HttpStatusCode.BadRequest;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Business or owner already exists");
            return BadRequest(_apiResponse);
        }

        using (var transaction = await _db.Database.BeginTransactionAsync())
        {
            try
            {
                _logger.LogInformation("Creating new business {BusinessName} ({BusinessEmail})",
                    registerBusinessDto.BusinessName, registerBusinessDto.BusinessEmail);
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

                _logger.LogInformation("Creating owner user {OwnerEmail} for new business Id={BusinessId}",
                    registerBusinessDto.Email, newBusiness.Id);
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
                    _logger.LogInformation("Owner user {OwnerEmail} created. Checking roles...",
                        registerBusinessDto.Email);
                    if (!await _roleManager.RoleExistsAsync(UserRole.Owner.ToString()))
                    {
                        _logger.LogInformation("Roles do not exist. Creating Owner, Employee, SuperAdmin roles.");
                        await _roleManager.CreateAsync(new IdentityRole(UserRole.SuperAdmin.ToString()));
                        await _roleManager.CreateAsync(new IdentityRole(UserRole.Owner.ToString()));
                        await _roleManager.CreateAsync(new IdentityRole(UserRole.Employee.ToString()));
                    }

                    await _userManager.AddToRoleAsync(newOwner, UserRole.Owner.ToString());
                    _logger.LogInformation("Owner {OwnerEmail} assigned to Owner role successfully.",
                        registerBusinessDto.Email);

                    await transaction.CommitAsync();

                    _logger.LogInformation("Business {BusinessName} with owner {OwnerEmail} registered successfully.",
                        registerBusinessDto.BusinessName, registerBusinessDto.Email);
                    _apiResponse.StatusCode = HttpStatusCode.OK;
                    _apiResponse.IsSuccess = true;
                    return Ok(_apiResponse);
                }
                else
                {
                    _logger.LogWarning(
                        "RegisterBusiness: Failed to create owner user {OwnerEmail}. Identity errors: {Errors}",
                        registerBusinessDto.Email, string.Join("; ", result.Errors.Select(e => e.Description)));

                    await transaction.RollbackAsync();

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
                _logger.LogError(ex, "Unexpected error while registering business {BusinessName}, owner {OwnerEmail}",
                    registerBusinessDto.BusinessName, registerBusinessDto.Email);
                await transaction.RollbackAsync();

                _apiResponse.StatusCode = HttpStatusCode.InternalServerError;
                _apiResponse.IsSuccess = false;
                _apiResponse.ErrorMessages.Add("An unexpected error occurred while registering the business.");
                return StatusCode(StatusCodes.Status500InternalServerError, _apiResponse);
            }
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto)
    {
        _logger.LogInformation("Received Login request for email {Email}", loginRequestDto.Email);

        _apiResponse = new ApiResponse();

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login: Invalid model. Errors: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return BadRequest(ModelState);
        }

        User userFromDb = await _userManager.FindByEmailAsync(loginRequestDto.Email);
        if (userFromDb == null || !await _userManager.CheckPasswordAsync(userFromDb, loginRequestDto.Password))
        {
            _logger.LogWarning("Login failed for email {Email}: Invalid credentials", loginRequestDto.Email);
            _apiResponse.StatusCode = HttpStatusCode.Unauthorized;
            _apiResponse.IsSuccess = false;
            _apiResponse.ErrorMessages.Add("Invalid email address or password");
            return Unauthorized(_apiResponse);
        }

        var roles = await _userManager.GetRolesAsync(userFromDb);
        var role = roles.FirstOrDefault();

        _logger.LogInformation("User {Email} logged in successfully. Role={Role}, BusinessId={BusinessId}",
            userFromDb.Email, role, userFromDb.BusinessId);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userFromDb.Id),
            new(JwtRegisteredClaimNames.Email, userFromDb.Email),
            new(ClaimTypes.Role, role),
            new("businessId", userFromDb.BusinessId.ToString())
        };

        SecurityTokenDescriptor securityTokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken = tokenHandler.CreateToken(securityTokenDescriptor);
        string token = tokenHandler.WriteToken(securityToken);

        var business = await _db.Businesses.FindAsync(userFromDb.BusinessId);

        LoginResponseDto loginResponse = new()
        {
            Email = userFromDb.Email,
            AuthToken = token,
            Role = role,
            BusinessId = userFromDb.BusinessId,
            Currency = business.Region
        };

        _logger.LogInformation("Login: Returning auth token for user {Email}", userFromDb.Email);
        _apiResponse.StatusCode = HttpStatusCode.OK;
        _apiResponse.Data = loginResponse;
        return Ok(_apiResponse);
    }
}