using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Domain.Entities;

public sealed class DocumentPlaceholderValue
{
    private DocumentPlaceholderValue()
    {
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public Guid? PlaceholderId { get; private set; }

    public string PlaceholderKeySnapshot { get; private set; } = string.Empty;

    public string LabelSnapshot { get; private set; } = string.Empty;

    public PlaceholderDataType DataTypeSnapshot { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public Document? Document { get; private set; }

    public Placeholder? Placeholder { get; private set; }

    internal static DocumentPlaceholderValue Create(
        Guid documentId,
        Placeholder placeholder,
        string value)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("A document is required.", nameof(documentId));
        }

        ArgumentNullException.ThrowIfNull(placeholder);

        return new DocumentPlaceholderValue
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PlaceholderId = placeholder.Id,
            PlaceholderKeySnapshot = placeholder.Key,
            LabelSnapshot = placeholder.Label,
            DataTypeSnapshot = placeholder.DataType,
            Value = value ?? throw new ArgumentNullException(nameof(value))
        };
    }

    internal void UpdateValue(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
