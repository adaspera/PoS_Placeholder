using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class UpdateAppointmentDto
{
    [DisplayName("Time of reservation (yyyyMMdd-HHmmss)")]
    public string? TimeReserved { get; set; }

    [MaxLength(255)]
    [DisplayName("Name of the customer making the reservation")]
    public string? CustomerName { get; set; }

    [MaxLength(30)]
    [DisplayName("Customer phone number")]
    public string? CustomerPhone { get; set; }
}