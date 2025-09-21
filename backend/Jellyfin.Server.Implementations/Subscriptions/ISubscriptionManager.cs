using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Server.Implementations.Subscriptions
{
    /// <summary>
    /// Interface for managing subscription configurations and user subscriptions.
    /// </summary>
    public interface ISubscriptionManager
    {
        /// <summary>
        /// Gets all active subscription configurations.
        /// </summary>
        /// <returns>A list of active subscription configurations.</returns>
        Task<IEnumerable<SubscriptionConfiguration>> GetActiveConfigurationsAsync();

        /// <summary>
        /// Gets a subscription configuration by ID.
        /// </summary>
        /// <param name="id">The configuration ID.</param>
        /// <returns>The subscription configuration or null if not found.</returns>
        Task<SubscriptionConfiguration?> GetConfigurationAsync(Guid id);

        /// <summary>
        /// Creates a new subscription configuration.
        /// </summary>
        /// <param name="configuration">The configuration to create.</param>
        /// <param name="createdByUserId">The ID of the user creating the configuration.</param>
        /// <returns>The created configuration.</returns>
        Task<SubscriptionConfiguration> CreateConfigurationAsync(SubscriptionConfiguration configuration, Guid createdByUserId);

        /// <summary>
        /// Updates an existing subscription configuration.
        /// </summary>
        /// <param name="configuration">The configuration to update.</param>
        /// <param name="modifiedByUserId">The ID of the user modifying the configuration.</param>
        /// <returns>The updated configuration.</returns>
        Task<SubscriptionConfiguration> UpdateConfigurationAsync(SubscriptionConfiguration configuration, Guid modifiedByUserId);

        /// <summary>
        /// Deletes a subscription configuration.
        /// </summary>
        /// <param name="id">The configuration ID to delete.</param>
        /// <returns>True if deleted successfully.</returns>
        Task<bool> DeleteConfigurationAsync(Guid id);

        /// <summary>
        /// Creates a user with a subscription based on the specified configuration.
        /// </summary>
        /// <param name="username">The username for the new user.</param>
        /// <param name="configurationId">The subscription configuration ID.</param>
        /// <param name="customDurationHours">Custom duration in hours (for custom subscriptions).</param>
        /// <returns>The created user and generated PIN.</returns>
        Task<(User user, string pin)> CreateUserWithSubscriptionAsync(string username, Guid configurationId, int? customDurationHours = null);

        /// <summary>
        /// Updates a user's subscription.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="configurationId">The new subscription configuration ID.</param>
        /// <param name="extendExisting">Whether to extend the existing subscription or replace it.</param>
        /// <returns>The updated user.</returns>
        Task<User> UpdateUserSubscriptionAsync(Guid userId, Guid configurationId, bool extendExisting = false);

        /// <summary>
        /// Extends a user's subscription by the specified duration.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="additionalHours">Additional hours to add to the subscription.</param>
        /// <returns>The updated user.</returns>
        Task<User> ExtendUserSubscriptionAsync(Guid userId, int additionalHours);

        /// <summary>
        /// Gets subscription statistics.
        /// </summary>
        /// <returns>Subscription statistics.</returns>
        Task<SubscriptionStatistics> GetStatisticsAsync();

        /// <summary>
        /// Validates if a user's subscription is still valid.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if the subscription is valid.</returns>
        Task<bool> IsSubscriptionValidAsync(Guid userId);

        /// <summary>
        /// Gets users with expiring subscriptions.
        /// </summary>
        /// <param name="hoursBeforeExpiration">Hours before expiration to check.</param>
        /// <returns>Users with expiring subscriptions.</returns>
        Task<IEnumerable<User>> GetUsersWithExpiringSubscriptionsAsync(int hoursBeforeExpiration = 24);
    }

    /// <summary>
    /// Statistics about subscriptions.
    /// </summary>
    public class SubscriptionStatistics
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
