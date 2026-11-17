using ServicesA.Application.Features.Identity.Common;
using ServicesA.Domain.Entities.Settings;

namespace ServicesA.Application.Features.Settings.Email;

/// <summary>Email settings as shown in Settings > Email. The password is never returned, only HasPassword.</summary>
/// <param name="Source">"database" (saved in Settings > Email) or "configuration" (fallback: appsettings / docker compose).</param>
/// <param name="Mode">"smtp", "pickup" (files, development) or "disabled".</param>
public sealed record EmailServerSettingsDto(
    string Source,
    string Mode,
    string? SmtpServer,
    int Port,
    string Security,
    string? Username,
    bool HasPassword,
    string? SenderEmail,
    string? SenderName,
    string? ResetPasswordUrl,
    string? PickupDirectory,
    DateTime? ModifiedAtUtc);

public sealed record GetEmailServerSettingsQuery : IQuery<EmailServerSettingsDto>;

public sealed class GetEmailServerSettingsHandler(
    IEmailSettingsProvider provider,
    IAppDbContext db,
    EmailSenderNameProvider senderName) : IQueryHandler<GetEmailServerSettingsQuery, EmailServerSettingsDto>
{
    public async Task<EmailServerSettingsDto> Handle(GetEmailServerSettingsQuery request, CancellationToken cancellationToken)
    {
        var s = await provider.GetAsync(cancellationToken);
        var row = await db.EmailServerSettings.AsNoTracking().OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);

        var mode = !string.IsNullOrWhiteSpace(s.SmtpServer) ? "smtp"
            : !string.IsNullOrWhiteSpace(s.PickupDirectory) ? "pickup"
            : "disabled";

        return new EmailServerSettingsDto(s.Source, mode, s.SmtpServer, s.Port, s.Security, s.Username,
            HasPassword: !string.IsNullOrEmpty(s.Password) || (row?.HasPassword ?? false),
            s.SenderEmail, await senderName.GetAsync(cancellationToken), s.ResetPasswordUrl, s.PickupDirectory,
            row?.ModifiedAtUtc ?? row?.CreatedAtUtc);
    }
}

/// <summary>
/// Saves the SMTP settings in the database. Password: null/empty keeps the saved one, RemovePassword deletes it.
/// An empty SmtpServer means "use the configuration" (Mailpit / files in development).
/// </summary>
public sealed record UpdateEmailServerSettingsCommand(
    string? SmtpServer,
    int Port,
    string Security,
    string? Username,
    string? Password,
    bool RemovePassword,
    string? SenderEmail,
    string? ResetPasswordUrl) : ICommand<EmailServerSettingsDto>;

public sealed class UpdateEmailServerSettingsValidator : AbstractValidator<UpdateEmailServerSettingsCommand>
{
    public UpdateEmailServerSettingsValidator()
    {
        RuleFor(x => x.SmtpServer).MaximumLength(255).Matches(@"^[A-Za-z0-9.-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.SmtpServer))
            .WithMessage("'SMTP server' must be a host name, e.g. smtp.example.com.");
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Security).Must(s => EmailServerSettings.SecurityModes.Contains(s))
            .WithMessage("'Security' must be Auto, SslOnConnect, StartTls or None.");
        RuleFor(x => x.Username).MaximumLength(256);
        RuleFor(x => x.Password).MaximumLength(256);
        RuleFor(x => x.SenderEmail).NotEmpty().EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.SmtpServer));
        RuleFor(x => x.ResetPasswordUrl).MaximumLength(500)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .When(x => !string.IsNullOrWhiteSpace(x.ResetPasswordUrl))
            .WithMessage("'Reset password page' must be an http(s) URL.");
    }
}

public sealed class UpdateEmailServerSettingsHandler(
    IAppDbContext db,
    ISecretProtector protector,
    IEmailSettingsProvider provider,
    ISender sender,
    IStringLocalizerService localizer) : ICommandHandler<UpdateEmailServerSettingsCommand, EmailServerSettingsDto>
{
    public async Task<EmailServerSettingsDto> Handle(UpdateEmailServerSettingsCommand request, CancellationToken cancellationToken)
    {
        var row = await db.EmailServerSettings.OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            row = EmailServerSettings.Create();
            db.EmailServerSettings.Add(row);
        }

        var newPassword = string.IsNullOrEmpty(request.Password) ? null : request.Password;

        // The saved password must not be sent to a different server/user without typing it again.
        var serverChanged = !string.Equals(row.SmtpServer, request.SmtpServer?.Trim(), StringComparison.OrdinalIgnoreCase);
        var userChanged = !string.Equals(row.Username, string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim(),
            StringComparison.OrdinalIgnoreCase);
        if (row.HasPassword && newPassword is null && !request.RemovePassword && (serverChanged || userChanged))
            throw new BadRequestException(localizer.Get(ResourceKeys.SmtpPasswordRequired), "SMTP_PASSWORD_REQUIRED");

        row.Update(request.SmtpServer, request.Port, request.Security, request.Username, request.SenderEmail,
            request.ResetPasswordUrl);

        if (request.RemovePassword) row.SetProtectedPassword(null);
        else if (newPassword is not null) row.SetProtectedPassword(protector.Protect(newPassword));

        await db.SaveChangesAsync(cancellationToken);
        provider.Invalidate(); // next email uses the new settings

        return await sender.Send(new GetEmailServerSettingsQuery(), cancellationToken);
    }
}

/// <summary>Sends one test email with the current settings and reports the result (does not throw on SMTP errors).</summary>
public sealed record SendTestEmailCommand(string ToEmail) : ICommand<EmailDeliveryResult>;

public sealed class SendTestEmailValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailValidator() => RuleFor(x => x.ToEmail).ValidEmail();
}

public sealed class SendTestEmailHandler(IEmailService email, EmailTemplates templates, EmailBrandingProvider branding,
    IStringLocalizerService localizer, TimeProvider clock) : ICommandHandler<SendTestEmailCommand, EmailDeliveryResult>
{
    public async Task<EmailDeliveryResult> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        // The time in the company's time zone (Cairo / Riyadh), not the server's UTC
        var now = (await branding.GetAsync(cancellationToken)).FormatTime(clock.GetUtcNow());
        var body = await templates.BuildAsync(
            localizer.Get(ResourceKeys.TestEmailTitle),
            localizer.Get(ResourceKeys.TestEmailBody),
            highlight: now);
        return await email.TrySendAsync(request.ToEmail.Trim(), localizer.Get(ResourceKeys.TestEmailTitle), body, cancellationToken);
    }
}
