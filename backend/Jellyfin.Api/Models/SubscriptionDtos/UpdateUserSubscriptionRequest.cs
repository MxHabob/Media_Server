using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.SubscriptionDtos
{
    /// <summary>
    /// Request to update a user's subscription.
    /// </summary>
    public class UpdateUserSubscriptionRequest
    {
        /// <summary>
        /// Gets or sets the subscription configuration ID.
        /// </summary>
        [Required]
        public Guid ConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets whether to extend the existing subscription or replace it.
        /// </summary>
        public bool ExtendExisting { get; set; } = false;
    }
}
