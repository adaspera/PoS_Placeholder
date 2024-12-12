using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoS_Placeholder.Server.Models.Dto;

public class CreateOrderDto
{
    public decimal? Tip { get; set; }
    
    [Required]
    [MinLength(1)]
    public List<OrderItemDto> OrderItems { get; set; }
    
    public string? PaymentIntentId { get; set; }
}