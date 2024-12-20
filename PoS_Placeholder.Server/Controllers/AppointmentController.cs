using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using PoS_Placeholder.Server.Services;
using Stripe;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly ServiceRepository _serviceRepository;
    private readonly AppointmentRepository _appointmentRepository;
    private readonly IDateTimeService _dateTimeService;
    private readonly UserManager<User> _userManager;
    private readonly UserRepository _userRepository;
    private readonly ILogger<AppointmentController> _logger;

    private readonly string _400statusMessage = "Bad request. The request could not be understood by the server due to malformed syntax.";
    private readonly string _401statusMessage = "Unauthorized. Please provide valid credentials.";
    private readonly string _403statusMessage = "Forbidden. You do not have access to this resource.";
    private readonly string _404statusMessage = "Resource not found.";

    public AppointmentController(AppointmentRepository appointmentRepository, ServiceRepository serviceRepository,
        UserManager<User> userManager, IDateTimeService dateTimeService, UserRepository userRepository,
        ILogger<AppointmentController> logger)
    {
        _appointmentRepository = appointmentRepository;
        _serviceRepository = serviceRepository;
        _dateTimeService = dateTimeService;
        _userManager = userManager;
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet("all", Name = "GetAllAppointments")]
    [Authorize]
    [Description("Gets all appointments for the current user's business.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Appointment>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetAllAppointments()
    {
        _logger.LogInformation("Received GetAllAppointments request from user {UserId}", User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllAppointments: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;
        var businessAppointments = await _appointmentRepository.GetAppointmentsByBusinessIdAsync(businessId);

        if (businessAppointments == null)
        {
            _logger.LogWarning("GetAllAppointments: No appointments found for user {UserId} businessId={BusinessId}", user.Id, user.BusinessId);
            return NotFound(_404statusMessage);
        }

        _logger.LogInformation("GetAllAppointments: Returning {Count} appointments for user {UserId}", businessAppointments.Count(), user.Id);
        return Ok(businessAppointments);
    }
        
    [HttpGet("{id:int}", Name = "GetAppointmentById")]
    [Authorize]
    [Description("Gets an appointment that belongs to the user's business using the appointment's ID.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Appointment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        _logger.LogInformation("Received GetAppointmentById request for AppointmentId={AppointmentId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);
        
        if (id <= 0)
        {
            _logger.LogWarning("GetAppointmentById: Invalid appointmentId={AppointmentId}", id);
            return BadRequest(_400statusMessage);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAppointmentById: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;

        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, businessId);
        if (appointment == null)
        {
            _logger.LogWarning("GetAppointmentById: AppointmentId={AppointmentId} not found for user {UserId}", id, user.Id);
            return NotFound(_404statusMessage);
        }

        if (businessId != appointment.BusinessId)
        {
            _logger.LogWarning("GetAppointmentById: Forbidden access to AppointmentId={AppointmentId}, businessId mismatch for user {UserId}", id, user.Id);
            return Forbid(_403statusMessage);
        }

        _logger.LogInformation("GetAppointmentById: Returning appointmentId={AppointmentId} for user {UserId}", id, user.Id);
        return Ok(appointment);
    }

    [HttpGet("user/{userId}", Name = "GetAllAppointmentsByUserId")]
    [Authorize]
    [Description("Get all appointments assigned to a user using the user's ID.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Appointment>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetAllAppointmentsByUserId(string userId)
    {
        _logger.LogInformation("Received GetAllAppointmentsByUserId request from user {UserId}, requestedUserId={RequestedUserId}", User?.Claims.FirstOrDefault()?.Value, userId);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetAllAppointmentsByUserId: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;
        _logger.LogInformation("Fetching appointments for requestedUserId={RequestedUserId} in businessId={BusinessId}, initiated by user {UserId}", userId, businessId, user.Id);

        var employee = await _userRepository.GetByIdAsync(userId);

        if (employee == null)
        {
            _logger.LogWarning("GetAllAppointmentsByUserId: Employee userId={RequestedUserId} not found for user {UserId}", userId, user.Id);
            return BadRequest(_400statusMessage);
        }

        if (businessId != employee.BusinessId)
        {
            _logger.LogWarning("GetAllAppointmentsByUserId: Forbidden access. Employee businessId={EmployeeBusinessId} != requestor businessId={BusinessId}", employee.BusinessId, businessId);
            return Forbid(_403statusMessage);
        }

        var userAppointments = await _appointmentRepository.GetAppointmentsByUserIdAsync(businessId, userId);

        if (userAppointments == null)
        {
            _logger.LogWarning("GetAllAppointmentsByUserId: No appointments found for userId={RequestedUserId}, businessId={BusinessId}", userId, businessId);
            return NotFound(_404statusMessage);
        }
        
        _logger.LogInformation("GetAllAppointmentsByUserId: Returning {Count} appointments for userId={RequestedUserId}, businessId={BusinessId}", userAppointments.Count(), userId, businessId);
        return Ok(userAppointments);
    }

    

    [HttpPost(Name = "CreateAppointment")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Create a new appointment.")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Appointment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto createAppointmentDto)
    {
        _logger.LogInformation("Received CreateAppointment request from user {UserId} for serviceId={ServiceId}", User?.Claims.FirstOrDefault()?.Value, createAppointmentDto.ServiceId);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreateAppointment: Invalid model for user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("CreateAppointment: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;
            var service = await _serviceRepository.GetServiceByIdAsync(createAppointmentDto.ServiceId, businessId);

            if(service == null)
            {
                _logger.LogWarning("CreateAppointment: ServiceId={ServiceId} not found for businessId={BusinessId}", createAppointmentDto.ServiceId, businessId);
                return BadRequest(_400statusMessage);
            }

            var appointmentService = await _serviceRepository.GetServiceByIdAsync(createAppointmentDto.ServiceId, businessId);

            var dateFormat = _dateTimeService.GetDateFormatByISO("ISO8601");

            if (DateTime.TryParseExact(createAppointmentDto.TimeReserved, "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timeReserved))
            {
                createAppointmentDto.TimeReserved = timeReserved.ToString(dateFormat);
            }
            else
            {
                _logger.LogWarning("CreateAppointment: Invalid time format {TimeReserved} provided by user {UserId}", createAppointmentDto.TimeReserved, user.Id);
                return BadRequest(_400statusMessage);
            }

            createAppointmentDto.TimeReserved = timeReserved.ToString(dateFormat);

            var newAppointment = new Appointment
            {
                TimeCreated = DateTime.Now.ToString(dateFormat),
                TimeReserved = createAppointmentDto.TimeReserved,
                CustomerName = createAppointmentDto.CustomerName,
                CustomerPhone = createAppointmentDto.CustomerPhone,
                BusinessId = businessId,
                ServiceId = createAppointmentDto.ServiceId,
                UserId = appointmentService.UserId
            };

            _logger.LogInformation("Creating appointment for user {UserId}, businessId={BusinessId}, serviceId={ServiceId}", user.Id, businessId, createAppointmentDto.ServiceId);
            _appointmentRepository.Add(newAppointment);
            await _appointmentRepository.SaveChangesAsync();

            _logger.LogInformation("CreateAppointment: AppointmentId={AppointmentId} created successfully for user {UserId}", newAppointment.Id, user.Id);
            return CreatedAtRoute("GetAppointmentById", new { id = newAppointment.Id }, newAppointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment for user {UserId}. Message: {Message}", User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut("{id:int}", Name = "UpdateAppointment")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Update an existing appointment.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Appointment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> UpdateAppointment([FromBody] UpdateAppointmentDto updateAppointmentDto, int id)
    {
        _logger.LogInformation("Received UpdateAppointment request for AppointmentId={AppointmentId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("UpdateAppointment: Invalid model for user {UserId}. Errors: {Errors}",
                    User?.Claims.FirstOrDefault()?.Value,
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("UpdateAppointment: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;
            _logger.LogInformation("Fetching appointmentId={AppointmentId} for update, businessId={BusinessId}, userId={UserId}", id, businessId, user.Id);

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, businessId);
            if (appointment == null)
            {
                _logger.LogWarning("UpdateAppointment: AppointmentId={AppointmentId} not found for user {UserId}", id, user.Id);
                return NotFound(_404statusMessage);
            }

            if (businessId != appointment.BusinessId)
            {
                _logger.LogWarning("UpdateAppointment: Forbidden access to AppointmentId={AppointmentId}, businessId mismatch for user {UserId}", id, user.Id);
                return Forbid(_403statusMessage);
            }

            var dateFormat = _dateTimeService.GetDateFormatByISO("ISO8601");

            appointment.TimeUpdated = DateTime.Now.ToString(dateFormat);

            if(updateAppointmentDto.TimeReserved != null)
            {
                if (DateTime.TryParseExact(updateAppointmentDto.TimeReserved, "yyyy-MM-ddTHH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timeReserved))
                {
                    updateAppointmentDto.TimeReserved = timeReserved.ToString(dateFormat);
                }
                else
                {
                    _logger.LogWarning("UpdateAppointment: Invalid time format {TimeReserved} provided by user {UserId}", updateAppointmentDto.TimeReserved, user.Id);
                    return BadRequest(_400statusMessage);
                }

                appointment.TimeReserved = updateAppointmentDto.TimeReserved;
            }
            if (updateAppointmentDto.CustomerName != null)
                appointment.CustomerName = updateAppointmentDto.CustomerName;
            if (updateAppointmentDto.CustomerPhone != null)
                appointment.CustomerPhone = updateAppointmentDto.CustomerPhone;

            _logger.LogInformation("Updating appointmentId={AppointmentId} for user {UserId}", id, user.Id);
            _appointmentRepository.Update(appointment);
            await _appointmentRepository.SaveChangesAsync();

            _logger.LogInformation("UpdateAppointment: AppointmentId={AppointmentId} updated successfully for user {UserId}", appointment.Id, user.Id);
            return Ok(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointmentId={AppointmentId} for user {UserId}. Message: {Message}", id, User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("{id:int}", Name = "DeleteAppointment")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Delete an existing appointment")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(NoContentResult))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        _logger.LogInformation("Received DeleteAppointment request for AppointmentId={AppointmentId} from user {UserId}", id, User?.Claims.FirstOrDefault()?.Value);

        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("DeleteAppointment: Invalid model state for user {UserId}", User?.Claims.FirstOrDefault()?.Value);
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("DeleteAppointment: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
                return Unauthorized(_401statusMessage);
            }

            var businessId = user.BusinessId;
            _logger.LogInformation("Fetching appointmentId={AppointmentId} for deletion, businessId={BusinessId}, userId={UserId}", id, businessId, user.Id);

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, businessId);
            if (appointment == null)
            {
                _logger.LogWarning("DeleteAppointment: AppointmentId={AppointmentId} not found for user {UserId}", id, user.Id);
                return NotFound(_404statusMessage);
            }

            if (businessId != appointment.BusinessId)
            {
                _logger.LogWarning("DeleteAppointment: Forbidden access to AppointmentId={AppointmentId}, businessId mismatch for user {UserId}", id, user.Id);
                return Forbid(_403statusMessage);
            }

            _logger.LogInformation("Deleting appointmentId={AppointmentId} for user {UserId}", id, user.Id);
            _appointmentRepository.Remove(appointment);
            await _appointmentRepository.SaveChangesAsync();

            _logger.LogInformation("DeleteAppointment: AppointmentId={AppointmentId} deleted successfully for user {UserId}", id, user.Id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointmentId={AppointmentId} for user {UserId}. Message: {Message}", id, User?.Claims.FirstOrDefault()?.Value, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}