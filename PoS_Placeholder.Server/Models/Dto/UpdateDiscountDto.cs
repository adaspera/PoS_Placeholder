using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoS_Placeholder.Server.Models.Dto;

public class UpdateDiscountDto
{
    [Key]
    public int Id { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal? Amount { get; set; }
    
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public bool IsPercentage { get; set; }
}