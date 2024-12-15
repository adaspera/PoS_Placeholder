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
[Route("/api/services")]
public class ServiceController : ControllerBase
{
    private readonly ServiceRepository _serviceRepository;
    private readonly UserManager<User> _userManager;

    public ServiceController(ServiceRepository serviceRepository, UserManager<User> userManager)
    {
        _serviceRepository = serviceRepository;
        _userManager = userManager;
    }

    [HttpGet("all", Name = "GetAllServices")]
    [Authorize]
    public async Task<IActionResult> GetAllServices()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;
        var businessServices = await _serviceRepository.GetServicesByBusinessIdAsync(userBusinessId);

        return Ok(businessServices);
    }

    [HttpGet("{id:int}", Name = "GetServiceById")]
    [Authorize]
    public async Task<IActionResult> GetServiceById(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid service ID.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;

        var service = await _serviceRepository.GetServiceByIdAsync(id, userBusinessId);
        if (service == null)
        {
            return NotFound("Service not found.");
        }

        if (userBusinessId != service.BusinessId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission.");
        }

        return Ok(service);
    }

    [HttpGet("user/{id:int}", Name = "GetServicesByUserId")]
    [Authorize]
    public async Task<IActionResult> GetServicesByUserId(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized("User not found.");
        }

        var userBusinessId = user.BusinessId;

        var services = await _serviceRepository.GetServicesByUserIdAsync(userBusinessId, id);
        if (services == null)
        {
            return NotFound("No services found.");
        }

        return Ok(services);
    }

    [HttpPost("create", Name = "CreateService")]
    [Authorize(Roles = nameof(UserRole.Owner))]
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
                return Unauthorized("User not found.");
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
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;

            var service = await _serviceRepository.GetServiceByIdAsync(id, userBusinessId);
            if (service == null)
            {
                return NotFound("Service not found.");
            }

            if (userBusinessId != service.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to update this service.");
            }

            service.Name = updateServiceDto.Name;
            service.ServiceCharge = updateServiceDto.ServiceCharge;
            service.IsPercentage = updateServiceDto.IsPercentage;
            service.Duration = updateServiceDto.Duration;
            service.UserId = updateServiceDto.UserId;

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
                return Unauthorized("User not found.");
            }

            var userBusinessId = user.BusinessId;

            var service = await _serviceRepository.GetServiceByIdAsync(id, userBusinessId);
            if (service == null)
            {
                return NotFound("Product not found.");
            }

            if (userBusinessId != service.BusinessId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You do not have permission to delete this service.");
            }

            _serviceRepository.Remove(service);
            await _serviceRepository.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}