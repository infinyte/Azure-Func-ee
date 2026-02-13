using System.Text.Json;
using Azure.Storage.Blobs;
using AzureFunctions.Shared.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;

namespace Scenario01.DocumentProcessing.Functions;

/// <summary>
/// Queue-triggered function that processes documents dispatched by <see cref="ProcessNewDocumentFunction"/>.
/// Downloads the blob, invokes the processing pipeline, and handles errors gracefully.
/// </summary>
public sealed class ProcessDocumentFunction
{
    private readonly IDocumentProcessingService _processingService;
    private readonly IDocumentRepository _repository;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ITelemetryService _telemetry;
    private readonly ILogger<ProcessDocumentFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ProcessDocumentFunction"/>.
    /// </summary>
    public ProcessDocumentFunction(
        IDocumentProcessingService processingService,
        IDocumentRepository repository,
        BlobServiceClient blobServiceClient,
        ITelemetryService telemetry,
        ILogger<ProcessDocumentFunction> logger)
    {
        _processingService = processingService ?? throw new ArgumentNullException(nameof(processingService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Triggered when a message arrives on the document-processing queue.
    /// Downloads the document blob and runs it through the processing pipeline.
    /// </summary>
    /// <param name="messageText">The raw queue message text (JSON-encoded <see cref="DocumentProcessingMessage"/>).</param>
    /// <param name="context">The function execution context.</param>
    [Function("ProcessDocument")]
    public async Task RunAsync(
        [QueueTrigger("document-processing", Connection = "AzureWebJobsStorage")] string messageText,
        FunctionContext context)
    {
        DocumentProcessingMessage? message;

        try
        {
            message = JsonSerializer.Deserialize<DocumentProcessingMessage>(messageText, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize queue message: {MessageText}", messageText);
            _telemetry.TrackException(ex, new Dictionary<string, string>
            {
                ["RawMessage"] = messageText
            });
            // Do not retry malformed messages; let it go to the poison queue.
            return;
        }

        if (message is null)
        {
            _logger.LogError("Deserialized queue message was null: {MessageText}", messageText);
            return;
        }

        _logger.LogInformation(
            "Processing document {DocumentId} from container {Container}, blob {BlobName}",
            message.DocumentId, message.ContainerName, message.BlobName);

        try
        {
            // Download the blob content.
            var containerClient = _blobServiceClient.GetBlobContainerClient(message.ContainerName);
            var blobClient = containerClient.GetBlobClient(message.BlobName);

            if (!await blobClient.ExistsAsync(context.CancellationToken).ConfigureAwait(false))
            {
                _logger.LogError(
                    "Blob {BlobName} not found in container {Container} for document {DocumentId}",
                    message.BlobName, message.ContainerName, message.DocumentId);

                await MarkDocumentAsFailedAsync(
                    message.DocumentId,
                    $"Blob '{message.BlobName}' not found in container '{message.ContainerName}'.",
                    context.CancellationToken).ConfigureAwait(false);
                return;
            }

            await using var blobStream = await blobClient
                .OpenReadAsync(cancellationToken: context.CancellationToken)
                .ConfigureAwait(false);

            // Process the document through the pipeline.
            var result = await _processingService.ProcessDocumentAsync(
                message.DocumentId,
                blobStream,
                message.ContentType,
                context.CancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Document {DocumentId} processed successfully. Classification: {Classification}",
                    message.DocumentId, result.Data?.Classification);
            }
            else
            {
                _logger.LogWarning(
                    "Document {DocumentId} processing returned failure: {ErrorMessage}",
                    message.DocumentId, result.Error?.Message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Processing cancelled for document {DocumentId}", message.DocumentId);
            throw; // Let the runtime handle retry/dead-letter.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled error processing document {DocumentId}", message.DocumentId);

            await MarkDocumentAsFailedAsync(
                message.DocumentId,
                ex.Message,
                context.CancellationToken).ConfigureAwait(false);

            _telemetry.TrackException(ex, new Dictionary<string, string>
            {
                ["DocumentId"] = message.DocumentId,
                ["BlobName"] = message.BlobName
            });

            throw; // Re-throw so the runtime can retry or dead-letter.
        }
    }

    /// <summary>
    /// Updates the document metadata to reflect a processing failure.
    /// </summary>
    private async Task MarkDocumentAsFailedAsync(
        string documentId,
        string errorMessage,
        CancellationToken ct)
    {
        try
        {
            var document = await _repository.GetAsync(documentId, ct).ConfigureAwait(false);
            if (document is not null)
            {
                document.Status = DocumentStatus.Failed;
                document.ProcessedAt = DateTimeOffset.UtcNow;
                document.ErrorMessage = errorMessage;
                await _repository.UpsertAsync(document, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            // Log but do not throw; the original error is more important.
            _logger.LogError(ex,
                "Failed to update document {DocumentId} status to Failed", documentId);
        }
    }
}
