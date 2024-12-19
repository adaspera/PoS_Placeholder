namespace PoS_Placeholder.Server.Models.Dto;

public class CreateSplitOrderDto
{
    public decimal? Tip { get; set; }
    public List<OrderItemDto> OrderItems { get; set; }
    public List<int> OrderServiceIds { get; set; }
    public List<PartialPaymentDto> Payments { get; set; } // {Method, PaidPrice, PaymentIntentId?, GiftCardId?}
}