using System.Collections.Generic;

namespace Jellyfin.Server.Implementations.PinGeneration;

/// <summary>
/// Contains settings for a PIN batch.
/// </summary>
public class BatchSettings
{
    /// <summary>
    /// Gets or sets the maximum concurrent sessions.
    /// </summary>
    public int? MaxConcurrentSessions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether remote access is allowed.
    /// </summary>
    public bool AllowRemoteAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether transcoding is allowed.
    /// </summary>
    public bool AllowTranscoding { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether downloads are allowed.
    /// </summary>
    public bool AllowDownload { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether sync play is allowed.
    /// </summary>
    public bool AllowSyncPlay { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum bitrate.
    /// </summary>
    public int? MaxBitrate { get; set; }

    /// <summary>
    /// Gets or sets the maximum parental rating.
    /// </summary>
    public int? MaxParentalRating { get; set; }

    /// <summary>
    /// Gets or sets the price for the batch.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Gets or sets the currency for the price.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the batch.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
