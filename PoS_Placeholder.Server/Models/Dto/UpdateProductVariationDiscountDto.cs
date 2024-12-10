using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class UpdateProductVariationDiscountDto
{
    [Required] 
    [Key]
    public int ProductVariationId { get; set; }

    // Determines if we are adding 'true' or removing 'false' a discount from productVariation
    [Required] 
    public bool IsAdd { get; set; }
}