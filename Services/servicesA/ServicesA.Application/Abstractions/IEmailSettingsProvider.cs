namespace ServicesA.Application.Abstractions;

/// <summary>
/// The email settings in use: the EmailServerSettings row in the database when it has an SMTP server,
/// otherwise the "EmailSettings" configuration section (Mailpit / files in development).
/// Cached in memory; call <see cref="Invalidate"/> after saving new settings.
/// </summary>
public interface IEmailSettingsProvider
{
    Task<EffectiveEmailSettings> GetAsync(CancellationToken cancellationToken = default);

    void Invalidate();
}

/// <param name="Source">"database" or "configuration".</param>
/// <param name="Password">Clear text, only for sending: never return it from the API or log it.</param>
public sealed record EffectiveEmailSettings(
    string Source,
    string? SmtpServer,
    int Port,
    string Security,
    string? Username,
    string? Password,
    string? SenderEmail,
    string? FallbackSenderName,
    string? ResetPasswordUrl,
    string? PickupDirectory);

/// <summary>Encrypts secrets kept in the database (ASP.NET Data Protection in Infrastructure).</summary>
public interface ISecretProtector
{
    string Protect(string plainText);

    /// <summary>Null when the value can't be decrypted (e.g. the keys were lost).</summary>
    string? Unprotect(string protectedText);
}
