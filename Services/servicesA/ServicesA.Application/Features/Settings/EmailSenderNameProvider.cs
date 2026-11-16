namespace ServicesA.Application.Features.Settings;

/// <summary>
/// The name shown as the email sender ("From"): the company name from Project configuration,
/// in the language of the current request (Arabic name for "ar", English name otherwise).
/// Falls back to EmailSettings:SenderName when no company name is saved.
/// </summary>
public sealed class EmailSenderNameProvider(EmailBrandingProvider branding, IOptions<EmailSettings> options)
{
    public async Task<string> GetAsync(CancellationToken cancellationToken = default)
    {
        var name = (await branding.GetAsync(cancellationToken)).CompanyName;
        return string.IsNullOrWhiteSpace(name) ? options.Value.SenderName : name;
    }
}
