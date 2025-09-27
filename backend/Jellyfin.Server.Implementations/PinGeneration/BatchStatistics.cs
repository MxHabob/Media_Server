using System;

namespace Jellyfin.Server.Implementations.PinGeneration;

/// <summary>
/// Contains statistics for a PIN batch.
/// </summary>
public class BatchStatistics
{
    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public Guid BatchId { get; set; }

    /// <summary>
    /// Gets or sets the batch name.
    /// </summary>
    public string BatchName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of PINs.
    /// </summary>
    public int TotalPins { get; set; }

    /// <summary>
    /// Gets or sets the number of active PINs.
    /// </summary>
    public int ActivePins { get; set; }

    /// <summary>
    /// Gets or sets the number of expired PINs.
    /// </summary>
    public int ExpiredPins { get; set; }

    /// <summary>
    /// Gets or sets the number of used PINs.
    /// </summary>
    public int UsedPins { get; set; }

    /// <summary>
    /// Gets or sets the number of unused PINs.
    /// </summary>
    public int UnusedPins { get; set; }

    /// <summary>
    /// Gets or sets the total usage count.
    /// </summary>
    public int TotalUsageCount { get; set; }

    /// <summary>
    /// Gets or sets the average usage per PIN.
    /// </summary>
    public double AverageUsagePerPin { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the last activity date.
    /// </summary>
    public DateTime? LastActivityDate { get; set; }
}
