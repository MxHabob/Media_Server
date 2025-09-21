using System.ComponentModel.DataAnnotations;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Api.Models.SubscriptionDtos
{
    /// <summary>
    /// Request to create a new subscription configuration.
    /// </summary>
    public class CreateSubscriptionConfigurationRequest
    {
        /// <summary>
        /// Gets or sets the name of the subscription configuration.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the subscription configuration.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the subscription type this configuration applies to.
        /// </summary>
        [Required]
        public SubscriptionType SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets the duration in hours for custom subscriptions.
        /// </summary>
        [Range(1, 8760)] // Max 1 year
        public int? CustomDurationHours { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent sessions allowed.
        /// </summary>
        [Range(1, 10)]
        public int MaxConcurrentSessions { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether remote access is allowed.
        /// </summary>
        public bool AllowRemoteAccess { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum bitrate for streaming.
        /// </summary>
        [Range(100, 100000)]
        public int? MaxBitrate { get; set; }

        /// <summary>
        /// Gets or sets whether transcoding is allowed.
        /// </summary>
        public bool AllowTranscoding { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum parental rating allowed.
        /// </summary>
        [Range(0, 18)]
        public int? MaxParentalRating { get; set; }

        /// <summary>
        /// Gets or sets whether download is allowed.
        /// </summary>
        public bool AllowDownload { get; set; } = false;

        /// <summary>
        /// Gets or sets whether sync play is allowed.
        /// </summary>
        public bool AllowSyncPlay { get; set; } = false;

        /// <summary>
        /// Gets or sets the price for this subscription (if applicable).
        /// </summary>
        [Range(0, 999999.99)]
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency for the price.
        /// </summary>
        [StringLength(3)]
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets the sort order for display purposes.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string? Metadata { get; set; }
    }
}
