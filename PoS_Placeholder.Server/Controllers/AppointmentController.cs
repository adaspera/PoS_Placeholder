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

    private readonly string _400statusMessage = "Bad request. The request could not be understood by the server due to malformed syntax.";
    private readonly string _401statusMessage = "Unauthorized. Please provide valid credentials.";
    private readonly string _403statusMessage = "Foribdden. You do not have access to this resource.";
    private readonly string _404statusMessage = "Resource not found.";

    public AppointmentController(AppointmentRepository appointmentRepository, ServiceRepository serviceRepository, 
        UserManager<User> userManager, IDateTimeService dateTimeService, UserRepository userRepository)
    {
        _appointmentRepository = appointmentRepository;
        _serviceRepository = serviceRepository;
        _dateTimeService = dateTimeService;
        _userManager = userManager;
        _userRepository = userRepository;
    }

    [HttpGet("all", Name = "GetAllAppointments")]
    [Authorize]
    [Description("Gets all appointments for the current user's business.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Appointment>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    public async Task<IActionResult> GetAllAppointments()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized(_401statusMessage);
        }

        var businessId = user.BusinessId;
        var businessAppointments = await _appointmentRepository.GetAppointmentsByBusinessIdAsync(businessId);

        if (businessAppointments == null)
        {
            return NotFound(_404statusMessage);
        }

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

        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, businessId);
        if (appointment == null)
        {
            return NotFound(_404statusMessage);
        }

        if (businessId != appointment.BusinessId)
        {
            return Forbid(_403statusMessage);
        }

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

        var userAppointments = await _appointmentRepository.GetAppointmentsByUserIdAsync(businessId, userId);

        if (userAppointments == null)
        {
            return NotFound(_404statusMessage);
        }
        

        return Ok(userAppointments);
    }

    

    [HttpPost("create", Name = "CreateAppointment")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Create a new appointment.")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Appointment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> CreateAppointment([FromForm] CreateAppointmentDto createAppointmentDto)
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
            var service = _serviceRepository.GetServiceByIdAsync(createAppointmentDto.ServiceId, businessId);

            if(service == null)
            {
                return BadRequest(_400statusMessage);
            }

            var appointmentService = await _serviceRepository.GetServiceByIdAsync(createAppointmentDto.ServiceId, businessId);

            var dateFormat = _dateTimeService.GetDateFormatByISO("ISO8601");

            if (DateTime.TryParseExact(createAppointmentDto.TimeReserved, "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeReserved))
            {
                createAppointmentDto.TimeReserved = timeReserved.ToString(dateFormat);
            }
            else
            {
                return BadRequest(_400statusMessage);
            }

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

            _appointmentRepository.Add(newAppointment);
            await _appointmentRepository.SaveChangesAsync();

            return CreatedAtRoute("GetAppointmentById", new { id = newAppointment.Id }, newAppointment);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPut("update/{id:int}", Name = "UpdateAppointment")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    [Description("Update an existing appointment.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Appointment))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(BadRequestResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(UnauthorizedResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ForbidResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(NotFoundResult))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(StatusCodeResult))]
    public async Task<IActionResult> UpdateAppointment([FromForm] UpdateAppointmentDto updateAppointmentDto, int id)
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

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, businessId);
            if (appointment == null)
            {
                return NotFound(_404statusMessage);
            }

            if (businessId != appointment.BusinessId)
            {
                return Forbid(_403statusMessage);
            }

            var dateFormat = _dateTimeService.GetDateFormatByISO("ISO8601");

            appointment.TimeUpdated = DateTime.Now.ToString(dateFormat);

            if(updateAppointmentDto.TimeReserved != null)
            {
                if (DateTime.TryParseExact(updateAppointmentDto.TimeReserved, "yyyyMMdd-HHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeReserved))
                {
                    updateAppointmentDto.TimeReserved = timeReserved.ToString(dateFormat);
                }
                else
                {
                    return BadRequest(_400statusMessage);
                }

                appointment.TimeReserved = updateAppointmentDto.TimeReserved;
            }
            if (updateAppointmentDto.CustomerName != null)
                appointment.CustomerName = updateAppointmentDto.CustomerName;
            if (updateAppointmentDto.CustomerPhone != null)
                appointment.CustomerPhone = updateAppointmentDto.CustomerPhone;

            _appointmentRepository.Update(appointment);
            await _appointmentRepository.SaveChangesAsync();

            return Ok(appointment);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpDelete("delete/{id:int}", Name = "DeleteAppointment")]
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

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, businessId);
            if (appointment == null)
            {
                return NotFound(_404statusMessage);
            }

            if (businessId != appointment.BusinessId)
            {
                return Forbid(_403statusMessage);
            }

            _appointmentRepository.Remove(appointment);
            await _appointmentRepository.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}