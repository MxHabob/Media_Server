using System;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Server.Implementations.PinGeneration;

/// <summary>
/// Contains information about a generated PIN.
/// </summary>
public class GeneratedPinInfo
{
    /// <summary>
    /// Gets or sets the generated PIN.
    /// </summary>
    public string Pin { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public Guid BatchId { get; set; }

    /// <summary>
    /// Gets or sets the subscription type.
    /// </summary>
    public SubscriptionType SubscriptionType { get; set; }

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent sessions.
    /// </summary>
    public int? MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether remote access is allowed.
    /// </summary>
    public bool AllowRemoteAccess { get; set; }

    /// <summary>
    /// Gets or sets the maximum bitrate.
    /// </summary>
    public int? MaxBitrate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether transcoding is allowed.
    /// </summary>
    public bool AllowTranscoding { get; set; }

    /// <summary>
    /// Gets or sets the maximum parental rating.
    /// </summary>
    public int? MaxParentalRating { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether downloads are allowed.
    /// </summary>
    public bool AllowDownload { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sync play is allowed.
    /// </summary>
    public bool AllowSyncPlay { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedDate { get; set; }
}
