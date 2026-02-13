using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Test fixture that initializes and manages Azure Storage resources using Azurite
/// (the local Azure Storage emulator). Creates blob containers, queues, and tables
/// needed for integration tests and cleans them up afterward.
/// </summary>
public sealed class AzuriteFixture : IAsyncLifetime
{
    /// <summary>
    /// Connection string for the Azurite local storage emulator.
    /// </summary>
    public const string ConnectionString = "UseDevelopmentStorage=true";

    /// <summary>
    /// The name of the blob container used for document upload tests.
    /// </summary>
    public const string DocumentsContainer = "integration-test-documents";

    /// <summary>
    /// The name of the queue used for document processing message tests.
    /// </summary>
    public const string ProcessingQueue = "integration-test-processing";

    /// <summary>
    /// The name of the table used for document metadata tests.
    /// </summary>
    public const string MetadataTable = "integrationtestmetadata";

    /// <summary>
    /// The Blob Service client connected to Azurite.
    /// </summary>
    public BlobServiceClient BlobServiceClient { get; private set; } = null!;

    /// <summary>
    /// The Queue Service client connected to Azurite.
    /// </summary>
    public QueueServiceClient QueueServiceClient { get; private set; } = null!;

    /// <summary>
    /// The Table Service client connected to Azurite.
    /// </summary>
    public TableServiceClient TableServiceClient { get; private set; } = null!;

    /// <summary>
    /// The blob container client for the documents container.
    /// </summary>
    public BlobContainerClient BlobContainerClient { get; private set; } = null!;

    /// <summary>
    /// The queue client for the processing queue.
    /// </summary>
    public QueueClient QueueClient { get; private set; } = null!;

    /// <summary>
    /// The table client for the metadata table.
    /// </summary>
    public TableClient TableClient { get; private set; } = null!;

    /// <summary>
    /// Initializes the Azurite storage resources: creates blob containers, queues, and tables.
    /// </summary>
    public async Task InitializeAsync()
    {
        BlobServiceClient = new BlobServiceClient(ConnectionString);
        QueueServiceClient = new QueueServiceClient(ConnectionString);
        TableServiceClient = new TableServiceClient(ConnectionString);

        // Create blob container.
        BlobContainerClient = BlobServiceClient.GetBlobContainerClient(DocumentsContainer);
        await BlobContainerClient.CreateIfNotExistsAsync();

        // Create queue.
        QueueClient = QueueServiceClient.GetQueueClient(ProcessingQueue);
        await QueueClient.CreateIfNotExistsAsync();

        // Create table.
        TableClient = TableServiceClient.GetTableClient(MetadataTable);
        await TableClient.CreateIfNotExistsAsync();
    }

    /// <summary>
    /// Cleans up test data by deleting the blob container, queue, and table created during initialization.
    /// </summary>
    public async Task DisposeAsync()
    {
        try
        {
            await BlobContainerClient.DeleteIfExistsAsync();
        }
        catch
        {
            // Swallow cleanup errors — Azurite may not be running.
        }

        try
        {
            await QueueClient.DeleteIfExistsAsync();
        }
        catch
        {
            // Swallow cleanup errors.
        }

        try
        {
            await TableClient.DeleteAsync();
        }
        catch
        {
            // Swallow cleanup errors.
        }
    }
}
