using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Services
{
    public interface IDateTimeService
    {
        string? GetDateFormatByISO(string isoCode);
        string? GetDisplayDateFormatByISO(string isoCode);
        string? GetDisplayFullDateFormatByISO(string isoCode);
        string? GetDisplayTimeFormatByISO(string isoCode);
        DateLocale? GetTaxLocaleByCountry(string countryIsoCode);
    }
}