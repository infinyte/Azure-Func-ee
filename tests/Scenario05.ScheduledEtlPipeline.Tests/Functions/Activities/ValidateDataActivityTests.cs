using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario05.ScheduledEtlPipeline.Functions.Activities;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;
using Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Functions.Activities;

public class ValidateDataActivityTests
{
    private readonly ValidateDataActivity _activity;

    public ValidateDataActivityTests()
    {
        var validator = new RuleBasedDataValidator();
        var mockLogger = new Mock<ILogger<ValidateDataActivity>>();
        _activity = new ValidateDataActivity(validator, mockLogger.Object);
    }

    [Fact]
    public void Run_ValidRecords_AllPassValidation()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithStandardFields().Build(),
            new DataRecordTestDataBuilder().WithStandardFields().WithField("name", "Another").Build()
        };

        // Act
        var result = _activity.Run(records);

        // Assert
        result.ValidRecords.Should().HaveCount(2);
        result.InvalidRecords.Should().BeEmpty();
    }

    [Fact]
    public void Run_MissingRequiredField_FailsValidation()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder()
                .WithField("email", "test@example.com")
                .WithField("amount", "100.00")
                .Build()
        };

        // Act
        var result = _activity.Run(records);

        // Assert
        result.ValidRecords.Should().BeEmpty();
        result.InvalidRecords.Should().HaveCount(1);
        result.Errors.Should().ContainKey(records[0].Id);
    }

    [Fact]
    public void Run_InvalidEmail_FailsValidation()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder()
                .WithField("name", "Test")
                .WithField("email", "not-an-email")
                .WithField("amount", "100.00")
                .Build()
        };

        // Act
        var result = _activity.Run(records);

        // Assert
        result.ValidRecords.Should().BeEmpty();
        result.InvalidRecords.Should().HaveCount(1);
    }

    [Fact]
    public void Run_AmountOutOfRange_FailsValidation()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder()
                .WithField("name", "Test")
                .WithField("email", "test@example.com")
                .WithField("amount", "-50")
                .Build()
        };

        // Act
        var result = _activity.Run(records);

        // Assert
        result.ValidRecords.Should().BeEmpty();
        result.InvalidRecords.Should().HaveCount(1);
    }
}
