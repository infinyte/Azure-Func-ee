# Architecture Overview

## System Overview

This project contains two Azure Functions applications, each deployed as an independent function app with its own infrastructure stack. Both share a common library of cross-cutting concerns.

```
+---------------------------------------------------+
|                 AzureFunctions.Shared              |
|  Middleware | Telemetry | Resilience | Models      |
+----------+--------------------+-------------------+
           |                    |
           v                    v
+---------------------+  +----------------------------+
| Scenario 01         |  | Scenario 03                |
| Document Processing |  | Event-Driven Orchestration |
|                     |  |                            |
| Blob Storage        |  | Service Bus                |
| Queue Storage       |  | Event Grid                 |
| Table Storage       |  | Cosmos DB                  |
|                     |  | Durable Functions          |
+---------------------+  +----------------------------+
           |                    |
           v                    v
+---------------------------------------------------+
|              Terraform Modules                     |
|  core-infrastructure | function-app                |
|  document-processing | event-orchestration         |
+---------------------------------------------------+
```

### Scenario 01: Document Processing Pipeline

Processes documents uploaded to Blob Storage through an asynchronous queue-based pipeline.

**Data flow:**

1. A file is uploaded to the `documents` blob container.
2. `ProcessNewDocument` (BlobTrigger) creates metadata in Table Storage and enqueues a processing message.
3. `ProcessDocument` (QueueTrigger) downloads the blob, extracts text via OCR, classifies the document, and updates metadata.
4. `GetDocumentStatus` (HTTP) provides a REST endpoint for querying document metadata.
5. `GenerateProcessingReport` (TimerTrigger) runs daily at 2:00 AM UTC, aggregates statistics, and tracks metrics in Application Insights.

**Storage:** Azure Table Storage with a single partition key (`documents`) and row key equal to the document ID.

### Scenario 03: Event-Driven Orchestration

Orchestrates order fulfillment using the saga pattern with Durable Functions.

**Data flow:**

1. An order arrives via the HTTP API (`POST /api/orders`) or the Service Bus `orders` queue.
2. The order is persisted to Cosmos DB with status `Processing`.
3. The `OrderSagaOrchestrator` (Durable Functions) executes three steps sequentially:
   - **ReserveInventory** -- Calls the inventory service to reserve stock.
   - **ProcessPayment** -- Calls the payment service to charge the customer.
   - **CreateShipment** -- Creates a shipment and retrieves a tracking ID.
4. If any step fails, compensating activities execute in reverse order:
   - `RefundPayment` (if payment was processed)
   - `CompensateInventory` (if inventory was reserved)
5. `HandleInventoryEvent` (EventGridTrigger) processes inventory domain events and raises low-stock alerts.

**Storage:** Cosmos DB (serverless) with order ID as the partition key (`/id`).

## Cross-Cutting Concerns

### Middleware Pipeline

Both function apps register two middleware components via `IFunctionsWorkerMiddleware`:

| Middleware | Purpose |
|------------|---------|
| `CorrelationIdMiddleware` | Extracts `X-Correlation-ID` from incoming HTTP headers (or generates a new GUID). Stores it in `FunctionContext.Items` and adds it to the outgoing response. |
| `ExceptionHandlingMiddleware` | Catches unhandled exceptions in HTTP functions and returns a consistent JSON `ErrorResponse` with mapped status codes. Non-HTTP triggers re-throw for runtime retry/dead-letter handling. |

**Exception-to-status-code mapping:**

| Exception Type | HTTP Status | Error Code |
|---------------|-------------|------------|
| `ArgumentNullException` | 400 | `ARGUMENT_NULL` |
| `ArgumentException` | 400 | `INVALID_ARGUMENT` |
| `KeyNotFoundException` | 404 | `RESOURCE_NOT_FOUND` |
| `UnauthorizedAccessException` | 403 | `ACCESS_DENIED` |
| `InvalidOperationException` | 409 | `INVALID_OPERATION` |
| `TimeoutException` | 504 | `TIMEOUT` |
| Other | 500 | `INTERNAL_ERROR` |

### Resilience

Outbound HTTP clients use Polly v8 strategies via `Microsoft.Extensions.Http.Resilience`:

| Strategy | Configuration |
|----------|--------------|
| **Retry** | 3 attempts, 2s base delay, exponential backoff with jitter |
| **Circuit Breaker** | Opens after 50% failure ratio in 30s window (min 10 requests), stays open 30s |
| **Timeout** | 30-second per-request timeout |

These are applied to the `InventoryService` and `PaymentService` HTTP clients in Scenario 03 via `AddStandardResilience()`.

### Telemetry

All function apps use Application Insights via `ITelemetryService`:

- **Custom events** -- `DocumentUploaded`, `DailyReportGenerated`, `InventoryEventReceived`, `LowStockAlert`
- **Custom metrics** -- `DailyDocumentsProcessed`, `DailyProcessingSuccessCount`, `InventoryQuantity`
- **Exception tracking** -- Structured exception data with document/order context
- **Correlation** -- `X-Correlation-ID` propagated through the middleware pipeline

## Security Model

### Managed Identity

- **Production:** `DefaultAzureCredential` authenticates to all Azure services (Blob, Table, Queue, Cosmos DB, Service Bus, Key Vault).
- **Local development:** Connection strings are used for Azurite (`UseDevelopmentStorage=true`) and emulators.
- **Terraform:** A user-assigned managed identity is provisioned per function app with least-privilege role assignments (e.g., `Storage Blob Data Contributor`, `Cosmos DB Data Contributor`, `Service Bus Data Sender/Receiver`).

### Key Vault

- RBAC authorization model (no access policies)
- Purge protection enabled
- Private endpoint access only
- Stores connection strings and secrets referenced by function app configuration

### Private Endpoints

All data-plane access is secured via private endpoints and private DNS zones:

| Service | DNS Zone |
|---------|----------|
| Blob Storage | `privatelink.blob.core.windows.net` |
| Table Storage | `privatelink.table.core.windows.net` |
| Queue Storage | `privatelink.queue.core.windows.net` |
| Key Vault | `privatelink.vaultcore.azure.net` |
| Cosmos DB | `privatelink.documents.azure.com` |
| Service Bus | `privatelink.servicebus.windows.net` |

### Network

- VNet with `10.0.0.0/16` address space
- Function subnet for VNet integration
- Private endpoint subnet for data-plane access
- Function apps use VNet integration to route outbound traffic through the VNet

## Technology Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Runtime | .NET | 8.0 (isolated worker) |
| Language | C# | 12.0 |
| Functions Host | Azure Functions | v4 |
| Orchestration | Durable Functions | 1.1.5 |
| Messaging | Azure Service Bus | 7.18.2 |
| Eventing | Azure Event Grid | 4.25.0 |
| Document store | Azure Cosmos DB | 3.43.1 |
| Table storage | Azure.Data.Tables | 12.9.1 |
| Blob storage | Azure.Storage.Blobs | 12.22.2 |
| Queue storage | Azure.Storage.Queues | 12.20.1 |
| Resilience | Polly | 8.4.2 |
| Telemetry | Application Insights | 2.22.0 |
| Infrastructure | Terraform | >= 1.5 |
| CI/CD | GitHub Actions | -- |
| Testing | xUnit + Moq + FluentAssertions | 2.9.2 / 4.20.72 / 6.12.2 |
