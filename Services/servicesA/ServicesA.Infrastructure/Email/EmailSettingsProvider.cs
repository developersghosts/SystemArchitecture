using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicesA.Application.Abstractions;

namespace ServicesA.Infrastructure.Email;

/// <summary>Database settings first, configuration as fallback; cached for 10 minutes (cleared on save).</summary>
public sealed class EmailSettingsProvider(
    IAppDbContext db,
    ISecretProtector protector,
    IMemoryCache cache,
    IOptions<EmailSettings> options,
    ILogger<EmailSettingsProvider> logger) : IEmailSettingsProvider
{
    private const string CacheKey = "email-settings";

    public async Task<EffectiveEmailSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out EffectiveEmailSettings? cached) && cached is not null)
            return cached;

        var settings = await LoadAsync(cancellationToken);
        cache.Set(CacheKey, settings, TimeSpan.FromMinutes(10));
        return settings;
    }

    public void Invalidate() => cache.Remove(CacheKey);

    private async Task<EffectiveEmailSettings> LoadAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        try
        {
            var row = await db.EmailServerSettings.AsNoTracking()
                .OrderBy(s => s.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (row is not null && !string.IsNullOrWhiteSpace(row.SmtpServer))
            {
                string? password = null;
                if (row.ProtectedPassword is not null)
                {
                    password = protector.Unprotect(row.ProtectedPassword);
                    if (password is null)
                        logger.LogError("The saved SMTP password could not be decrypted (Data Protection keys changed?). Save it again in Settings > Email.");
                }

                return new EffectiveEmailSettings("database", row.SmtpServer, row.Port, row.Security, row.Username,
                    password, row.SenderEmail ?? config.SenderEmail, config.SenderName,
                    row.ResetPasswordUrl ?? config.ResetPasswordUrl, PickupDirectory: null);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not read the email settings from the database; using the configuration");
        }

        return new EffectiveEmailSettings("configuration", NullIfEmpty(config.SmtpServer), config.Port, config.Security,
            NullIfEmpty(config.Username), NullIfEmpty(config.Password), NullIfEmpty(config.SenderEmail),
            config.SenderName, NullIfEmpty(config.ResetPasswordUrl), NullIfEmpty(config.PickupDirectory));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
