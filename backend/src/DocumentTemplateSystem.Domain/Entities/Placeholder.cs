using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Domain.Entities;

public sealed class Placeholder
{
    private readonly List<DocumentPlaceholderValue> _documentValues = [];

    private Placeholder()
    {
    }

    public Placeholder(
        Guid templateVersionId,
        string key,
        string label,
        PlaceholderDataType dataType,
        bool isRequired,
        string? defaultValue = null)
    {
        if (templateVersionId == Guid.Empty)
        {
            throw new ArgumentException(
                "A template version is required.",
                nameof(templateVersionId));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Placeholder key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Placeholder label is required.", nameof(label));
        }

        Id = Guid.NewGuid();
        TemplateVersionId = templateVersionId;
        Key = key.Trim();
        Label = label.Trim();
        DataType = dataType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }

    public Guid Id { get; private set; }

    public Guid TemplateVersionId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public PlaceholderDataType DataType { get; private set; }

    public bool IsRequired { get; private set; }

    public string? DefaultValue { get; private set; }

    public TemplateVersion? TemplateVersion { get; private set; }

    public IReadOnlyCollection<DocumentPlaceholderValue> DocumentValues =>
        _documentValues.AsReadOnly();

    internal void Update(
        string key,
        string label,
        PlaceholderDataType dataType,
        bool isRequired,
        string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Placeholder key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Placeholder label is required.", nameof(label));
        }

        Key = key.Trim();
        Label = label.Trim();
        DataType = dataType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }
}
