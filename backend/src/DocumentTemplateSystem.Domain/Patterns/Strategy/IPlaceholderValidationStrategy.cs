namespace DocumentTemplateSystem.Domain.Patterns.Strategy;

public interface IPlaceholderValidationStrategy
{
    bool Validate(string value);
}
