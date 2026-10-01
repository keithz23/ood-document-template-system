using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public sealed record AdminAuditLogEntry(AuditLog AuditLog, User PerformedBy);

public interface IAdminUserRepository
{
    Task<IReadOnlyList<User>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminAuditLogEntry>> GetAuditLogsAsync(
        CancellationToken cancellationToken = default);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
