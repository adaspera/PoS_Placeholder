using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class CreateAppointmentDto
{
    [Required]
    [DisplayName("Time of reservation (yyyyMMdd-HHmmss)")]
    public string TimeReserved { get; set; }

    [Required]
    [MaxLength(255)]
    [DisplayName("Name of the customer making the reservation")]
    public string CustomerName { get; set; }

    [Required]
    [MaxLength(30)]
    [DisplayName("Customer phone number")]
    public string CustomerPhone { get; set; }

    [Required]
    [DisplayName("ID of the service for which the appointment is made")]
    public int ServiceId { get; set; }
}