using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.DTOs;

public sealed record AdminUserDto(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record UpdateUserRoleRequestDto(UserRole Role);

public sealed record AuditActorDto(
    Guid Id,
    string Username,
    string FullName);

public sealed record AdminAuditLogDto(
    Guid Id,
    AuditActorDto PerformedBy,
    string ActionType,
    string EntityType,
    Guid EntityId,
    string Description,
    DateTimeOffset CreatedAt);
