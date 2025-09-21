using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.SubscriptionDtos
{
    /// <summary>
    /// Request to create a user with a subscription.
    /// </summary>
    public class CreateUserWithSubscriptionRequest
    {
        /// <summary>
        /// Gets or sets the username for the new user.
        /// </summary>
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subscription configuration ID.
        /// </summary>
        [Required]
        public Guid ConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets custom duration in hours (for custom subscriptions).
        /// </summary>
        [Range(1, 8760)] // Max 1 year
        public int? CustomDurationHours { get; set; }
    }
}
