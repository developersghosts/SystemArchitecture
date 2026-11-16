using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Settings;

/// <summary>
/// SMTP settings saved in the database (one row). The password is stored already encrypted
/// (Data Protection, done in Infrastructure): the domain never sees it in clear text.
/// When SmtpServer is empty, the system falls back to the "EmailSettings" configuration section.
/// </summary>
public sealed class EmailServerSettings : AggregateRoot<Guid>, IAuditableEntity
{
    public static readonly string[] SecurityModes = ["Auto", "SslOnConnect", "StartTls", "None"];

    public string? SmtpServer { get; private set; }
    public int Port { get; private set; } = 587;
    public string Security { get; private set; } = "Auto";
    public string? Username { get; private set; }

    /// <summary>Encrypted password (never the clear text).</summary>
    public string? ProtectedPassword { get; private set; }

    public string? SenderEmail { get; private set; }

    /// <summary>UI page that opens the reset link from the email (?email=&amp;token=).</summary>
    public string? ResetPasswordUrl { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }

    public bool HasPassword => !string.IsNullOrEmpty(ProtectedPassword);

    private EmailServerSettings() { }

    public static EmailServerSettings Create() => new() { Id = Guid.CreateVersion7() };

    public void Update(string? smtpServer, int port, string security, string? username, string? senderEmail,
        string? resetPasswordUrl)
    {
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        if (!SecurityModes.Contains(security)) throw new ArgumentException("Unknown security mode.", nameof(security));

        SmtpServer = Clean(smtpServer);
        Port = port;
        Security = security;
        Username = Clean(username);
        SenderEmail = Clean(senderEmail);
        ResetPasswordUrl = Clean(resetPasswordUrl);
    }

    /// <summary>Stores a new encrypted password, or removes it when null.</summary>
    public void SetProtectedPassword(string? protectedPassword) =>
        ProtectedPassword = string.IsNullOrEmpty(protectedPassword) ? null : protectedPassword;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void SetCreated(DateTime utcNow) => CreatedAtUtc = utcNow;

    public void SetModified(DateTime utcNow) => ModifiedAtUtc = utcNow;
}
