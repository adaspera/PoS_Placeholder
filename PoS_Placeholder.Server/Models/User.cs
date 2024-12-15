using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Models;

public class User : IdentityUser
{
    // is not up to specification
    // refer to pdf pg. 19 User table


    [Required]
    [MaxLength(255)]
    public string FirstName { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string LastName { get; set; }


    [Required]
    public AvailabilityStatus AvailabilityStatus { get; set; }
    
    [Required]
    public int BusinessId { get; set; }
    
    [ForeignKey("BusinessId")]
    public Business Business { get; set; }
    
    // Navigation Properties
    public ICollection<Order> Orders { get; set; }

    public ICollection<Appointment>? Appointments { get; set; }

    public ICollection<Service>? Services {  get; set; } // changed to a user (employee) can have multiple services to offer
                                                         // hairdresser can do haircut, hairstyle, beard trim, hair dying, etc.
}