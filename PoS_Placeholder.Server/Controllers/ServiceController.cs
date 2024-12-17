using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using System.ComponentModel;
using System.Diagnostics;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/services")]
public class ServiceController : ControllerBase
{
    private readonly ServiceRepository _serviceRepository;
    private readonly UserManager<User> _userManager;
    private readonly UserRepository _userRepository;

    private readonly string _400statusMessage = "Bad request. The request could not be understood by the server due to malformed syntax.";
    private readonly string _401statusMessage = "Unauthorized. Please provide valid credentials.";
    private readonly string _403statusMessage = "Foribdden. You do not have access to this resource.";
    private readonly string _404statusMessage = "Resource not found.";
    public ServiceController(ServiceRepository serviceRepository, UserManager<User> userManager,
        UserRepository userRepository)
    {
        _serviceRepository = serviceRepository;
        _userManager = userManager;
        _userRepository = userRepository;
    }

    [HttpGet("all", Name = "GetAllServices")]
    [Authorize]
    [Description("Gets all services for the current user's business.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Service>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetAllServices()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;
        var businessServices = await _serviceRepository.GetServicesByBusinessIdAsync(businessId);

        if(businessServices == null)
        {
            return NotFound(_404statusMessage);
        }

        return Ok(businessServices);
    }

    [HttpGet("{id:int}", Name = "GetServiceById")]
    [Authorize]
    [Description("Gets an service that belongs to the user's business using the serivce's ID.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Service))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetServiceById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(_400statusMessage);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;

        var service = await _serviceRepository.GetServiceByIdAsync(id, businessId);
        if (service == null)
        {
            return NotFound(_404statusMessage);
        }

        if (businessId != service.BusinessId)
        {
            return Forbid(_403statusMessage);
        }

        return Ok(service);
    }

    [HttpGet("user/{userId}", Name = "GetAllServicesByUserId")]
    [Authorize]
    [Description("Get all services assigned to a user using the user's ID.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Service>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetServicesByUserId(string userId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;

        var employee = await _userRepository.GetByIdAsync(userId);

        if (employee == null)
        {
            return BadRequest(_400statusMessage);
        }

        if (businessId != employee.BusinessId)
        {
            return Forbid(_403statusMessage);
        }

        var userServices = await _serviceRepository.GetServicesByUserIdAsync(businessId, userId);

        if (userServices == null)
        {
            return NotFound(_404statusMessage);
        }


        return Ok(userServices);
    }

    [HttpPost("create", Name = "CreateService")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Creates a new service.")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Appointment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> CreateService([FromForm] CreateServiceDto createServiceDto)
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
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;

            var newService = new Service
            {
                Name = createServiceDto.Name,
                ServiceCharge = createServiceDto.ServiceCharge,
                IsPercentage = createServiceDto.IsPercentage,
                Duration = createServiceDto.Duration,
                BusinessId = businessId,
                UserId = createServiceDto.UserId
            };

            _serviceRepository.Add(newService);
            await _serviceRepository.SaveChangesAsync();

            return CreatedAtRoute("GetServiceById", new { id = newService.Id }, newService);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut("update/{id:int}", Name = "UpdateService")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Update an existing service.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Service))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> UpdateService([FromForm] UpdateServiceDto updateServiceDto, int id)
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
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;

            var service = await _serviceRepository.GetServiceByIdAsync(id, businessId);
            if (service == null)
            {
                return NotFound(_404statusMessage);
            }

            if (businessId != service.BusinessId)
            {
                return Forbid(_403statusMessage);
            }

            if(updateServiceDto.Name != null)
                service.Name = updateServiceDto.Name;
            if (updateServiceDto.ServiceCharge != null)
                service.ServiceCharge = (decimal)updateServiceDto.ServiceCharge;
            if (updateServiceDto.IsPercentage != null)
                service.IsPercentage = (bool)updateServiceDto.IsPercentage;
            if (updateServiceDto.Duration != null)
                service.Duration = (uint)updateServiceDto.Duration;

            _serviceRepository.Update(service);
            await _serviceRepository.SaveChangesAsync();

            return Ok(service);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("delete/{id:int}", Name = "DeleteService")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Delete an existing service.")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(NoContentResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> DeleteService(int id)
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
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;

            var service = await _serviceRepository.GetServiceByIdAsync(id, businessId);
            if (service == null)
            {
                return NotFound(_404statusMessage);
            }

            if (businessId != service.BusinessId)
            {
                return Forbid(_403statusMessage);
            }

            _serviceRepository.Remove(service);
            await _serviceRepository.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}