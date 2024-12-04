using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Services;

public interface ITaxService
{
    public TaxLocale? GetTaxLocaleByCountry(string countryIsoCode);

    public string? GetCurrencyByCountry(string countryIsoCode);

    public List<Tax>? GetTaxesByCountry(string countryIsoCode);
}