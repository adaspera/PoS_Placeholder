using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class UpdateServiceDto
{
    [MaxLength(255)]
    [DisplayName("Name of the service")]
    public string? Name { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    [DisplayName("Service charge")]
    public decimal? ServiceCharge { get; set; }

    [DisplayName("Is the service charge a percentage?")]
    public bool? IsPercentage { get; set; }

    [DisplayName("Duration of the service in minutes")]
    public uint? Duration { get; set; }
    
    public string? UserId { get; set; }
}