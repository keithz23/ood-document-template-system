using System.Globalization;

namespace DocumentTemplateSystem.Domain.Patterns.Strategy;

public sealed class NumberValidationStrategy : IPlaceholderValidationStrategy
{
    public bool Validate(string value)
    {
        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out _);
    }
}
