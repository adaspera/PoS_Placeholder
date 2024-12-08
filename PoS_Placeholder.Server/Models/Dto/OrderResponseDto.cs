namespace PoS_Placeholder.Server.Models.Dto;

public class OrderResponseDto
{
    public int Id { get; set; }
    public decimal? Tip { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
    public decimal TotalPrice { get; set; }
    public List<OrderProductDto> Products { get; set; }
}