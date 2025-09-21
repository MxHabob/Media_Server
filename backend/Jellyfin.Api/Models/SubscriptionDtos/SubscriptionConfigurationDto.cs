using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Api.Models.SubscriptionDtos
{
    /// <summary>
    /// DTO for subscription configuration.
    /// </summary>
    public class SubscriptionConfigurationDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for this subscription configuration.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the subscription configuration.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the subscription configuration.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the subscription type this configuration applies to.
        /// </summary>
        public SubscriptionType SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets the duration in hours for custom subscriptions.
        /// </summary>
        public int? CustomDurationHours { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent sessions allowed.
        /// </summary>
        public int MaxConcurrentSessions { get; set; }

        /// <summary>
        /// Gets or sets whether remote access is allowed.
        /// </summary>
        public bool AllowRemoteAccess { get; set; }

        /// <summary>
        /// Gets or sets the maximum bitrate for streaming.
        /// </summary>
        public int? MaxBitrate { get; set; }

        /// <summary>
        /// Gets or sets whether transcoding is allowed.
        /// </summary>
        public bool AllowTranscoding { get; set; }

        /// <summary>
        /// Gets or sets the maximum parental rating allowed.
        /// </summary>
        public int? MaxParentalRating { get; set; }

        /// <summary>
        /// Gets or sets whether download is allowed.
        /// </summary>
        public bool AllowDownload { get; set; }

        /// <summary>
        /// Gets or sets whether sync play is allowed.
        /// </summary>
        public bool AllowSyncPlay { get; set; }

        /// <summary>
        /// Gets or sets the price for this subscription (if applicable).
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency for the price.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets whether this configuration is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the date this configuration was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the date this configuration was last modified.
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the sort order for display purposes.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets additional metadata as JSON.
        /// </summary>
        public string? Metadata { get; set; }
    }
}
