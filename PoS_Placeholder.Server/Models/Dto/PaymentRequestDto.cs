using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class PaymentRequestDto
{
    [Required]
    public decimal TotalAmount { get; set; }
}