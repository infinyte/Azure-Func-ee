using Scenario02.RealtimeNotifications.Models;

namespace Scenario02.RealtimeNotifications.Repositories;

/// <summary>
/// Repository for persisting and retrieving user notification subscriptions.
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>
    /// Gets all subscriptions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read-only list of subscriptions.</returns>
    Task<IReadOnlyList<UserSubscription>> GetByUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a subscription record.
    /// </summary>
    /// <param name="subscription">The subscription entity.</param>
    /// <param name="ct">A cancellation token.</param>
    Task UpsertAsync(UserSubscription subscription, CancellationToken ct = default);
}
