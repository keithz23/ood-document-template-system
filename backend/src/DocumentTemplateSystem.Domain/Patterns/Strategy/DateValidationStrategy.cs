using System.Globalization;

namespace DocumentTemplateSystem.Domain.Patterns.Strategy;

public sealed class DateValidationStrategy : IPlaceholderValidationStrategy
{
    public bool Validate(string value)
    {
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }
}
