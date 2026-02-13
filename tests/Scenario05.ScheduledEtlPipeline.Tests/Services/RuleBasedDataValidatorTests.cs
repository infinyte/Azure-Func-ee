using FluentAssertions;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;
using Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Services;

public class RuleBasedDataValidatorTests
{
    private readonly RuleBasedDataValidator _validator = new();

    [Fact]
    public void Validate_RequiredRule_FailsForMissingField()
    {
        // Arrange
        var records = new List<DataRecord> { new DataRecordTestDataBuilder().Build() };
        var rules = new List<ValidationRule>
        {
            new("name", ValidationRuleType.Required, null, "Name is required.")
        };

        // Act
        var result = _validator.Validate(records, rules);

        // Assert
        result.InvalidRecords.Should().HaveCount(1);
        result.Errors[records[0].Id].Should().Contain("Name is required.");
    }

    [Fact]
    public void Validate_RequiredRule_PassesForPresentField()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithField("name", "Alice").Build()
        };
        var rules = new List<ValidationRule>
        {
            new("name", ValidationRuleType.Required, null, "Name is required.")
        };

        // Act
        var result = _validator.Validate(records, rules);

        // Assert
        result.ValidRecords.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user@domain.org", true)]
    [InlineData("not-an-email", false)]
    [InlineData("", false)]
    public void Validate_RegexRule_ValidatesEmailFormat(string email, bool shouldPass)
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithField("email", email).Build()
        };
        var rules = new List<ValidationRule>
        {
            new("email", ValidationRuleType.Regex, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Invalid email.")
        };

        // Act
        var result = _validator.Validate(records, rules);

        // Assert
        if (shouldPass)
        {
            result.ValidRecords.Should().HaveCount(1);
        }
        else
        {
            result.InvalidRecords.Should().HaveCount(1);
        }
    }

    [Theory]
    [InlineData("50", "0-100", true)]
    [InlineData("0", "0-100", true)]
    [InlineData("100", "0-100", true)]
    [InlineData("-1", "0-100", false)]
    [InlineData("101", "0-100", false)]
    [InlineData("abc", "0-100", false)]
    public void Validate_RangeRule_ValidatesNumericRange(string value, string range, bool shouldPass)
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithField("amount", value).Build()
        };
        var rules = new List<ValidationRule>
        {
            new("amount", ValidationRuleType.Range, range, "Amount out of range.")
        };

        // Act
        var result = _validator.Validate(records, rules);

        // Assert
        if (shouldPass)
        {
            result.ValidRecords.Should().HaveCount(1);
        }
        else
        {
            result.InvalidRecords.Should().HaveCount(1);
        }
    }

    [Fact]
    public void Validate_MultipleRules_AccumulatesErrors()
    {
        // Arrange — record fails both required and email rules.
        var records = new List<DataRecord> { new DataRecordTestDataBuilder().Build() };
        var rules = new List<ValidationRule>
        {
            new("name", ValidationRuleType.Required, null, "Name is required."),
            new("email", ValidationRuleType.Regex, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Invalid email.")
        };

        // Act
        var result = _validator.Validate(records, rules);

        // Assert
        result.InvalidRecords.Should().HaveCount(1);
        result.Errors[records[0].Id].Should().HaveCount(2);
    }
}
