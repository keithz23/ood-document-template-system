using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public sealed record PasswordResetCandidate(
    Guid TokenId,
    string TokenHash,
    User User);

public interface IAccountRepository
{
    Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<User?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        Guid excludingUserId,
        CancellationToken cancellationToken = default);

    Task<PasswordResetCandidate?> FindPasswordResetCandidateAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    void AddPasswordResetToken(PasswordResetToken token);

    void AddAuditLog(AuditLog auditLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<bool> TryCompletePasswordResetAsync(
        Guid tokenId,
        string tokenHash,
        Guid userId,
        string passwordHash,
        DateTimeOffset usedAt,
        AuditLog auditLog,
        CancellationToken cancellationToken = default);
}
