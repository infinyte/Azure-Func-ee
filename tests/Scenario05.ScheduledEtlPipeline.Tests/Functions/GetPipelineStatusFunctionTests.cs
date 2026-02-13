using System.Net;
using Azure.Core.Serialization;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Scenario05.ScheduledEtlPipeline.Functions;
using Scenario05.ScheduledEtlPipeline.Models;
using Scenario05.ScheduledEtlPipeline.Services;
using Scenario05.ScheduledEtlPipeline.Tests.TestHelpers;
using Xunit;

namespace Scenario05.ScheduledEtlPipeline.Tests.Functions;

public class GetPipelineStatusFunctionTests
{
    private readonly Mock<IPipelineService> _mockPipelineService;
    private readonly GetPipelineStatusFunction _function;

    public GetPipelineStatusFunctionTests()
    {
        _mockPipelineService = new Mock<IPipelineService>();
        var mockLogger = new Mock<ILogger<GetPipelineStatusFunction>>();
        _function = new GetPipelineStatusFunction(_mockPipelineService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_ExistingRun_ReturnsOkWithStatus()
    {
        // Arrange
        var run = new PipelineRunTestDataBuilder()
            .WithRunId("run-123")
            .WithStatus(PipelineStatus.Completed)
            .WithCounts(100, 90, 10, 90)
            .Build();

        _mockPipelineService
            .Setup(s => s.GetRunAsync("run-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);

        var mockRequest = CreateMockRequest();

        // Act
        var response = await _function.RunAsync(mockRequest.Object, "run-123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RunAsync_NonExistentRun_ReturnsNotFound()
    {
        // Arrange
        _mockPipelineService
            .Setup(s => s.GetRunAsync("run-999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineRun?)null);

        var mockRequest = CreateMockRequest();

        // Act
        var response = await _function.RunAsync(mockRequest.Object, "run-999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static Mock<HttpRequestData> CreateMockRequest()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.Configure<WorkerOptions>(options =>
        {
            options.Serializer = new JsonObjectSerializer();
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);

        var mockRequest = new Mock<HttpRequestData>(mockContext.Object);

        mockRequest.Setup(r => r.CreateResponse())
            .Returns(() =>
            {
                var mockResponse = new Mock<HttpResponseData>(mockContext.Object);
                mockResponse.SetupProperty(r => r.StatusCode);
                mockResponse.SetupProperty(r => r.Body, new MemoryStream());
                mockResponse.Setup(r => r.Headers).Returns(new HttpHeadersCollection());
                return mockResponse.Object;
            });

        return mockRequest;
    }
}
