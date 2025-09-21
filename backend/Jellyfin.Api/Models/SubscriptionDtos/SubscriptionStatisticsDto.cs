using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Api.Models.SubscriptionDtos
{
    /// <summary>
    /// DTO for subscription statistics.
    /// </summary>
    public class SubscriptionStatisticsDto
    {
        /// <summary>
        /// Gets or sets the total number of active subscriptions.
        /// </summary>
        public int TotalActiveSubscriptions { get; set; }

        /// <summary>
        /// Gets or sets the total number of expired subscriptions.
        /// </summary>
        public int TotalExpiredSubscriptions { get; set; }

        /// <summary>
        /// Gets or sets the total number of lifetime subscriptions.
        /// </summary>
        public int TotalLifetimeSubscriptions { get; set; }

        /// <summary>
        /// Gets or sets the breakdown by subscription type.
        /// </summary>
        public Dictionary<SubscriptionType, int> SubscriptionsByType { get; set; } = new();

        /// <summary>
        /// Gets or sets the total revenue (if pricing is configured).
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Gets or sets the average subscription duration in hours.
        /// </summary>
        public double AverageDurationHours { get; set; }
    }
}
