namespace DocumentTemplateSystem.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid performedBy,
        string actionType,
        string entityType,
        Guid entityId,
        string description,
        DateTimeOffset? createdAt = null)
    {
        if (performedBy == Guid.Empty)
        {
            throw new ArgumentException("A performer is required.", nameof(performedBy));
        }

        if (string.IsNullOrWhiteSpace(actionType))
        {
            throw new ArgumentException("Action type is required.", nameof(actionType));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("An entity identifier is required.", nameof(entityId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        Id = Guid.NewGuid();
        PerformedBy = performedBy;
        ActionType = actionType.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId;
        Description = description.Trim();
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PerformedBy { get; private set; }

    public string ActionType { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
}
