using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class UpdateAppointmentDto
{
    [Required]
    public DateTime TimeReserved { get; set; }

    [Required]
    [MaxLength(255)]
    public string CustomerName { get; set; }

    [Required]
    [MaxLength(30)]
    public string CustomerPhone { get; set; }
}