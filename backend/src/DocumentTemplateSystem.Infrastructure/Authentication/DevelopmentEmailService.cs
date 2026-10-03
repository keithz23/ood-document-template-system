using DocumentTemplateSystem.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace DocumentTemplateSystem.Infrastructure.Authentication;

public sealed class DevelopmentEmailService(
    IOptions<DevelopmentEmailOptions> options,
    IHostEnvironment environment,
    ILogger<DevelopmentEmailService> logger) : IEmailService
{
    public Task SendPasswordResetAsync(
        string recipientEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        if (environment.IsDevelopment()
            && options.Value.ExposePasswordResetUrlInLogs)
        {
            var baseUrl = options.Value.FrontendBaseUrl.TrimEnd('/');
            var resetUrl = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
            logger.LogInformation(
                "Development password reset requested for {RecipientEmail}. Reset URL: {ResetUrl}",
                recipientEmail,
                resetUrl);
        }
        else
        {
            logger.LogInformation(
                "Password reset delivery requested for {RecipientEmail}; no development URL was exposed.",
                recipientEmail);
        }

        return Task.CompletedTask;
    }
}
