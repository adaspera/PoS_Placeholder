using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using System.Diagnostics;

namespace PoS_Placeholder.Server.Controllers;

[ApiController]
[Route("/api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly ServiceRepository _serviceRepository;
    private readonly AppointmentRepository _appointmentRepository;
    private readonly UserManager<User> _userManager;

    public AppointmentController(AppointmentRepository appointmentRepository, ServiceRepository serviceRepository, UserManager<User> userManager)
    {
        _appointmentRepository = appointmentRepository;
        _serviceRepository = serviceRepository;
        _userManager = userManager;
    }

    [HttpGet("all", Name = "GetAllAppointments")]
    [Authorize]
    public async Task<IActionResult> GetAllAppointments()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var businessAppointments = await _appointmentRepository.GetAppointmentsByBusinessIdAsync(userBusinessId);

        if (businessAppointments == null)
        {
            return NotFound("Appointments not found.");
        }

        return Ok(businessAppointments);
    }

    [HttpGet("{id:int}", Name = "GetAppointmentById")]
    [Authorize]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid appointment ID.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;

        var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, userBusinessId);
        if (appointment == null)
        {
            return NotFound("Appointment not found.");
        }

        return Ok(appointment);
    }

    [HttpGet("user/{id:int}", Name = "GetAllAppointmentsByUserId")]
    [Authorize]
    public async Task<IActionResult> GetAllAppointmentsByUserId(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var userAppointments = await _appointmentRepository.GetAppointmentsByUserIdAsync(userBusinessId, id);

        if (userAppointments == null)
        {
            return NotFound("Appointments not found.");
        }

        return Ok(userAppointments);
    }

    

    [HttpPost("create", Name = "CreateAppointment")]
    [Authorize(Roles = nameof(UserRole.Owner))]
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
                return Unauthorized("User not found.");
            }

            var businessId = user.BusinessId;
            var appointmentService = await _serviceRepository.GetServiceByIdAsync(createAppointmentDto.ServiceId, businessId);

            var newAppointment = new Appointment
            {
                TimeCreated = DateTime.Now,
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
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, userBusinessId);
            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            if (userBusinessId != appointment.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this appointment.");
            }

            appointment.TimeReserved = updateAppointmentDto.TimeReserved;
            appointment.CustomerName = updateAppointmentDto.CustomerName;
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
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(id, userBusinessId);
            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            if (userBusinessId != appointment.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this service.");
            }

            _appointmentRepository.Remove(appointment);
            await _appointmentRepository.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}