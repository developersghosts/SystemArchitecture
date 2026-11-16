using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Settings;

public sealed class CurrencySettings : ValueObject
{
    /// <summary>ISO 4217 code, e.g. USD, EGP, SAR.</summary>
    public string Code { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public int DecimalPlaces { get; private set; }

    private CurrencySettings() { }

    public static CurrencySettings Create(string code, string symbol, int decimalPlaces)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(decimalPlaces, 4);

        return new CurrencySettings
        {
            Code = code.Trim().ToUpperInvariant(),
            Symbol = symbol.Trim(),
            DecimalPlaces = decimalPlaces
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Symbol;
        yield return DecimalPlaces;
    }
}
