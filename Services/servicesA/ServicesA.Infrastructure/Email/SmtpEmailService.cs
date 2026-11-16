using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using ServicesA.Application.Abstractions;
using ServicesA.Application.Features.Settings;

namespace ServicesA.Infrastructure.Email;

/// <summary>
/// Sends HTML email over SMTP (MailKit) with up to 3 attempts, using the settings saved in the database
/// (Settings > Email) or, when none are saved, the configuration. Without an SMTP server it saves the email to
/// EmailSettings:PickupDirectory (development) or only logs it. Failures are logged and never break the request.
/// </summary>
public sealed class SmtpEmailService(
    IEmailSettingsProvider settingsProvider,
    EmailSenderNameProvider senderName,
    EmailBrandingProvider branding,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private const int MaxAttempts = 3;

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        await DeliverAsync(toEmail, subject, htmlBody, MaxAttempts, cancellationToken);

    public Task<EmailDeliveryResult> TrySendAsync(string toEmail, string subject, string htmlBody,
        CancellationToken cancellationToken = default) =>
        DeliverAsync(toEmail, subject, htmlBody, attempts: 1, cancellationToken);

    private async Task<EmailDeliveryResult> DeliverAsync(string toEmail, string subject, string htmlBody, int attempts,
        CancellationToken cancellationToken)
    {
        var s = await settingsProvider.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(s.SmtpServer))
        {
            if (!string.IsNullOrWhiteSpace(s.PickupDirectory))
                return await SaveToPickupDirectoryAsync(s.PickupDirectory, toEmail, subject, htmlBody, cancellationToken);

            logger.LogWarning("SMTP is not configured; email '{Subject}' to {Email} was not sent", subject, toEmail);
            return new EmailDeliveryResult(false, "disabled", "No SMTP server is set.");
        }

        if (string.IsNullOrWhiteSpace(s.SenderEmail))
            return new EmailDeliveryResult(false, "smtp", "The sender email is empty.");

        var message = new MimeMessage();
        // Sender name = company name (Arabic or English, per request language) from Project configuration.
        message.From.Add(new MailboxAddress(await senderName.GetAsync(cancellationToken), s.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = await BuildBodyAsync(htmlBody, cancellationToken);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var client = new SmtpClient(); // certificate validation stays on
                await client.ConnectAsync(s.SmtpServer, s.Port, SocketOptions(s), cancellationToken);

                if (!string.IsNullOrEmpty(s.Username))
                    await client.AuthenticateAsync(s.Username, s.Password ?? string.Empty, cancellationToken);

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                logger.LogInformation("Email '{Subject}' sent to {Email}", subject, toEmail);
                return new EmailDeliveryResult(true, "smtp", $"{s.SmtpServer}:{s.Port}");
            }
            catch (Exception ex) when (attempt < attempts && ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Sending email to {Email} failed (attempt {Attempt}), retrying", toEmail, attempt);
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Sending email '{Subject}' to {Email} failed", subject, toEmail);
                return new EmailDeliveryResult(false, "smtp", ex.Message);
            }
        }
    }

    /// <summary>
    /// HTML + a plain-text copy (better delivery, fewer spam flags). The company logo travels inside the email
    /// (cid:company-logo) because Gmail/Outlook block data: images.
    /// </summary>
    private async Task<MimeEntity> BuildBodyAsync(string htmlBody, CancellationToken cancellationToken)
    {
        var builder = new BodyBuilder { HtmlBody = htmlBody, TextBody = HtmlToText(htmlBody) };

        var logo = (await branding.GetAsync(cancellationToken)).InlineLogo;
        if (logo is not null && htmlBody.Contains($"cid:{logo.ContentId}", StringComparison.Ordinal))
        {
            var resource = builder.LinkedResources.Add($"logo.{logo.MediaSubtype}", logo.Content,
                new ContentType("image", logo.MediaSubtype));
            resource.ContentId = logo.ContentId;
        }

        return builder.ToMessageBody();
    }

    /// <summary>Rough text version of the HTML email for mail apps that don't show HTML.</summary>
    private static string HtmlToText(string html)
    {
        var body = System.Text.RegularExpressions.Regex.Replace(html, "<(head|style)[^>]*>.*?</\\1>", " ",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        body = System.Text.RegularExpressions.Regex.Replace(body, "<(br|/p|/div|/h1|/tr)[^>]*>", "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        body = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]+>", " ");
        body = System.Net.WebUtility.HtmlDecode(body);
        var lines = body.Split('\n').Select(l => System.Text.RegularExpressions.Regex.Replace(l, "[ \t\r]+", " ").Trim())
            .Where(l => l.Length > 0);
        return string.Join("\n", lines);
    }

    private static SecureSocketOptions SocketOptions(EffectiveEmailSettings s) => s.Security?.Trim().ToLowerInvariant() switch
    {
        "none" => SecureSocketOptions.None,
        "sslonconnect" => SecureSocketOptions.SslOnConnect,
        "starttls" => SecureSocketOptions.StartTls,
        _ => s.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls
    };

    /// <summary>Development: writes the email to the pickup folder as an .html file (newest last by name).</summary>
    private async Task<EmailDeliveryResult> SaveToPickupDirectoryAsync(string folder, string toEmail, string subject,
        string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(folder);
            var safeTo = string.Concat(toEmail.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));
            var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}_{safeTo}.html";
            var header = $"<!-- To: {System.Net.WebUtility.HtmlEncode(toEmail)} | Subject: {System.Net.WebUtility.HtmlEncode(subject)} -->\n";
            // A saved file can't carry attachments: put the logo back as a data URL so it shows in the browser.
            var logo = (await branding.GetAsync(cancellationToken)).InlineLogo;
            if (logo is not null)
                htmlBody = htmlBody.Replace($"cid:{logo.ContentId}",
                    $"data:image/{logo.MediaSubtype};base64,{Convert.ToBase64String(logo.Content)}", StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(folder, fileName), header + htmlBody, cancellationToken);
            logger.LogWarning("SMTP is not configured; email '{Subject}' to {Email} was saved to {File}",
                subject, toEmail, fileName);
            return new EmailDeliveryResult(true, "pickup", fileName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Saving email '{Subject}' to the pickup directory failed", subject);
            return new EmailDeliveryResult(false, "pickup", ex.Message);
        }
    }
}
