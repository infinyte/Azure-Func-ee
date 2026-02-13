using System.Text;
using System.Text.Json;
using Azure.Data.Tables;
using FluentAssertions;
using Scenario01.DocumentProcessing.Models;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Integration tests for the document processing pipeline that exercise the full flow
/// from blob upload through queue dispatch and metadata persistence using Azurite.
/// These tests require the Azurite local storage emulator to be running.
/// </summary>
[Trait("Category", "Integration")]
public class DocumentProcessingPipelineTests : IClassFixture<AzuriteFixture>
{
    private readonly AzuriteFixture _fixture;

    public DocumentProcessingPipelineTests(AzuriteFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UploadBlob_ShouldStoreDocumentInBlobStorage()
    {
        // Arrange
        var blobName = $"test-doc-{Guid.NewGuid():N}.txt";
        var content = "This is a test document for integration testing.";
        var blobClient = _fixture.BlobContainerClient.GetBlobClient(blobName);

        // Act
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true);

        // Assert
        var exists = await blobClient.ExistsAsync();
        exists.Value.Should().BeTrue("the blob should have been uploaded to Azurite");

        // Cleanup
        await blobClient.DeleteIfExistsAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendProcessingMessage_ShouldBeReceivableFromQueue()
    {
        // Arrange
        var message = new DocumentProcessingMessage(
            DocumentId: Guid.NewGuid().ToString("D"),
            BlobName: "test-doc.pdf",
            ContainerName: AzuriteFixture.DocumentsContainer,
            ContentType: "application/pdf",
            FileSizeBytes: 2048);

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        // Act
        await _fixture.QueueClient.SendMessageAsync(base64Message);

        // Assert — receive the message and verify.
        var received = await _fixture.QueueClient.ReceiveMessageAsync();
        received.Value.Should().NotBeNull("a message should have been enqueued");

        var decodedBody = Encoding.UTF8.GetString(Convert.FromBase64String(received.Value.Body.ToString()));
        var receivedMessage = JsonSerializer.Deserialize<DocumentProcessingMessage>(
            decodedBody,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        receivedMessage.Should().NotBeNull();
        receivedMessage!.DocumentId.Should().Be(message.DocumentId);
        receivedMessage.BlobName.Should().Be("test-doc.pdf");

        // Cleanup
        await _fixture.QueueClient.DeleteMessageAsync(received.Value.MessageId, received.Value.PopReceipt);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpsertDocumentMetadata_ShouldPersistAndRetrieveFromTable()
    {
        // Arrange
        var documentId = Guid.NewGuid().ToString("D");
        var metadata = new DocumentMetadata(documentId)
        {
            FileName = "integration-test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 4096,
            Status = DocumentStatus.Pending,
            UploadedAt = DateTimeOffset.UtcNow
        };

        // Act
        await _fixture.TableClient.UpsertEntityAsync(metadata, TableUpdateMode.Replace);

        // Assert
        var response = await _fixture.TableClient.GetEntityAsync<DocumentMetadata>(
            DocumentMetadata.DefaultPartitionKey,
            documentId);

        response.Value.Should().NotBeNull();
        response.Value.FileName.Should().Be("integration-test.pdf");
        response.Value.Status.Should().Be(DocumentStatus.Pending);

        // Cleanup
        await _fixture.TableClient.DeleteEntityAsync(
            DocumentMetadata.DefaultPartitionKey, documentId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullPipeline_UploadBlobAndPersistMetadata_BothSucceed()
    {
        // Arrange
        var documentId = Guid.NewGuid().ToString("D");
        var blobName = $"{documentId}.txt";
        var content = "Integration test: full pipeline document content.";

        // Step 1: Upload blob.
        var blobClient = _fixture.BlobContainerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true);

        // Step 2: Create metadata.
        var metadata = new DocumentMetadata(documentId)
        {
            FileName = blobName,
            ContentType = "text/plain",
            FileSizeBytes = Encoding.UTF8.GetByteCount(content),
            Status = DocumentStatus.Pending,
            UploadedAt = DateTimeOffset.UtcNow
        };
        await _fixture.TableClient.UpsertEntityAsync(metadata, TableUpdateMode.Replace);

        // Step 3: Enqueue processing message.
        var message = new DocumentProcessingMessage(
            DocumentId: documentId,
            BlobName: blobName,
            ContainerName: AzuriteFixture.DocumentsContainer,
            ContentType: "text/plain",
            FileSizeBytes: Encoding.UTF8.GetByteCount(content));

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        await _fixture.QueueClient.SendMessageAsync(base64Message);

        // Assert — all three storage operations succeeded.
        var blobExists = await blobClient.ExistsAsync();
        blobExists.Value.Should().BeTrue();

        var tableEntity = await _fixture.TableClient.GetEntityAsync<DocumentMetadata>(
            DocumentMetadata.DefaultPartitionKey, documentId);
        tableEntity.Value.Should().NotBeNull();
        tableEntity.Value.FileName.Should().Be(blobName);

        var queueMessage = await _fixture.QueueClient.ReceiveMessageAsync();
        queueMessage.Value.Should().NotBeNull();

        // Cleanup
        await blobClient.DeleteIfExistsAsync();
        await _fixture.TableClient.DeleteEntityAsync(DocumentMetadata.DefaultPartitionKey, documentId);
        await _fixture.QueueClient.DeleteMessageAsync(queueMessage.Value.MessageId, queueMessage.Value.PopReceipt);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateDocumentStatus_ShouldReflectNewStatusInTable()
    {
        // Arrange
        var documentId = Guid.NewGuid().ToString("D");
        var metadata = new DocumentMetadata(documentId)
        {
            FileName = "status-update-test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            Status = DocumentStatus.Pending,
            UploadedAt = DateTimeOffset.UtcNow
        };

        await _fixture.TableClient.UpsertEntityAsync(metadata, TableUpdateMode.Replace);

        // Act — update status to Completed.
        metadata.Status = DocumentStatus.Completed;
        metadata.ProcessedAt = DateTimeOffset.UtcNow;
        metadata.Classification = DocumentClassification.Invoice;
        await _fixture.TableClient.UpsertEntityAsync(metadata, TableUpdateMode.Replace);

        // Assert
        var result = await _fixture.TableClient.GetEntityAsync<DocumentMetadata>(
            DocumentMetadata.DefaultPartitionKey, documentId);

        result.Value.Status.Should().Be(DocumentStatus.Completed);
        result.Value.ProcessedAt.Should().NotBeNull();
        result.Value.Classification.Should().Be(DocumentClassification.Invoice);

        // Cleanup
        await _fixture.TableClient.DeleteEntityAsync(DocumentMetadata.DefaultPartitionKey, documentId);
    }
}
