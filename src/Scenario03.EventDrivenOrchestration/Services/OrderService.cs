using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Models.Dtos;
using Scenario03.EventDrivenOrchestration.Repositories;

namespace Scenario03.EventDrivenOrchestration.Services;

/// <summary>
/// Provides order lifecycle operations backed by an <see cref="IOrderRepository"/>.
/// </summary>
public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.CustomerId))
            throw new ArgumentException("Customer ID is required.", nameof(request));

        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("At least one order item is required.", nameof(request));

        var order = new Order
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            Items = request.Items.Select(dto => new OrderItem(
                ProductId: dto.ProductId,
                ProductName: dto.ProductName,
                Quantity: dto.Quantity,
                UnitPrice: dto.UnitPrice)).ToList()
        };

        await _repository.UpsertAsync(order, ct);

        _logger.LogInformation(
            "Created order {OrderId} for customer {CustomerId} with {ItemCount} items totalling {TotalAmount:C}",
            order.Id, order.CustomerId, order.Items.Count, order.TotalAmount);

        return order;
    }

    /// <inheritdoc />
    public async Task<Order?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        return await _repository.GetAsync(orderId, ct);
    }

    /// <inheritdoc />
    public async Task UpdateOrderStatusAsync(string orderId, OrderStatus status, string? reason = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);

        var order = await _repository.GetAsync(orderId, ct)
            ?? throw new KeyNotFoundException($"Order '{orderId}' not found.");

        var previousStatus = order.Status;
        order.Status = status;

        if (reason is not null)
            order.FailureReason = reason;

        if (status is OrderStatus.Completed or OrderStatus.Failed or OrderStatus.Cancelled)
            order.CompletedAt = DateTimeOffset.UtcNow;

        await _repository.UpsertAsync(order, ct);

        _logger.LogInformation(
            "Updated order {OrderId} status from {PreviousStatus} to {NewStatus}. Reason: {Reason}",
            orderId, previousStatus, status, reason ?? "(none)");
    }
}
