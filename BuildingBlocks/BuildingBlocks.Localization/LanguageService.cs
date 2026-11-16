using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Localization;

/// <summary>Reads the language from the Accept-Language header (e.g. "ar-EG,ar;q=0.9" becomes "ar").</summary>
public class LanguageService(IHttpContextAccessor httpContextAccessor) : ILanguageService
{
    public string CurrentLanguage
    {
        get
        {
            var header = httpContextAccessor.HttpContext?.Request.Headers[LocalizationConstants.HeaderLanguage].ToString();
            if (string.IsNullOrWhiteSpace(header))
                return LocalizationConstants.DefaultLanguage;

            var language = header.Split(',')[0].Split(';')[0].Split('-')[0].Trim().ToLowerInvariant();

            return LocalizationConstants.SupportedLanguages.Contains(language)
                ? language
                : LocalizationConstants.DefaultLanguage;
        }
    }
}
