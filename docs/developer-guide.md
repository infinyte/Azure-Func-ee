# Developer Guide

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 8.0.400+ | Build and run function apps |
| Azure Functions Core Tools | v4.x | Local function host |
| Azurite | Latest | Local Azure Storage emulator |
| Azure CLI | Latest | Azure resource management |
| Terraform | >= 1.5 | Infrastructure provisioning |
| Visual Studio 2022 or VS Code | Latest | IDE |
| Git | Latest | Source control |

**Optional:**
- Azure Cosmos DB Emulator (for Scenario 03 local development)
- Docker (for running Azurite and Cosmos DB Emulator in containers)

## Local Setup

### 1. Clone the repository

```bash
git clone <repository-url>
cd Azure-Func-ee
```

### 2. Verify the .NET SDK

The repository pins the SDK version in `global.json`:

```json
{
  "sdk": {
    "version": "8.0.400",
    "rollForward": "latestFeature"
  }
}
```

Verify your installed version:

```bash
dotnet --version
```

### 3. Restore and build

```bash
dotnet restore Azure-Functions-Portfolio.sln
dotnet build Azure-Functions-Portfolio.sln
```

### 4. Start Azurite

Azurite is required for local Blob, Queue, and Table Storage emulation. Start it before running any function app:

```bash
azurite --silent --location .azurite --debug .azurite/debug.log
```

Or run via npm:

```bash
npx azurite --silent --location .azurite
```

### 5. Configure local settings

Each function app needs a `local.settings.json` file. These files are git-ignored. Create them from the templates described in each scenario README:

**Scenario 01** (`src/Scenario01.DocumentProcessing/local.settings.json`):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DocumentProcessing:DocumentsContainer": "documents",
    "DocumentProcessing:ProcessingQueue": "document-processing",
    "DocumentProcessing:TableName": "documentmetadata"
  }
}
```

**Scenario 03** (`src/Scenario03.EventDrivenOrchestration/local.settings.json`):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "<your-connection-string>",
    "OrderProcessing:CosmosConnectionString": "<your-connection-string>",
    "OrderProcessing:CosmosDatabaseName": "orders-db",
    "OrderProcessing:CosmosContainerName": "orders"
  }
}
```

### 6. Run a function app

```bash
cd src/Scenario01.DocumentProcessing
func start
```

## Debugging

### Visual Studio 2022

1. Open `Azure-Functions-Portfolio.sln`.
2. Right-click the desired scenario project and select **Set as Startup Project**.
3. Press **F5** to start debugging. Visual Studio will launch the function host automatically.
4. Set breakpoints in function classes, middleware, or services as needed.

### VS Code

1. Open the repository root folder in VS Code.
2. Install the Azure Functions extension (`ms-azuretools.vscode-azurefunctions`).
3. Open `.vscode/launch.json` (create if needed) with a configuration for the target scenario:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Scenario01.DocumentProcessing",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "func: host start (Scenario01)",
      "program": "${workspaceFolder}/src/Scenario01.DocumentProcessing/bin/Debug/net8.0/Scenario01.DocumentProcessing.dll",
      "cwd": "${workspaceFolder}/src/Scenario01.DocumentProcessing"
    }
  ]
}
```

4. Set breakpoints and press **F5**.

### Useful debugging tools

- **Azure Storage Explorer** -- Browse and manage Azurite containers, queues, and tables.
- **Cosmos DB Explorer** -- View and query documents in the Cosmos DB emulator.
- **Service Bus Explorer** -- Inspect messages in Service Bus queues (e.g., [ServiceBusExplorer](https://github.com/paolosalvatori/ServiceBusExplorer)).

## Testing Strategy

### Test projects

| Project | Scope | Dependencies |
|---------|-------|-------------|
| `AzureFunctions.Shared.Tests` | Shared middleware, telemetry, resilience | Moq, FluentAssertions |
| `Scenario01.DocumentProcessing.Tests` | Document processing functions and services | Moq, FluentAssertions |
| `Scenario03.EventDrivenOrchestration.Tests` | Order orchestration functions, services, saga | Moq, FluentAssertions |
| `Integration.Tests` | End-to-end flows against Azurite | Azurite, FluentAssertions |

### Running tests

Run all tests:

```bash
dotnet test Azure-Functions-Portfolio.sln
```

Run with coverage:

```bash
dotnet test Azure-Functions-Portfolio.sln --collect:"XPlat Code Coverage" --results-directory ./coverage
```

Run a specific test project:

```bash
dotnet test tests/Scenario01.DocumentProcessing.Tests/
```

### Unit testing approach

- **Framework:** xUnit 2.9.2
- **Mocking:** Moq 4.20.72 for interface dependencies (repositories, services, loggers)
- **Assertions:** FluentAssertions 6.12.2 for readable, expressive assertions
- **Coverage:** Coverlet 6.0.2 for cross-platform code coverage collection

**Example test pattern:**

```csharp
public class ProcessNewDocumentFunctionTests
{
    private readonly Mock<IDocumentRepository> _repositoryMock = new();
    private readonly Mock<QueueServiceClient> _queueServiceClientMock = new();
    private readonly Mock<ITelemetryService> _telemetryMock = new();
    private readonly Mock<ILogger<ProcessNewDocumentFunction>> _loggerMock = new();

    [Fact]
    public async Task RunAsync_ShouldCreateMetadataAndEnqueueMessage()
    {
        // Arrange
        var function = new ProcessNewDocumentFunction(
            _repositoryMock.Object,
            _queueServiceClientMock.Object,
            _telemetryMock.Object,
            Options.Create(new DocumentProcessingOptions()),
            _loggerMock.Object);

        // Act
        await function.RunAsync(stream, "test.pdf", context);

        // Assert
        _repositoryMock.Verify(r => r.UpsertAsync(
            It.Is<DocumentMetadata>(m => m.FileName == "test.pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration tests

Integration tests in `Integration.Tests` run against Azurite. They verify:

- Blob upload triggers are processed correctly
- Queue messages are dispatched and consumed
- Table Storage entities are created and queryable
- End-to-end data flows produce expected results

Start Azurite before running integration tests.

## Coding Standards

### C# conventions

The `Directory.Build.props` enforces these settings across all projects:

| Setting | Value | Purpose |
|---------|-------|---------|
| `TargetFramework` | `net8.0` | .NET 8 LTS |
| `LangVersion` | `12.0` | C# 12 features |
| `Nullable` | `enable` | Nullable reference types |
| `ImplicitUsings` | `enable` | Auto-imported namespaces |
| `TreatWarningsAsErrors` | `true` | All warnings are build errors |
| `AnalysisLevel` | `latest-recommended` | Latest Roslyn analyzers |

### Style guidelines

- **File-scoped namespaces** -- Use `namespace Foo.Bar;` instead of block-scoped.
- **Records for immutable types** -- Use `record` for DTOs, messages, and value objects (e.g., `DocumentProcessingMessage`, `OrderResult`, `CreateOrderRequest`).
- **Constructor injection** -- All functions and services receive dependencies via constructor injection with null guards.
- **Async/await** -- Use `ConfigureAwait(false)` on all awaited calls in library code. Suffix async methods with `Async`.
- **Sealed classes** -- Mark classes as `sealed` when they are not designed for inheritance.
- **XML documentation** -- All public types and members have `<summary>` documentation comments.
- **JSON serialization** -- Use `System.Text.Json` with `JsonNamingPolicy.CamelCase`. Never use `Newtonsoft.Json`.

### Project organization

Each function app follows this layout:

```
Scenario.Name/
├── Functions/          # Function classes (one per file)
│   └── Activities/     # Durable Functions activities (if applicable)
├── Models/             # Domain models, options, enums
│   ├── Dtos/           # Data transfer objects for API boundaries
│   └── Events/         # Event-specific models
├── Repositories/       # Data access interfaces and implementations
├── Services/           # Business logic interfaces and implementations
└── Program.cs          # Host builder, DI registration, middleware
```

## Adding a New Scenario

1. Create a new project directory under `src/` following the naming convention `ScenarioNN.ShortName`.
2. Add a new `.csproj` file referencing `AzureFunctions.Shared`:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\AzureFunctions.Shared\AzureFunctions.Shared.csproj" />
   </ItemGroup>
   ```
3. Configure `Program.cs` with middleware registration and DI:
   ```csharp
   var host = new HostBuilder()
       .ConfigureFunctionsWebApplication(builder =>
       {
           builder.UseMiddleware<CorrelationIdMiddleware>();
           builder.UseMiddleware<ExceptionHandlingMiddleware>();
       })
       .ConfigureServices((context, services) =>
       {
           services.AddSharedServices();
           // Register scenario-specific services...
       })
       .Build();
   host.Run();
   ```
4. Add a corresponding test project under `tests/` named `ScenarioNN.ShortName.Tests`.
5. Add both projects to `Azure-Functions-Portfolio.sln`.
6. Create a Terraform module under `terraform/modules/` for scenario-specific infrastructure.
7. Wire the module into the environment composition under `terraform/environments/dev/main.tf`.
8. Add a `README.md` in the scenario project directory documenting functions, configuration, and API endpoints.
9. Update the root `README.md` with a summary of the new scenario.
