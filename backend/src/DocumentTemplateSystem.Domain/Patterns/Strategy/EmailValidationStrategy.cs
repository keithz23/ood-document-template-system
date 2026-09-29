using System.Net.Mail;

namespace DocumentTemplateSystem.Domain.Patterns.Strategy;

public sealed class EmailValidationStrategy : IPlaceholderValidationStrategy
{
    public bool Validate(string value)
    {
        var normalizedValue = value?.Trim();

        return !string.IsNullOrWhiteSpace(normalizedValue)
            && MailAddress.TryCreate(normalizedValue, out var address)
            && string.Equals(
                address.Address,
                normalizedValue,
                StringComparison.OrdinalIgnoreCase);
    }
}
