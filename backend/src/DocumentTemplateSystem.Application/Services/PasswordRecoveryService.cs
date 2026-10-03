using System.Net.Mail;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Services;

public sealed class PasswordRecoveryService(
    IAccountRepository repository,
    IPasswordHashService passwordHashService,
    IPasswordResetTokenService tokenService,
    IEmailService emailService,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    private const string GenericMessage =
        "If an active account matches that email, password reset instructions have been sent.";

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var email = request.Email?.Trim() ?? string.Empty;
        var errors = new List<ValidationErrorDto>();
        if (email.Length == 0)
        {
            errors.Add(new ValidationErrorDto("email", "REQUIRED", "Email is required."));
        }
        else if (!MailAddress.TryCreate(email, out _))
        {
            errors.Add(new ValidationErrorDto(
                "email",
                "INVALID_EMAIL",
                "Email must be a valid email address."));
        }

        if (errors.Count > 0)
        {
            throw UseCaseException.Validation([.. errors]);
        }

        var user = await repository.FindUserByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponseDto(GenericMessage);
        }

        var now = timeProvider.GetUtcNow();
        var generated = tokenService.Generate();
        repository.AddPasswordResetToken(new PasswordResetToken(
            user.Id,
            generated.TokenHash,
            now.Add(TokenLifetime),
            now));
        await repository.SaveChangesAsync(cancellationToken);
        await emailService.SendPasswordResetAsync(
            user.Email,
            generated.RawToken,
            cancellationToken);

        return new ForgotPasswordResponseDto(GenericMessage);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rawToken = request.Token?.Trim() ?? string.Empty;
        var newPassword = request.NewPassword ?? string.Empty;
        var errors = new List<ValidationErrorDto>();
        if (rawToken.Length == 0)
        {
            errors.Add(new ValidationErrorDto("token", "REQUIRED", "Reset token is required."));
        }

        if (newPassword.Length == 0)
        {
            errors.Add(new ValidationErrorDto(
                "newPassword",
                "REQUIRED",
                "New password is required."));
        }

        if (errors.Count > 0)
        {
            throw UseCaseException.Validation([.. errors]);
        }

        var now = timeProvider.GetUtcNow();
        var tokenHash = tokenService.Hash(rawToken);
        var candidate = await repository.FindPasswordResetCandidateAsync(
            tokenHash,
            now,
            cancellationToken);

        if (candidate is null || !candidate.User.IsActive)
        {
            throw InvalidResetToken();
        }

        var passwordHash = passwordHashService.HashPassword(candidate.User, newPassword);
        var auditLog = new AuditLog(
            candidate.User.Id,
            "ResetPassword",
            nameof(User),
            candidate.User.Id,
            $"Reset password for user '{candidate.User.Username}'.",
            now);
        var completed = await repository.TryCompletePasswordResetAsync(
            candidate.TokenId,
            candidate.TokenHash,
            candidate.User.Id,
            passwordHash,
            now,
            auditLog,
            cancellationToken);

        if (!completed)
        {
            throw InvalidResetToken();
        }
    }

    private static UseCaseException InvalidResetToken() =>
        UseCaseException.Unprocessable(
            "INVALID_RESET_TOKEN",
            "The password reset link is invalid or has expired.");
}
