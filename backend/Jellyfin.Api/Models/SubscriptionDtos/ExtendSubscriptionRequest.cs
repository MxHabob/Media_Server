using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.SubscriptionDtos
{
    /// <summary>
    /// Request to extend a user's subscription.
    /// </summary>
    public class ExtendSubscriptionRequest
    {
        /// <summary>
        /// Gets or sets the additional hours to add to the subscription.
        /// </summary>
        [Required]
        [Range(1, 8760)] // Max 1 year
        public int AdditionalHours { get; set; }
    }
}
