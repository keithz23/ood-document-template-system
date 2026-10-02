using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentTemplateSystem.Infrastructure.Repositories;

public sealed class AdminUserRepository(AppDbContext context) : IAdminUserRepository
{
    public async Task<IReadOnlyList<User>> GetUsersAsync(
        CancellationToken cancellationToken = default) =>
        await context.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .ToArrayAsync(cancellationToken);

    public Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(
            user => user.Id == userId,
            cancellationToken);

    public Task<bool> UsernameExistsAsync(
        string username,
        CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(user => user.Username == username, cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public async Task<IReadOnlyList<AdminAuditLogEntry>> GetAuditLogsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from auditLog in context.AuditLogs.AsNoTracking()
            join performer in context.Users.AsNoTracking()
                on auditLog.PerformedBy equals performer.Id
            orderby auditLog.CreatedAt descending, auditLog.Id descending
            select new { AuditLog = auditLog, Performer = performer })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(row => new AdminAuditLogEntry(row.AuditLog, row.Performer))
            .ToArray();
    }

    public void AddAuditLog(AuditLog auditLog) => context.AuditLogs.Add(auditLog);

    public void AddUser(User user) => context.Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
