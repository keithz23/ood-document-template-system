namespace DocumentTemplateSystem.Domain.Patterns.Composite;

public sealed class SectionComponent : DocumentComponent
{
    private readonly List<DocumentComponent> _children;

    public SectionComponent()
        : this([])
    {
    }

    public SectionComponent(IEnumerable<DocumentComponent> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        _children = [.. children];

        if (_children.Any(child => child is null))
        {
            throw new ArgumentException("Section children cannot contain null.", nameof(children));
        }
    }

    public IReadOnlyList<DocumentComponent> Children => _children.AsReadOnly();

    public void Add(DocumentComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        _children.Add(component);
    }

    public bool Remove(DocumentComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        return _children.Remove(component);
    }

    public override string Render()
    {
        return $"<section>{string.Concat(_children.Select(child => child.Render()))}</section>";
    }

    public override DocumentComponent Clone()
    {
        return new SectionComponent(_children.Select(child => child.Clone()));
    }
}
