namespace ServicesA.Application.Abstractions;

public interface IEmailService
{
    /// <summary>Sends an email; failures are logged and never thrown (used by the Identity flows).</summary>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>Sends once and reports what happened (used by the "send test email" screen).</summary>
    Task<EmailDeliveryResult> TrySendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

/// <summary>
/// Mode: "smtp" (sent to the SMTP server), "pickup" (saved as a file, development) or "disabled" (nothing configured).
/// Detail: the file name, or the SMTP error message when Delivered is false.
/// </summary>
public sealed record EmailDeliveryResult(bool Delivered, string Mode, string? Detail);
