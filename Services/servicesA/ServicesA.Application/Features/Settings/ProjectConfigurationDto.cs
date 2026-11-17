using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Application.Features.Settings;

public sealed record CompanyDetailsDto(
    string NameEn,
    string NameAr,
    string? Email,
    string? Phone,
    string? Address,
    string? Website,
    string? TaxNumber,
    string? LogoUrl,
    string? Country = null);

public sealed record CurrencyDto(string Code, string Symbol, int DecimalPlaces);

public sealed record ColorsDto(string Primary, string LightBackground, string DarkBackground);

public sealed record FontsDto(string FontFamilyEn, string FontFamilyAr, int BaseFontSize);

public sealed record ProjectConfigurationDto(
    CompanyDetailsDto Company,
    CurrencyDto Currency,
    ColorsDto Colors,
    FontsDto Fonts,
    string DefaultLanguage,
    string DefaultTheme,
    DateTime? ModifiedAtUtc,
    string? TimeZone = null);

internal static class ProjectConfigurationMapping
{
    public static ProjectConfigurationDto ToDto(this ProjectConfiguration c) => new(
        new CompanyDetailsDto(c.Company.NameEn, c.Company.NameAr, c.Company.Email, c.Company.Phone,
            c.Company.Address, c.Company.Website, c.Company.TaxNumber, c.Company.LogoUrl, c.Company.Country),
        new CurrencyDto(c.Currency.Code, c.Currency.Symbol, c.Currency.DecimalPlaces),
        new ColorsDto(c.Colors.Primary, c.Colors.LightBackground, c.Colors.DarkBackground),
        new FontsDto(c.Fonts.FontFamilyEn, c.Fonts.FontFamilyAr, c.Fonts.BaseFontSize),
        c.DefaultLanguage,
        c.DefaultTheme,
        c.ModifiedAtUtc,
        c.Company.TimeZoneId);
}
