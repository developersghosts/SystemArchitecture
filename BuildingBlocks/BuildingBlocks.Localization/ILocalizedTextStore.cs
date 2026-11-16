namespace BuildingBlocks.Localization;

/// <summary>
/// Optional source of message texts that overrides the .resx files (e.g. a dictionary saved in a database).
/// Implemented by the service; when it is not registered, only the .resx files are used.
/// </summary>
public interface ILocalizedTextStore
{
    /// <summary>Culture used for Arabic messages, e.g. "ar-EG" or "ar-SA"; null = standard Arabic (.resx).</summary>
    string? ArabicDialect { get; }

    /// <summary>The text of a key in a culture ("en", "ar-EG", "ar-SA"), or null when there is none.</summary>
    string? Find(string key, string culture);
}
