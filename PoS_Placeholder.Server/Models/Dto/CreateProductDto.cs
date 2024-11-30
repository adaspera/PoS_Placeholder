using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class CreateProductDto
{
    [Required]
    [MaxLength(255)]
    public string ProductName { get; set; }

    [Required]
    [MaxLength(255)]
    public string VariationName { get; set; }

    [Required]
    [MaxLength(255)]
    public string ItemGroup { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public IFormFile PictureFile { get; set; }
}