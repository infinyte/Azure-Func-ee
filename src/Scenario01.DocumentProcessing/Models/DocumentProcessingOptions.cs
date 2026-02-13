namespace Scenario01.DocumentProcessing.Models;

/// <summary>
/// Configuration options for the document processing pipeline.
/// Bound from the "DocumentProcessing" configuration section.
/// </summary>
public sealed class DocumentProcessingOptions
{
    /// <summary>
    /// The configuration key name for the Azure Storage connection.
    /// </summary>
    public string StorageConnectionName { get; set; } = "AzureWebJobsStorage";

    /// <summary>
    /// The blob container name where uploaded documents are stored.
    /// </summary>
    public string DocumentsContainer { get; set; } = "documents";

    /// <summary>
    /// The queue name used to dispatch document processing messages.
    /// </summary>
    public string ProcessingQueue { get; set; } = "document-processing";

    /// <summary>
    /// The Azure Table Storage table name for document metadata.
    /// </summary>
    public string TableName { get; set; } = "documentmetadata";

    /// <summary>
    /// The maximum number of retry attempts for processing a single document.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// The maximum time in seconds allowed for processing a single document before timeout.
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; } = 120;
}
