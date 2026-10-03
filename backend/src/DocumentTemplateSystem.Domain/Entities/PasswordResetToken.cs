namespace DocumentTemplateSystem.Domain.Entities;

public sealed class PasswordResetToken
{
    private PasswordResetToken()
    {
    }

    public PasswordResetToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset? createdAt = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("A token hash is required.", nameof(tokenHash));
        }

        var created = createdAt ?? DateTimeOffset.UtcNow;
        if (expiresAt <= created)
        {
            throw new ArgumentException("The expiry must be after creation.", nameof(expiresAt));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = created;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool CanBeUsedAt(DateTimeOffset timestamp) =>
        UsedAt is null && ExpiresAt > timestamp;

    public void MarkUsed(DateTimeOffset usedAt)
    {
        if (!CanBeUsedAt(usedAt))
        {
            throw new InvalidOperationException("The reset token is no longer valid.");
        }

        UsedAt = usedAt;
    }
}
