using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class RegisterBusinessDto
{
    // User Properties 

    [Required]
    [MaxLength(255)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(255)]
    public string LastName { get; set; }

    [Required]
    [Phone]
    [MaxLength(30)]
    public string PhoneNumber { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; }

    // Business Properties

    [Required]
    [MaxLength(255)]
    public string BusinessName { get; set; }

    [Required]
    [Phone]
    [MaxLength(30)]
    public string BusinessPhone { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string BusinessEmail { get; set; }

    [Required]
    [MaxLength(255)]
    public string BusinessStreet { get; set; }

    [Required]
    [MaxLength(255)]
    public string BusinessCity { get; set; }

    [Required]
    [MaxLength(255)]
    public string BusinessRegion { get; set; }

    [Required]
    [MaxLength(255)]
    public string BusinessCountry { get; set; }
}