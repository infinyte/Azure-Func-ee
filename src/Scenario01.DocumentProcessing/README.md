# Scenario 01: Document Processing Pipeline

## Overview

A serverless document processing pipeline built on Azure Functions (.NET 8 isolated worker). When a file is uploaded to Blob Storage, the pipeline automatically creates metadata, enqueues a processing message, downloads and processes the document through OCR and classification, and persists results to Azure Table Storage. A daily timer function aggregates processing statistics for operational reporting.

## Architecture

```
                                    +-----------------------+
                                    |   Azure Blob Storage  |
                                    |   "documents" container|
                                    +----------+------------+
                                               |
                                          BlobTrigger
                                               |
                                               v
                                   +------------------------+
                                   | ProcessNewDocument     |
                                   | - Creates metadata     |
                                   | - Infers content type  |
                                   | - Enqueues message     |
                                   +----------+-------------+
                                              |
                              Queue: "document-processing"
                                              |
                                              v
                                   +------------------------+
                                   | ProcessDocument        |
                                   | - Downloads blob       |
                                   | - Runs OCR/extraction  |
                                   | - Classifies content   |
                                   | - Updates metadata     |
                                   +----------+-------------+
                                              |
                                              v
                                   +------------------------+
                                   | Azure Table Storage    |
                                   | "documentmetadata"     |
                                   +------------------------+
                                              ^
                                              |
                     +------------------------+------------------------+
                     |                                                 |
          +----------+-----------+                        +------------+----------+
          | GetDocumentStatus    |                        | GenerateProcessingReport|
          | GET /api/documents/{id}|                      | Timer: daily 2:00 AM  |
          +----------------------+                        +------------------------+
```

## Functions

| Function | Trigger | Route | Description |
|----------|---------|-------|-------------|
| `ProcessNewDocument` | BlobTrigger (`documents/{name}`) | -- | Fires on blob upload. Creates `DocumentMetadata` in Table Storage and enqueues a `DocumentProcessingMessage`. |
| `ProcessDocument` | QueueTrigger (`document-processing`) | -- | Downloads the blob, runs the processing pipeline (OCR, classification), updates metadata with results or failure status. |
| `GenerateProcessingReport` | TimerTrigger (`0 0 2 * * *`) | -- | Runs daily at 2:00 AM UTC. Aggregates processing statistics for the previous day and tracks metrics in Application Insights. |
| `GetDocumentStatus` | HTTP GET | `api/documents/{id}` | Returns the current metadata and processing status for a document by ID. |

## Configuration Reference

The `DocumentProcessingOptions` class is bound from the `DocumentProcessing` configuration section:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StorageConnectionName` | `string` | `AzureWebJobsStorage` | Configuration key for the storage connection |
| `DocumentsContainer` | `string` | `documents` | Blob container for uploaded documents |
| `ProcessingQueue` | `string` | `document-processing` | Queue name for processing messages |
| `TableName` | `string` | `documentmetadata` | Table Storage table for document metadata |
| `MaxRetryAttempts` | `int` | `3` | Max retry attempts per document |
| `ProcessingTimeoutSeconds` | `int` | `120` | Timeout in seconds for a single document |

### local.settings.json

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

## Sample HTTP Requests

### Get document status

```http
GET /api/documents/{id}
```

**200 OK** -- Document found:

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "fileName": "invoice-2024.pdf",
  "contentType": "application/pdf",
  "fileSizeBytes": 245760,
  "status": "completed",
  "classification": "invoice",
  "ocrText": "Invoice #12345...",
  "uploadedAt": "2024-01-15T10:30:00Z",
  "processedAt": "2024-01-15T10:30:45Z",
  "errorMessage": null
}
```

**404 Not Found** -- Document not found:

```json
{
  "message": "Document 'abc123' was not found.",
  "errorCode": "DOCUMENT_NOT_FOUND"
}
```

## Models

- **DocumentMetadata** -- Azure Table Storage entity (`ITableEntity`) tracking the full document lifecycle: file name, content type, size, status, classification, OCR text, timestamps, and error details.
- **DocumentProcessingMessage** -- Immutable record dispatched to the processing queue containing the document ID, blob name, container, content type, and file size.
- **ProcessingReport** -- Aggregated daily statistics: total processed, success/failure counts, average processing time, classification breakdown.
- **DocumentStatus** -- Enum: `Pending`, `Processing`, `Completed`, `Failed`.
- **DocumentClassification** -- Enum: `Unknown`, `Invoice`, `Receipt`, `Contract`, `Report`, `Correspondence`.

## Services

- **IDocumentRepository / TableStorageDocumentRepository** -- Data access layer for `DocumentMetadata` in Azure Table Storage. Supports get by ID, get by status, upsert, and date-range queries. Uses lazy table initialization.
- **IDocumentProcessingService / DocumentProcessingService** -- Orchestrates document processing: text extraction, classification, metadata updates, and report generation.
- **IClassificationService / SimpleClassificationService** -- Classifies documents into categories based on extracted content.

## Local Development

1. Start Azurite:
   ```bash
   azurite --silent --location .azurite --debug .azurite/debug.log
   ```

2. Create a `local.settings.json` from the template above.

3. Run the function app:
   ```bash
   cd src/Scenario01.DocumentProcessing
   func start
   ```

4. Upload a file to the `documents` container in Azurite to trigger processing. You can use Azure Storage Explorer or the Azure CLI:
   ```bash
   az storage blob upload \
     --connection-string "UseDevelopmentStorage=true" \
     --container-name documents \
     --name test-invoice.pdf \
     --file ./test-files/test-invoice.pdf
   ```

5. Check processing status:
   ```bash
   curl http://localhost:7071/api/documents/{documentId}
   ```

## Testing

```bash
dotnet test tests/Scenario01.DocumentProcessing.Tests/
```

Unit tests use xUnit with Moq for service dependencies and FluentAssertions for readable assertions.
