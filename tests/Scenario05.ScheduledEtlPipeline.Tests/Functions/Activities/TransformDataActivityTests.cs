using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario05.ScheduledEtlPipeline.Functions.Activities;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;
using Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Functions.Activities;

public class TransformDataActivityTests
{
    private readonly TransformDataActivity _activity;

    public TransformDataActivityTests()
    {
        var transformer = new MappingDataTransformer();
        var mockLogger = new Mock<ILogger<TransformDataActivity>>();
        _activity = new TransformDataActivity(transformer, mockLogger.Object);
    }

    [Fact]
    public void Run_WithStandardFields_AppliesDefaultMappings()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithStandardFields().Build()
        };

        // Act
        var result = _activity.Run(records);

        // Assert
        result.Should().HaveCount(1);
        var transformed = result[0];
        transformed.Fields.Should().ContainKey("fullName");
        transformed.Fields["fullName"].Should().Be("TEST RECORD"); // uppercase
        transformed.Fields.Should().ContainKey("emailAddress");
        transformed.Fields["emailAddress"].Should().Be("test@example.com"); // lowercase
        transformed.Fields.Should().ContainKey("totalAmount");
        transformed.Fields["totalAmount"].Should().Be("100.00"); // renamed
        transformed.Fields.Should().ContainKey("tier");
        transformed.Fields["tier"].Should().Be("Standard"); // default
    }

    [Fact]
    public void Run_MissingCategory_UsesDefaultValue()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder()
                .WithField("name", "Test")
                .WithField("email", "test@example.com")
                .WithField("amount", "100")
                .Build()
        };

        // Act
        var result = _activity.Run(records);

        // Assert
        result[0].Fields["tier"].Should().Be("Standard");
    }

    [Fact]
    public void Run_MultipleRecords_TransformsAll()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithStandardFields().Build(),
            new DataRecordTestDataBuilder().WithStandardFields().WithField("name", "Other").Build()
        };

        // Act
        var result = _activity.Run(records);

        // Assert
        result.Should().HaveCount(2);
    }
}
