using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PoS_Placeholder.Server.Models.Enum;
using PoS_Placeholder.Server.Utilities;

namespace PoS_Placeholder.Server.Models.Dto;

[AtLeastOneRequired] // At least one product(item) or service required
public class CreateOrderDto
{
    public decimal? Tip { get; set; }
    
    public List<OrderItemDto> OrderItems { get; set; }
    public List<int>? OrderServiceIds { get; set; }
    
    public string? PaymentIntentId { get; set; }
    public string? GiftCardId { get; set; }
    public PaymentMethod? Method { get; set; }
}