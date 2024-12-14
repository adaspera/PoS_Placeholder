using PoS_Placeholder.Server.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class UpdateEmployeeDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public AvailabilityStatus? AvailabilityStatus { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}
