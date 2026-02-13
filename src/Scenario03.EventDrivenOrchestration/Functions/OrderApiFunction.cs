using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Scenario03.EventDrivenOrchestration.Models;
using Scenario03.EventDrivenOrchestration.Models.Dtos;
using Scenario03.EventDrivenOrchestration.Repositories;
using Scenario03.EventDrivenOrchestration.Services;

namespace Scenario03.EventDrivenOrchestration.Functions;

/// <summary>
/// HTTP-triggered function providing a REST API for order operations.
/// Supports retrieving orders by ID, creating new orders (which starts the saga),
/// and retrieving orders by customer.
/// </summary>
public sealed class OrderApiFunction
{
    private readonly IOrderService _orderService;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderApiFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public OrderApiFunction(
        IOrderService orderService,
        IOrderRepository orderRepository,
        ILogger<OrderApiFunction> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves an order by its unique identifier.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="id">The order identifier from the route.</param>
    /// <returns>200 OK with the order, or 404 Not Found.</returns>
    [Function("GetOrderById")]
    public async Task<HttpResponseData> GetOrderByIdAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation("GET /api/orders/{OrderId}", id);

        var order = await _orderService.GetOrderAsync(id);

        if (order is null)
        {
            _logger.LogInformation("Order {OrderId} not found", id);
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(new { message = $"Order '{id}' not found." });
            return notFoundResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(OrderResponse.FromOrder(order));
        return response;
    }

    /// <summary>
    /// Creates a new order and starts the saga orchestration for processing.
    /// Returns 202 Accepted with the created order.
    /// </summary>
    /// <param name="req">The HTTP request containing the order creation payload.</param>
    /// <param name="durableClient">The durable task client for starting orchestrations.</param>
    /// <returns>202 Accepted with the order details.</returns>
    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrderAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient)
    {
        _logger.LogInformation("POST /api/orders");

        var requestBody = await req.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new { message = "Request body is required." });
            return badRequestResponse;
        }

        var createRequest = JsonSerializer.Deserialize<CreateOrderRequest>(requestBody, JsonOptions);

        if (createRequest is null)
        {
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new { message = "Invalid order request." });
            return badRequestResponse;
        }

        // Create the order.
        var order = await _orderService.CreateOrderAsync(createRequest);

        // Update status to Processing and start the saga.
        await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);

        var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            "OrderSagaOrchestrator",
            order);

        _logger.LogInformation(
            "Order {OrderId} created via API. Saga instance: {InstanceId}",
            order.Id, instanceId);

        // Refresh the order to include the Processing status.
        var updatedOrder = await _orderService.GetOrderAsync(order.Id) ?? order;

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(OrderResponse.FromOrder(updatedOrder));
        return response;
    }

    /// <summary>
    /// Retrieves all orders for a specific customer.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="customerId">The customer identifier from the route.</param>
    /// <returns>200 OK with a list of orders for the customer.</returns>
    [Function("GetOrdersByCustomer")]
    public async Task<HttpResponseData> GetOrdersByCustomerAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/customer/{customerId}")] HttpRequestData req,
        string customerId)
    {
        _logger.LogInformation("GET /api/orders/customer/{CustomerId}", customerId);

        var orders = await _orderRepository.GetByCustomerAsync(customerId);

        var orderResponses = orders.Select(OrderResponse.FromOrder).ToList();

        _logger.LogInformation(
            "Found {Count} orders for customer {CustomerId}",
            orderResponses.Count, customerId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(orderResponses);
        return response;
    }
}
