using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Settings;

public sealed class CompanyDetails : ValueObject
{
    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? Website { get; private set; }
    public string? TaxNumber { get; private set; }

    /// <summary>An https URL or a data:image (png/jpeg/webp/gif) URL.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>
    /// ISO country code. Decides the Arabic dialect of the system messages: "EG" Egyptian, "SA" Saudi,
    /// empty = standard Arabic.
    /// </summary>
    public string? Country { get; private set; }

    public static readonly string[] SupportedCountries = ["EG", "SA"];

    private CompanyDetails() { }

    public static CompanyDetails Create(string nameEn, string nameAr, string? email, string? phone, string? address,
        string? website, string? taxNumber, string? logoUrl, string? country = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameEn);
        ArgumentException.ThrowIfNullOrWhiteSpace(nameAr);
        var countryCode = Clean(country)?.ToUpperInvariant();
        if (countryCode is not null && !SupportedCountries.Contains(countryCode))
            throw new ArgumentException("Unsupported country.", nameof(country));

        return new CompanyDetails
        {
            NameEn = nameEn.Trim(),
            NameAr = nameAr.Trim(),
            Email = Clean(email),
            Phone = Clean(phone),
            Address = Clean(address),
            Website = Clean(website),
            TaxNumber = Clean(taxNumber),
            LogoUrl = Clean(logoUrl),
            Country = countryCode
        };
    }

    /// <summary>Culture of the Arabic messages for this company: "ar-EG", "ar-SA", or null for standard Arabic.</summary>
    public string? ArabicDialect => Country switch
    {
        "EG" => "ar-EG",
        "SA" => "ar-SA",
        _ => null
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NameEn;
        yield return NameAr;
        yield return Email;
        yield return Phone;
        yield return Address;
        yield return Website;
        yield return TaxNumber;
        yield return LogoUrl;
        yield return Country;
    }
}
