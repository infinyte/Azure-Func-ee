using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AzureFunctions.Shared.Tests.Helpers;

/// <summary>
/// Factory that creates mock <see cref="FunctionContext"/> objects for use in unit tests.
/// Because <see cref="FunctionContext"/> is an abstract class with many internal dependencies,
/// this factory provides a usable stub with configurable Items, ServiceProvider, and FunctionDefinition.
/// </summary>
public static class FunctionContextMockFactory
{
    /// <summary>
    /// Creates a mock <see cref="FunctionContext"/> with a working Items dictionary,
    /// ServiceProvider containing an <see cref="ILoggerFactory"/>, and a named FunctionDefinition.
    /// Also configures a mock <see cref="IInvocationFeatures"/> so that extension methods like
    /// <c>GetHttpRequestDataAsync</c> do not throw a <see cref="NullReferenceException"/>.
    /// </summary>
    /// <param name="functionName">The name to assign to the function definition.</param>
    /// <returns>A mock <see cref="FunctionContext"/> suitable for unit testing.</returns>
    public static Mock<FunctionContext> Create(string functionName = "TestFunction")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var items = new Dictionary<object, object>();

        var functionDefinition = new Mock<FunctionDefinition>();
        functionDefinition.Setup(fd => fd.Name).Returns(functionName);
        functionDefinition.Setup(fd => fd.InputBindings)
            .Returns(ImmutableDictionary<string, BindingMetadata>.Empty);
        functionDefinition.Setup(fd => fd.OutputBindings)
            .Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        var mockFeatures = new Mock<IInvocationFeatures>();

        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);
        mockContext.Setup(c => c.Items).Returns(items);
        mockContext.Setup(c => c.FunctionDefinition).Returns(functionDefinition.Object);
        mockContext.Setup(c => c.InvocationId).Returns(Guid.NewGuid().ToString());
        mockContext.Setup(c => c.FunctionId).Returns(Guid.NewGuid().ToString());
        mockContext.Setup(c => c.Features).Returns(mockFeatures.Object);

        return mockContext;
    }

    /// <summary>
    /// Creates a mock <see cref="FunctionContext"/> with a custom <see cref="IServiceProvider"/>.
    /// Also configures a mock <see cref="IInvocationFeatures"/> so that extension methods like
    /// <c>GetHttpRequestDataAsync</c> do not throw a <see cref="NullReferenceException"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider to use.</param>
    /// <param name="functionName">The name to assign to the function definition.</param>
    /// <returns>A mock <see cref="FunctionContext"/> suitable for unit testing.</returns>
    public static Mock<FunctionContext> Create(IServiceProvider serviceProvider, string functionName = "TestFunction")
    {
        var items = new Dictionary<object, object>();

        var functionDefinition = new Mock<FunctionDefinition>();
        functionDefinition.Setup(fd => fd.Name).Returns(functionName);
        functionDefinition.Setup(fd => fd.InputBindings)
            .Returns(ImmutableDictionary<string, BindingMetadata>.Empty);
        functionDefinition.Setup(fd => fd.OutputBindings)
            .Returns(ImmutableDictionary<string, BindingMetadata>.Empty);

        var mockFeatures = new Mock<IInvocationFeatures>();

        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);
        mockContext.Setup(c => c.Items).Returns(items);
        mockContext.Setup(c => c.FunctionDefinition).Returns(functionDefinition.Object);
        mockContext.Setup(c => c.InvocationId).Returns(Guid.NewGuid().ToString());
        mockContext.Setup(c => c.FunctionId).Returns(Guid.NewGuid().ToString());
        mockContext.Setup(c => c.Features).Returns(mockFeatures.Object);

        return mockContext;
    }
}
