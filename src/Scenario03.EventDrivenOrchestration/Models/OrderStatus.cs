namespace Scenario03.EventDrivenOrchestration.Models;

/// <summary>
/// Represents the lifecycle status of an order as it progresses through the saga orchestration.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order has been received but processing has not yet started.</summary>
    Pending = 0,

    /// <summary>Order is actively being processed through the saga steps.</summary>
    Processing = 1,

    /// <summary>Inventory has been successfully reserved for all order items.</summary>
    InventoryReserved = 2,

    /// <summary>Payment has been successfully processed.</summary>
    PaymentProcessed = 3,

    /// <summary>Shipment has been created and dispatched.</summary>
    Shipped = 4,

    /// <summary>All saga steps completed successfully; the order is fulfilled.</summary>
    Completed = 5,

    /// <summary>One or more saga steps failed and compensation may have been applied.</summary>
    Failed = 6,

    /// <summary>The saga is currently executing compensating transactions.</summary>
    Compensating = 7,

    /// <summary>The order has been cancelled, with all completed steps compensated.</summary>
    Cancelled = 8
}
