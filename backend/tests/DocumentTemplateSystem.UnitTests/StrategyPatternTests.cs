using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Strategy;

namespace DocumentTemplateSystem.UnitTests;

public sealed class StrategyPatternTests
{
    [Theory]
    [InlineData("plain text", true)]
    [InlineData("   ", false)]
    public void TextStrategy_ValidatesExpectedValues(string value, bool expected)
    {
        Assert.Equal(expected, new TextValidationStrategy().Validate(value));
    }

    [Theory]
    [InlineData("1250.75", true)]
    [InlineData("not-a-number", false)]
    public void NumberStrategy_ValidatesExpectedValues(string value, bool expected)
    {
        Assert.Equal(expected, new NumberValidationStrategy().Validate(value));
    }

    [Theory]
    [InlineData("2026-09-30", true)]
    [InlineData("30/09/2026", false)]
    [InlineData("2026-02-30", false)]
    public void DateStrategy_RequiresIsoCalendarDate(string value, bool expected)
    {
        Assert.Equal(expected, new DateValidationStrategy().Validate(value));
    }

    [Theory]
    [InlineData("author@example.test", true)]
    [InlineData("not-an-email", false)]
    public void EmailStrategy_ValidatesExpectedValues(string value, bool expected)
    {
        Assert.Equal(expected, new EmailValidationStrategy().Validate(value));
    }

    [Fact]
    public void Validator_EnforcesRequiredAndAllowsEmptyOptionalValue()
    {
        var versionId = Guid.NewGuid();
        var required = new Placeholder(
            versionId,
            "email",
            "Email",
            PlaceholderDataType.Email,
            true);
        var optional = new Placeholder(
            versionId,
            "completion_date",
            "Completion date",
            PlaceholderDataType.Date,
            false);
        var validator = new PlaceholderValidator();

        Assert.False(validator.Validate(required, string.Empty));
        Assert.False(validator.Validate(required, "invalid"));
        Assert.True(validator.Validate(required, "author@example.test"));
        Assert.True(validator.Validate(optional, null));
        Assert.True(validator.Validate(optional, string.Empty));
    }
}
