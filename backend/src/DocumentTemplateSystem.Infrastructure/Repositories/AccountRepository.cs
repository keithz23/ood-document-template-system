using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentTemplateSystem.Infrastructure.Repositories;

public sealed class AccountRepository(AppDbContext context) : IAccountRepository
{
    public Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<User?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email,
        Guid excludingUserId,
        CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(
            user => user.Email == email && user.Id != excludingUserId,
            cancellationToken);

    public async Task<PasswordResetCandidate?> FindPasswordResetCandidateAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from token in context.PasswordResetTokens.AsNoTracking()
            join user in context.Users.AsNoTracking()
                on token.UserId equals user.Id
            where token.TokenHash == tokenHash
                && token.UsedAt == null
                && token.ExpiresAt > now
            select new { Token = token, User = user })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new PasswordResetCandidate(row.Token.Id, row.Token.TokenHash, row.User);
    }

    public void AddPasswordResetToken(PasswordResetToken token) =>
        context.PasswordResetTokens.Add(token);

    public void AddAuditLog(AuditLog auditLog) => context.AuditLogs.Add(auditLog);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw PersistenceConflictTranslator.Translate(exception);
        }
    }

    public async Task<bool> TryCompletePasswordResetAsync(
        Guid tokenId,
        string tokenHash,
        Guid userId,
        string passwordHash,
        DateTimeOffset usedAt,
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var consumed = await context.PasswordResetTokens
            .Where(token => token.Id == tokenId
                && token.TokenHash == tokenHash
                && token.UserId == userId
                && token.UsedAt == null
                && token.ExpiresAt > usedAt)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.UsedAt, usedAt),
                cancellationToken);

        if (consumed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var passwordUpdated = await context.Users
            .Where(user => user.Id == userId && user.IsActive)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(user => user.PasswordHash, passwordHash),
                cancellationToken);

        if (passwordUpdated != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
