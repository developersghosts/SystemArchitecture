using System.Globalization;
using BuildingBlocks.Localization.Resources;
using Microsoft.Extensions.Localization;

namespace BuildingBlocks.Localization;

/// <summary>
/// Text of a message key in the request language (Accept-Language).
/// Order: the text store (database dictionary) in the company's Arabic dialect -> the store in the language
/// -> the .resx files. The store is optional.
/// </summary>
public class StringLocalizerService(
    IStringLocalizer<SharedResources> localizer,
    ILanguageService languageService,
    ILocalizedTextStore? store = null) : IStringLocalizerService
{
    public string CurrentLanguage => languageService.CurrentLanguage;

    public string Get(string key)
    {
        var language = languageService.CurrentLanguage;
        if (store is not null)
        {
            if (language == "ar" && store.ArabicDialect is { } dialect && store.Find(key, dialect) is { } inDialect)
                return inDialect;
            if (store.Find(key, language) is { } inLanguage)
                return inLanguage;
        }

        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(language);
            return localizer[key];
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
