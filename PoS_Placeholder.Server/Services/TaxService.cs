using System.Text.Json;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Services;

public class TaxService : ITaxService
{
    private readonly Dictionary<string, TaxLocale> _taxData;

    public TaxService(string filePath)
    {
        var jsonData = File.ReadAllText(filePath);
        _taxData = JsonSerializer.Deserialize<Dictionary<string, TaxLocale>>(jsonData)
                   ?? new Dictionary<string, TaxLocale>();
    }
    
    public TaxLocale? GetTaxLocaleByCountry(string countryIsoCode)
    {
        if (_taxData.TryGetValue(countryIsoCode.ToUpperInvariant(), out var countryTaxInfo))
        {
            return countryTaxInfo;
        }

        return null;
    }

    public string? GetCurrencyByCountry(string countryIsoCode)
    {
        return GetTaxLocaleByCountry(countryIsoCode)?.Currency;
    }

    public List<Tax>? GetTaxesByCountry(string countryIsoCode)
    {
        return GetTaxLocaleByCountry(countryIsoCode)?.Taxes;
    }
}