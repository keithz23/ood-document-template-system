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

    Task<bool> UsernameExistsAsync(
        string username,
        CancellationToken cancellationToken = default,
        Guid? excludingUserId = null);

    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default,
        Guid? excludingUserId = null);

    Task<IReadOnlyList<AdminAuditLogEntry>> GetAuditLogsAsync(
        CancellationToken cancellationToken = default);

    void AddAuditLog(AuditLog auditLog);

    void AddUser(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
