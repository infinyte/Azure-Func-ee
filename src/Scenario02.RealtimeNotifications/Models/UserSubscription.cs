using Azure;
using Azure.Data.Tables;

namespace Scenario02.RealtimeNotifications.Models;

/// <summary>
/// Represents a user's notification channel subscription stored in Azure Table Storage.
/// Uses the user ID as the partition key and the channel name as the row key.
/// </summary>
public sealed class UserSubscription : ITableEntity
{
    /// <inheritdoc />
    public string PartitionKey { get; set; } = string.Empty;

    /// <inheritdoc />
    public string RowKey { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc />
    public ETag ETag { get; set; }

    /// <summary>
    /// The user ID this subscription belongs to.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The notification channel.
    /// </summary>
    public int Channel { get; set; }

    /// <summary>
    /// Whether this channel is enabled for the user.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to include this channel in daily digest emails.
    /// </summary>
    public bool IncludeInDigest { get; set; } = true;
}
