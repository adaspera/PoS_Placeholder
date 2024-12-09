using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoS_Placeholder.Server.Models;

public class Discount
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public bool IsPercentage { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [ForeignKey("BusinessId")]
    public virtual Business Business { get; set; }
}