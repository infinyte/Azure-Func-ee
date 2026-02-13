using Scenario01.DocumentProcessing.Models;

namespace Scenario01.DocumentProcessing.Tests.TestData;

/// <summary>
/// Builder pattern for constructing <see cref="DocumentMetadata"/> instances in tests.
/// Provides fluent methods for configuring individual properties with sensible defaults.
/// </summary>
public sealed class DocumentTestDataBuilder
{
    private string _id = Guid.NewGuid().ToString("D");
    private string _fileName = "test-document.pdf";
    private string _contentType = "application/pdf";
    private long _fileSizeBytes = 1024;
    private DocumentStatus _status = DocumentStatus.Pending;
    private DocumentClassification _classification = DocumentClassification.Unknown;
    private string? _ocrText;
    private DateTimeOffset _uploadedAt = DateTimeOffset.UtcNow;
    private DateTimeOffset? _processedAt;
    private string? _errorMessage;

    /// <summary>
    /// Creates a builder with sensible default values for a standard document.
    /// </summary>
    public static DocumentTestDataBuilder WithDefaults() => new();

    /// <summary>
    /// Sets the document ID.
    /// </summary>
    public DocumentTestDataBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the file name.
    /// </summary>
    public DocumentTestDataBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    /// <summary>
    /// Sets the content type.
    /// </summary>
    public DocumentTestDataBuilder WithContentType(string contentType)
    {
        _contentType = contentType;
        return this;
    }

    /// <summary>
    /// Sets the file size in bytes.
    /// </summary>
    public DocumentTestDataBuilder WithFileSizeBytes(long fileSizeBytes)
    {
        _fileSizeBytes = fileSizeBytes;
        return this;
    }

    /// <summary>
    /// Sets the document processing status.
    /// </summary>
    public DocumentTestDataBuilder WithStatus(DocumentStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// Sets the document classification.
    /// </summary>
    public DocumentTestDataBuilder WithClassification(DocumentClassification classification)
    {
        _classification = classification;
        return this;
    }

    /// <summary>
    /// Sets the OCR-extracted text.
    /// </summary>
    public DocumentTestDataBuilder WithOcrText(string? ocrText)
    {
        _ocrText = ocrText;
        return this;
    }

    /// <summary>
    /// Sets the uploaded-at timestamp.
    /// </summary>
    public DocumentTestDataBuilder WithUploadedAt(DateTimeOffset uploadedAt)
    {
        _uploadedAt = uploadedAt;
        return this;
    }

    /// <summary>
    /// Sets the processed-at timestamp.
    /// </summary>
    public DocumentTestDataBuilder WithProcessedAt(DateTimeOffset? processedAt)
    {
        _processedAt = processedAt;
        return this;
    }

    /// <summary>
    /// Sets the error message.
    /// </summary>
    public DocumentTestDataBuilder WithErrorMessage(string? errorMessage)
    {
        _errorMessage = errorMessage;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="DocumentMetadata"/> instance.
    /// </summary>
    public DocumentMetadata Build()
    {
        return new DocumentMetadata(_id)
        {
            FileName = _fileName,
            ContentType = _contentType,
            FileSizeBytes = _fileSizeBytes,
            Status = _status,
            Classification = _classification,
            OcrText = _ocrText,
            UploadedAt = _uploadedAt,
            ProcessedAt = _processedAt,
            ErrorMessage = _errorMessage
        };
    }
}
