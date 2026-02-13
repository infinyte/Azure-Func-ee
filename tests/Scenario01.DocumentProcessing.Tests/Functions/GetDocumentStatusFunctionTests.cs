using System.Net;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario01.DocumentProcessing.Functions;
using Scenario01.DocumentProcessing.Models;
using Scenario01.DocumentProcessing.Services;
using Scenario01.DocumentProcessing.Tests.TestData;
using Xunit;

namespace Scenario01.DocumentProcessing.Tests.Functions;

public class GetDocumentStatusFunctionTests
{
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly Mock<ILogger<GetDocumentStatusFunction>> _mockLogger;
    private readonly GetDocumentStatusFunction _sut;

    public GetDocumentStatusFunctionTests()
    {
        _mockRepository = new Mock<IDocumentRepository>();
        _mockLogger = new Mock<ILogger<GetDocumentStatusFunction>>();

        _sut = new GetDocumentStatusFunction(
            _mockRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_WhenDocumentExists_Returns200WithDocumentMetadata()
    {
        // Arrange
        var documentId = "doc-found";
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .WithStatus(DocumentStatus.Completed)
            .WithFileName("found-doc.pdf")
            .Build();

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var mockRequest = CreateMockHttpRequestData();
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var mockResponse = CreateMockHttpResponseData(mockRequest.Object);
        mockRequest
            .Setup(r => r.CreateResponse())
            .Returns(mockResponse);

        // Act
        var response = await _sut.RunAsync(mockRequest.Object, documentId, mockContext.Object);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RunAsync_WhenDocumentNotFound_Returns404()
    {
        // Arrange
        var documentId = "doc-missing";

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentMetadata?)null);

        var mockRequest = CreateMockHttpRequestData();
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var notFoundResponse = CreateMockHttpResponseData(mockRequest.Object);
        mockRequest
            .Setup(r => r.CreateResponse())
            .Returns(notFoundResponse);

        // Act
        var response = await _sut.RunAsync(mockRequest.Object, documentId, mockContext.Object);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RunAsync_WithEmptyId_Returns400BadRequest()
    {
        // Arrange
        var mockRequest = CreateMockHttpRequestData();
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var badRequestResponse = CreateMockHttpResponseData(mockRequest.Object);
        mockRequest
            .Setup(r => r.CreateResponse())
            .Returns(badRequestResponse);

        // Act
        var response = await _sut.RunAsync(mockRequest.Object, "   ", mockContext.Object);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RunAsync_WhenDocumentFound_VerifiesRepositoryCalledWithCorrectId()
    {
        // Arrange
        var documentId = "doc-verify-id";
        var document = DocumentTestDataBuilder.WithDefaults()
            .WithId(documentId)
            .Build();

        _mockRepository
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var mockRequest = CreateMockHttpRequestData();
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var okResponse = CreateMockHttpResponseData(mockRequest.Object);
        mockRequest
            .Setup(r => r.CreateResponse())
            .Returns(okResponse);

        // Act
        await _sut.RunAsync(mockRequest.Object, documentId, mockContext.Object);

        // Assert
        _mockRepository.Verify(
            r => r.GetAsync(documentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Creates a minimal mock <see cref="HttpRequestData"/> for testing.
    /// </summary>
    private static Mock<HttpRequestData> CreateMockHttpRequestData()
    {
        var mockContext = new Mock<FunctionContext>();
        var mockRequest = new Mock<HttpRequestData>(mockContext.Object);
        mockRequest.Setup(r => r.Url).Returns(new Uri("https://localhost/api/documents/test"));
        mockRequest.Setup(r => r.Headers).Returns(new HttpHeadersCollection());
        return mockRequest;
    }

    /// <summary>
    /// Creates a minimal mock <see cref="HttpResponseData"/> with a settable status code.
    /// The <c>CreateResponse(HttpStatusCode)</c> extension method on <see cref="HttpRequestData"/>
    /// calls the parameterless <c>CreateResponse()</c> and then sets <c>StatusCode</c>,
    /// so the status code property must be writable.
    /// </summary>
    private static HttpResponseData CreateMockHttpResponseData(HttpRequestData request)
    {
        var mockResponse = new Mock<HttpResponseData>(request.FunctionContext);
        mockResponse.SetupProperty(r => r.StatusCode);
        mockResponse.SetupProperty(r => r.Body, new MemoryStream());
        mockResponse.Setup(r => r.Headers).Returns(new HttpHeadersCollection());
        return mockResponse.Object;
    }
}
