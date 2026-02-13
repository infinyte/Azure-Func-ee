using Azure;
using Azure.Data.Tables;

namespace Scenario01.DocumentProcessing.Models;

/// <summary>
/// Represents metadata for a document stored in Azure Table Storage.
/// Tracks the full lifecycle from upload through processing and classification.
/// </summary>
public sealed class DocumentMetadata : ITableEntity
{
    /// <summary>
    /// The default partition key used for all document metadata entities.
    /// </summary>
    public const string DefaultPartitionKey = "documents";

    /// <summary>
    /// Parameterless constructor required by Azure Table Storage deserialization.
    /// </summary>
    public DocumentMetadata()
    {
        PartitionKey = DefaultPartitionKey;
        RowKey = string.Empty;
    }

    /// <summary>
    /// Creates a new <see cref="DocumentMetadata"/> with the specified document ID.
    /// </summary>
    /// <param name="id">The unique document identifier.</param>
    public DocumentMetadata(string id)
    {
        Id = id;
        PartitionKey = DefaultPartitionKey;
        RowKey = id;
    }

    /// <summary>
    /// The unique identifier for this document.
    /// </summary>
    public string Id
    {
        get => RowKey;
        set => RowKey = value;
    }

    /// <summary>
    /// The original file name of the uploaded document.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The MIME content type of the document (e.g., "application/pdf", "image/png").
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// The current processing status of the document.
    /// Stored as an integer in Table Storage for compatibility.
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>
    /// The classification assigned to the document after content analysis.
    /// Stored as an integer in Table Storage for compatibility.
    /// </summary>
    public DocumentClassification Classification { get; set; } = DocumentClassification.Unknown;

    /// <summary>
    /// The text extracted from the document via OCR or text parsing.
    /// Null if extraction has not yet been performed.
    /// </summary>
    public string? OcrText { get; set; }

    /// <summary>
    /// The timestamp when the document was originally uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The timestamp when document processing completed (successfully or with failure).
    /// Null if processing has not yet completed.
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>
    /// An error message describing why processing failed.
    /// Null if no error has occurred.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // ITableEntity implementation

    /// <inheritdoc />
    public string PartitionKey { get; set; }

    /// <inheritdoc />
    public string RowKey { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc />
    public ETag ETag { get; set; }
}
