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
    private readonly ILogger<ServiceController> _logger;

    private readonly string _400statusMessage = "Bad request. The request could not be understood by the server due to malformed syntax.";
    private readonly string _401statusMessage = "Unauthorized. Please provide valid credentials.";
    private readonly string _403statusMessage = "Foribdden. You do not have access to this resource.";
    private readonly string _404statusMessage = "Resource not found.";

    public ServiceController(ServiceRepository serviceRepository, UserManager<User> userManager,
        UserRepository userRepository, ILogger<ServiceController> logger)
    {
        _serviceRepository = serviceRepository;
        _userManager = userManager;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet("all", Name = "GetAllServices")]
    [Authorize]
    [Description("Gets all services for the current user's business.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Service>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetAllServices()
    {
        _logger.LogInformation("Received GetAllServices request from user {UserId}", User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllServices: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized(_401statusMessage);
        }

        _logger.LogInformation("Fetching services for businessId={BusinessId}, userId={UserId}", user.BusinessId, user.Id);
        var businessId = user.BusinessId;
        var businessServices = await _serviceRepository.GetServicesByBusinessIdAsync(businessId);

        if(businessServices == null)
        {
            _logger.LogWarning("GetAllServices: No services found for businessId={BusinessId}, userId={UserId}", user.BusinessId, user.Id);
            return NotFound(_404statusMessage);
        }

        _logger.LogInformation("Returning {Count} services for businessId={BusinessId}, userId={UserId}", businessServices.Count(), user.BusinessId, user.Id);
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
        _logger.LogInformation("Received GetServiceById request for ServiceId={ServiceId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);

        if (id <= 0)
        {
            _logger.LogWarning("GetServiceById: Invalid serviceId={ServiceId}", id);
            return BadRequest(_400statusMessage);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetServiceById: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized(_401statusMessage);
        }

        _logger.LogInformation("Fetching ServiceId={ServiceId} for businessId={BusinessId}, userId={UserId}", id, user.BusinessId, user.Id);
        var businessId = user.BusinessId;
        var service = await _serviceRepository.GetServiceByIdAsync(id, businessId);
        
        if (service == null)
        {
            _logger.LogWarning("GetServiceById: ServiceId={ServiceId} not found for user {UserId}", id, user.Id);
            return NotFound(_404statusMessage);
        }

        if (businessId != service.BusinessId)
        {
            _logger.LogWarning("GetServiceById: Forbidden access to ServiceId={ServiceId}, businessId mismatch for user {UserId}", id, user.Id);
            return Forbid(_403statusMessage);
        }

        _logger.LogInformation("Returning ServiceId={ServiceId} for user {UserId}", id, user.Id);
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
        _logger.LogInformation("Received GetServicesByUserId request from user {UserIdInitiator}, requestedUserId={RequestedUserId}", User?.Claims.FirstOrDefault()?.Value, userId);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetServicesByUserId: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;
        _logger.LogInformation("Validating employee userId={RequestedUserId} for businessId={BusinessId}, userId={UserIdInitiator}", userId, businessId, user.Id);

        var employee = await _userRepository.GetByIdAsync(userId);

        if (employee == null)
        {
            _logger.LogWarning("GetServicesByUserId: Employee userId={RequestedUserId} not found for user {UserId}", userId, user.Id);
            return BadRequest(_400statusMessage);
        }

        if (businessId != employee.BusinessId)
        {
            _logger.LogWarning("GetServicesByUserId: Forbidden access. Employee businessId={EmployeeBusinessId} != requestor businessId={BusinessId}", employee.BusinessId, businessId);
            return Forbid(_403statusMessage);
        }

        _logger.LogInformation("Fetching services for requestedUserId={RequestedUserId} in businessId={BusinessId}", userId, businessId);
        var userServices = await _serviceRepository.GetServicesByUserIdAsync(businessId, userId);

        if (userServices == null)
        {
            _logger.LogWarning("GetServicesByUserId: No services found for userId={RequestedUserId}, businessId={BusinessId}", userId, businessId);
            return NotFound(_404statusMessage);
        }

        _logger.LogInformation("Returning {Count} services for requestedUserId={RequestedUserId}, businessId={BusinessId}", userServices.Count(), userId, businessId);
        return Ok(userServices);
    }

    [HttpPost(Name = "CreateService")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Creates a new service.")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Appointment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceDto createServiceDto)
    {
        _logger.LogInformation("Received CreateService request from user {UserId} with dto={Dto}", User?.Claims.FirstOrDefault()?.Value, createServiceDto);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateService: Invalid model for user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("CreateService: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;
            _logger.LogInformation("Creating service for businessId={BusinessId}, userId={UserId}", businessId, user.Id);

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

            _logger.LogInformation("Created service ServiceId={ServiceId} for businessId={BusinessId}, userId={UserId}", newService.Id, businessId, user.Id);
            return CreatedAtRoute("GetServiceById", new { id = newService.Id }, newService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service. UserId={UserId}. Message: {Message}", User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut("{id:int}", Name = "UpdateService")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Update an existing service.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Service))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> UpdateService([FromBody] UpdateServiceDto updateServiceDto, int id)
    {
        _logger.LogInformation("Received UpdateService request for ServiceId={ServiceId} from user {UserId} with dto={Dto}", id, User?.Claims.FirstOrDefault()?.Value, updateServiceDto);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateService: Invalid model state from user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("UpdateService: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;
            _logger.LogInformation("Fetching ServiceId={ServiceId} for update, businessId={BusinessId}, userId={UserId}", id, businessId, user.Id);

            var service = await _serviceRepository.GetServiceByIdAsync(id, businessId);
            if (service == null)
            {
                _logger.LogWarning("UpdateService: ServiceId={ServiceId} not found for user {UserId}", id, user.Id);
                return NotFound(_404statusMessage);
            }

            if (businessId != service.BusinessId)
            {
                _logger.LogWarning("UpdateService: Forbidden access to ServiceId={ServiceId}, businessId mismatch for user {UserId}", id, user.Id);
                return Forbid(_403statusMessage);
            }

            _logger.LogInformation("Updating ServiceId={ServiceId} for user {UserId}", id, user.Id);
            if (updateServiceDto.Name != null)
                service.Name = updateServiceDto.Name;
            if (updateServiceDto.ServiceCharge != null)
                service.ServiceCharge = (decimal)updateServiceDto.ServiceCharge;
            if (updateServiceDto.IsPercentage != null)
                service.IsPercentage = (bool)updateServiceDto.IsPercentage;
            if (updateServiceDto.Duration != null)
                service.Duration = (uint)updateServiceDto.Duration;

            _serviceRepository.Update(service);
            await _serviceRepository.SaveChangesAsync();

            _logger.LogInformation("ServiceId={ServiceId} updated successfully for user {UserId}", service.Id, user.Id);
            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ServiceId={ServiceId} for user {UserId}. Message: {Message}", id, User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{id:int}", Name = "DeleteService")]
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
        _logger.LogInformation("Received DeleteService request for ServiceId={ServiceId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("DeleteService: Invalid model state from user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("DeleteService: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;
            _logger.LogInformation("Fetching ServiceId={ServiceId} for deletion, businessId={BusinessId}, userId={UserId}", id, businessId, user.Id);

            var service = await _serviceRepository.GetServiceByIdAsync(id, businessId);
            if (service == null)
            {
                _logger.LogWarning("DeleteService: ServiceId={ServiceId} not found for user {UserId}", id, user.Id);
                return NotFound(_404statusMessage);
            }

            if (businessId != service.BusinessId)
            {
                _logger.LogWarning("DeleteService: Forbidden access to ServiceId={ServiceId}, businessId mismatch for user {UserId}", id, user.Id);
                return Forbid(_403statusMessage);
            }

            _logger.LogInformation("Deleting ServiceId={ServiceId} for user {UserId}", id, user.Id);
            _serviceRepository.Remove(service);
            await _serviceRepository.SaveChangesAsync();

            _logger.LogInformation("ServiceId={ServiceId} deleted successfully for user {UserId}", id, user.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ServiceId={ServiceId} for user {UserId}. Message: {Message}", id, User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}