using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;
using Scenario01.DocumentProcessing.Tests.TestData;
using Xunit;

namespace Scenario01.DocumentProcessing.Tests.Services;

public class TableStorageDocumentRepositoryTests
{
    private readonly Mock<TableServiceClient> _mockTableServiceClient;
    private readonly Mock<TableClient> _mockTableClient;
    private readonly IOptions<DocumentProcessingOptions> _options;
    private readonly TableStorageDocumentRepository _sut;

    public TableStorageDocumentRepositoryTests()
    {
        _mockTableServiceClient = new Mock<TableServiceClient>();
        _mockTableClient = new Mock<TableClient>();

        _options = Options.Create(new DocumentProcessingOptions
        {
            TableName = "testdocumentmetadata"
        });

        // Setup the service client to return the mock table client.
        _mockTableServiceClient
            .Setup(s => s.GetTableClient(It.IsAny<string>()))
            .Returns(_mockTableClient.Object);

        // Setup CreateIfNotExistsAsync so table initialization succeeds.
        _mockTableClient
            .Setup(t => t.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<TableItem>>());

        _sut = new TableStorageDocumentRepository(_mockTableServiceClient.Object, _options);
    }

    [Fact]
    public async Task UpsertAsync_StoresDocumentInTable()
    {
        // Arrange
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId("doc-upsert-1")
            .Build();

        _mockTableClient
            .Setup(t => t.UpsertEntityAsync(
                It.IsAny<DocumentMetadata>(),
                TableUpdateMode.Replace,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _sut.UpsertAsync(document, CancellationToken.None);

        // Assert
        _mockTableClient.Verify(
            t => t.UpsertEntityAsync(
                It.Is<DocumentMetadata>(d => d.Id == "doc-upsert-1"),
                TableUpdateMode.Replace,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenDocumentExists_ReturnsDocument()
    {
        // Arrange
        var documentId = "doc-get-1";
        var expected = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .WithFileName("found.pdf")
            .Build();

        var mockResponse = new Mock<Response<DocumentMetadata>>();
        mockResponse.Setup(r => r.Value).Returns(expected);

        _mockTableClient
            .Setup(t => t.GetEntityAsync<DocumentMetadata>(
                DocumentMetadata.DefaultPartitionKey,
                documentId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await _sut.GetAsync(documentId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(documentId);
        result.FileName.Should().Be("found.pdf");
    }

    [Fact]
    public async Task GetAsync_WhenDocumentNotFound_ReturnsNull()
    {
        // Arrange
        var documentId = "doc-not-found";

        _mockTableClient
            .Setup(t => t.GetEntityAsync<DocumentMetadata>(
                DocumentMetadata.DefaultPartitionKey,
                documentId,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not Found"));

        // Act
        var result = await _sut.GetAsync(documentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithNullDocumentId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAsync_WithWhitespaceDocumentId_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetAsync("   ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpsertAsync_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.UpsertAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByStatusAsync_QueriesTableWithCorrectFilter()
    {
        // Arrange
        var mockPage = Page<DocumentMetadata>.FromValues(
            new List<DocumentMetadata>
            {
                DocumentTestDataBuilder.WithDefaults()
                    .WithId("doc-pending-1")
                    .WithStatus(DocumentStatus.Pending)
                    .Build()
            },
            continuationToken: null,
            Mock.Of<Response>());

        var mockPageable = AsyncPageable<DocumentMetadata>.FromPages(new[] { mockPage });

        _mockTableClient
            .Setup(t => t.QueryAsync<DocumentMetadata>(
                It.Is<string>(f => f.Contains("Status eq 0")),
                It.IsAny<int?>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(mockPageable);

        // Act
        var results = await _sut.GetByStatusAsync(DocumentStatus.Pending, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].Id.Should().Be("doc-pending-1");
    }

    [Fact]
    public void Constructor_WithNullTableServiceClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TableStorageDocumentRepository(null!, _options);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tableServiceClient");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TableStorageDocumentRepository(_mockTableServiceClient.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }
}
