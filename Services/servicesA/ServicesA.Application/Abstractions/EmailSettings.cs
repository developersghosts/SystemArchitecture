namespace ServicesA.Application.Abstractions;

/// <summary>Bound from the "EmailSettings" section. When SmtpServer is empty, emails are only logged (development).</summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Connection security: "Auto" (default: SSL on port 465, STARTTLS otherwise), "SslOnConnect", "StartTls",
    /// or "None" (plain text, only for a local test server such as Mailpit).
    /// </summary>
    public string Security { get; set; } = "Auto";

    /// <summary>Front-end page that receives ?email=...&amp;token=... for password reset.</summary>
    public string ResetPasswordUrl { get; set; } = string.Empty;

    /// <summary>
    /// Development only: when SmtpServer is empty and this is set, each email is saved as an .html file in this
    /// folder instead of being sent (so OTP codes and reset links can be opened locally). Keep it empty in production.
    /// </summary>
    public string PickupDirectory { get; set; } = string.Empty;
}
