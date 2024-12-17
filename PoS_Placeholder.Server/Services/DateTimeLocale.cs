using System.Text.Json;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Services;

public class DateTimeService : IDateTimeService
{
    private readonly Dictionary<string, DateLocale> _dateTimeData;

    public DateTimeService(string filePath)
    {
        var jsonData = File.ReadAllText(filePath);
        _dateTimeData = JsonSerializer.Deserialize<Dictionary<string, DateLocale>>(jsonData)
                   ?? new Dictionary<string, DateLocale>();
    }

    public DateLocale? GetTaxLocaleByCountry(string countryIsoCode)
    {
        if (_dateTimeData.TryGetValue(countryIsoCode.ToUpperInvariant(), out var countryDateInfo))
        {
            return countryDateInfo;
        }

        return null;
    }

    public string? GetDateFormatByISO(string isoCode)
    {
        return GetTaxLocaleByCountry(isoCode)?.Format;
    }

    public string? GetDisplayDateFormatByISO(string isoCode)
    {
        return GetTaxLocaleByCountry(isoCode)?.DisplayDateFormat;
    }

    public string? GetDisplayTimeFormatByISO(string isoCode)
    {
        return GetTaxLocaleByCountry(isoCode)?.DisplayTimeFormat;
    }

    public string? GetDisplayFullDateFormatByISO(string isoCode)
    {
        return GetTaxLocaleByCountry(isoCode)?.DisplayFullDateFormat;
    }
}