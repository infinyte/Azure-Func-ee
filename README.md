# Innovative Azure Functions Projects

## 1. Real-Time Document Processing Pipeline

### Overview
A serverless document processing system that automatically processes documents uploaded to blob storage, extracts text using Cognitive Services, classifies documents, and makes them searchable.

### Architecture Components
- **Blob Trigger Function**: Activates when new documents are uploaded
- **Queue Trigger Function**: Manages processing backlog
- **Timer Function**: Generates regular reports on processing statistics
- **HTTP Trigger Function**: Provides API access to processed document data
- **Azure Cognitive Services**: For OCR and text analysis
- **Azure Cosmos DB**: To store document metadata and extracted information

### Implementation Highlights
```csharp
// BlobTrigger function that processes new document uploads
public static class DocumentProcessor
{
    [FunctionName("ProcessNewDocument")]
    public static async Task Run(
        [BlobTrigger("documents/{name}", Connection = "StorageConnection")] Stream document,
        string name,
        [Queue("document-processing")] IAsyncCollector<DocumentProcessingMessage> outputQueue,
        ILogger log)
    {
        log.LogInformation($"Processing document: {name}");
        
        // Create a processing message
        var message = new DocumentProcessingMessage
        {
            DocumentId = Guid.NewGuid().ToString(),
            DocumentName = name,
            UploadTime = DateTime.UtcNow,
            Status = "Queued"
        };
        
        // Add to processing queue
        await outputQueue.AddAsync(message);
        
        log.LogInformation($"Document {name} queued for processing with ID: {message.DocumentId}");
    }
}
```

### Benefits
This architecture leverages Azure Functions' event-driven nature to create a scalable, cost-effective document processing system that only runs when needed. It's perfect for organizations that need to process variable volumes of documents without maintaining constant infrastructure.

## 2. IoT Device Management Platform

### Overview
A comprehensive IoT platform that receives telemetry from devices through IoT Hub, processes data streams in real-time, triggers alerts, and provides a management API.

### Architecture Components
- **Event Hub Trigger Functions**: Process device telemetry streams
- **Timer Functions**: Run device health checks and aggregate statistics
- **HTTP Trigger Functions**: Management API for device configuration
- **Durable Functions**: Orchestrate complex device provisioning workflows
- **SignalR Service Bindings**: Push real-time alerts to dashboards
- **Azure Cosmos DB**: Store device information and processed telemetry
- **Azure IoT Hub**: Connect and manage IoT devices

### Implementation Highlights
```python
# Event Hub trigger function that processes device telemetry
import logging
import json
import azure.functions as func
import numpy as np

def main(event: func.EventHubEvent, doc: func.Out[func.Document], signalr: func.Out[str]):
    logging.info('Python Event Hub trigger function processed an event')
    
    # Parse the telemetry data
    body = event.get_body().decode('utf-8')
    telemetry = json.loads(body)
    device_id = telemetry.get('deviceId')
    
    # Process anomaly detection on temperature readings
    if 'temperature' in telemetry:
        # Simple anomaly detection - in reality, this could be more sophisticated
        if telemetry['temperature'] > 30:  # Example threshold
            alert = {
                'deviceId': device_id,
                'type': 'HighTemperature',
                'value': telemetry['temperature'],
                'timestamp': telemetry['timestamp'],
                'message': f"High temperature detected: {telemetry['temperature']}°C"
            }
            
            # Send real-time alert via SignalR
            signalr.set(json.dumps({
                'target': 'newAlert',
                'arguments': [alert]
            }))
            
            logging.info(f"Alert triggered for device {device_id}")
    
    # Store processed telemetry in Cosmos DB
    doc.set(func.Document.from_dict({
        'id': f"{device_id}-{telemetry.get('timestamp')}",
        'deviceId': device_id,
        'processedTelemetry': telemetry,
        'processedTimestamp': datetime.datetime.utcnow().isoformat()
    }))
```

### Benefits
This solution takes advantage of Azure Functions' native integration with IoT Hub and event processing capabilities to create a responsive and scalable IoT backend. The serverless architecture handles variable IoT traffic efficiently while keeping costs proportional to actual usage.

## 3. Event-Driven Microservices Orchestration

### Overview
A set of interconnected microservices that communicate through events, using Azure Functions as the processing units and event handlers.

### Architecture Components
- **Event Grid Trigger Functions**: React to system events
- **Queue Trigger Functions**: Handle asynchronous processing tasks
- **Durable Functions**: Manage long-running workflows and transactions
- **HTTP Trigger Functions**: Provide API endpoints for services
- **Azure Service Bus**: Message broker for reliable service communication
- **Azure Event Grid**: Event routing between services
- **Azure Cosmos DB**: Consistent data storage across services

### Implementation Highlights
```javascript
// Node.js implementation of an Order Processing function
module.exports = async function (context, orderMessage) {
    context.log('Processing new order', orderMessage.orderId);
    
    try {
        // Validate order data
        if (!orderMessage.items || orderMessage.items.length === 0) {
            throw new Error('Order contains no items');
        }
        
        // Process the order (in reality, this would be more complex)
        const processedOrder = {
            id: orderMessage.orderId,
            customer: orderMessage.customerId,
            items: orderMessage.items,
            total: orderMessage.items.reduce((sum, item) => sum + (item.price * item.quantity), 0),
            status: 'processing',
            processedAt: new Date().toISOString()
        };
        
        // Store the order in Cosmos DB
        context.bindings.orderDocument = JSON.stringify(processedOrder);
        
        // Publish event for inventory service
        context.bindings.inventoryMessage = {
            orderId: orderMessage.orderId,
            items: orderMessage.items,
            action: 'reserve'
        };
        
        context.log(`Order ${orderMessage.orderId} processed successfully`);
        
    } catch (error) {
        context.log.error(`Error processing order ${orderMessage.orderId}: ${error.message}`);
        
        // Publish failure event
        context.bindings.failureMessage = {
            orderId: orderMessage.orderId,
            reason: error.message,
            timestamp: new Date().toISOString()
        };
    }
};
```

### Benefits
This architecture leverages the event-driven nature of Azure Functions to create loosely coupled microservices that can evolve independently. The serverless approach simplifies deployment and scaling of individual components while maintaining system resilience through asynchronous communication.

## 4. Intelligent Content Moderation System

### Overview
A content moderation system for user-generated content that uses AI to detect inappropriate content and human reviewers for edge cases.

### Architecture Components
- **HTTP Trigger Functions**: Receive content for moderation
- **Queue Trigger Functions**: Process moderation backlog
- **Durable Functions**: Orchestrate human review workflows
- **Azure Cognitive Services**: AI-based content analysis
- **Azure Storage Tables**: Track moderation decisions and history
- **Azure Cosmos DB**: Store moderation policies and content metadata

### Implementation Highlights
```csharp
// HTTP trigger function to submit content for moderation
[FunctionName("SubmitContent")]
public static async Task<IActionResult> SubmitContent(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
    [Queue("content-moderation")] IAsyncCollector<ModerationRequest> moderationQueue,
    ILogger log)
{
    log.LogInformation("Content submission received");
    
    // Read request body
    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    var content = JsonConvert.DeserializeObject<ContentSubmission>(requestBody);
    
    if (content == null || string.IsNullOrEmpty(content.Text))
    {
        return new BadRequestObjectResult("Please provide content to moderate");
    }
    
    // Create moderation request
    var moderationRequest = new ModerationRequest
    {
        ContentId = Guid.NewGuid().ToString(),
        ContentType = "text",
        Content = content.Text,
        SubmittedBy = content.UserId,
        SubmissionTime = DateTime.UtcNow
    };
    
    // Queue for processing
    await moderationQueue.AddAsync(moderationRequest);
    
    return new OkObjectResult(new { 
        moderationId = moderationRequest.ContentId,
        status = "submitted"
    });
}

// Queue trigger function to perform AI-based moderation
[FunctionName("ModerateContent")]
public static async Task ModerateContent(
    [QueueTrigger("content-moderation")] ModerationRequest request,
    [Queue("human-review")] IAsyncCollector<ModerationRequest> humanReviewQueue,
    [Table("moderation", "Results")] IAsyncCollector<ModerationResult> resultTable,
    [CosmosDB(
        databaseName: "ContentDb",
        collectionName: "Moderation",
        ConnectionStringSetting = "CosmosDbConnection")] IAsyncCollector<object> moderationHistory,
    ILogger log)
{
    log.LogInformation($"Processing moderation request: {request.ContentId}");
    
    // Call Azure Content Moderator API (simplified for example)
    var moderationResult = await CallContentModeratorApi(request.Content);
    
    var result = new ModerationResult
    {
        PartitionKey = "Results",
        RowKey = request.ContentId,
        ContentType = request.ContentType,
        IsApproved = !moderationResult.HasIssues,
        ConfidenceScore = moderationResult.ConfidenceScore,
        ModeratedAt = DateTime.UtcNow
    };
    
    // If AI is uncertain, send for human review
    if (moderationResult.ConfidenceScore < 0.8 && moderationResult.ConfidenceScore > 0.3)
    {
        log.LogInformation($"Content {request.ContentId} sent for human review");
        request.AiScore = moderationResult.ConfidenceScore;
        await humanReviewQueue.AddAsync(request);
        result.Status = "PendingHumanReview";
    }
    else
    {
        result.Status = "Complete";
    }
    
    // Store result
    await resultTable.AddAsync(result);
    
    // Add to moderation history
    await moderationHistory.AddAsync(new {
        id = request.ContentId,
        content = request.Content.Substring(0, Math.Min(100, request.Content.Length)),
        moderationResult = result,
        timestamp = DateTime.UtcNow
    });
}
```

### Benefits
This system combines AI-powered moderation with human oversight to create a scalable content moderation solution. Azure Functions enable the system to handle variable content submission rates while maintaining cost efficiency.

## 5. Scheduled Data Pipeline for Business Intelligence

### Overview
A data processing pipeline that extracts data from various sources, transforms it according to business rules, and loads it into a data warehouse for business intelligence and reporting.

### Architecture Components
- **Timer Trigger Functions**: Schedule and initiate ETL processes
- **Activity Functions**: Perform individual data processing tasks
- **Orchestrator Functions**: Coordinate the overall ETL workflow
- **HTTP Trigger Functions**: Manual control and monitoring API
- **Azure Data Factory**: For complex data transformations
- **Azure SQL Database**: Destination data warehouse
- **Azure Storage**: Intermediate data storage

### Implementation Highlights
```python
# Timer-triggered orchestrator function that manages the ETL process
import datetime
import logging
import azure.functions as func
import azure.durable_functions as df

def orchestrator_function(context: df.DurableOrchestrationContext):
    """Orchestrates the ETL pipeline execution."""
    
    # Get the current date for processing
    current_date = context.current_utc_datetime.date().isoformat()
    
    # Step 1: Extract data from various sources in parallel
    extraction_tasks = []
    data_sources = ["sales", "inventory", "customers", "products"]
    
    for source in data_sources:
        extraction_tasks.append(context.call_activity("ExtractData", {
            "source": source,
            "date": current_date
        }))
    
    # Wait for all extraction tasks to complete
    extraction_results = yield context.task_all(extraction_tasks)
    
    # Step 2: Transform the data
    transform_tasks = []
    for i, source in enumerate(data_sources):
        if extraction_results[i]["success"]:
            transform_tasks.append(context.call_activity("TransformData", {
                "source": source,
                "blob_path": extraction_results[i]["blob_path"],
                "date": current_date
            }))
    
    # Wait for all transformation tasks to complete
    transform_results = yield context.task_all(transform_tasks)
    
    # Step 3: Load data into the data warehouse
    load_results = yield context.call_activity("LoadDataWarehouse", {
        "transformed_data": [r["output_path"] for r in transform_results if r["success"]],
        "date": current_date
    })
    
    # Step 4: Generate reports
    if load_results["success"]:
        yield context.call_activity("GenerateReports", {
            "date": current_date
        })
    
    return {
        "pipeline_id": context.instance_id,
        "execution_date": current_date,
        "status": "completed",
        "extraction_success": sum(1 for r in extraction_results if r["success"]),
        "transform_success": sum(1 for r in transform_results if r["success"]),
        "load_success": load_results["success"]
    }

main = df.Orchestrator.create(orchestrator_function)
```

### Benefits
This solution leverages Azure Functions' scheduling capabilities and the Durable Functions extension to create a reliable, orchestrated data pipeline. The serverless approach eliminates the need to maintain dedicated ETL servers while providing the necessary compute power when needed.

## 6. Serverless API for Legacy System Integration

### Overview
A modern API layer built with Azure Functions that sits in front of legacy systems, providing standardized API access while handling authentication, rate limiting, and data transformation.

### Architecture Components
- **HTTP Trigger Functions**: Modern API endpoints
- **Azure API Management**: API governance and developer portal
- **Azure Key Vault**: Secure storage for legacy system credentials
- **Azure Active Directory**: Modern authentication for API consumers
- **Azure Cache for Redis**: Performance optimization for frequent queries
- **Azure Application Insights**: API monitoring and analytics

### Implementation Highlights
```csharp
// HTTP Trigger function that provides a modern API for a legacy inventory system
[FunctionName("GetInventoryItems")]
public static async Task<IActionResult> GetInventoryItems(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "inventory")] HttpRequest req,
    [CosmosDB(
        databaseName: "CacheDb",
        collectionName: "InventoryCache",
        ConnectionStringSetting = "CosmosDbConnection",
        SqlQuery = "SELECT * FROM c WHERE c.type = 'inventory' AND c.expiryTime > {DateTime.UtcNow}")] IEnumerable<dynamic> cacheItems,
    ILogger log)
{
    log.LogInformation("Processing inventory API request");
    
    // Try to get from cache first
    if (cacheItems != null && cacheItems.Any())
    {
        log.LogInformation("Returning cached inventory data");
        return new OkObjectResult(cacheItems.FirstOrDefault().data);
    }
    
    // Parse query parameters
    string category = req.Query["category"];
    string location = req.Query["location"];
    
    try
    {
        // Get credentials from Key Vault (in a real app, use Managed Identity)
        string legacySystemUsername = Environment.GetEnvironmentVariable("LegacySystemUsername");
        string legacySystemPassword = Environment.GetEnvironmentVariable("LegacySystemPassword");
        
        // Connect to legacy system (simplified for example)
        var legacyClient = new LegacySystemClient(legacySystemUsername, legacySystemPassword);
        
        // Map modern API parameters to legacy system format
        var legacyParams = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(category))
            legacyParams["CAT"] = TranslateCategoryCode(category);
        if (!string.IsNullOrEmpty(location))
            legacyParams["LOC_ID"] = location;
        
        // Call legacy system
        var legacyResponse = await legacyClient.GetInventoryAsync(legacyParams);
        
        // Transform legacy response to modern API format
        var apiResponse = TransformLegacyInventoryResponse(legacyResponse);
        
        // Cache the result for future requests
        await CacheInventoryData(apiResponse);
        
        return new OkObjectResult(apiResponse);
    }
    catch (Exception ex)
    {
        log.LogError($"Error accessing legacy system: {ex.Message}");
        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
    }
}

// Helper method to transform legacy system response
private static object TransformLegacyInventoryResponse(string legacyResponse)
{
    // Parse the fixed-width or other legacy format
    // Transform to clean JSON structure
    // (Implementation details omitted for brevity)
    
    // Example return structure
    return new
    {
        items = new[]
        {
            new { id = "123", name = "Widget A", quantity = 42, location = "WAREHOUSE-1" },
            new { id = "456", name = "Widget B", quantity = 16, location = "WAREHOUSE-2" }
        },
        totalCount = 2,
        asOf = DateTime.UtcNow
    };
}
```

### Benefits
This approach uses Azure Functions to create a modern API façade over legacy systems without requiring significant changes to the legacy applications themselves. The serverless API layer handles modern concerns like authentication, rate limiting, and API management while abstracting away the complexities of the legacy systems.

## 7. Automated Media Processing Workflow

### Overview
A serverless workflow for processing media files (images, videos, audio) that handles tasks like format conversion, thumbnail generation, transcoding, and metadata extraction.

### Architecture Components
- **Blob Trigger Functions**: Process new media file uploads
- **Queue Trigger Functions**: Manage processing tasks
- **Durable Functions**: Orchestrate complex media workflows
- **Event Grid**: Notify subscribers of processing completion
- **Azure Media Services**: For advanced video processing
- **Azure Cognitive Services**: Extract metadata from media
- **Azure Blob Storage**: Store original and processed media

### Implementation Highlights
```csharp
// Blob trigger that initiates media processing workflow
[FunctionName("ProcessNewMedia")]
public static async Task ProcessNewMedia(
    [BlobTrigger("uploads/{name}", Connection = "StorageConnection")] Stream mediaFile,
    string name,
    [DurableClient] IDurableOrchestrationClient starter,
    ILogger log)
{
    log.LogInformation($"Processing new media file: {name}");
    
    // Determine media type based on extension
    string extension = Path.GetExtension(name).ToLowerInvariant();
    string mediaType = GetMediaType(extension);
    
    // Start orchestration based on media type
    string instanceId;
    switch (mediaType)
    {
        case "image":
            instanceId = await starter.StartNewAsync("ImageProcessingOrchestrator", null, new
            {
                FileName = name,
                ContainerName = "uploads",
                OutputContainer = "processed-images"
            });
            break;
            
        case "video":
            instanceId = await starter.StartNewAsync("VideoProcessingOrchestrator", null, new
            {
                FileName = name,
                ContainerName = "uploads",
                OutputContainer = "processed-videos"
            });
            break;
            
        case "audio":
            instanceId = await starter.StartNewAsync("AudioProcessingOrchestrator", null, new
            {
                FileName = name,
                ContainerName = "uploads",
                OutputContainer = "processed-audio"
            });
            break;
            
        default:
            log.LogWarning($"Unsupported media type for file: {name}");
            return;
    }
    
    log.LogInformation($"Started {mediaType} processing orchestration with ID: {instanceId}");
}

// Durable orchestrator function for image processing
[FunctionName("ImageProcessingOrchestrator")]
public static async Task<object> RunImageOrchestrator(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    var input = context.GetInput<dynamic>();
    string fileName = input.FileName;
    string containerName = input.ContainerName;
    string outputContainer = input.OutputContainer;
    
    // Define list of outputs to generate
    var outputs = new List<string>();
    
    // Step 1: Generate thumbnail
    var thumbnailResult = await context.CallActivityAsync<string>(
        "GenerateThumbnail",
        new { FileName = fileName, ContainerName = containerName });
    outputs.Add(thumbnailResult);
    
    // Step 2: Extract metadata in parallel with optimizing the image
    var metadataTask = context.CallActivityAsync<dynamic>(
        "ExtractImageMetadata",
        new { FileName = fileName, ContainerName = containerName });
    
    var optimizeTask = context.CallActivityAsync<string>(
        "OptimizeImage",
        new { FileName = fileName, ContainerName = containerName });
    
    // Wait for both tasks to complete
    await Task.WhenAll(metadataTask, optimizeTask);
    outputs.Add(optimizeTask.Result);
    
    // Step 3: Store metadata
    await context.CallActivityAsync(
        "StoreMediaMetadata",
        new { 
            FileName = fileName, 
            MediaType = "image", 
            Metadata = metadataTask.Result 
        });
    
    // Step 4: Notify subscribers
    await context.CallActivityAsync(
        "NotifyMediaProcessed",
        new { 
            FileName = fileName, 
            MediaType = "image", 
            Outputs = outputs 
        });
    
    return new {
        Status = "Completed",
        FileName = fileName,
        ProcessedOutputs = outputs,
        Metadata = metadataTask.Result
    };
}
```

### Benefits
This architecture leverages Azure Functions' event-driven nature to create a scalable media processing pipeline. The serverless approach handles variable processing loads efficiently, scaling out during high-demand periods and scaling to zero during quiet times.