namespace DocumentTemplateSystem.Domain.Patterns.Strategy;

public sealed class TextValidationStrategy : IPlaceholderValidationStrategy
{
    public bool Validate(string value) => !string.IsNullOrWhiteSpace(value);
}
