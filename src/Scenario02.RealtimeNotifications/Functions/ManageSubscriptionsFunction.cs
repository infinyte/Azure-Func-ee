using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Scenario02.RealtimeNotifications.Models;
using Scenario02.RealtimeNotifications.Models.Dtos;
using Scenario02.RealtimeNotifications.Repositories;

namespace Scenario02.RealtimeNotifications.Functions;

/// <summary>
/// HTTP-triggered function for managing user notification channel subscriptions.
/// Supports GET (list) and PUT (update) operations.
/// </summary>
public sealed class ManageSubscriptionsFunction
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<ManageSubscriptionsFunction> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ManageSubscriptionsFunction"/>.
    /// </summary>
    /// <param name="subscriptionRepository">The subscription repository.</param>
    /// <param name="logger">The logger instance.</param>
    public ManageSubscriptionsFunction(
        ISubscriptionRepository subscriptionRepository,
        ILogger<ManageSubscriptionsFunction> logger)
    {
        _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all notification subscriptions for a user.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="userId">The user ID from the route.</param>
    /// <returns>200 OK with the list of subscriptions.</returns>
    [Function("GetSubscriptions")]
    public async Task<HttpResponseData> GetAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "subscriptions/{userId}")] HttpRequestData req,
        string userId)
    {
        _logger.LogInformation("GET /api/subscriptions/{UserId}", userId);

        var subscriptions = await _subscriptionRepository.GetByUserAsync(userId).ConfigureAwait(false);
        var responses = subscriptions.Select(SubscriptionResponse.FromSubscription).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(responses).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Updates a user's notification channel subscription.
    /// </summary>
    /// <param name="req">The HTTP request containing the subscription update payload.</param>
    /// <param name="userId">The user ID from the route.</param>
    /// <returns>200 OK with the updated subscription.</returns>
    [Function("UpdateSubscription")]
    public async Task<HttpResponseData> PutAsync(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "subscriptions/{userId}")] HttpRequestData req,
        string userId)
    {
        _logger.LogInformation("PUT /api/subscriptions/{UserId}", userId);

        var body = await req.ReadAsStringAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(body))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { message = "Request body is required." }).ConfigureAwait(false);
            return badResponse;
        }

        var request = JsonSerializer.Deserialize<SubscriptionRequest>(body, JsonOptions);

        if (request is null || string.IsNullOrWhiteSpace(request.Channel))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { message = "Channel is required." }).ConfigureAwait(false);
            return badResponse;
        }

        if (!Enum.TryParse<NotificationChannel>(request.Channel, ignoreCase: true, out var channel))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { message = $"Invalid channel: {request.Channel}" }).ConfigureAwait(false);
            return badResponse;
        }

        var subscription = new UserSubscription
        {
            PartitionKey = userId,
            RowKey = channel.ToString(),
            UserId = userId,
            Channel = (int)channel,
            IsEnabled = request.IsEnabled,
            IncludeInDigest = request.IncludeInDigest
        };

        await _subscriptionRepository.UpsertAsync(subscription).ConfigureAwait(false);

        _logger.LogInformation(
            "Updated subscription for user {UserId}: {Channel} enabled={Enabled}",
            userId, channel, request.IsEnabled);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(SubscriptionResponse.FromSubscription(subscription)).ConfigureAwait(false);
        return response;
    }
}
