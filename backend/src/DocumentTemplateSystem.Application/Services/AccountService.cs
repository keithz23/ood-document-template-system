using System.Net.Mail;
using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Services;

public sealed class AccountService(
    IAccountRepository repository,
    ICurrentUserContext currentUser,
    IPasswordHashService passwordHashService)
{
    public async Task<UserDto> UpdateProfileAsync(
        UpdateOwnProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var errors = new List<ValidationErrorDto>();

        AddRequiredError(errors, "fullName", fullName);
        AddRequiredError(errors, "email", email);
        if (email.Length > 0 && !MailAddress.TryCreate(email, out _))
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

        var user = await GetCurrentUserAsync(cancellationToken);
        if (await repository.EmailExistsAsync(email, user.Id, cancellationToken))
        {
            throw UseCaseException.Conflict(
                "EMAIL_ALREADY_EXISTS",
                "That email address is already in use.");
        }

        if (user.FullName == fullName && user.Email == email)
        {
            return MapUser(user);
        }

        user.UpdateProfile(fullName, email);
        repository.AddAuditLog(new AuditLog(
            user.Id,
            "ProfileUpdate",
            nameof(User),
            user.Id,
            $"Updated profile details for user '{user.Username}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var currentPassword = request.CurrentPassword ?? string.Empty;
        var newPassword = request.NewPassword ?? string.Empty;
        var errors = new List<ValidationErrorDto>();
        AddRequiredError(errors, "currentPassword", currentPassword);
        AddRequiredError(errors, "newPassword", newPassword);

        if (errors.Count > 0)
        {
            throw UseCaseException.Validation([.. errors]);
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (!passwordHashService.VerifyPassword(user, currentPassword))
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                "currentPassword",
                "CURRENT_PASSWORD_INVALID",
                "The current password is incorrect."));
        }

        user.ChangePasswordHash(passwordHashService.HashPassword(user, newPassword));
        repository.AddAuditLog(new AuditLog(
            user.Id,
            "ChangePassword",
            nameof(User),
            user.Id,
            $"Changed password for user '{user.Username}'."));
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var user = await repository.GetUserByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw UseCaseException.AuthenticationRequired();
        }

        return user;
    }

    private static UserDto MapUser(User user) =>
        new(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Role,
            Permissions.ForRole(user.Role));

    private static void AddRequiredError(
        ICollection<ValidationErrorDto> errors,
        string field,
        string value)
    {
        if (value.Length == 0)
        {
            errors.Add(new ValidationErrorDto(field, "REQUIRED", $"{field} is required."));
        }
    }
}
