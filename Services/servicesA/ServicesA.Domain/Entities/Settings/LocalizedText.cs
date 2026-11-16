using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Settings;

/// <summary>
/// One entry of the message dictionary: the text of a message key (e.g. "OTP_Sent") in one culture.
/// Cultures: "en" English, "ar-EG" Egyptian Arabic, "ar-SA" Saudi Arabic.
/// The system picks the Arabic dialect from the company's country (Project configuration).
/// </summary>
public sealed class LocalizedText : Entity<Guid>, IAuditableEntity
{
    public const string English = "en";
    public const string Egyptian = "ar-EG";
    public const string Saudi = "ar-SA";
    public static readonly string[] Cultures = [English, Egyptian, Saudi];
    public const int MaxLength = 1000;

    public string Key { get; private set; } = string.Empty;
    public string Culture { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }

    private LocalizedText() { }

    public static LocalizedText Create(string key, string culture, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!Cultures.Contains(culture)) throw new ArgumentException("Unsupported culture.", nameof(culture));
        var text = new LocalizedText { Id = Guid.CreateVersion7(), Key = key.Trim(), Culture = culture };
        text.ChangeValue(value);
        return text;
    }

    public void ChangeValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value.Trim();
    }

    public void SetCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    public void SetModified(DateTime utcNow) => ModifiedAtUtc = utcNow;
}
