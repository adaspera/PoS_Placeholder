using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;
using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Repositories;
using Stripe;

namespace PoS_Placeholder.Server.Controllers;

[Route("api/business")]
[ApiController]
public class BusinessController : ControllerBase
{
    private readonly BusinessRepository _businessRepository;
    private readonly UserRepository _userRepository;
    private readonly UserWorkTimeRepository _userWorkTimeRepository;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<BusinessController> _logger;

    public BusinessController(BusinessRepository businessRepository, UserManager<User> userManager, 
        UserRepository userRepository, UserWorkTimeRepository userWorkTimeRepository, ILogger<BusinessController> logger)
    {
        _businessRepository = businessRepository;
        _userManager = userManager;
        _userRepository = userRepository;
        _userWorkTimeRepository = userWorkTimeRepository;
        _logger = logger;
    }
    
    
    // Not in use
    [HttpPut("{business_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateBusiness(int business_id, [FromBody] UpdateBusinessDto dto)
    {
        _logger.LogInformation("Received UpdateBusiness request for business_id={BusinessId} from user {UserId}", business_id, User?.Claims.FirstOrDefault()?.Value);

        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            _logger.LogWarning("UpdateBusiness: Business not found. business_id={BusinessId}", business_id);
            return NotFound("Could not find the business.");
        }

        _logger.LogInformation("Updating fields for business_id={BusinessId}", business_id);

        if (!string.IsNullOrWhiteSpace(dto.Name)) business.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) business.Phone = dto.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Email)) business.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Street)) business.Street = dto.Street;
        if (!string.IsNullOrWhiteSpace(dto.City)) business.City = dto.City;
        if (!string.IsNullOrWhiteSpace(dto.Region)) business.Region = dto.Region;
        if (!string.IsNullOrWhiteSpace(dto.Country)) business.Country = dto.Country;

        if (await _businessRepository.UniquePhoneOrEmailAsync(business.Phone, business.Email, business_id))
        {
            _logger.LogWarning("UpdateBusiness conflict: Phone/Email already exists for business_id={BusinessId}", business_id);
            return Conflict("Phone/email already exists.");
        }

        try
        {
            _logger.LogInformation("Saving updated business_id={BusinessId}", business_id);
            _businessRepository.Update(business);
            await _businessRepository.SaveChangesAsync();
            _logger.LogInformation("Business_id={BusinessId} updated successfully", business_id);
            return Ok(business);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business_id={BusinessId}. Message: {Message}", business_id, ex.Message);
            return StatusCode(500, "An error occurred while updating the business.");
        }
    }

    [HttpGet("employees")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> GetEmployees()
    {
        _logger.LogInformation("Received GetEmployees request from user {UserId}", User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetEmployees: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Fetching employees for businessId={BusinessId}, userId={UserId}", user.BusinessId, user.Id);
        var users = await _userRepository.GetUsersByBusinessIdAsync(user.BusinessId);
        
        var employees = new List<User>();
        foreach (var u in users)
        {
            if (await _userManager.IsInRoleAsync(u, nameof(UserRole.Employee)))
            {
                employees.Add(u);
            }
        }

        _logger.LogInformation("Returning {Count} employees for businessId={BusinessId}, userId={UserId}", employees.Count, user.BusinessId, user.Id);
        return Ok(employees); // 200 OK
    }

    [HttpPut("employees")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> UpdateEmployee([FromBody] UpdateEmployeeDto dto)
    {
        _logger.LogInformation("Received UpdateEmployee request from user {UserId}", User?.Claims.FirstOrDefault()?.Value);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("UpdateEmployee: Invalid model state from user {UserId}. Errors: {Errors}",
                User?.Claims.FirstOrDefault()?.Value,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return UnprocessableEntity(ModelState);
        }
        
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("UpdateEmployee: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Fetching employee_id={EmployeeId} for businessId={BusinessId}, userId={UserId}",
            dto.Id, user.BusinessId, user.Id);
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(dto.Id, user.BusinessId);
        if (employee == null)
        {
            _logger.LogWarning("UpdateEmployee: Employee_id={EmployeeId} not found or not in businessId={BusinessId}", dto.Id, user.BusinessId);
            return NotFound("Employee not found or does not belong to the business.");
        }

        _logger.LogInformation("Updating employee_id={EmployeeId}", dto.Id);
        if (!string.IsNullOrWhiteSpace(dto.FirstName)) employee.FirstName = dto.FirstName;
        if (!string.IsNullOrWhiteSpace(dto.LastName)) employee.LastName = dto.LastName;
        if (!string.IsNullOrWhiteSpace(dto.Email)) employee.Email = dto.Email;
        if (dto.AvailabilityStatus.HasValue) employee.AvailabilityStatus = dto.AvailabilityStatus.Value;
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) employee.PhoneNumber = dto.PhoneNumber;

        try
        {
            _logger.LogInformation("Saving updated employee_id={EmployeeId} for businessId={BusinessId}", dto.Id, user.BusinessId);
            _userRepository.Update(employee);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Employee_id={EmployeeId} updated successfully", dto.Id);
            return Ok(employee); // 200 OK
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee_id={EmployeeId} for businessId={BusinessId}. Message: {Message}", dto.Id, user.BusinessId, ex.Message);
            return StatusCode(500, "An error occurred while updating the employee.");
        }
    }
    
    [HttpDelete("employees/{employee_id}")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> DeleteEmployee(string employee_id)
    {
        _logger.LogInformation("Received DeleteEmployee request for employee_id={EmployeeId} from user {UserId}", employee_id, User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("DeleteEmployee: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Fetching employee_id={EmployeeId} for businessId={BusinessId}, userId={UserId}", employee_id, user.BusinessId, user.Id);
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, user.BusinessId);
        if (employee == null)
        {
            _logger.LogWarning("DeleteEmployee: Employee_id={EmployeeId} not found or not in businessId={BusinessId}", employee_id, user.BusinessId);
            return NotFound("Employee not found or does not belong to the business.");
        }

        try
        {
            _logger.LogInformation("Deleting employee_id={EmployeeId}", employee_id);
            _userRepository.Remove(employee);
            await _userRepository.SaveChangesAsync();
            _logger.LogInformation("Employee_id={EmployeeId} deleted successfully", employee_id);
            return NoContent(); // 204 No Content
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting employee_id={EmployeeId} for businessId={BusinessId}. Message: {Message}", employee_id, user.BusinessId, ex.Message);
            return StatusCode(500, "An error occurred while deleting the employee.");
        }
    }

    [HttpGet("users/{employee_id}/schedules")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> GetSchedules(string employee_id)
    {
        _logger.LogInformation("Received GetSchedules request for employee_id={EmployeeId} from user {UserId}", employee_id, User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetSchedules: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Validating employee_id={EmployeeId} for businessId={BusinessId}, userId={UserId}", employee_id, user.BusinessId, user.Id);
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, user.BusinessId);
        if (employee == null)
        {
            _logger.LogWarning("GetSchedules: Employee_id={EmployeeId} not found or not in businessId={BusinessId}", employee_id, user.BusinessId);
            return NotFound("Employee not found or does not belong to the business.");
        }

        _logger.LogInformation("Fetching schedules for employee_id={EmployeeId}", employee_id);
        var schedules = await _userWorkTimeRepository.GetSchedulesByEmployeeIdAsync(employee_id);

        _logger.LogInformation("Returning {Count} schedules for employee_id={EmployeeId}", schedules.Count(), employee_id);
        return Ok(schedules); // 200 OK
    }

    [HttpPost("users/{employee_id}/schedule")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> AssignSchedule(string employee_id, [FromBody] AssignScheduleDto scheduleDto)
    {
        _logger.LogInformation("Received AssignSchedule request for employee_id={EmployeeId} from user {UserId}", employee_id, User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("AssignSchedule: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Validating employee_id={EmployeeId} for businessId={BusinessId}, userId={UserId}", employee_id, user.BusinessId, user.Id);
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, user.BusinessId);
        if (employee == null)
        {
            _logger.LogWarning("AssignSchedule: Employee_id={EmployeeId} not found or not in businessId={BusinessId}", employee_id, user.BusinessId);
            return NotFound("Employee not found or does not belong to the business.");
        }

        _logger.LogInformation("Creating schedule for employee_id={EmployeeId}", employee_id);
        var userWorkTime = new UserWorkTime
        {
            Day = scheduleDto.Day,
            StartTime = scheduleDto.StartTime,
            EndTime = scheduleDto.EndTime,
            BreakStart = scheduleDto.BreakStart,
            BreakEnd = scheduleDto.BreakEnd,
            UserId = employee_id
        };

        try
        {
            _logger.LogInformation("Saving new schedule for employee_id={EmployeeId}", employee_id);
            _userWorkTimeRepository.Add(userWorkTime);
            await _userWorkTimeRepository.SaveChangesAsync();

            _logger.LogInformation("ScheduleId={ScheduleId} created successfully for employee_id={EmployeeId}", userWorkTime.Id, employee_id);
            return CreatedAtAction(nameof(AssignSchedule), new { id = userWorkTime.Id }, userWorkTime); // 201 Created
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule for employee_id={EmployeeId}. Message: {Message}", employee_id, ex.Message);
            return StatusCode(500, "An error occurred while creating the schedule.");
        }
    }

    [HttpPut("users/{employee_id}/schedule/{schedule_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateSchedule(string employee_id, int schedule_id, [FromBody] AssignScheduleDto scheduleDto)
    {
        _logger.LogInformation("Received UpdateSchedule request for employee_id={EmployeeId}, schedule_id={ScheduleId} from user {UserId}",
            employee_id, schedule_id, User?.Claims.FirstOrDefault()?.Value);
        
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("UpdateSchedule: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Validating employee_id={EmployeeId} for businessId={BusinessId}, userId={UserId}", employee_id, user.BusinessId, user.Id);
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, user.BusinessId);
        if (employee == null)
        {
            _logger.LogWarning("UpdateSchedule: Employee_id={EmployeeId} not found or not in businessId={BusinessId}", employee_id, user.BusinessId);
            return NotFound("Employee not found or does not belong to the business.");
        }

        _logger.LogInformation("Fetching schedule_id={ScheduleId} for employee_id={EmployeeId}", schedule_id, employee_id);
        var schedule = await _userWorkTimeRepository.GetScheduleByIdAndEmployeeAsync(employee_id, schedule_id);
        if (schedule == null)
        {
            _logger.LogWarning("UpdateSchedule: schedule_id={ScheduleId} not found for employee_id={EmployeeId}", schedule_id, employee_id);
            return NotFound("Schedule not found.");
        }

        _logger.LogInformation("Updating schedule_id={ScheduleId} for employee_id={EmployeeId}", schedule_id, employee_id);
        schedule.Day = scheduleDto.Day;
        schedule.StartTime = scheduleDto.StartTime;
        schedule.EndTime = scheduleDto.EndTime;
        schedule.BreakStart = scheduleDto.BreakStart;
        schedule.BreakEnd = scheduleDto.BreakEnd;

        try
        {
            _logger.LogInformation("Saving updated schedule_id={ScheduleId} for employee_id={EmployeeId}", schedule_id, employee_id);
            _userWorkTimeRepository.Update(schedule);
            await _userWorkTimeRepository.SaveChangesAsync();

            _logger.LogInformation("Schedule_id={ScheduleId} updated successfully for employee_id={EmployeeId}", schedule_id, employee_id);
            return Ok(schedule); // 200 OK
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule_id={ScheduleId} for employee_id={EmployeeId}. Message: {Message}", schedule_id, employee_id, ex.Message);
            return StatusCode(500, "An error occurred while updating the schedule.");
        }
    }

    [HttpDelete("users/{employee_id}/schedule/{schedule_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteSchedule(string employee_id, int schedule_id)
    {
        _logger.LogInformation("Received DeleteSchedule request for employee_id={EmployeeId}, schedule_id={ScheduleId} from user {UserId}",
            employee_id, schedule_id, User?.Claims.FirstOrDefault()?.Value);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("DeleteSchedule: User not found or unauthorized. UserId={UserId}", User?.Claims.FirstOrDefault()?.Value);
            return Unauthorized("User not found.");
        }

        _logger.LogInformation("Validating employee_id={EmployeeId} for businessId={BusinessId}, userId={UserId}", employee_id, user.BusinessId, user.Id);
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, user.BusinessId);
        if (employee == null)
        {
            _logger.LogWarning("DeleteSchedule: Employee_id={EmployeeId} not found or not in businessId={BusinessId}", employee_id, user.BusinessId);
            return NotFound("Employee not found or does not belong to the business.");
        }

        _logger.LogInformation("Fetching schedule_id={ScheduleId} for deletion, employee_id={EmployeeId}", schedule_id, employee_id);
        var schedule = await _userWorkTimeRepository.GetScheduleByIdAndEmployeeAsync(employee_id, schedule_id);
        if (schedule == null)
        {
            _logger.LogWarning("DeleteSchedule: Schedule_id={ScheduleId} not found for employee_id={EmployeeId}", schedule_id, employee_id);
            return NotFound("Schedule not found.");
        }

        try
        {
            _logger.LogInformation("Deleting schedule_id={ScheduleId} for employee_id={EmployeeId}", schedule_id, employee_id);
            _userWorkTimeRepository.Remove(schedule);
            await _userWorkTimeRepository.SaveChangesAsync();

            _logger.LogInformation("Schedule_id={ScheduleId} deleted successfully for employee_id={EmployeeId}", schedule_id, employee_id);
            return NoContent(); // 204 No Content
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule_id={ScheduleId} for employee_id={EmployeeId}. Message: {Message}", schedule_id, employee_id, ex.Message);
            return StatusCode(500, "An error occurred while deleting the schedule.");
        }
    }
}
