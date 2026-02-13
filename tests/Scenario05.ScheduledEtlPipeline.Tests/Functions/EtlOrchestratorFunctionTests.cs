using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario05.ScheduledEtlPipeline.Functions;
using Scenario05.ScheduledEtlPipeline.Models;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Functions;

public class EtlOrchestratorFunctionTests
{
    private readonly Mock<TaskOrchestrationContext> _mockContext;

    public EtlOrchestratorFunctionTests()
    {
        _mockContext = new Mock<TaskOrchestrationContext>();
        _mockContext
            .Setup(c => c.CreateReplaySafeLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());
        _mockContext
            .Setup(c => c.GetInput<string>())
            .Returns("run-001");
    }

    [Fact]
    public async Task RunAsync_AllSourcesSucceed_ReturnsCorrectCounts()
    {
        // Arrange
        var apiRecords = CreateRecords("ExternalApi", 5);
        var csvRecords = CreateRecords("CsvFile", 3);
        var dbRecords = CreateRecords("Database", 4);

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromApi", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("ExternalApi", apiRecords, true));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromCsv", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("CsvFile", csvRecords, true));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromDatabase", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("Database", dbRecords, true));

        var allRecords = apiRecords.Concat(csvRecords).Concat(dbRecords).ToList();

        _mockContext.Setup(c => c.CallActivityAsync<ValidationResult>(
                "ValidateData", It.IsAny<List<DataRecord>>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ValidationResult(
                allRecords.AsReadOnly(),
                Array.Empty<DataRecord>(),
                new Dictionary<string, IReadOnlyList<string>>()));

        _mockContext.Setup(c => c.CallActivityAsync<IReadOnlyList<DataRecord>>(
                "TransformData", It.IsAny<IReadOnlyList<DataRecord>>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(allRecords.AsReadOnly());

        _mockContext.Setup(c => c.CallActivityAsync<int>(
                "LoadData", It.IsAny<LoadDataInput>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(12);

        // Act
        var result = await EtlOrchestratorFunction.RunAsync(_mockContext.Object);

        // Assert
        result.RunId.Should().Be("run-001");
        result.TotalExtracted.Should().Be(12);
        result.ValidCount.Should().Be(12);
        result.InvalidCount.Should().Be(0);
        result.LoadedCount.Should().Be(12);
        result.FailedSources.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_OneSourceFails_ContinuesWithOtherSources()
    {
        // Arrange
        var csvRecords = CreateRecords("CsvFile", 3);
        var dbRecords = CreateRecords("Database", 4);

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromApi", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("ExternalApi", Array.Empty<DataRecord>(), false, "API timeout"));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromCsv", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("CsvFile", csvRecords, true));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromDatabase", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("Database", dbRecords, true));

        var validRecords = csvRecords.Concat(dbRecords).ToList();

        _mockContext.Setup(c => c.CallActivityAsync<ValidationResult>(
                "ValidateData", It.IsAny<List<DataRecord>>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ValidationResult(
                validRecords.AsReadOnly(),
                Array.Empty<DataRecord>(),
                new Dictionary<string, IReadOnlyList<string>>()));

        _mockContext.Setup(c => c.CallActivityAsync<IReadOnlyList<DataRecord>>(
                "TransformData", It.IsAny<IReadOnlyList<DataRecord>>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(validRecords.AsReadOnly());

        _mockContext.Setup(c => c.CallActivityAsync<int>(
                "LoadData", It.IsAny<LoadDataInput>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(7);

        // Act
        var result = await EtlOrchestratorFunction.RunAsync(_mockContext.Object);

        // Assert
        result.TotalExtracted.Should().Be(7);
        result.FailedSources.Should().ContainSingle().Which.Should().Be("ExternalApi");
        result.LoadedCount.Should().Be(7);
    }

    [Fact]
    public async Task RunAsync_AllSourcesFail_ReturnsZeroCounts()
    {
        // Arrange
        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromApi", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("ExternalApi", Array.Empty<DataRecord>(), false, "API down"));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromCsv", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("CsvFile", Array.Empty<DataRecord>(), false, "Blob missing"));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromDatabase", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("Database", Array.Empty<DataRecord>(), false, "DB timeout"));

        // Act
        var result = await EtlOrchestratorFunction.RunAsync(_mockContext.Object);

        // Assert
        result.TotalExtracted.Should().Be(0);
        result.LoadedCount.Should().Be(0);
        result.FailedSources.Should().HaveCount(3);
    }

    [Fact]
    public async Task RunAsync_NullRunId_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockContext.Setup(c => c.GetInput<string>()).Returns((string?)null);

        // Act
        var act = () => EtlOrchestratorFunction.RunAsync(_mockContext.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pipeline run ID is required*");
    }

    [Fact]
    public async Task RunAsync_FanOutExtraction_CallsAllThreeSourcesInParallel()
    {
        // Arrange
        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromApi", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("ExternalApi", Array.Empty<DataRecord>(), true));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromCsv", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("CsvFile", Array.Empty<DataRecord>(), true));

        _mockContext.Setup(c => c.CallActivityAsync<ExtractionResult>(
                "ExtractFromDatabase", "run-001", It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ExtractionResult("Database", Array.Empty<DataRecord>(), true));

        // Act
        await EtlOrchestratorFunction.RunAsync(_mockContext.Object);

        // Assert — all three extraction activities were called.
        _mockContext.Verify(c => c.CallActivityAsync<ExtractionResult>(
            "ExtractFromApi", "run-001", It.IsAny<TaskOptions>()), Times.Once);
        _mockContext.Verify(c => c.CallActivityAsync<ExtractionResult>(
            "ExtractFromCsv", "run-001", It.IsAny<TaskOptions>()), Times.Once);
        _mockContext.Verify(c => c.CallActivityAsync<ExtractionResult>(
            "ExtractFromDatabase", "run-001", It.IsAny<TaskOptions>()), Times.Once);
    }

    private static List<DataRecord> CreateRecords(string source, int count)
    {
        var records = new List<DataRecord>();
        for (var i = 0; i < count; i++)
        {
            records.Add(new DataRecord
            {
                SourceName = source,
                Fields = new Dictionary<string, string>
                {
                    ["name"] = $"{source} Record {i}",
                    ["email"] = $"user{i}@{source.ToLowerInvariant()}.com",
                    ["amount"] = (i * 100.0).ToString("F2")
                }
            });
        }
        return records;
    }
}
