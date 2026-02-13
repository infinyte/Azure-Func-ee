using AzureFunctions.Shared.Models;
using Scenario01.DocumentProcessing.Models;

namespace Scenario01.DocumentProcessing.Services;

/// <summary>
/// Orchestrates the end-to-end document processing pipeline, including
/// OCR text extraction, classification, and report generation.
/// </summary>
public interface IDocumentProcessingService
{
    /// <summary>
    /// Processes a document by extracting text, classifying content, and updating metadata.
    /// </summary>
    /// <param name="documentId">The unique identifier of the document to process.</param>
    /// <param name="documentStream">A stream containing the document content.</param>
    /// <param name="contentType">The MIME content type of the document.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the updated <see cref="DocumentMetadata"/>
    /// on success, or error details on failure.
    /// </returns>
    Task<OperationResult<DocumentMetadata>> ProcessDocumentAsync(
        string documentId,
        Stream documentStream,
        string contentType,
        CancellationToken ct);

    /// <summary>
    /// Generates a daily processing report aggregating statistics for the specified date.
    /// </summary>
    /// <param name="date">The date to generate the report for.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>A <see cref="ProcessingReport"/> containing aggregated processing statistics.</returns>
    Task<ProcessingReport> GenerateReportAsync(DateTimeOffset date, CancellationToken ct);
}
