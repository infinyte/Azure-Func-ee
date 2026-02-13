using FluentAssertions;
using Scenario05.ScheduledEtlPipeline.Functions.Activities;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Functions.Activities;

public class ExtractFromCsvActivityTests
{
    [Fact]
    public void ParseCsv_ValidCsv_ReturnsCorrectRecords()
    {
        // Arrange
        var csv = "name,email,amount\nAlice,alice@example.com,100.00\nBob,bob@example.com,200.00";

        // Act
        var records = ExtractFromCsvActivity.ParseCsv(csv);

        // Assert
        records.Should().HaveCount(2);
        records[0].Fields["name"].Should().Be("Alice");
        records[0].Fields["email"].Should().Be("alice@example.com");
        records[0].Fields["amount"].Should().Be("100.00");
        records[1].Fields["name"].Should().Be("Bob");
    }

    [Fact]
    public void ParseCsv_EmptyContent_ReturnsEmptyList()
    {
        // Act
        var records = ExtractFromCsvActivity.ParseCsv(string.Empty);

        // Assert
        records.Should().BeEmpty();
    }

    [Fact]
    public void ParseCsv_HeaderOnly_ReturnsEmptyList()
    {
        // Arrange
        var csv = "name,email,amount";

        // Act
        var records = ExtractFromCsvActivity.ParseCsv(csv);

        // Assert
        records.Should().BeEmpty();
    }

    [Fact]
    public void ParseCsv_UnevenColumns_HandlesGracefully()
    {
        // Arrange — second row has fewer columns than the header.
        var csv = "name,email,amount\nAlice,alice@example.com";

        // Act
        var records = ExtractFromCsvActivity.ParseCsv(csv);

        // Assert
        records.Should().HaveCount(1);
        records[0].Fields.Should().ContainKey("name");
        records[0].Fields.Should().ContainKey("email");
        records[0].Fields.Should().NotContainKey("amount");
    }

    [Fact]
    public void ParseCsv_AllRecords_HaveCsvFileSource()
    {
        // Arrange
        var csv = "name\nAlice\nBob\nCharlie";

        // Act
        var records = ExtractFromCsvActivity.ParseCsv(csv);

        // Assert
        records.Should().HaveCount(3);
        records.Should().AllSatisfy(r => r.SourceName.Should().Be("CsvFile"));
    }
}
