namespace PoS_Placeholder.Server.Models;

public class Tax
{
    public string NameOfTax { get; set; } = string.Empty;
    public decimal TaxAmount { get; set; }
    public bool IsPercentage { get; set; }
}