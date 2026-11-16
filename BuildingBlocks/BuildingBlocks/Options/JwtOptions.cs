namespace BuildingBlocks.Options;

/// <summary>JWT settings. Bind from the "JWT" configuration section.</summary>
public class JwtOptions
{
    public const string SectionName = "JWT";

    public string? Key { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }

    /// <summary>Session/token length in minutes (e.g. 120 = 2 hours). Takes priority over DurationInDays.</summary>
    public double DurationInMinutes { get; set; }

    /// <summary>Older setting, used only when DurationInMinutes is not set.</summary>
    public double DurationInDays { get; set; }

    public TimeSpan Lifetime =>
        DurationInMinutes > 0 ? TimeSpan.FromMinutes(DurationInMinutes) : TimeSpan.FromDays(DurationInDays);
}
