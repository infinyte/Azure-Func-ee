using System.Text.Json;
using Azure.Storage.Queues;
using AzureFunctions.Shared.Telemetry;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;

namespace Scenario01.DocumentProcessing.Functions;

/// <summary>
/// Blob-triggered function that fires when a new document is uploaded to the documents container.
/// Creates initial metadata in Table Storage and dispatches a processing message to the queue.
/// </summary>
public sealed class ProcessNewDocumentFunction
{
    private readonly IDocumentRepository _repository;
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ITelemetryService _telemetry;
    private readonly DocumentProcessingOptions _options;
    private readonly ILogger<ProcessNewDocumentFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ProcessNewDocumentFunction"/>.
    /// </summary>
    public ProcessNewDocumentFunction(
        IDocumentRepository repository,
        QueueServiceClient queueServiceClient,
        ITelemetryService telemetry,
        IOptions<DocumentProcessingOptions> options,
        ILogger<ProcessNewDocumentFunction> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _queueServiceClient = queueServiceClient ?? throw new ArgumentNullException(nameof(queueServiceClient));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Triggered when a new blob is created in the documents container.
    /// Creates document metadata and enqueues a processing message.
    /// </summary>
    /// <param name="blobContent">The blob content stream.</param>
    /// <param name="name">The blob name extracted from the trigger path.</param>
    /// <param name="context">The function execution context.</param>
    [Function("ProcessNewDocument")]
    public async Task RunAsync(
        [BlobTrigger("documents/{name}", Connection = "AzureWebJobsStorage")] Stream blobContent,
        string name,
        FunctionContext context)
    {
        _logger.LogInformation("New document detected: {BlobName}, Size: {Size} bytes", name, blobContent.Length);

        var documentId = Guid.NewGuid().ToString("D");
        var contentType = InferContentType(name);

        // Create and persist initial metadata.
        var metadata = new DocumentMetadata(documentId)
        {
            FileName = name,
            ContentType = contentType,
            FileSizeBytes = blobContent.Length,
            Status = DocumentStatus.Pending,
            UploadedAt = DateTimeOffset.UtcNow
        };

        await _repository.UpsertAsync(metadata, context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Created metadata for document {DocumentId} from blob {BlobName}",
            documentId, name);

        // Dispatch a processing message to the queue.
        var message = new DocumentProcessingMessage(
            DocumentId: documentId,
            BlobName: name,
            ContainerName: _options.DocumentsContainer,
            ContentType: contentType,
            FileSizeBytes: blobContent.Length);

        var queueClient = _queueServiceClient.GetQueueClient(_options.ProcessingQueue);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: context.CancellationToken).ConfigureAwait(false);

        var messageJson = JsonSerializer.Serialize(message, JsonOptions);
        var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson));
        await queueClient.SendMessageAsync(base64Message, context.CancellationToken).ConfigureAwait(false);

        _telemetry.TrackEvent("DocumentUploaded", new Dictionary<string, string>
        {
            ["DocumentId"] = documentId,
            ["FileName"] = name,
            ["ContentType"] = contentType,
            ["FileSizeBytes"] = blobContent.Length.ToString()
        });

        _logger.LogInformation(
            "Enqueued processing message for document {DocumentId} on queue {QueueName}",
            documentId, _options.ProcessingQueue);
    }

    /// <summary>
    /// Infers the MIME content type from the blob file extension.
    /// </summary>
    private static string InferContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tiff" or ".tif" => "image/tiff",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" or ".htm" => "text/html",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
