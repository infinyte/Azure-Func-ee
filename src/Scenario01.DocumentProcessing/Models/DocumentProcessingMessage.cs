namespace Scenario01.DocumentProcessing.Models;

/// <summary>
/// Message dispatched to the processing queue when a new document is uploaded.
/// Contains all information needed to locate and process the document.
/// </summary>
/// <param name="DocumentId">The unique identifier for the document.</param>
/// <param name="BlobName">The name of the blob in Azure Storage.</param>
/// <param name="ContainerName">The name of the blob container.</param>
/// <param name="ContentType">The MIME content type of the document.</param>
/// <param name="FileSizeBytes">The file size in bytes.</param>
public record DocumentProcessingMessage(
    string DocumentId,
    string BlobName,
    string ContainerName,
    string ContentType,
    long FileSizeBytes);
