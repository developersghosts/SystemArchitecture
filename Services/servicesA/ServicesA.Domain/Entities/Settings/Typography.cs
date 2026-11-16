using System.Text.RegularExpressions;
using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Settings;

/// <summary>Font families (Google Fonts names) for English and Arabic, and the base font size in px.</summary>
public sealed partial class Typography : ValueObject
{
    public const int MinFontSize = 12;
    public const int MaxFontSize = 20;

    public string FontFamilyEn { get; private set; } = string.Empty;
    public string FontFamilyAr { get; private set; } = string.Empty;
    public int BaseFontSize { get; private set; }

    private Typography() { }

    public static Typography Create(string fontFamilyEn, string fontFamilyAr, int baseFontSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseFontSize, MinFontSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(baseFontSize, MaxFontSize);

        return new Typography
        {
            FontFamilyEn = Font(fontFamilyEn, nameof(fontFamilyEn)),
            FontFamilyAr = Font(fontFamilyAr, nameof(fontFamilyAr)),
            BaseFontSize = baseFontSize
        };
    }

    /// <summary>Letters, digits and spaces only, so the name is safe inside CSS and a Google Fonts URL.</summary>
    public static bool IsValidFontName(string? value) => value is not null && FontName().IsMatch(value.Trim());

    private static string Font(string value, string name) =>
        IsValidFontName(value) ? value.Trim() : throw new ArgumentException("Invalid font name.", name);

    [GeneratedRegex("^[A-Za-z0-9 ]{2,60}$")]
    private static partial Regex FontName();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FontFamilyEn;
        yield return FontFamilyAr;
        yield return BaseFontSize;
    }
}
