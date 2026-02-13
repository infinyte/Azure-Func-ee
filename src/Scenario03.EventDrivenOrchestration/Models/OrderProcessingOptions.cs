namespace Scenario03.EventDrivenOrchestration.Models;

/// <summary>
/// Configuration options for the order processing pipeline, including Service Bus queue names,
/// Cosmos DB connection settings, and saga timeout configuration.
/// </summary>
public class OrderProcessingOptions
{
    /// <summary>
    /// The configuration section name used to bind these options.
    /// </summary>
    public const string SectionName = "OrderProcessing";

    /// <summary>
    /// The name of the Service Bus connection string setting in the Functions host configuration.
    /// </summary>
    public string ServiceBusConnectionName { get; set; } = "ServiceBusConnection";

    /// <summary>
    /// The Service Bus queue name for inbound order messages.
    /// </summary>
    public string OrdersQueue { get; set; } = "orders";

    /// <summary>
    /// The Service Bus queue name for dead-lettered / failed order messages.
    /// </summary>
    public string OrderFailuresQueue { get; set; } = "order-failures";

    /// <summary>
    /// The connection string for the Cosmos DB account used to persist orders.
    /// </summary>
    public string CosmosConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The Cosmos DB database name that contains the orders container.
    /// </summary>
    public string CosmosDatabaseName { get; set; } = "orders-db";

    /// <summary>
    /// The Cosmos DB container name where order documents are stored.
    /// </summary>
    public string CosmosContainerName { get; set; } = "orders";

    /// <summary>
    /// The maximum duration in minutes before a saga orchestration is considered timed out.
    /// </summary>
    public int SagaTimeoutMinutes { get; set; } = 5;
}
