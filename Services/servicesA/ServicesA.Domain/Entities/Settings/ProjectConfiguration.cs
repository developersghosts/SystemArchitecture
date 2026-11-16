using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Settings;

/// <summary>
/// App-wide settings used by the UI: company details, currency, colors, fonts and the default language/theme.
/// There is one row; when it does not exist yet, <see cref="CreateDefault"/> is used.
/// </summary>
public sealed class ProjectConfiguration : AggregateRoot<Guid>, IAuditableEntity
{
    public const string LanguageEnglish = "en";
    public const string LanguageArabic = "ar";
    public const string ThemeLight = "light";
    public const string ThemeDark = "dark";

    public CompanyDetails Company { get; private set; } = null!;
    public CurrencySettings Currency { get; private set; } = null!;
    public ThemeColors Colors { get; private set; } = null!;
    public Typography Fonts { get; private set; } = null!;
    public string DefaultLanguage { get; private set; } = LanguageEnglish;
    public string DefaultTheme { get; private set; } = ThemeLight;

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }

    private ProjectConfiguration() { }

    public static ProjectConfiguration CreateDefault() => new()
    {
        Id = Guid.CreateVersion7(),
        Company = CompanyDetails.Create("System Architecture", "System Architecture", null, null, null, null, null, null),
        Currency = CurrencySettings.Create("USD", "$", 2),
        Colors = ThemeColors.Create("#4F46E5", "#F4F6FB", "#0F172A"),
        Fonts = Typography.Create("Inter", "Cairo", 14),
        DefaultLanguage = LanguageEnglish,
        DefaultTheme = ThemeLight
    };

    public void Update(CompanyDetails company, CurrencySettings currency, ThemeColors colors, Typography fonts,
        string defaultLanguage, string defaultTheme)
    {
        if (defaultLanguage is not (LanguageEnglish or LanguageArabic))
            throw new ArgumentException("Unsupported language.", nameof(defaultLanguage));
        if (defaultTheme is not (ThemeLight or ThemeDark))
            throw new ArgumentException("Unsupported theme.", nameof(defaultTheme));

        Company = company;
        Currency = currency;
        Colors = colors;
        Fonts = fonts;
        DefaultLanguage = defaultLanguage;
        DefaultTheme = defaultTheme;
    }

    public void SetCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    public void SetModified(DateTime utcNow) => ModifiedAtUtc = utcNow;
}
