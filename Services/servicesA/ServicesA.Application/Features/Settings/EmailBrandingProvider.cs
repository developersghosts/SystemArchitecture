using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Application.Features.Settings;

/// <summary>Everything an email needs from Project configuration, already in the request language.</summary>
public sealed record EmailBranding(
    bool IsArabic,
    string CompanyName,
    string PrimaryColor,
    string OnPrimaryColor,
    string SoftColor,
    string BackgroundColor,
    string FontStack,
    string? LogoSrc,
    EmailInlineImage? InlineLogo,
    string? Email,
    string? Phone,
    string? Address,
    string? Website);

/// <summary>An image sent inside the email (referenced as cid:ContentId), because mail apps block data: URLs.</summary>
public sealed record EmailInlineImage(string ContentId, byte[] Content, string MediaSubtype);

/// <summary>
/// Reads company name, colors, font, logo and contact details from Project configuration (the database),
/// cached in memory for 10 minutes and cleared when the configuration is saved.
/// </summary>
public sealed class EmailBrandingProvider(
    IAppDbContext db,
    IStringLocalizerService localizer,
    IMemoryCache cache,
    ILogger<EmailBrandingProvider> logger)
{
    public const string LogoContentId = "company-logo";
    private const string CacheKey = "email-branding";

    private sealed record Source(ProjectConfiguration Configuration, EmailInlineImage? Logo);

    public async Task<EmailBranding> GetAsync(CancellationToken cancellationToken = default)
    {
        var source = await GetSourceAsync(cancellationToken);
        var c = source.Configuration;
        var arabic = localizer.CurrentLanguage == "ar";

        var primary = c.Colors.Primary;
        var name = (arabic ? c.Company.NameAr : c.Company.NameEn).Trim();
        var font = arabic ? c.Fonts.FontFamilyAr : c.Fonts.FontFamilyEn; // letters/digits/spaces only (validated)
        var fontStack = $"'{font}', {(arabic ? "Tahoma, 'Segoe UI', Arial" : "'Segoe UI', Roboto, Helvetica, Arial")}, sans-serif";

        var logoSrc = source.Logo is not null
            ? $"cid:{source.Logo.ContentId}"
            : IsHttpUrl(c.Company.LogoUrl) ? c.Company.LogoUrl : null;

        return new EmailBranding(arabic, name, primary, ReadableTextColor(primary), Mix(primary, "#FFFFFF", 0.9),
            c.Colors.LightBackground, fontStack, logoSrc, source.Logo, c.Company.Email, c.Company.Phone,
            c.Company.Address, IsHttpUrl(c.Company.Website) ? c.Company.Website : null);
    }

    public void Invalidate() => cache.Remove(CacheKey);

    private async Task<Source> GetSourceAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out Source? cached) && cached is not null)
            return cached;

        ProjectConfiguration? configuration = null;
        try
        {
            configuration = await db.ProjectConfigurations.AsNoTracking()
                .OrderBy(c => c.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not read Project configuration for the email design; using defaults");
        }

        configuration ??= ProjectConfiguration.CreateDefault();
        var source = new Source(configuration, ParseDataUrl(configuration.Company.LogoUrl));
        cache.Set(CacheKey, source, TimeSpan.FromMinutes(10));
        return source;
    }

    /// <summary>data:image/png;base64,... -> bytes, so the logo can travel inside the email.</summary>
    private static EmailInlineImage? ParseDataUrl(string? url)
    {
        if (url is null || !url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) return null;
        var comma = url.IndexOf(',');
        var semicolon = url.IndexOf(';');
        if (comma < 0 || semicolon < 0 || semicolon > comma) return null;
        try
        {
            var subtype = url["data:image/".Length..semicolon].ToLowerInvariant();
            return new EmailInlineImage(LogoContentId, Convert.FromBase64String(url[(comma + 1)..]), subtype);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    /// <summary>Black or white text, whichever reads better on the color.</summary>
    private static string ReadableTextColor(string hex)
    {
        var (r, g, b) = Rgb(hex);
        static double L(int c) { var v = c / 255.0; return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
        return 0.2126 * L(r) + 0.7152 * L(g) + 0.0722 * L(b) > 0.4 ? "#0F172A" : "#FFFFFF";
    }

    /// <summary>Blends two #RRGGBB colors; weight = share of the second color.</summary>
    private static string Mix(string hex, string with, double weight)
    {
        var (r1, g1, b1) = Rgb(hex);
        var (r2, g2, b2) = Rgb(with);
        int M(int a, int b) => (int)Math.Round(a + (b - a) * weight);
        return $"#{M(r1, r2):X2}{M(g1, g2):X2}{M(b1, b2):X2}";
    }

    private static (int R, int G, int B) Rgb(string hex) => (
        int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber),
        int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber),
        int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber));
}
