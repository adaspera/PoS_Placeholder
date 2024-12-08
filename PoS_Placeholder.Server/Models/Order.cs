using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Tip { get; set; }

    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    public OrderStatus Status { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public User User { get; set; }
    
    // Navigation Properties
    public ICollection<ProductArchive> Products { get; set; }
}