using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Prototype;

namespace DocumentTemplateSystem.Domain.Entities;

public sealed class TemplateVersion : IPrototype<Document>
{
    private readonly List<Placeholder> _placeholders = [];
    private readonly List<Document> _documents = [];

    private TemplateVersion()
    {
    }

    public TemplateVersion(
        Guid templateId,
        int versionNumber,
        string content,
        ContentFormat contentFormat,
        Guid createdBy,
        DateTimeOffset? createdAt = null)
    {
        if (templateId == Guid.Empty)
        {
            throw new ArgumentException("A template is required.", nameof(templateId));
        }

        if (versionNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                "Version number must be greater than zero.");
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creator is required.", nameof(createdBy));
        }

        var timestamp = createdAt ?? DateTimeOffset.UtcNow;

        Id = Guid.NewGuid();
        TemplateId = templateId;
        VersionNumber = versionNumber;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentFormat = contentFormat;
        Status = VersionStatus.Draft;
        IsCurrent = false;
        CreatedBy = createdBy;
        CreatedAt = timestamp;
        UpdatedAt = timestamp;
    }

    public Guid Id { get; private set; }

    public Guid TemplateId { get; private set; }

    public int VersionNumber { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public ContentFormat ContentFormat { get; private set; }

    public VersionStatus Status { get; private set; }

    public bool IsCurrent { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? PublishedBy { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Template? Template { get; private set; }

    public IReadOnlyCollection<Placeholder> Placeholders => _placeholders.AsReadOnly();

    public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();

    public void UpdateContent(
        string content,
        ContentFormat contentFormat,
        DateTimeOffset? updatedAt = null)
    {
        EnsureDraft();
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ContentFormat = contentFormat;
        UpdatedAt = updatedAt ?? DateTimeOffset.UtcNow;
    }

    public Placeholder AddPlaceholder(
        string key,
        string label,
        PlaceholderDataType dataType,
        bool isRequired,
        string? defaultValue = null)
    {
        EnsureDraft();

        if (_placeholders.Any(
                placeholder => string.Equals(
                    placeholder.Key,
                    key?.Trim(),
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Placeholder keys must be unique within a template version.");
        }

        var placeholder = new Placeholder(
            Id,
            key,
            label,
            dataType,
            isRequired,
            defaultValue);

        _placeholders.Add(placeholder);
        return placeholder;
    }

    public Placeholder UpdatePlaceholder(
        Guid placeholderId,
        string key,
        string label,
        PlaceholderDataType dataType,
        bool isRequired,
        string? defaultValue = null)
    {
        EnsureDraft();

        var placeholder = _placeholders.SingleOrDefault(candidate => candidate.Id == placeholderId)
            ?? throw new InvalidOperationException(
                "The placeholder does not belong to this template version.");
        var normalizedKey = key.Trim();
        if (_placeholders.Any(candidate =>
                candidate.Id != placeholderId
                && string.Equals(candidate.Key, normalizedKey, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Placeholder keys must be unique within a template version.");
        }

        placeholder.Update(key, label, dataType, isRequired, defaultValue);
        return placeholder;
    }

    public Placeholder RemovePlaceholder(Guid placeholderId)
    {
        EnsureDraft();

        var placeholder = _placeholders.SingleOrDefault(candidate => candidate.Id == placeholderId)
            ?? throw new InvalidOperationException(
                "The placeholder does not belong to this template version.");
        _placeholders.Remove(placeholder);
        return placeholder;
    }

    public void Publish(Guid publishedBy, DateTimeOffset? publishedAt = null)
    {
        EnsureDraft();

        if (publishedBy == Guid.Empty)
        {
            throw new ArgumentException("A publisher is required.", nameof(publishedBy));
        }

        var timestamp = publishedAt ?? DateTimeOffset.UtcNow;
        Status = VersionStatus.Published;
        PublishedBy = publishedBy;
        PublishedAt = timestamp;
        UpdatedAt = timestamp;
    }

    public Document Clone()
    {
        return Clone(CreatedBy, "Untitled document", DateTimeOffset.UtcNow);
    }

    public Document Clone(Guid createdBy, string title, DateTimeOffset? createdAt = null)
    {
        return Document.CreateFromTemplateVersion(
            Id,
            createdBy,
            title,
            Content,
            createdAt ?? DateTimeOffset.UtcNow);
    }

    internal void SetCurrent(bool isCurrent)
    {
        if (isCurrent && Status != VersionStatus.Published)
        {
            throw new InvalidOperationException("Only a published template version may be current.");
        }

        IsCurrent = isCurrent;
    }

    private void EnsureDraft()
    {
        if (Status != VersionStatus.Draft)
        {
            throw new InvalidOperationException(
                "A published template version cannot be modified in place.");
        }
    }
}
