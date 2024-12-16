using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class Appointment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public DateTime TimeCreated { get; set; }

    [Required]
    public DateTime TimeReserved { get; set; }

    [Required]
    [MaxLength(255)]
    public string CustomerName { get; set; }

    [Required]
    [MaxLength(30)]
    public string CustomerPhone { get; set; }

    [Required]
    public int BusinessId { get; set; } // added for convenience, not in line with spec

    [Required]
    [ForeignKey("BusinessId")]
    public Business Business { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public int ServiceId { get; set; }

    [Required]
    [ForeignKey("ServiceId")]
    public Service Service { get; set; }

}