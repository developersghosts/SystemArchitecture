using System.Text.RegularExpressions;
using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Settings;

/// <summary>Hex colors (#RRGGBB) for the main color and the page background in light and dark mode.</summary>
public sealed partial class ThemeColors : ValueObject
{
    public string Primary { get; private set; } = string.Empty;
    public string LightBackground { get; private set; } = string.Empty;
    public string DarkBackground { get; private set; } = string.Empty;

    private ThemeColors() { }

    public static ThemeColors Create(string primary, string lightBackground, string darkBackground) => new()
    {
        Primary = Hex(primary, nameof(primary)),
        LightBackground = Hex(lightBackground, nameof(lightBackground)),
        DarkBackground = Hex(darkBackground, nameof(darkBackground))
    };

    public static bool IsHexColor(string? value) => value is not null && HexColor().IsMatch(value);

    private static string Hex(string value, string name) =>
        IsHexColor(value) ? value.ToUpperInvariant() : throw new ArgumentException("Expected a #RRGGBB color.", name);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Primary;
        yield return LightBackground;
        yield return DarkBackground;
    }
}
