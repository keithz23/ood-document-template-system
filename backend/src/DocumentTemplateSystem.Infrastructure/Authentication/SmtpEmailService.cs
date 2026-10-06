using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using DocumentTemplateSystem.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DocumentTemplateSystem.Infrastructure.Authentication;

public sealed class SmtpEmailService(IOptions<EmailOptions> options) : IEmailService
{
    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        EnsureConfigured(settings);

        var resetUrl = $"{settings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var safeResetUrl = HtmlEncoder.Default.Encode(resetUrl);
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = "Reset your password",
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            BuildPlainTextBody(resetUrl),
            System.Text.Encoding.UTF8,
            "text/plain"));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            BuildHtmlBody(settings.FromName, safeResetUrl),
            System.Text.Encoding.UTF8,
            "text/html"));
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpAppPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static string BuildHtmlBody(string productName, string safeResetUrl)
    {
        var safeProductName = HtmlEncoder.Default.Encode(productName);
        return $"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <meta name="color-scheme" content="light">
          <title>Reset your password</title>
        </head>
        <body
          style="margin:0;padding:0;background-color:#f3f5f9;font-family:Arial,Helvetica,sans-serif;color:#20283a;">
          <table
            role="presentation"
            width="100%"
            cellspacing="0"
            cellpadding="0"
            border="0"
            style="background-color:#f3f5f9;">
            <tr>
              <td align="center" style="padding:40px 16px;">
                <table
                  role="presentation"
                  width="100%"
                  cellspacing="0"
                  cellpadding="0"
                  border="0"
                  style="max-width:560px;">
                  <tr>
                    <td style="padding:0 8px 18px;font-size:15px;font-weight:700;letter-spacing:.01em;color:#263b68;">
                      <span
                        style="display:inline-block;width:9px;height:9px;margin-right:9px;border-radius:3px;background-color:#3158a8;vertical-align:1px;"></span>{safeProductName}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0;background-color:#ffffff;border:1px solid #e2e7f0;border-radius:12px;">
                      <table
                        role="presentation"
                        width="100%"
                        cellspacing="0"
                        cellpadding="0"
                        border="0">
                        <tr>
                          <td style="padding:36px 36px 12px;">
                            <div
                              style="width:42px;height:4px;border-radius:2px;background-color:#3158a8;margin-bottom:24px;"></div>
                            <h1
                              style="margin:0 0 14px;font-size:26px;line-height:1.25;font-weight:700;letter-spacing:-.02em;color:#17233b;">
                              Reset your password
                            </h1>
                            <p style="margin:0;font-size:15px;line-height:1.65;color:#536078;">
                              We received a request to reset your password.
                            </p>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:24px 36px 30px;">
                            <table
                              role="presentation"
                              cellspacing="0"
                              cellpadding="0"
                              border="0">
                              <tr>
                                <td align="center" style="border-radius:7px;background-color:#3158a8;">
                                  <a
                                    href="{safeResetUrl}"
                                    style="display:inline-block;padding:13px 22px;border:1px solid #3158a8;border-radius:7px;color:#ffffff;font-size:15px;font-weight:700;line-height:1.2;text-decoration:none;">
                                    Reset your password
                                  </a>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:0 36px 30px;">
                            <p style="margin:0;padding-top:20px;border-top:1px solid #edf0f5;font-size:13px;line-height:1.65;color:#657087;">
                              If you did not request this, you can ignore this email.
                            </p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td
                      style="padding:18px 8px 0;font-size:12px;line-height:1.6;color:#778197;">
                      If the button does not work, copy and paste this link into your browser:<br>
                      <a
                        href="{safeResetUrl}"
                        style="color:#3158a8;overflow-wrap:anywhere;word-break:break-all;">{safeResetUrl}</a>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }

    private static string BuildPlainTextBody(string resetUrl) =>
        $"We received a request to reset your password.\n\n"
        + $"Reset your password: {resetUrl}\n\n"
        + "If you did not request this, you can ignore this email.";

    private static void EnsureConfigured(EmailOptions settings)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(settings.SmtpHost)) missing.Add("Email:SmtpHost");
        if (settings.SmtpPort is < 1 or > 65535) missing.Add("Email:SmtpPort");
        if (string.IsNullOrWhiteSpace(settings.SmtpUser)) missing.Add("Email:SmtpUser");
        if (string.IsNullOrWhiteSpace(settings.SmtpAppPassword)) missing.Add("Email:SmtpAppPassword");
        if (string.IsNullOrWhiteSpace(settings.FromAddress)) missing.Add("Email:FromAddress");
        if (!Uri.TryCreate(settings.FrontendBaseUrl, UriKind.Absolute, out _))
        {
            missing.Add("Email:FrontendBaseUrl");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Email SMTP settings are incomplete. Configure: {string.Join(", ", missing)}.");
        }
    }
}
