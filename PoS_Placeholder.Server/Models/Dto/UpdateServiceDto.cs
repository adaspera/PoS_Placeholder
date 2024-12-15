using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class UpdateServiceDto
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ServiceCharge { get; set; }

    [Required]
    public bool IsPercentage { get; set; }

    [Required]
    public uint Duration { get; set; }

    [Required]
    public string UserId { get; set; }
}