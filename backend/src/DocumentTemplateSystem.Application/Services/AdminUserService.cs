using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.Services;

public sealed class AdminUserService(
    IAdminUserRepository repository,
    ICurrentUserContext currentUser)
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
}
