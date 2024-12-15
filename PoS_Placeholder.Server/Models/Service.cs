using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml;

namespace PoS_Placeholder.Server.Models;

public class Service
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
    public bool IsPercentage { get; set; } // default value is 'false'
                                           // allows service charge to be a flat value or a percentage
                                           // percentage can be used to charge for servicing a table in a restaurant

    [Required]
    public uint Duration { get; set; }

    [Required]
    public int BusinessId { get; set; }
    
    [ForeignKey("BusinessId")]
    public Business Business { get; set; }

    [Required]
    public string UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }
}