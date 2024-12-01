using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoS_Placeholder.Server.Models;

public class ProductVariation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }
    
    [Required]
    [Url]
    public string PictureUrl { get; set; }
    
    [Required]
    public int ProductId { get; set; }
    
    [ForeignKey("ProductId")]
    public Product Product { get; set; }
}