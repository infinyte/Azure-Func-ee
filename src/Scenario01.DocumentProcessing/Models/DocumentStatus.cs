namespace Scenario01.DocumentProcessing.Models;

/// <summary>
/// Represents the current processing state of a document in the pipeline.
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// The document has been uploaded and is awaiting processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The document is currently being processed (OCR, classification, etc.).
    /// </summary>
    Processing = 1,

    /// <summary>
    /// The document has been fully processed without errors.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The document processing encountered an error.
    /// </summary>
    Failed = 3
}
