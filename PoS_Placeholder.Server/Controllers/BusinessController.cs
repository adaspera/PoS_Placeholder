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
    private readonly ApplicationDbContext _db;
    public BusinessController(ApplicationDbContext context)
    {
        _db = context;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SuperAdmin))] // Restrict to Super Admin
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessDto dto)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState); // 422 Validation exception
        }

        // Check for uniqueness
        if (_db.Businesses.Any(b => b.Phone == dto.Phone || b.Email == dto.Email))
        {
            return Conflict("Phone/email already in use."); // 409 Conflict
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
            _db.Businesses.Add(business);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateBusiness), new { id = business.Id }, business); // 201 Created
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the business.");
        }
    }

    [HttpPut("{business_id:int}")]
    [Authorize(Roles = nameof(UserRole.Owner))] // Restrict to Business Owners
    public async Task<IActionResult> UpdateBusiness(int business_id, [FromBody] UpdateBusinessDto dto)
    {
        // Find the business by ID
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Could not find the business.");
        }

        // Update fields only if provided
        if (!string.IsNullOrWhiteSpace(dto.Name)) business.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) business.Phone = dto.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Email)) business.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Street)) business.Street = dto.Street;
        if (!string.IsNullOrWhiteSpace(dto.City)) business.City = dto.City;
        if (!string.IsNullOrWhiteSpace(dto.Region)) business.Region = dto.Region;
        if (!string.IsNullOrWhiteSpace(dto.Country)) business.Country = dto.Country;

        // Check for uniqueness constraints
        if (_db.Businesses.Any(b => (b.Phone == business.Phone || b.Email == business.Email) && b.Id != business_id))
        {
            return Conflict("Phone/email already exists.");
        }

        // Save changes
        try
        {
            await _db.SaveChangesAsync();
            return Ok(business);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "An error occurred while updating the business.");
        }
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
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Find the employee by ID and ensure they belong to the business
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employee_id && u.BusinessId == business_id);
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
            _db.Users.Update(employee);
            await _db.SaveChangesAsync();
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
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Find the employee by ID and ensure they belong to the business
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employee_id && u.BusinessId == business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        try
        {
            // Delete the employee
            _db.Users.Remove(employee);
            await _db.SaveChangesAsync();
            return NoContent(); // 204 No Content
        }
        catch (DbUpdateException ex)
        {
            // Handle any database constraint issues
            return StatusCode(500, "An error occurred while deleting the employee.");
        }
    }
  
    [HttpPost("{business_id:int}/users/{employee_id}/schedule")]
    [Authorize(Roles = nameof(UserRole.Owner))]
    public async Task<IActionResult> AssignSchedule(int business_id, string employee_id, [FromForm] AssignScheduleDto scheduleDto)
    {
        // Validate business existence
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Validate user belongs to the business
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employee_id && u.BusinessId == business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        // Validate input data
        if (scheduleDto.StartTime >= scheduleDto.EndTime)
            return BadRequest("Start time must be before end time.");

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
            _db.UserWorkTimes.Add(userWorkTime);
            await _db.SaveChangesAsync();

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
        // Validate schedule existence
        var schedule = await _db.UserWorkTimes.FirstOrDefaultAsync(s => s.Id == schedule_id && s.UserId == employee_id);
        if (schedule == null)
        {
            return NotFound("Schedule not found.");
        }

        // Validate business existence
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Validate user belongs to the business
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employee_id && u.BusinessId == business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
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
            _db.UserWorkTimes.Update(schedule);
            await _db.SaveChangesAsync();

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
        // Validate schedule existence
        var schedule = await _db.UserWorkTimes.FirstOrDefaultAsync(s => s.Id == schedule_id && s.UserId == employee_id);
        if (schedule == null)
        {
            return NotFound("Schedule not found.");
        }

        // Validate business existence
        var business = await _db.Businesses.FindAsync(business_id);
        if (business == null)
        {
            return NotFound("Business not found.");
        }

        // Validate user belongs to the business
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employee_id && u.BusinessId == business_id);
        if (employee == null)
        {
            return NotFound("Employee not found or does not belong to the business.");
        }

        try
        {
            _db.UserWorkTimes.Remove(schedule);
            await _db.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while deleting the schedule.");
        }
    }

}

