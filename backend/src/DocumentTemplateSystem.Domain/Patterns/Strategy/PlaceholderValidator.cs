using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Domain.Patterns.Strategy;

public sealed class PlaceholderValidator
{
    private readonly IReadOnlyDictionary<PlaceholderDataType, IPlaceholderValidationStrategy>
        _strategies;

    public PlaceholderValidator()
        : this(new Dictionary<PlaceholderDataType, IPlaceholderValidationStrategy>
        {
            [PlaceholderDataType.Text] = new TextValidationStrategy(),
            [PlaceholderDataType.Number] = new NumberValidationStrategy(),
            [PlaceholderDataType.Date] = new DateValidationStrategy(),
            [PlaceholderDataType.Email] = new EmailValidationStrategy()
        })
    {
    }

    public PlaceholderValidator(
        IReadOnlyDictionary<PlaceholderDataType, IPlaceholderValidationStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies;
    }

    public bool Validate(Placeholder placeholder, string? value)
    {
        ArgumentNullException.ThrowIfNull(placeholder);
        return Validate(placeholder.DataType, value, placeholder.IsRequired);
    }

    public bool Validate(
        PlaceholderDataType dataType,
        string? value,
        bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return !isRequired;
        }

        return _strategies.TryGetValue(dataType, out var strategy)
            && strategy.Validate(value);
    }
}
