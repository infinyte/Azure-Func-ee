# Scenario 02: Real-Time Notification System

A multi-channel notification system using Azure SignalR Service for real-time in-app delivery, queue-based email routing, Event Grid integration for system events, and user subscription management.

## Architecture

```
HTTP POST /api/notifications
        │
        v
  SendNotificationFunction ──queue──> notification-delivery
        │                                    │
        v                                    v
  Table Storage                  ProcessNotificationFunction
  (notifications)                   │                │
                                    v                v
                              InApp channel    Email channel
                                    │                │
                                    v                v
                          signalr-broadcast    SimulatedEmailService
                                    │
                                    v
                        BroadcastRealtimeFunction
                                    │
                                    v
                          Azure SignalR Service
                                    │
                                    v
                            Connected clients
```

## Functions

| Function | Trigger | Route | Purpose |
|----------|---------|-------|---------|
| NegotiateFunction | HTTP POST | `/api/negotiate` | Returns SignalR connection info |
| SendNotificationFunction | HTTP POST | `/api/notifications` | Validates, persists, queues for delivery |
| ProcessNotificationFunction | Queue | `notification-delivery` | Routes to channel (InApp or Email) |
| BroadcastRealtimeFunction | Queue | `signalr-broadcast` | Sends SignalR message to user |
| SendDigestFunction | Timer | `0 0 8 * * *` (daily 8 AM) | Aggregates unread notifications into digest |
| ManageSubscriptionsFunction | HTTP GET/PUT | `/api/subscriptions/{userId}` | CRUD for user notification preferences |
| HandleSystemEventFunction | EventGrid | -- | Converts system events to notifications |

## Key Patterns

- **SignalR Service integration** -- Negotiate endpoint + output binding for serverless real-time messaging
- **Multi-channel delivery** -- Fan-out to InApp (SignalR) and Email channels based on notification type
- **Queue-based routing** -- Decoupled processing via `notification-delivery` and `signalr-broadcast` queues
- **User preferences** -- Subscription management for per-channel notification control
- **Daily digest** -- Timer-triggered aggregation of unread notifications

## Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `NotificationOptions:NotificationsTable` | Table Storage table name | `notifications` |
| `NotificationOptions:SubscriptionsTable` | Subscriptions table name | `subscriptions` |
| `NotificationOptions:DeliveryQueue` | Delivery queue name | `notification-delivery` |
| `NotificationOptions:BroadcastQueue` | SignalR broadcast queue name | `signalr-broadcast` |

## Local Development

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureSignalRConnectionString": "<your-signalr-connection-string>"
  }
}
```

> **Note:** Azure SignalR Service does not have a local emulator. For local development, create a Free-tier SignalR Service instance in Azure and use its connection string.

## API Examples

**Send a notification:**

```bash
curl -X POST http://localhost:7071/api/notifications \
  -H "Content-Type: application/json" \
  -d '{"userId":"user-001","title":"Order Shipped","body":"Your order has shipped.","channel":"InApp","category":"Orders"}'
```

**Get user subscriptions:**

```bash
curl http://localhost:7071/api/subscriptions/user-001
```

**Negotiate SignalR connection:**

```bash
curl -X POST http://localhost:7071/api/negotiate \
  -H "x-ms-signalr-userid: user-001"
```
