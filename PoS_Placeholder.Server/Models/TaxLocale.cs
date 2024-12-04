namespace PoS_Placeholder.Server.Models;

public class TaxLocale
{
    public string Currency { get; set; } = string.Empty;
    public List<Tax> Taxes { get; set; } = new();
}