using Scenario01.DocumentProcessing.Models;

namespace Scenario01.DocumentProcessing.Services;

/// <summary>
/// Provides data access operations for <see cref="DocumentMetadata"/> entities
/// persisted in Azure Table Storage.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Retrieves a document metadata entity by its unique identifier.
    /// </summary>
    /// <param name="documentId">The document identifier (RowKey).</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>The document metadata if found; otherwise <c>null</c>.</returns>
    Task<DocumentMetadata?> GetAsync(string documentId, CancellationToken ct);

    /// <summary>
    /// Retrieves all documents that match the specified processing status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>A read-only list of matching document metadata entities.</returns>
    Task<IReadOnlyList<DocumentMetadata>> GetByStatusAsync(DocumentStatus status, CancellationToken ct);

    /// <summary>
    /// Inserts or updates (upserts) a document metadata entity in Table Storage.
    /// </summary>
    /// <param name="document">The document metadata to persist.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    Task UpsertAsync(DocumentMetadata document, CancellationToken ct);

    /// <summary>
    /// Retrieves all documents that were processed (completed or failed) since the specified date.
    /// </summary>
    /// <param name="since">The earliest processed-at timestamp to include.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>A read-only list of matching document metadata entities.</returns>
    Task<IReadOnlyList<DocumentMetadata>> GetProcessedSinceDateAsync(DateTimeOffset since, CancellationToken ct);
}
