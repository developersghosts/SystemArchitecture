namespace BuildingBlocks.Localization;

public interface IStringLocalizerService
{
    /// <summary>Language of the current request ("en" or "ar").</summary>
    string CurrentLanguage { get; }

    string Get(string key);
}
