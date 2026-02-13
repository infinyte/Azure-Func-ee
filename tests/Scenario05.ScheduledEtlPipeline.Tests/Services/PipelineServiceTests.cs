using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Repositories;
using Scenario05.ScheduledEtlPipeline.Services;
using Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Services;

public class PipelineServiceTests
{
    private readonly Mock<IPipelineRepository> _mockRepo;
    private readonly PipelineService _service;

    public PipelineServiceTests()
    {
        _mockRepo = new Mock<IPipelineRepository>();
        var mockLogger = new Mock<ILogger<PipelineService>>();
        _service = new PipelineService(_mockRepo.Object, mockLogger.Object);
    }

    [Fact]
    public async Task CreateRunAsync_CreatesAndPersistsRun()
    {
        // Act
        var run = await _service.CreateRunAsync("Scheduled", CancellationToken.None);

        // Assert
        run.RunId.Should().NotBeNullOrWhiteSpace();
        run.TriggerSource.Should().Be("Scheduled");
        run.Status.Should().Be((int)PipelineStatus.Pending);
        _mockRepo.Verify(r => r.UpsertAsync(It.IsAny<PipelineRun>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesRunStatus()
    {
        // Arrange
        var run = new PipelineRunTestDataBuilder().WithRunId("run-001").Build();
        _mockRepo.Setup(r => r.GetAsync("run-001", It.IsAny<CancellationToken>())).ReturnsAsync(run);

        // Act
        await _service.UpdateStatusAsync("run-001", PipelineStatus.Extracting, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.UpsertAsync(
            It.Is<PipelineRun>(p => p.Status == (int)PipelineStatus.Extracting),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteRunAsync_SetsSummaryStatistics()
    {
        // Arrange
        var run = new PipelineRunTestDataBuilder().WithRunId("run-001").Build();
        _mockRepo.Setup(r => r.GetAsync("run-001", It.IsAny<CancellationToken>())).ReturnsAsync(run);

        // Act
        await _service.CompleteRunAsync("run-001", 100, 90, 10, 90, CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.UpsertAsync(
            It.Is<PipelineRun>(p =>
                p.Status == (int)PipelineStatus.Completed &&
                p.TotalRecordsExtracted == 100 &&
                p.ValidRecordCount == 90 &&
                p.InvalidRecordCount == 10 &&
                p.RecordsLoaded == 90 &&
                p.CompletedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailRunAsync_SetsErrorMessage()
    {
        // Arrange
        var run = new PipelineRunTestDataBuilder().WithRunId("run-001").Build();
        _mockRepo.Setup(r => r.GetAsync("run-001", It.IsAny<CancellationToken>())).ReturnsAsync(run);

        // Act
        await _service.FailRunAsync("run-001", "Something went wrong", CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.UpsertAsync(
            It.Is<PipelineRun>(p =>
                p.Status == (int)PipelineStatus.Failed &&
                p.ErrorMessage == "Something went wrong"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistentRun_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((PipelineRun?)null);

        // Act
        var act = () => _service.UpdateStatusAsync("missing", PipelineStatus.Extracting, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pipeline run 'missing' not found*");
    }

    [Fact]
    public async Task GetRunAsync_ExistingRun_ReturnsRun()
    {
        // Arrange
        var run = new PipelineRunTestDataBuilder().WithRunId("run-001").Build();
        _mockRepo.Setup(r => r.GetAsync("run-001", It.IsAny<CancellationToken>())).ReturnsAsync(run);

        // Act
        var result = await _service.GetRunAsync("run-001", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.RunId.Should().Be("run-001");
    }
}
