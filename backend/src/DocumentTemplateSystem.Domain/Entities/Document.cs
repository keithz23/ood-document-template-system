using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Domain.Entities;

public sealed class Document
{
    private readonly List<DocumentPlaceholderValue> _placeholderValues = [];

    private Document()
    {
    }

    public Guid Id { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DocumentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? FinalizedAt { get; private set; }

    public TemplateVersion? TemplateVersion { get; private set; }

    public IReadOnlyCollection<DocumentPlaceholderValue> PlaceholderValues =>
        _placeholderValues.AsReadOnly();

    internal static Document CreateFromTemplateVersion(
        Guid templateVersionId,
        Guid createdBy,
        string title,
        string content,
        DateTimeOffset createdAt)
    {
        if (templateVersionId == Guid.Empty)
        {
            throw new ArgumentException(
                "A template version is required.",
                nameof(templateVersionId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creator is required.", nameof(createdBy));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title is required.", nameof(title));
        }

        return new Document
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = templateVersionId,
            CreatedBy = createdBy,
            Title = title.Trim(),
            Content = content ?? throw new ArgumentNullException(nameof(content)),
            Status = DocumentStatus.Draft,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdateDraft(
        string title,
        string content,
        DateTimeOffset? updatedAt = null)
    {
        EnsureDraft();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title is required.", nameof(title));
        }

        Title = title.Trim();
        Content = content ?? throw new ArgumentNullException(nameof(content));
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
    }

    public DocumentPlaceholderValue SetPlaceholderValue(
        Placeholder placeholder,
        string value,
        DateTimeOffset? updatedAt = null)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(placeholder);

        if (placeholder.TemplateVersionId != TemplateVersionId)
        {
            throw new InvalidOperationException(
                "The placeholder does not belong to the document's source template version.");
        }

        var existingValue = _placeholderValues.SingleOrDefault(
            candidate => candidate.PlaceholderId == placeholder.Id);

        if (existingValue is null)
        {
            existingValue = DocumentPlaceholderValue.Create(Id, placeholder, value);
            _placeholderValues.Add(existingValue);
        }
        else
        {
            existingValue.UpdateValue(value);
        }

        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
        return existingValue;
    }

    public void Finalize(DateTimeOffset? finalizedAt = null)
    {
        EnsureDraft();
        var timestamp = finalizedAt ?? DateTimeOffset.UtcNow;
        Status = DocumentStatus.Finalized;
        FinalizedAt = timestamp;
        UpdatedAt = timestamp;
    }

    private void EnsureDraft()
    {
        if (Status != DocumentStatus.Draft)
        {
            throw new InvalidOperationException("A finalized document is read-only.");
        }
    }
}
