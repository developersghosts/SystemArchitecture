using System.Net;
using ServicesA.Application.Features.Settings;

namespace ServicesA.Application.Features.Identity.Common;

/// <summary>
/// Builds every system email with the company's design from Project configuration: main color, background,
/// font, logo, company name (Arabic/English per request language) and contact details.
/// Table-based HTML with inline styles so it looks the same in Gmail, Outlook and phone apps.
/// All dynamic values are HTML-encoded.
/// </summary>
public sealed class EmailTemplates(EmailBrandingProvider brandingProvider, IStringLocalizerService localizer)
{
    public async Task<string> BuildAsync(string title, string paragraph,
        string? highlight = null, string? buttonText = null, string? buttonUrl = null)
    {
        var b = await brandingProvider.GetAsync();
        var dir = b.IsArabic ? "rtl" : "ltr";
        var align = b.IsArabic ? "right" : "left";
        var font = b.FontStack;

        // Header: logo, or the company initials in a colored circle
        var logo = b.LogoSrc is not null
            ? $"""<img src="{E(b.LogoSrc)}" alt="{E(b.CompanyName)}" height="56" style="display:block;margin:0 auto;height:56px;max-width:200px;border:0;outline:none;text-decoration:none">"""
            : $"""<div style="width:56px;height:56px;line-height:56px;margin:0 auto;border-radius:50%;background:{b.PrimaryColor};color:{b.OnPrimaryColor};font:700 22px {font};text-align:center">{E(Initials(b.CompanyName))}</div>""";

        // Highlight: a code (digits) is shown big and spaced; anything else as a soft pill
        var highlightHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(highlight))
        {
            var isCode = highlight.All(char.IsDigit);
            highlightHtml = $"""
                <table role="presentation" align="center" cellpadding="0" cellspacing="0" style="margin:24px auto">
                  <tr><td dir="ltr" style="background:{b.SoftColor};border:1px dashed {b.PrimaryColor};border-radius:12px;padding:{(isCode ? "16px 30px" : "10px 22px")};font-family:{(isCode ? "'Courier New', Consolas, monospace" : font)};font-size:{(isCode ? "30px" : "17px")};font-weight:700;letter-spacing:{(isCode ? "8px" : "0")};color:{b.PrimaryColor};text-align:center">{E(highlight)}</td></tr>
                </table>
                """;
        }

        // Button that works in Outlook too, plus the plain link under it
        var buttonHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(buttonText) && !string.IsNullOrWhiteSpace(buttonUrl))
        {
            buttonHtml = $"""
                <table role="presentation" align="center" cellpadding="0" cellspacing="0" style="margin:28px auto 12px">
                  <tr><td bgcolor="{b.PrimaryColor}" style="border-radius:10px;background:{b.PrimaryColor}">
                    <a href="{E(buttonUrl)}" target="_blank" style="display:inline-block;padding:14px 34px;font:700 15px {font};color:{b.OnPrimaryColor};text-decoration:none;border-radius:10px">{E(buttonText)}</a>
                  </td></tr>
                </table>
                <p style="margin:12px 0 0;font-size:12px;color:#64748B;text-align:center">{E(localizer.Get(ResourceKeys.EmailButtonFallback))}<br>
                  <a href="{E(buttonUrl)}" style="color:{b.PrimaryColor};word-break:break-all">{E(buttonUrl)}</a></p>
                """;
        }

        // Footer: company and contact details (only what is filled in)
        var contacts = new List<string>();
        if (!string.IsNullOrWhiteSpace(b.Phone))
            contacts.Add($"""<span dir="ltr">{E(b.Phone)}</span>""");
        if (!string.IsNullOrWhiteSpace(b.Email))
            contacts.Add($"""<a href="mailto:{E(b.Email)}" style="color:{b.PrimaryColor};text-decoration:none">{E(b.Email)}</a>""");
        if (b.Website is not null)
            contacts.Add($"""<a href="{E(b.Website)}" style="color:{b.PrimaryColor};text-decoration:none">{E(new Uri(b.Website).Host)}</a>""");

        var addressHtml = string.IsNullOrWhiteSpace(b.Address) ? string.Empty : $"<div>{E(b.Address)}</div>";
        var contactsHtml = contacts.Count == 0 ? string.Empty : $"<div style=\"margin-top:4px\">{string.Join(" &nbsp;&middot;&nbsp; ", contacts)}</div>";

        return $"""
            <!DOCTYPE html>
            <html lang="{(b.IsArabic ? "ar" : "en")}" dir="{dir}">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="light">
              <meta name="supported-color-schemes" content="light">
              <title>{E(title)}</title>
            </head>
            <body style="margin:0;padding:0;background:{b.BackgroundColor};-webkit-text-size-adjust:100%">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent">{E(paragraph)}</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" dir="ltr" style="background:{b.BackgroundColor}">
                <tr><td align="center" style="padding:32px 12px;text-align:center">
                  <table role="presentation" align="center" width="100%" cellpadding="0" cellspacing="0" dir="{dir}" style="max-width:600px;margin:0 auto;background:#FFFFFF;border:1px solid #E2E8F0;border-radius:16px;overflow:hidden">
                    <tr><td style="height:6px;line-height:6px;font-size:0;background:{b.PrimaryColor}">&nbsp;</td></tr>
                    <tr><td align="center" style="padding:28px 32px 4px">
                      {logo}
                      <div style="margin-top:12px;font:700 17px {font};color:#0F172A">{E(b.CompanyName)}</div>
                    </td></tr>
                    <tr><td style="padding:12px 32px 32px;text-align:{align};font-family:{font};font-size:15px;line-height:1.75;color:#334155">
                      <h1 style="margin:18px 0 10px;font-family:{font};font-size:22px;line-height:1.35;color:#0F172A">{E(title)}</h1>
                      <p style="margin:0">{E(paragraph)}</p>
                      {highlightHtml}
                      {buttonHtml}
                    </td></tr>
                    <tr><td style="padding:20px 32px;background:{b.SoftColor};border-top:1px solid #E2E8F0;text-align:center;font-family:{font};font-size:12px;line-height:1.8;color:#64748B">
                      <div style="font-weight:700;color:#0F172A">{E(b.CompanyName)}</div>
                      {addressHtml}
                      {contactsHtml}
                      <div style="margin-top:10px">&copy; {DateTime.UtcNow.Year} {E(b.CompanyName)} &mdash; {E(localizer.Get(ResourceKeys.AllRightsReserved))}</div>
                      <div style="margin-top:2px;color:#94A3B8">{E(localizer.Get(ResourceKeys.EmailAutomatedNote))}</div>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => char.ToUpperInvariant(w[0])));

    private static string E(string value) => WebUtility.HtmlEncode(value);
}
