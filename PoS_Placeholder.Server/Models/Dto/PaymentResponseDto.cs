namespace PoS_Placeholder.Server.Models.Dto;

public class PaymentResponseDto
{
    public string ClientSecret { get; set; }
    public string PaymentIntentId { get; set; }
}