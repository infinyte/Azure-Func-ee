using FluentAssertions;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;
using Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Services;

public class MappingDataTransformerTests
{
    private readonly MappingDataTransformer _transformer = new();

    [Fact]
    public void Transform_RenameMapping_CopiesValue()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithField("old_name", "value123").Build()
        };
        var mappings = new List<TransformationMapping>
        {
            new("old_name", "new_name", TransformationType.Rename)
        };

        // Act
        var result = _transformer.Transform(records, mappings);

        // Assert
        result[0].Fields["new_name"].Should().Be("value123");
    }

    [Fact]
    public void Transform_UppercaseMapping_ConvertsToUpper()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithField("name", "alice smith").Build()
        };
        var mappings = new List<TransformationMapping>
        {
            new("name", "fullName", TransformationType.Uppercase)
        };

        // Act
        var result = _transformer.Transform(records, mappings);

        // Assert
        result[0].Fields["fullName"].Should().Be("ALICE SMITH");
    }

    [Fact]
    public void Transform_LowercaseMapping_ConvertsToLower()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithField("email", "User@Example.COM").Build()
        };
        var mappings = new List<TransformationMapping>
        {
            new("email", "emailLower", TransformationType.Lowercase)
        };

        // Act
        var result = _transformer.Transform(records, mappings);

        // Assert
        result[0].Fields["emailLower"].Should().Be("user@example.com");
    }

    [Fact]
    public void Transform_DefaultMapping_UsesDefaultWhenMissing()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().Build() // no "category" field
        };
        var mappings = new List<TransformationMapping>
        {
            new("category", "tier", TransformationType.Default, "Standard")
        };

        // Act
        var result = _transformer.Transform(records, mappings);

        // Assert
        result[0].Fields["tier"].Should().Be("Standard");
    }

    [Fact]
    public void Transform_DefaultMapping_PreservesExistingValue()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithField("category", "Premium").Build()
        };
        var mappings = new List<TransformationMapping>
        {
            new("category", "tier", TransformationType.Default, "Standard")
        };

        // Act
        var result = _transformer.Transform(records, mappings);

        // Assert
        result[0].Fields["tier"].Should().Be("Premium");
    }

    [Fact]
    public void Transform_PreservesSourceMetadata()
    {
        // Arrange
        var records = new List<DataRecord>
        {
            new DataRecordTestDataBuilder().WithSource("TestDB").WithField("a", "b").Build()
        };
        var mappings = new List<TransformationMapping>
        {
            new("a", "b", TransformationType.Rename)
        };

        // Act
        var result = _transformer.Transform(records, mappings);

        // Assert
        result[0].SourceName.Should().Be("TestDB");
        result[0].Id.Should().Be(records[0].Id);
    }
}
