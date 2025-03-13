# Azure Functions Sample Projects

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
This architecture leverages Azure Functions' event-driven nature to create a scalable, cost-effective document processing system that only runs when needed. It's perfect for use cases that need to process variable volumes of documents without maintaining constant infrastructure.

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
```csharp
// Event Hub trigger function that processes device telemetry
[FunctionName("ProcessDeviceTelemetry")]
public static async Task Run(
    [EventHubTrigger("telemetry", Connection = "EventHubConnection")] string eventData,
    [CosmosDB(
        databaseName: "IoTDatabase",
        collectionName: "ProcessedTelemetry",
        ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<dynamic> documentsOut,
    [SignalR(HubName = "deviceAlerts")] IAsyncCollector<SignalRMessage> signalRMessages,
    ILogger log)
{
    log.LogInformation("C# Event Hub trigger function processed an event");
    
    // Parse the telemetry data
    var telemetry = JsonConvert.DeserializeObject<dynamic>(eventData);
    string deviceId = telemetry.deviceId;
    
    // Process anomaly detection on temperature readings
    if (telemetry.temperature != null)
    {
        // Simple anomaly detection - in reality, this could be more sophisticated
        if (telemetry.temperature > 30) // Example threshold
        {
            var alert = new
            {
                deviceId = deviceId,
                type = "HighTemperature",
                value = telemetry.temperature,
                timestamp = telemetry.timestamp,
                message = $"High temperature detected: {telemetry.temperature}°C"
            };
            
            // Send real-time alert via SignalR
            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "newAlert",
                    Arguments = new[] { alert }
                });
            
            log.LogInformation($"Alert triggered for device {deviceId}");
        }
    }
    
    // Store processed telemetry in Cosmos DB
    await documentsOut.AddAsync(new
    {
        id = $"{deviceId}-{telemetry.timestamp}",
        deviceId = deviceId,
        processedTelemetry = telemetry,
        processedTimestamp = DateTime.UtcNow.ToString("o")
    });
}
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
```csharp
// C# implementation of an Order Processing function
public static class OrderProcessor
{
    [FunctionName("ProcessOrder")]
    public static async Task Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")] string orderMessage,
        [CosmosDB(
            databaseName: "OrdersDB",
            collectionName: "ProcessedOrders",
            ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<dynamic> orderDocumentOut,
        [ServiceBus("inventory", Connection = "ServiceBusConnection")] IAsyncCollector<dynamic> inventoryMessageOut,
        [ServiceBus("order-failures", Connection = "ServiceBusConnection")] IAsyncCollector<dynamic> failureMessageOut,
        ILogger log)
    {
        log.LogInformation("Processing new order");
        
        try
        {
            // Parse the order message
            var order = JsonConvert.DeserializeObject<OrderMessage>(orderMessage);
            
            // Validate order data
            if (order.Items == null || order.Items.Count == 0)
            {
                throw new Exception("Order contains no items");
            }
            
            // Process the order (in reality, this would be more complex)
            var processedOrder = new
            {
                id = order.OrderId,
                customer = order.CustomerId,
                items = order.Items,
                total = order.Items.Sum(item => item.Price * item.Quantity),
                status = "processing",
                processedAt = DateTime.UtcNow.ToString("o")
            };
            
            // Store the order in Cosmos DB
            await orderDocumentOut.AddAsync(processedOrder);
            
            // Publish event for inventory service
            await inventoryMessageOut.AddAsync(new
            {
                orderId = order.OrderId,
                items = order.Items,
                action = "reserve"
            });
            
            log.LogInformation($"Order {order.OrderId} processed successfully");
        }
        catch (Exception ex)
        {
            log.LogError($"Error processing order: {ex.Message}");
            
            // Publish failure event
            await failureMessageOut.AddAsync(new
            {
                orderId = JsonConvert.DeserializeObject<dynamic>(orderMessage)?.orderId ?? "unknown",
                reason = ex.Message,
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }
    }
    
    // Helper class to deserialize order messages
    public class OrderMessage
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public List<OrderItem> Items { get; set; }
    }
    
    public class OrderItem
    {
        public string ProductId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
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
```csharp
// Timer-triggered orchestrator function that manages the ETL process
public static class ETLPipelineOrchestrator
{
    [FunctionName("ETLPipelineOrchestrator")]
    public static async Task<object> RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context,
        ILogger log)
    {
        // Get the current date for processing
        string currentDate = context.CurrentUtcDateTime.Date.ToString("yyyy-MM-dd");
        
        // Step 1: Extract data from various sources in parallel
        var dataSources = new[] { "sales", "inventory", "customers", "products" };
        var extractionTasks = new List<Task<Dictionary<string, object>>>();
        
        foreach (var source in dataSources)
        {
            extractionTasks.Add(context.CallActivityAsync<Dictionary<string, object>>("ExtractData", 
                new Dictionary<string, string>
                {
                    { "source", source },
                    { "date", currentDate }
                }));
        }
        
        // Wait for all extraction tasks to complete
        var extractionResults = await Task.WhenAll(extractionTasks);
        
        // Step 2: Transform the data
        var transformTasks = new List<Task<Dictionary<string, object>>>();
        
        for (int i = 0; i < dataSources.Length; i++)
        {
            if ((bool)extractionResults[i]["success"])
            {
                transformTasks.Add(context.CallActivityAsync<Dictionary<string, object>>("TransformData", 
                    new Dictionary<string, string>
                    {
                        { "source", dataSources[i] },
                        { "blob_path", (string)extractionResults[i]["blob_path"] },
                        { "date", currentDate }
                    }));
            }
        }
        
        // Wait for all transformation tasks to complete
        var transformResults = await Task.WhenAll(transformTasks);
        
        // Step 3: Load data into the data warehouse
        var transformedPaths = transformResults
            .Where(r => (bool)r["success"])
            .Select(r => (string)r["output_path"])
            .ToList();
            
        var loadResults = await context.CallActivityAsync<Dictionary<string, object>>("LoadDataWarehouse", 
            new Dictionary<string, object>
            {
                { "transformed_data", transformedPaths },
                { "date", currentDate }
            });
        
        // Step 4: Generate reports
        if ((bool)loadResults["success"])
        {
            await context.CallActivityAsync("GenerateReports", 
                new Dictionary<string, string>
                {
                    { "date", currentDate }
                });
        }
        
        return new
        {
            pipeline_id = context.InstanceId,
            execution_date = currentDate,
            status = "completed",
            extraction_success = extractionResults.Count(r => (bool)r["success"]),
            transform_success = transformResults.Count(r => (bool)r["success"]),
            load_success = loadResults["success"]
        };
    }
    
    [FunctionName("ETLPipelineTrigger")]
    public static async Task TimerTrigger(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timer, // Runs daily
