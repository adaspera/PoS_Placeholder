using System.ComponentModel.DataAnnotations;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Models.Dto;

public class PartialPaymentDto
{
    public string? PaymentIntentId { get; set; }
    public string? GiftCardId { get; set; }
    [Required]
    public PaymentMethod Method { get; set; }
    [Required]
    public decimal PaidPrice { get; set; }
}