namespace DocumentTemplateSystem.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(
        string recipientEmail,
        string rawToken,
        CancellationToken cancellationToken = default);
}
