using System.Net.Mail;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.Services;

public sealed class AdminUserService(
    IAdminUserRepository repository,
    ICurrentUserContext currentUser,
    IPasswordHashService passwordHashService)
{
    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await repository.GetUsersAsync(cancellationToken);
        return users.Select(MapUser).ToArray();
    }

    public async Task<AdminUserDto> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(userId, "userId");
        return MapUser(await GetUserEntityAsync(userId, cancellationToken));
    }

    public async Task<AdminUserDto> CreateUserAsync(
        CreateAdminUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var username = request.Username?.Trim() ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var password = request.InitialPassword ?? string.Empty;
        var errors = new List<ValidationErrorDto>();

        AddRequiredError(errors, "username", username);
        AddRequiredError(errors, "fullName", fullName);
        AddRequiredError(errors, "email", email);
        AddRequiredError(errors, "initialPassword", password);

        if (email.Length > 0 && !MailAddress.TryCreate(email, out _))
        {
            errors.Add(new ValidationErrorDto(
                "email",
                "INVALID_EMAIL",
                "Email must be a valid email address."));
        }

        if (!Enum.IsDefined(request.Role))
        {
            errors.Add(new ValidationErrorDto(
                "role",
                "INVALID_ROLE",
                "Role must be Admin or User."));
        }

        if (errors.Count > 0)
        {
            throw UseCaseException.Validation([.. errors]);
        }

        if (await repository.UsernameExistsAsync(username, cancellationToken))
        {
            throw UseCaseException.Conflict(
                "USERNAME_ALREADY_EXISTS",
                "That username is already in use.");
        }

        if (await repository.EmailExistsAsync(email, cancellationToken))
        {
            throw UseCaseException.Conflict(
                "EMAIL_ALREADY_EXISTS",
                "That email address is already in use.");
        }

        var hashSubject = new User(
            username,
            "pending-password-hash",
            fullName,
            email,
            request.Role);
        var passwordHash = passwordHashService.HashPassword(hashSubject, password);
        var user = new User(
            username,
            passwordHash,
            fullName,
            email,
            request.Role);

        repository.AddUser(user);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "CreateUser",
            nameof(User),
            user.Id,
            $"Created {user.Role} user '{user.Username}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public async Task<AdminUserDto> UpdateUserAsync(
        Guid userId,
        UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(userId, "userId");
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username?.Trim() ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var errors = new List<ValidationErrorDto>();
        AddRequiredError(errors, "username", username);
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

        var user = await GetUserEntityAsync(userId, cancellationToken);
        if (await repository.UsernameExistsAsync(
                username,
                cancellationToken,
                excludingUserId: userId))
        {
            throw UseCaseException.Conflict(
                "USERNAME_ALREADY_EXISTS",
                "That username is already in use.");
        }

        if (await repository.EmailExistsAsync(
                email,
                cancellationToken,
                excludingUserId: userId))
        {
            throw UseCaseException.Conflict(
                "EMAIL_ALREADY_EXISTS",
                "That email address is already in use.");
        }

        if (user.Username == username
            && user.FullName == fullName
            && user.Email == email)
        {
            return MapUser(user);
        }

        var previousUsername = user.Username;
        user.UpdateIdentity(username, fullName, email);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "AdminEditUser",
            nameof(User),
            user.Id,
            $"Updated identity details for user '{previousUsername}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public Task<AdminUserDto> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        SetUserStateAsync(userId, true, cancellationToken);

    public Task<AdminUserDto> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        SetUserStateAsync(userId, false, cancellationToken);

    public async Task<AdminUserDto> UpdateRoleAsync(
        Guid userId,
        UpdateUserRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(userId, "userId");
        if (!Enum.IsDefined(request.Role))
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                "role",
                "INVALID_ROLE",
                "Role must be Admin or User."));
        }

        var user = await GetUserEntityAsync(userId, cancellationToken);
        if (user.Role == request.Role)
        {
            return MapUser(user);
        }

        if (user.Id == currentUser.UserId)
        {
            throw UseCaseException.Conflict(
                "SELF_ROLE_CHANGE_NOT_ALLOWED",
                "You cannot change your own role.");
        }

        var previousRole = user.Role;
        user.ChangeRole(request.Role);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "UpdateUserRole",
            nameof(User),
            user.Id,
            $"Changed user '{user.Username}' role from {previousRole} to {user.Role}."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public async Task<IReadOnlyList<AdminAuditLogDto>> GetAuditLogsAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await repository.GetAuditLogsAsync(cancellationToken);
        return entries.Select(entry => new AdminAuditLogDto(
            entry.AuditLog.Id,
            new AuditActorDto(
                entry.PerformedBy.Id,
                entry.PerformedBy.Username,
                entry.PerformedBy.FullName),
            entry.AuditLog.ActionType,
            entry.AuditLog.EntityType,
            entry.AuditLog.EntityId,
            entry.AuditLog.Description,
            entry.AuditLog.CreatedAt)).ToArray();
    }

    private async Task<AdminUserDto> SetUserStateAsync(
        Guid userId,
        bool activate,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(userId, "userId");
        var user = await GetUserEntityAsync(userId, cancellationToken);

        if (!activate && user.Id == currentUser.UserId)
        {
            throw UseCaseException.Conflict(
                "SELF_DEACTIVATION_NOT_ALLOWED",
                "You cannot deactivate your own account.");
        }

        if (user.IsActive == activate)
        {
            return MapUser(user);
        }

        if (activate)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            activate ? "ActivateUser" : "DeactivateUser",
            nameof(User),
            user.Id,
            $"{(activate ? "Activated" : "Deactivated")} user '{user.Username}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    private async Task<User> GetUserEntityAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await repository.GetUserByIdAsync(userId, cancellationToken)
        ?? throw UseCaseException.NotFound(
            "USER_NOT_FOUND",
            "The requested user was not found.");

    private static AdminUserDto MapUser(User user) =>
        new(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Role,
            user.IsActive,
            user.CreatedAt);

    private static void EnsureIdentifier(Guid id, string field)
    {
        if (id == Guid.Empty)
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                field,
                "REQUIRED",
                $"{field} is required."));
        }
    }

    private static void AddRequiredError(
        ICollection<ValidationErrorDto> errors,
        string field,
        string value)
    {
        if (value.Length == 0)
        {
            errors.Add(new ValidationErrorDto(
                field,
                "REQUIRED",
                $"{field} is required."));
        }
    }
}
