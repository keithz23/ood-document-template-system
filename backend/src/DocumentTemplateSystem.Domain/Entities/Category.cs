namespace DocumentTemplateSystem.Domain.Entities;

public sealed class Category
{
    private readonly List<Template> _templates = [];

    private Category()
    {
    }

    public Category(string name, Guid createdBy, DateTimeOffset? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creator is required.", nameof(createdBy));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<Template> Templates => _templates.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
