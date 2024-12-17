using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class UpdateGiftcardDto
{
    [Key]
    public string Id { get; set; }
    
    public decimal? BalanceAmount { get; set; }
}