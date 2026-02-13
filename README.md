# Azure Functions Portfolio -- Production-Grade Patterns

A portfolio of production-grade Azure Functions implementations built on .NET 8 isolated worker model. Each scenario demonstrates real-world architectural patterns with clean separation of concerns, infrastructure as code, CI/CD automation, and comprehensive testing.

## Highlights

- **Saga orchestration with compensation** -- Durable Functions coordinate multi-step workflows and automatically roll back completed steps on failure
- **Fan-out/fan-in ETL** -- Durable Functions extract from three sources in parallel, then merge, validate, transform, and load sequentially
- **Real-time notifications** -- Azure SignalR Service serverless integration for instant in-app delivery with multi-channel routing
- **Circuit breaker and resilience** -- Polly v8 retry, circuit breaker, and timeout strategies applied to all outbound HTTP calls
- **Dead-letter handling** -- Service Bus messages that fail validation or processing are explicitly dead-lettered with structured reasons
- **Middleware pipeline** -- Correlation ID propagation and centralized exception handling via `IFunctionsWorkerMiddleware`
- **Repository pattern** -- Table Storage and Cosmos DB repositories behind interfaces for testability and swap-ability
- **Managed identity** -- `DefaultAzureCredential` for all Azure service authentication; connection strings used only during local development
- **Blue/green deployment** -- Staging slots with smoke tests before production swap
- **Modular Terraform** -- Reusable modules for function apps, storage, Service Bus, Cosmos DB, SignalR, and networking with private endpoints

## Scenarios

### Scenario 1: Document Processing Pipeline

An event-driven document processing pipeline that ingests files uploaded to Blob Storage, processes them through a queue-based workflow with OCR and classification, persists metadata to Table Storage, and generates daily aggregate reports.

**Triggers:** BlobTrigger, QueueTrigger, TimerTrigger, HTTP
**Key patterns:** Blob-to-queue fan-out, Table Storage repository, content-type inference, daily scheduled reporting
**Source:** [`src/Scenario01.DocumentProcessing/`](src/Scenario01.DocumentProcessing/)

### Scenario 2: Real-Time Notification System

A multi-channel notification system using Azure SignalR Service for real-time in-app delivery, queue-based email routing, Event Grid integration for system events, and user subscription management with daily digest aggregation.

**Triggers:** HTTP, QueueTrigger, EventGridTrigger, TimerTrigger, SignalR (negotiate + output binding)
**Key patterns:** SignalR serverless integration, multi-channel fan-out delivery, queue-based routing, user preference management
**Source:** [`src/Scenario02.RealtimeNotifications/`](src/Scenario02.RealtimeNotifications/)

### Scenario 3: Event-Driven Microservices Orchestration

A distributed order fulfillment system using the saga pattern to coordinate inventory reservation, payment processing, and shipment creation. Failed steps trigger compensating transactions in reverse order. Orders can arrive via Service Bus or the HTTP API.

**Triggers:** ServiceBusTrigger, EventGridTrigger, Durable Functions (Orchestration + Activity), HTTP
**Key patterns:** Saga orchestrator with compensation, Cosmos DB repository, dead-letter routing, event-driven inventory alerts
**Source:** [`src/Scenario03.EventDrivenOrchestration/`](src/Scenario03.EventDrivenOrchestration/)

### Scenario 5: Scheduled ETL Pipeline

A scheduled ETL pipeline using Durable Functions fan-out/fan-in pattern. Extracts data from three sources in parallel (API, CSV, database), merges results, validates against configurable rules, transforms with field mappings, and loads to blob storage.

**Triggers:** TimerTrigger, HTTP, Durable Functions (Orchestration + Activity)
**Key patterns:** Fan-out/fan-in orchestration, multi-stage pipeline (raw → validated → transformed → output), rule-based validation, partial failure tolerance
**Source:** [`src/Scenario05.ScheduledEtlPipeline/`](src/Scenario05.ScheduledEtlPipeline/)

## Project Structure

```
Azure-Func-ee/
├── src/
│   ├── AzureFunctions.Shared/          # Cross-cutting: middleware, telemetry, resilience
│   │   ├── Middleware/                  # CorrelationId, ExceptionHandling
│   │   ├── Resilience/                  # Polly retry, circuit breaker, timeout
│   │   ├── Telemetry/                   # ITelemetryService abstraction
│   │   ├── Models/                      # ErrorResponse, OperationResult
│   │   └── Extensions/                  # ServiceCollection helpers
│   ├── Scenario01.DocumentProcessing/   # Document processing function app
│   │   ├── Functions/                   # ProcessNewDocument, ProcessDocument, GenerateProcessingReport, GetDocumentStatus
│   │   ├── Models/                      # DocumentMetadata, DocumentProcessingMessage, DocumentProcessingOptions
│   │   └── Services/                    # IDocumentRepository, IDocumentProcessingService, IClassificationService
│   ├── Scenario02.RealtimeNotifications/  # Real-time notifications function app
│   │   ├── Functions/                   # Negotiate, SendNotification, ProcessNotification, BroadcastRealtime, SendDigest, ManageSubscriptions, HandleSystemEvent
│   │   ├── Models/                      # Notification, UserSubscription, NotificationOptions
│   │   ├── Repositories/               # INotificationRepository, ISubscriptionRepository
│   │   └── Services/                    # INotificationService, IEmailService, ITemplateService
│   ├── Scenario03.EventDrivenOrchestration/  # Order orchestration function app
│   │   ├── Functions/                   # OrderSagaOrchestrator, ProcessOrder, OrderApi, HandleInventoryEvent
│   │   │   └── Activities/              # ReserveInventory, ProcessPayment, CreateShipment + compensations
│   │   ├── Models/                      # Order, SagaState, OrderResult, OrderProcessingOptions
│   │   │   ├── Dtos/                    # CreateOrderRequest, OrderResponse, OrderItemDto
│   │   │   └── Events/                  # InventoryEvent
│   │   ├── Repositories/               # IOrderRepository, CosmosDbOrderRepository
│   │   └── Services/                    # IOrderService, IInventoryService, IPaymentService
│   └── Scenario05.ScheduledEtlPipeline/  # ETL pipeline function app
│       ├── Functions/                   # ScheduledEtl, TriggerEtl, GetPipelineStatus, EtlOrchestrator
│       │   └── Activities/              # ExtractFromApi, ExtractFromCsv, ExtractFromDatabase, Validate, Transform, Load
│       ├── Models/                      # PipelineRun, DataRecord, ValidationRule, TransformationMapping, EtlOptions
│       ├── Repositories/               # IPipelineRepository, TableStoragePipelineRepository
│       └── Services/                    # IPipelineService, IDataValidator, IDataTransformer, IExternalApiClient
├── tests/
│   ├── AzureFunctions.Shared.Tests/
│   ├── Scenario01.DocumentProcessing.Tests/
│   ├── Scenario02.RealtimeNotifications.Tests/
│   ├── Scenario03.EventDrivenOrchestration.Tests/
│   ├── Scenario05.ScheduledEtlPipeline.Tests/
│   └── Integration.Tests/
├── terraform/
│   ├── modules/
│   │   ├── core-infrastructure/         # Resource group, VNet, Key Vault, Log Analytics
│   │   ├── function-app/                # Reusable function app with slots, diagnostics
│   │   ├── document-processing/         # Blob, queue, table storage + function app
│   │   ├── realtime-notifications/      # SignalR, queues, tables + function app
│   │   ├── event-orchestration/         # Service Bus, Event Grid, Cosmos DB + function app
│   │   └── scheduled-etl-pipeline/      # Blob containers, table, Durable storage + function app
│   └── environments/
│       └── dev/                         # Dev environment composition
├── .github/workflows/
│   ├── build-and-test.yml               # CI: restore, build, test, coverage, publish
│   ├── terraform-plan.yml               # PR: fmt, validate, plan with PR comment
│   └── deploy-functions.yml             # CD: build, terraform apply, slot deploy, swap
├── Azure-Functions-Portfolio.sln
├── Directory.Build.props                # Centralized .NET 8, C# 12, nullable, package versions
└── global.json                          # SDK 8.0.400
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.400 or later)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (local storage emulator)
- [Terraform](https://www.terraform.io/downloads) >= 1.5 (for infrastructure deployment)

### Local Development

```bash
dotnet restore Azure-Functions-Portfolio.sln
dotnet build Azure-Functions-Portfolio.sln
dotnet test Azure-Functions-Portfolio.sln
```

Start Azurite before running functions locally:

```bash
azurite --silent --location .azurite --debug .azurite/debug.log
```

Run a specific scenario:

```bash
cd src/Scenario01.DocumentProcessing
func start
```

## Architecture Principles

- **Isolated worker model** -- All function apps use the .NET 8 isolated worker process, decoupling from the Functions host
- **Managed identity** -- `DefaultAzureCredential` for Azure service auth in deployed environments; connection strings only for Azurite
- **Private endpoints** -- All data-plane access (Blob, Table, Queue, Cosmos DB, Service Bus, Key Vault) secured via private endpoints
- **Middleware pipeline** -- `CorrelationIdMiddleware` for distributed tracing, `ExceptionHandlingMiddleware` for consistent error responses
- **Resilience** -- Polly v8 retry (exponential backoff with jitter), circuit breaker, and timeout on all outbound HTTP clients
- **Event-driven** -- Blob triggers, queue triggers, Service Bus, Event Grid for loose coupling between components
- **Infrastructure as Code** -- Modular Terraform with reusable modules, OIDC-based CI/CD, and remote state

## CI/CD

Three GitHub Actions workflows automate the full lifecycle:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| **Build and Test** | Push/PR to `main` (src/tests changes) | Restore, build, test with coverage, publish artifacts |
| **Terraform Plan** | PR to `main` (terraform changes) | Format check, validate, plan with PR comment |
| **Deploy Functions** | Push to `main` or manual dispatch | Build, Terraform apply, deploy to staging slots, smoke test, swap to production |

## Documentation

- [Architecture Overview](docs/architecture.md) -- System design, data flows, cross-cutting concerns
- [Developer Guide](docs/developer-guide.md) -- Local setup, debugging, testing, coding standards
- [Deployment Guide](docs/deployment-guide.md) -- Terraform setup, deployment steps, blue/green, rollback
- [Terraform Reference](terraform/README.md) -- Module structure, variables, usage

## License

MIT
