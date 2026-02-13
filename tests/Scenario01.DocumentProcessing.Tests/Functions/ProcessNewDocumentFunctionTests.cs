using Azure.Storage.Queues;
using AzureFunctions.Shared.Telemetry;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Scenario01.DocumentProcessing.Functions;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;
using Xunit;

namespace Scenario01.DocumentProcessing.Tests.Functions;

public class ProcessNewDocumentFunctionTests
{
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly Mock<QueueServiceClient> _mockQueueServiceClient;
    private readonly Mock<QueueClient> _mockQueueClient;
    private readonly Mock<ITelemetryService> _mockTelemetry;
    private readonly Mock<ILogger<ProcessNewDocumentFunction>> _mockLogger;
    private readonly Mock<FunctionContext> _mockContext;
    private readonly ProcessNewDocumentFunction _sut;

    public ProcessNewDocumentFunctionTests()
    {
        _mockRepository = new Mock<IDocumentRepository>();
        _mockQueueServiceClient = new Mock<QueueServiceClient>();
        _mockQueueClient = new Mock<QueueClient>();
        _mockTelemetry = new Mock<ITelemetryService>();
        _mockLogger = new Mock<ILogger<ProcessNewDocumentFunction>>();
        _mockContext = new Mock<FunctionContext>();

        _mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var options = Options.Create(new DocumentProcessingOptions
        {
            DocumentsContainer = "documents",
            ProcessingQueue = "document-processing"
        });

        _mockQueueServiceClient
            .Setup(q => q.GetQueueClient(It.IsAny<string>()))
            .Returns(_mockQueueClient.Object);

        _mockQueueClient
            .Setup(q => q.CreateIfNotExistsAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(value: null!);

        _mockQueueClient
            .Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(value: null!);

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<DocumentMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new ProcessNewDocumentFunction(
            _mockRepository.Object,
            _mockQueueServiceClient.Object,
            _mockTelemetry.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_WithNewBlob_CreatesMetadataAndQueuesMessage()
    {
        // Arrange
        using var blobStream = new MemoryStream(new byte[512]);
        var blobName = "test-document.pdf";

        // Act
        await _sut.RunAsync(blobStream, blobName, _mockContext.Object);

        // Assert — metadata should be persisted.
        _mockRepository.Verify(
            r => r.UpsertAsync(
                It.Is<DocumentMetadata>(d =>
                    d.FileName == blobName &&
                    d.ContentType == "application/pdf" &&
                    d.FileSizeBytes == 512 &&
                    d.Status == DocumentStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert — a message should be sent to the queue.
        _mockQueueClient.Verify(
            q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithNewBlob_TracksDocumentUploadedEvent()
    {
        // Arrange
        using var blobStream = new MemoryStream(new byte[256]);

        // Act
        await _sut.RunAsync(blobStream, "report.txt", _mockContext.Object);

        // Assert
        _mockTelemetry.Verify(
            t => t.TrackEvent("DocumentUploaded", It.Is<IDictionary<string, string>>(d =>
                d.ContainsKey("DocumentId") &&
                d.ContainsKey("FileName") &&
                d["FileName"] == "report.txt")),
            Times.Once);
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("photo.png", "image/png")]
    [InlineData("data.json", "application/json")]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    public async Task RunAsync_InfersCorrectContentType(string blobName, string expectedContentType)
    {
        // Arrange
        using var blobStream = new MemoryStream(new byte[100]);

        // Act
        await _sut.RunAsync(blobStream, blobName, _mockContext.Object);

        // Assert
        _mockRepository.Verify(
            r => r.UpsertAsync(
                It.Is<DocumentMetadata>(d => d.ContentType == expectedContentType),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_SendsBase64EncodedQueueMessage()
    {
        // Arrange
        using var blobStream = new MemoryStream(new byte[100]);
        string? capturedMessage = null;

        _mockQueueClient
            .Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(value: null!);

        // Act
        await _sut.RunAsync(blobStream, "file.pdf", _mockContext.Object);

        // Assert — message should be base64 encoded.
        capturedMessage.Should().NotBeNullOrWhiteSpace();
        var act = () => Convert.FromBase64String(capturedMessage!);
        act.Should().NotThrow("the message should be valid base64");
    }
}
