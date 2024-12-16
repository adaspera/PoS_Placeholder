using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class Appointment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string TimeCreated { get; set; }
    public string? TimeUpdated { get; set; }

    [Required]
    public string TimeReserved { get; set; }

    [Required]
    [MaxLength(255)]
    public string CustomerName { get; set; }

    [Required]
    [MaxLength(30)]
    public string CustomerPhone { get; set; }

    [Required]
    public int BusinessId { get; set; } // added for convenience, not in line with spec

    [Required]
    [JsonIgnore]
    [ForeignKey("BusinessId")]
    public Business Business { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    [JsonIgnore]
    [ForeignKey("UserId")]
    public User User { get; set; }

    [Required]
    public int ServiceId { get; set; }

    [Required]
    [JsonIgnore]
    [ForeignKey("ServiceId")]
    public Service Service { get; set; }

}