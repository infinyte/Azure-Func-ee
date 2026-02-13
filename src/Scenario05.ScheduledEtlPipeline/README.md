# Scenario 05: Scheduled ETL Pipeline

A scheduled ETL (Extract, Transform, Load) pipeline using Durable Functions fan-out/fan-in pattern. Extracts data from three sources in parallel, validates against configurable rules, transforms with field mappings, and loads to blob storage.

## Architecture

```
Timer (daily 1 AM) or HTTP POST /api/etl/trigger
        │
        v
  ScheduledEtlFunction / TriggerEtlFunction
        │
        v
  EtlOrchestratorFunction (Durable)
        │
        ├──fan-out──> ExtractFromApiActivity
        ├──fan-out──> ExtractFromCsvActivity
        └──fan-out──> ExtractFromDatabaseActivity
        │
        v (fan-in: merge results)
        │
        v
  ValidateDataActivity
        │
        v
  TransformDataActivity
        │
        v
  LoadDataActivity ──> etl-output blob container
```

## Functions

| Function | Trigger | Route | Purpose |
|----------|---------|-------|---------|
| ScheduledEtlFunction | Timer | `0 0 1 * * *` (daily 1 AM) | Creates PipelineRun, starts orchestration |
| TriggerEtlFunction | HTTP POST | `/api/etl/trigger` | On-demand trigger, returns 202 with run ID |
| GetPipelineStatusFunction | HTTP GET | `/api/etl/runs/{runId}` | Returns pipeline run status |
| EtlOrchestratorFunction | Orchestration | -- | Fan-out extract, fan-in, validate, transform, load |
| ExtractFromApiActivity | Activity | -- | Fetches from external API (simulated) |
| ExtractFromCsvActivity | Activity | -- | Parses CSV from blob storage |
| ExtractFromDatabaseActivity | Activity | -- | Reads from data source (simulated) |
| ValidateDataActivity | Activity | -- | Applies validation rules (Required, Regex, Range) |
| TransformDataActivity | Activity | -- | Field mapping, normalization, enrichment |
| LoadDataActivity | Activity | -- | Writes JSON to output blob container |

## Key Patterns

- **Fan-out/Fan-in** -- Three extraction activities run in parallel via `Task.WhenAll`, then results are merged
- **Pipeline staging** -- Data flows through etl-raw, etl-validated, etl-transformed, and etl-output containers
- **Rule-based validation** -- Configurable validation rules (Required, Regex, Range) with invalid record partitioning
- **Field transformation** -- Rename, Uppercase, Lowercase, and Default value mappings
- **Partial failure tolerance** -- Pipeline continues if some extraction sources fail

## Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `EtlOptions:RawContainer` | Raw extraction container | `etl-raw` |
| `EtlOptions:ValidatedContainer` | Validated data container | `etl-validated` |
| `EtlOptions:TransformedContainer` | Transformed data container | `etl-transformed` |
| `EtlOptions:OutputContainer` | Final output container | `etl-output` |
| `EtlOptions:PipelineRunsTable` | Pipeline runs table name | `pipelineruns` |
| `EtlOptions:ExternalApiBaseUrl` | External API base URL | `https://api.example.com` |

## Local Development

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

## API Examples

**Trigger ETL pipeline:**

```bash
curl -X POST http://localhost:7071/api/etl/trigger \
  -H "Content-Type: application/json" \
  -d '{"triggerSource":"Manual"}'
```

**Check pipeline status:**

```bash
curl http://localhost:7071/api/etl/runs/run-abc123
```

## Pipeline Statuses

| Status | Description |
|--------|-------------|
| Pending | Run created, orchestration not yet started |
| Extracting | Parallel extraction from sources in progress |
| Validating | Data validation rules being applied |
| Transforming | Field mappings and normalization in progress |
| Loading | Writing results to output blob container |
| Completed | Pipeline finished successfully |
| Failed | Pipeline encountered an unrecoverable error |
