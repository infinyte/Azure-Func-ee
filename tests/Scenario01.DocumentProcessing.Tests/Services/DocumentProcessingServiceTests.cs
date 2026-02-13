using System.Text;
using AzureFunctions.Shared.Models;
using AzureFunctions.Shared.Telemetry;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;
using Scenario01.DocumentProcessing.Tests.TestData;
using Xunit;

namespace Scenario01.DocumentProcessing.Tests.Services;

public class DocumentProcessingServiceTests
{
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly Mock<IClassificationService> _mockClassificationService;
    private readonly Mock<ITelemetryService> _mockTelemetry;
    private readonly Mock<ILogger<DocumentProcessingService>> _mockLogger;
    private readonly DocumentProcessingService _sut;

    public DocumentProcessingServiceTests()
    {
        _mockRepository = new Mock<IDocumentRepository>();
        _mockClassificationService = new Mock<IClassificationService>();
        _mockTelemetry = new Mock<ITelemetryService>();
        _mockLogger = new Mock<ILogger<DocumentProcessingService>>();

        _sut = new DocumentProcessingService(
            _mockRepository.Object,
            _mockClassificationService.Object,
            _mockTelemetry.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessDocumentAsync_WithValidDocument_ReturnsSuccessAndUpdatesStatusToCompleted()
    {
        // Arrange
        var documentId = "doc-123";
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .WithStatus(DocumentStatus.Pending)
            .WithContentType("text/plain")
            .Build();

        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("This is an invoice for payment."));

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<DocumentMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClassificationService
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentClassification.Invoice);

        // Act
        var result = await _sut.ProcessDocumentAsync(documentId, contentStream, "text/plain", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(DocumentStatus.Completed);
        result.Data.Classification.Should().Be(DocumentClassification.Invoice);
        result.Data.ProcessedAt.Should().NotBeNull();
        result.Data.ErrorMessage.Should().BeNull();

        // Verify repository was called to upsert at least twice (Processing, then Completed).
        _mockRepository.Verify(
            r => r.UpsertAsync(It.IsAny<DocumentMetadata>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenDocumentNotFound_ReturnsFailure()
    {
        // Arrange
        var documentId = "nonexistent-doc";
        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentMetadata?)null);

        // Act
        var result = await _sut.ProcessDocumentAsync(documentId, contentStream, "text/plain", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("DOCUMENT_NOT_FOUND");
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenClassificationThrows_MarksDocumentAsFailed()
    {
        // Arrange
        var documentId = "doc-fail";
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .WithStatus(DocumentStatus.Pending)
            .Build();

        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("some text"));

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<DocumentMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClassificationService
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Classification engine unavailable"));

        // Act
        var result = await _sut.ProcessDocumentAsync(documentId, contentStream, "text/plain", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("PROCESSING_ERROR");
        document.Status.Should().Be(DocumentStatus.Failed);
        document.ErrorMessage.Should().Contain("Classification engine unavailable");
    }

    [Fact]
    public async Task ProcessDocumentAsync_WhenProcessingFails_TracksExceptionTelemetry()
    {
        // Arrange
        var documentId = "doc-telemetry";
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .Build();

        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("some text"));

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<DocumentMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClassificationService
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Boom"));

        // Act
        await _sut.ProcessDocumentAsync(documentId, contentStream, "text/plain", CancellationToken.None);

        // Assert
        _mockTelemetry.Verify(
            t => t.TrackException(It.IsAny<Exception>(), It.IsAny<IDictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDocumentAsync_OnSuccess_TracksEventAndMetricTelemetry()
    {
        // Arrange
        var documentId = "doc-success-telemetry";
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .Build();

        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("text"));

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<DocumentMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClassificationService
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentClassification.Report);

        // Act
        await _sut.ProcessDocumentAsync(documentId, contentStream, "text/plain", CancellationToken.None);

        // Assert
        _mockTelemetry.Verify(
            t => t.TrackEvent("DocumentProcessed", It.IsAny<IDictionary<string, string>>()),
            Times.Once);

        _mockTelemetry.Verify(
            t => t.TrackMetric("DocumentProcessingTimeMs", It.IsAny<double>(), It.IsAny<IDictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDocumentAsync_SetsStatusToProcessingBeforeClassification()
    {
        // Arrange
        var documentId = "doc-processing-status";
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .Build();

        var statusDuringClassification = DocumentStatus.Pending;

        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("some text"));

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockRepository
            .Setup(r => r.UpsertAsync(It.IsAny<DocumentMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClassificationService
            .Setup(c => c.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => statusDuringClassification = document.Status)
            .ReturnsAsync(DocumentClassification.Unknown);

        // Act
        await _sut.ProcessDocumentAsync(documentId, contentStream, "text/plain", CancellationToken.None);

        // Assert — during classification, status should have been Processing.
        statusDuringClassification.Should().Be(DocumentStatus.Processing);
    }

    [Fact]
    public async Task GenerateReportAsync_WithProcessedDocuments_ProducesCorrectStatistics()
    {
        // Arrange
        var reportDate = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var uploadTime = reportDate.AddHours(-1);

        var documents = new List<DocumentMetadata>
        {
            DocumentTestDataBuilder.WithDefaults()
                .WithId("doc-1")
                .WithStatus(DocumentStatus.Completed)
                .WithClassification(DocumentClassification.Invoice)
                .WithUploadedAt(uploadTime)
                .WithProcessedAt(reportDate.AddHours(1))
                .Build(),
            DocumentTestDataBuilder.WithDefaults()
                .WithId("doc-2")
                .WithStatus(DocumentStatus.Completed)
                .WithClassification(DocumentClassification.Receipt)
                .WithUploadedAt(uploadTime)
                .WithProcessedAt(reportDate.AddHours(2))
                .Build(),
            DocumentTestDataBuilder.WithDefaults()
                .WithId("doc-3")
                .WithStatus(DocumentStatus.Failed)
                .WithClassification(DocumentClassification.Unknown)
                .WithUploadedAt(uploadTime)
                .WithProcessedAt(reportDate.AddHours(3))
                .WithErrorMessage("OCR failed")
                .Build()
        };

        _mockRepository
            .Setup(r => r.GetProcessedSinceDateAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents.AsReadOnly());

        // Act
        var report = await _sut.GenerateReportAsync(reportDate, CancellationToken.None);

        // Assert
        report.TotalProcessed.Should().Be(3);
        report.SuccessCount.Should().Be(2);
        report.FailureCount.Should().Be(1);
        report.AverageProcessingTimeMs.Should().BeGreaterThan(0);
        report.ClassificationBreakdown.Should().ContainKey("Invoice");
        report.ClassificationBreakdown.Should().ContainKey("Receipt");
        report.ClassificationBreakdown.Should().ContainKey("Unknown");
    }

    [Fact]
    public async Task GenerateReportAsync_WithNoDocuments_ReturnsZeroStatistics()
    {
        // Arrange
        var reportDate = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);

        _mockRepository
            .Setup(r => r.GetProcessedSinceDateAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentMetadata>().AsReadOnly());

        // Act
        var report = await _sut.GenerateReportAsync(reportDate, CancellationToken.None);

        // Assert
        report.TotalProcessed.Should().Be(0);
        report.SuccessCount.Should().Be(0);
        report.FailureCount.Should().Be(0);
        report.AverageProcessingTimeMs.Should().Be(0);
        report.ClassificationBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessDocumentAsync_WithNullDocumentId_ThrowsArgumentException()
    {
        // Arrange
        var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        // Act
        var act = () => _sut.ProcessDocumentAsync(null!, contentStream, "text/plain", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProcessDocumentAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.ProcessDocumentAsync("doc-1", null!, "text/plain", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
