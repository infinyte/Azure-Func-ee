# Scenario 03: Event-Driven Microservices Orchestration

## Overview

A distributed order fulfillment system built on Azure Functions (.NET 8 isolated worker) using the saga pattern with Durable Functions. Orders arrive via Service Bus or the HTTP API, and the saga orchestrator coordinates three sequential steps -- inventory reservation, payment processing, and shipment creation. If any step fails, compensating transactions are executed in reverse order to maintain data consistency across services.

## Architecture

```
                  +-------------------+
                  |   HTTP API        |
                  | POST /api/orders  |       +-------------------+
                  | GET /api/orders/* |       | Azure Service Bus |
                  +--------+----------+       | "orders" queue    |
                           |                  +--------+----------+
                           |                           |
                           v                           v
                  +-------------------+      +-------------------+
                  |  CreateOrder      |      | ProcessOrder      |
                  |  (HTTP trigger)   |      | (SB trigger)      |
                  +--------+----------+      +--------+----------+
                           |                          |
                           +----------+---------------+
                                      |
                                      v
                           +---------------------+
                           | OrderSagaOrchestrator|
                           | (Durable Orchestrator)|
                           +-----+-------+-------+
                                 |       |       |
                    Step 1       |       |       |    Step 3
                    +------------+       |       +------------+
                    v                    v                    v
          +------------------+ +------------------+ +------------------+
          | ReserveInventory | | ProcessPayment   | | CreateShipment   |
          | (Activity)       | | (Activity)       | | (Activity)       |
          +------------------+ +------------------+ +------------------+
                    |                    |
                    v                    v
          +------------------+ +------------------+
          | CompensateInventory| RefundPayment    |
          | (Compensation)   | | (Compensation)   |
          +------------------+ +------------------+

                           +---------------------+
                           | HandleInventoryEvent |
                           | (EventGrid trigger)  |
                           +---------------------+
                                      ^
                                      |
                           +---------------------+
                           | Azure Event Grid    |
                           | Inventory events    |
                           +---------------------+
```

## Saga Compensation Flow

The saga orchestrator executes three steps in sequence. Each step records its outcome in a `SagaState` object so the orchestrator knows which compensating actions to invoke on failure.

```
Happy path:
  ReserveInventory --> ProcessPayment --> CreateShipment --> Completed

Failure at Step 2 (payment fails):
  ReserveInventory --> ProcessPayment (FAIL)
                       |
                       v
                  CompensateInventory  <--  (reverse order)
                       |
                       v
                  Order status: Failed

Failure at Step 3 (shipment fails):
  ReserveInventory --> ProcessPayment --> CreateShipment (FAIL)
                                              |
                                              v
                                         RefundPayment       <-- (reverse order)
                                              |
                                              v
                                         CompensateInventory
                                              |
                                              v
                                         Order status: Failed
```

**Compensation guarantees:**
- Compensating actions are executed in reverse order of the completed steps.
- If a compensation action itself fails, the error is logged with a "manual intervention required" warning. The saga does not retry compensations to avoid infinite loops.
- The `SagaState` tracks `CompletedSteps` and `CompensatedSteps` for full auditability.

## Functions

| Function | Trigger | Route | Description |
|----------|---------|-------|-------------|
| `CreateOrder` | HTTP POST | `api/orders` | Creates an order via HTTP and starts the saga orchestration. Returns 202 Accepted. |
| `GetOrderById` | HTTP GET | `api/orders/{id}` | Retrieves an order by its unique identifier. |
| `GetOrdersByCustomer` | HTTP GET | `api/orders/customer/{customerId}` | Retrieves all orders for a specific customer. |
| `ProcessOrder` | ServiceBusTrigger (`orders`) | -- | Receives an order from Service Bus, persists it, and starts the saga. Uses manual message completion; dead-letters invalid messages. |
| `OrderSagaOrchestrator` | OrchestrationTrigger | -- | Durable orchestrator implementing the saga pattern with three steps and compensation. |
| `ReserveInventory` | ActivityTrigger | -- | Reserves inventory for all order items. |
| `ProcessPayment` | ActivityTrigger | -- | Processes payment for the order total. |
| `CreateShipment` | ActivityTrigger | -- | Creates a shipment and returns a tracking ID. |
| `CompensateInventory` | ActivityTrigger | -- | Compensation: releases a previously made inventory reservation. |
| `RefundPayment` | ActivityTrigger | -- | Compensation: refunds a previously processed payment. |
| `HandleInventoryEvent` | EventGridTrigger | -- | Processes inventory domain events (stock updates, low-stock alerts) and tracks telemetry. |

## Configuration Reference

The `OrderProcessingOptions` class is bound from the `OrderProcessing` configuration section:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceBusConnectionName` | `string` | `ServiceBusConnection` | Configuration key for Service Bus connection |
| `OrdersQueue` | `string` | `orders` | Service Bus queue for inbound orders |
| `OrderFailuresQueue` | `string` | `order-failures` | Queue for dead-lettered/failed orders |
| `CosmosConnectionString` | `string` | `""` | Cosmos DB connection string (empty = use managed identity) |
| `CosmosDatabaseName` | `string` | `orders-db` | Cosmos DB database name |
| `CosmosContainerName` | `string` | `orders` | Cosmos DB container name |
| `SagaTimeoutMinutes` | `int` | `5` | Maximum saga orchestration duration |

### local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "<your-service-bus-connection-string>",
    "OrderProcessing:CosmosConnectionString": "<your-cosmos-connection-string>",
    "OrderProcessing:CosmosDatabaseName": "orders-db",
    "OrderProcessing:CosmosContainerName": "orders",
    "InventoryService:BaseUrl": "https://inventory-api.internal",
    "PaymentService:BaseUrl": "https://payment-api.internal"
  }
}
```

## Sample HTTP Requests

### Create an order

```http
POST /api/orders
Content-Type: application/json

{
  "customerId": "cust-001",
  "items": [
    {
      "productId": "prod-100",
      "productName": "Wireless Headphones",
      "quantity": 2,
      "unitPrice": 49.99
    },
    {
      "productId": "prod-200",
      "productName": "USB-C Cable",
      "quantity": 5,
      "unitPrice": 9.99
    }
  ]
}
```

**202 Accepted:**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "customerId": "cust-001",
  "status": "processing",
  "totalAmount": 149.93,
  "createdAt": "2024-01-15T10:30:00Z",
  "completedAt": null,
  "failureReason": null,
  "items": [
    {
      "productId": "prod-100",
      "productName": "Wireless Headphones",
      "quantity": 2,
      "unitPrice": 49.99
    },
    {
      "productId": "prod-200",
      "productName": "USB-C Cable",
      "quantity": 5,
      "unitPrice": 9.99
    }
  ]
}
```

### Get order by ID

```http
GET /api/orders/{id}
```

**200 OK:**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "customerId": "cust-001",
  "status": "completed",
  "totalAmount": 149.93,
  "createdAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:30:12Z",
  "failureReason": null,
  "items": [...]
}
```

### Get orders by customer

```http
GET /api/orders/customer/{customerId}
```

**200 OK:** Returns an array of `OrderResponse` objects.

## Dead-Letter Handling

The `ProcessOrder` function explicitly dead-letters Service Bus messages in the following cases:

| Reason | Description |
|--------|-------------|
| `DeserializationFailed` | Message body could not be deserialized to `CreateOrderRequest` |
| `InvalidJson` | Message body is not valid JSON |
| `ValidationFailed` | Order request failed validation (e.g., missing required fields) |
| `ProcessingFailed` | Unexpected error during order creation or saga scheduling |

## Models

- **Order** -- Domain entity persisted in Cosmos DB. Contains customer ID, items, status, timestamps, and failure reason. `TotalAmount` is computed from item totals.
- **OrderItem** -- Immutable record for a line item with `ProductId`, `ProductName`, `Quantity`, `UnitPrice`, and computed `TotalPrice`.
- **SagaState** -- Tracks saga progress: completed steps, compensated steps, reservation/transaction/tracking IDs, and failure details.
- **OrderResult** -- Immutable record returned by the orchestrator with success flag, final status, tracking ID, and failure reason.
- **OrderStatus** -- Enum: `Pending`, `Processing`, `InventoryReserved`, `PaymentProcessed`, `Shipped`, `Completed`, `Failed`, `Compensating`, `Cancelled`.

## Local Development

1. Start Azurite for Durable Functions task hub storage:
   ```bash
   azurite --silent --location .azurite --debug .azurite/debug.log
   ```

2. Set up external dependencies (Service Bus namespace and Cosmos DB account) or use the emulator.

3. Create a `local.settings.json` from the template above.

4. Run the function app:
   ```bash
   cd src/Scenario03.EventDrivenOrchestration
   func start
   ```

5. Create a test order:
   ```bash
   curl -X POST http://localhost:7071/api/orders \
     -H "Content-Type: application/json" \
     -d '{"customerId":"cust-001","items":[{"productId":"p1","productName":"Widget","quantity":1,"unitPrice":19.99}]}'
   ```

## Testing

```bash
dotnet test tests/Scenario03.EventDrivenOrchestration.Tests/
```

Unit tests use xUnit with Moq for service dependencies and FluentAssertions for readable assertions.
