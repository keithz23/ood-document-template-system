using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Domain.Entities;

public sealed class Template
{
    private readonly List<TemplateVersion> _versions = [];

    private Template()
    {
    }

    public Template(
        string name,
        Guid categoryId,
        Guid createdBy,
        string initialContent = "",
        ContentFormat contentFormat = ContentFormat.Html,
        DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template name is required.", nameof(name));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("A category is required.", nameof(categoryId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creator is required.", nameof(createdBy));
        }

        var timestamp = createdAt ?? DateTimeOffset.UtcNow;

        Id = Guid.NewGuid();
        Name = name.Trim();
        CategoryId = categoryId;
        Status = TemplateStatus.Draft;
        CreatedBy = createdBy;
        CreatedAt = timestamp;
        _versions.Add(new TemplateVersion(
            Id,
            1,
            initialContent,
            contentFormat,
            createdBy,
            timestamp));
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Guid CategoryId { get; private set; }

    public TemplateStatus Status { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Category? Category { get; private set; }

    public IReadOnlyCollection<TemplateVersion> Versions => _versions.AsReadOnly();

    public TemplateVersion AddVersion(
        string content,
        ContentFormat contentFormat,
        Guid createdBy,
        DateTimeOffset? createdAt = null)
    {
        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creator is required.", nameof(createdBy));
        }

        var nextNumber = _versions.Count == 0
            ? 1
            : _versions.Max(version => version.VersionNumber) + 1;

        var version = new TemplateVersion(
            Id,
            nextNumber,
            content,
            contentFormat,
            createdBy,
            createdAt ?? DateTimeOffset.UtcNow);

        _versions.Add(version);
        return version;
    }

    public void SetCurrentVersion(Guid templateVersionId)
    {
        var version = _versions.SingleOrDefault(candidate => candidate.Id == templateVersionId)
            ?? throw new InvalidOperationException("The template version does not belong to this template.");

        if (version.Status != VersionStatus.Published)
        {
            throw new InvalidOperationException("Only a published template version may be current.");
        }

        foreach (var existingVersion in _versions)
        {
            existingVersion.SetCurrent(existingVersion.Id == templateVersionId);
        }
    }

    public void Activate()
    {
        if (!_versions.Any(version => version.IsCurrent && version.Status == VersionStatus.Published))
        {
            throw new InvalidOperationException(
                "A template requires a current published version before it can be activated.");
        }

        Status = TemplateStatus.Active;
    }

    public void Deactivate() => Status = TemplateStatus.Inactive;

    public Document CreateDocument(
        Guid createdBy,
        string title,
        DateTimeOffset? createdAt = null)
    {
        if (Status != TemplateStatus.Active)
        {
            throw new InvalidOperationException("Only an active template may create a document.");
        }

        var currentVersion = _versions.SingleOrDefault(
            version => version.IsCurrent && version.Status == VersionStatus.Published)
            ?? throw new InvalidOperationException(
                "An active template requires a current published version.");

        return currentVersion.Clone(createdBy, title, createdAt ?? DateTimeOffset.UtcNow);
    }
}
