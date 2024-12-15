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

    public BusinessController(BusinessRepository businessRepository, UserManager<User> userManager, UserRepository userRepository, UserWorkTimeRepository userWorkTimeRepository)
    {
        _businessRepository = businessRepository;
        _userManager = userManager;
        _userRepository = userRepository;
        _userWorkTimeRepository = userWorkTimeRepository;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SuperAdmin))]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessDto dto)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        // Check for uniqueness
        if (await _businessRepository.ExistsByPhoneOrEmailAsync(dto.Phone, dto.Email))
        {
            return Conflict("Phone/email already in use.");
        }

        var business = new Business
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Street = dto.Street,
            City = dto.City,
            Region = dto.Region,
            Country = dto.Country
        };

        try
        {
            _businessRepository.Add(business);
            await _businessRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateBusiness), new { id = business.Id }, business);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the business.");
        }
    }

    [HttpPut("{business_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateBusiness(int business_id, [FromBody] UpdateBusinessDto dto)
    {
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Name)) business.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) business.Phone = dto.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Email)) business.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Street)) business.Street = dto.Street;
        if (!string.IsNullOrWhiteSpace(dto.City)) business.City = dto.City;
        if (!string.IsNullOrWhiteSpace(dto.Region)) business.Region = dto.Region;
        if (!string.IsNullOrWhiteSpace(dto.Country)) business.Country = dto.Country;

        if (await _businessRepository.UniquePhoneOrEmailAsync(business.Phone, business.Email, business_id))
        {
            return Conflict("Phone/email already exists.");
        }

        try
        {
            _businessRepository.Update(business);
            await _businessRepository.SaveChangesAsync();
            return Ok(business);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while updating the business.");
        }
    }

    [HttpGet("{business_id:int}/employees")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> GetEmployees(int business_id)
    {
        // Validate business existence
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Retrieve employees for the business
        var employees = await _userRepository.GetEmployeesByBusinessIdAsync(business_id);
        if (employees == null || !employees.Any())
        {
            return NotFound("No employees found for this business.");
        }

        return Ok(employees); // 200 OK
    }

    [HttpPut("{business_id:int}/employees/{employee_id}")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> UpdateEmployee(int business_id, string employee_id, [FromBody] UpdateEmployeeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState); // 422 Validation exception
        }

        // Find the business and ensure it exists
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Find the employee by ID and ensure they belong to the business
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        // Update employee fields if provided
        if (!string.IsNullOrWhiteSpace(dto.FirstName)) employee.FirstName = dto.FirstName;
        if (!string.IsNullOrWhiteSpace(dto.LastName)) employee.LastName = dto.LastName;
        if (!string.IsNullOrWhiteSpace(dto.Email)) employee.Email = dto.Email;
        if (dto.AvailabilityStatus.HasValue) employee.AvailabilityStatus = dto.AvailabilityStatus.Value;
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) employee.PhoneNumber = dto.PhoneNumber;

        // Save changes
        try
        {
            _userRepository.Update(employee);
            await _userRepository.SaveChangesAsync();
            return Ok(employee); // 200 OK
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while updating the employee.");
        }
    }
    [HttpDelete("{business_id:int}/employees/{employee_id}")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> DeleteEmployee(int business_id, string employee_id)
    {
        // Find the business and ensure it exists
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Find the employee by ID and ensure they belong to the business
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        try
        {
            // Delete the employee
            _userRepository.Remove(employee);
            await _userRepository.SaveChangesAsync();
            return NoContent(); // 204 No Content
        }
        catch (DbUpdateException ex)
        {
            // Handle any database constraint issues
            return StatusCode(500, "An error occurred while deleting the employee.");
        }
    }

    [HttpGet("{business_id:int}/users/{employee_id}/schedules")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> GetSchedules(int business_id, string employee_id)
    {
        // Validate business existence
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Validate user belongs to the business
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        // Retrieve schedules for the employee
        var schedules = await _userWorkTimeRepository.GetSchedulesByEmployeeIdAsync(employee_id);
        if (schedules == null || !schedules.Any())
        {
            return NotFound("No schedules found for this employee.");
        }

        return Ok(schedules); // 200 OK
    }

    [HttpPost("{business_id:int}/users/{employee_id}/schedule")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> AssignSchedule(int business_id, string employee_id, [FromBody] AssignScheduleDto scheduleDto)
    {
        // Validate business existence
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Validate user belongs to the business
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        // Validate input data
        if (scheduleDto.StartTime >= scheduleDto.EndTime)
            return BadRequest("Start time must be before end time.");

        if (scheduleDto.BreakStart >= scheduleDto.BreakEnd)
            return BadRequest("Break start time must be before end time.");

        // Create the schedule
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
            _userWorkTimeRepository.Add(userWorkTime);
            await _userWorkTimeRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(AssignSchedule), new { id = userWorkTime.Id }, userWorkTime); // 201 Created
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the schedule.");
        }
    }
    // Endpoint to update a schedule
    [HttpPut("{business_id:int}/users/{employee_id}/schedule/{schedule_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> UpdateSchedule(int business_id, string employee_id, int schedule_id, [FromForm] AssignScheduleDto scheduleDto)
    {
        // Validate business existence
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Validate user belongs to the business
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        // Validate schedule existence
        var schedule = await _userWorkTimeRepository.GetScheduleByIdAndEmployeeAsync(employee_id, schedule_id);
        if (schedule == null)
        {
            return NotFound("Schedule not found.");
        }

        // Validate input data
        if (scheduleDto.StartTime >= scheduleDto.EndTime)
            return BadRequest("Start time must be before end time.");

        // Update the schedule
        schedule.Day = scheduleDto.Day;
        schedule.StartTime = scheduleDto.StartTime;
        schedule.EndTime = scheduleDto.EndTime;
        schedule.BreakStart = scheduleDto.BreakStart;
        schedule.BreakEnd = scheduleDto.BreakEnd;

        try
        {
            _userWorkTimeRepository.Update(schedule);
            await _userWorkTimeRepository.SaveChangesAsync();

            return Ok(schedule); // 200 OK
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while updating the schedule.");
        }
    }

    // Endpoint to delete a schedule
    [HttpDelete("{business_id:int}/users/{employee_id}/schedule/{schedule_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> DeleteSchedule(int business_id, string employee_id, int schedule_id)
    {
        // Validate business existence
        var business = await _businessRepository.GetByIdAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Validate user belongs to the business
        var employee = await _userRepository.GetEmployeeByIdAndBusinessAsync(employee_id, business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        // Validate schedule existence
        var schedule = await _userWorkTimeRepository.GetScheduleByIdAndEmployeeAsync(employee_id, schedule_id);
        if (schedule == null)
        {
            return NotFound("Schedule not found.");
        }

        try
        {
            _userWorkTimeRepository.Remove(schedule);
            await _userWorkTimeRepository.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while deleting the schedule.");
        }
    }
}

