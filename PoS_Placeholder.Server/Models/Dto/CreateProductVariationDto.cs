using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class CreateProductVariationDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public IFormFile PictureFile { get; set; }
}