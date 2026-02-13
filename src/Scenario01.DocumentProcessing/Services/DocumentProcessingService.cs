using System.Diagnostics;
using System.Text;
using AzureFunctions.Shared.Models;
using AzureFunctions.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using Scenario01.DocumentProcessing.Models;

namespace Scenario01.DocumentProcessing.Services;

/// <summary>
/// Orchestrates the document processing pipeline: text extraction, classification,
/// metadata updates, and report generation.
/// </summary>
public sealed class DocumentProcessingService : IDocumentProcessingService
{
    /// <summary>
    /// Content types considered as text-based, where direct text extraction is possible.
    /// </summary>
    private static readonly HashSet<string> TextBasedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/csv",
        "text/html",
        "text/xml",
        "application/json",
        "application/xml"
    };

    private readonly IDocumentRepository _repository;
    private readonly IClassificationService _classificationService;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<DocumentProcessingService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentProcessingService"/>.
    /// </summary>
    /// <param name="repository">The document metadata repository.</param>
    /// <param name="classificationService">The document classification service.</param>
    /// <param name="telemetry">The telemetry service for tracking metrics and events.</param>
    /// <param name="logger">The logger instance.</param>
    public DocumentProcessingService(
        IDocumentRepository repository,
        IClassificationService classificationService,
        ITelemetryService telemetry,
        ILogger<DocumentProcessingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<OperationResult<DocumentMetadata>> ProcessDocumentAsync(
        string documentId,
        Stream documentStream,
        string contentType,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentNullException.ThrowIfNull(documentStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting processing for document {DocumentId} with content type {ContentType}",
            documentId, contentType);

        var document = await _repository.GetAsync(documentId, ct).ConfigureAwait(false);
        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} not found in repository", documentId);
            return OperationResult<DocumentMetadata>.Failure(
                $"Document '{documentId}' not found.",
                "DOCUMENT_NOT_FOUND");
        }

        try
        {
            // Update status to Processing.
            document.Status = DocumentStatus.Processing;
            await _repository.UpsertAsync(document, ct).ConfigureAwait(false);

            // Extract text from the document stream.
            var extractedText = await ExtractTextAsync(documentStream, contentType, ct).ConfigureAwait(false);
            document.OcrText = extractedText;

            _logger.LogInformation(
                "Extracted {CharacterCount} characters from document {DocumentId}",
                extractedText.Length, documentId);

            // Classify the document based on extracted text.
            document.Classification = await _classificationService
                .ClassifyAsync(extractedText, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Document {DocumentId} classified as {Classification}",
                documentId, document.Classification);

            // Mark as completed.
            document.Status = DocumentStatus.Completed;
            document.ProcessedAt = DateTimeOffset.UtcNow;
            document.ErrorMessage = null;
            await _repository.UpsertAsync(document, ct).ConfigureAwait(false);

            stopwatch.Stop();

            _telemetry.TrackEvent("DocumentProcessed", new Dictionary<string, string>
            {
                ["DocumentId"] = documentId,
                ["Classification"] = document.Classification.ToString(),
                ["ContentType"] = contentType
            });
            _telemetry.TrackMetric("DocumentProcessingTimeMs", stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Successfully processed document {DocumentId} in {ElapsedMs}ms",
                documentId, stopwatch.ElapsedMilliseconds);

            return OperationResult<DocumentMetadata>.Success(document);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processing cancelled for document {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Failed to process document {DocumentId} after {ElapsedMs}ms",
                documentId, stopwatch.ElapsedMilliseconds);

            document.Status = DocumentStatus.Failed;
            document.ProcessedAt = DateTimeOffset.UtcNow;
            document.ErrorMessage = ex.Message;
            await _repository.UpsertAsync(document, ct).ConfigureAwait(false);

            _telemetry.TrackException(ex, new Dictionary<string, string>
            {
                ["DocumentId"] = documentId,
                ["ContentType"] = contentType
            });

            return OperationResult<DocumentMetadata>.Failure(
                "Document processing failed.",
                "PROCESSING_ERROR",
                ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ProcessingReport> GenerateReportAsync(DateTimeOffset date, CancellationToken ct)
    {
        var startOfDay = new DateTimeOffset(date.Date, TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        _logger.LogInformation("Generating processing report for {Date}", date.Date.ToString("yyyy-MM-dd"));

        var processedDocuments = await _repository
            .GetProcessedSinceDateAsync(startOfDay, ct)
            .ConfigureAwait(false);

        // Filter to only documents processed within the target day.
        var documentsForDay = processedDocuments
            .Where(d => d.ProcessedAt.HasValue && d.ProcessedAt.Value < endOfDay)
            .ToList();

        var successCount = documentsForDay.Count(d => d.Status == DocumentStatus.Completed);
        var failureCount = documentsForDay.Count(d => d.Status == DocumentStatus.Failed);

        // Calculate average processing time from upload to processed timestamps.
        var processingTimes = documentsForDay
            .Where(d => d.ProcessedAt.HasValue)
            .Select(d => (d.ProcessedAt!.Value - d.UploadedAt).TotalMilliseconds)
            .ToList();

        var averageProcessingTimeMs = processingTimes.Count > 0
            ? processingTimes.Average()
            : 0.0;

        // Build classification breakdown.
        var classificationBreakdown = documentsForDay
            .GroupBy(d => d.Classification.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var report = new ProcessingReport(
            ReportDate: startOfDay,
            TotalProcessed: documentsForDay.Count,
            SuccessCount: successCount,
            FailureCount: failureCount,
            AverageProcessingTimeMs: Math.Round(averageProcessingTimeMs, 2),
            ClassificationBreakdown: classificationBreakdown);

        _logger.LogInformation(
            "Report for {Date}: {Total} processed, {Success} succeeded, {Failed} failed, avg {AvgMs}ms",
            date.Date.ToString("yyyy-MM-dd"),
            report.TotalProcessed,
            report.SuccessCount,
            report.FailureCount,
            report.AverageProcessingTimeMs);

        return report;
    }

    /// <summary>
    /// Extracts text from a document stream. For text-based content types, reads the stream
    /// directly. For binary content types (PDF, images), generates a placeholder indicating
    /// that a real OCR engine would be used in production.
    /// </summary>
    private static async Task<string> ExtractTextAsync(
        Stream documentStream,
        string contentType,
        CancellationToken ct)
    {
        if (TextBasedContentTypes.Contains(contentType))
        {
            using var reader = new StreamReader(documentStream, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }

        // For non-text content types (PDF, images, etc.), a production system would invoke
        // Azure AI Document Intelligence or a similar OCR service. Here we simulate extraction
        // by reading available bytes and generating a placeholder.
        var buffer = new byte[Math.Min(documentStream.Length, 4096)];
        var bytesRead = await documentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);

        return $"[OCR Placeholder] Extracted text from {contentType} document ({bytesRead} bytes sampled). " +
               "In production, this would use Azure AI Document Intelligence for full text extraction.";
    }
}
