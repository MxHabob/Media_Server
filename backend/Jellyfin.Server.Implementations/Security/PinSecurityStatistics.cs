namespace Jellyfin.Server.Implementations.Security;

/// <summary>
/// Contains PIN security statistics.
/// </summary>
public class PinSecurityStatistics
{
    /// <summary>
    /// Gets or sets the number of currently locked PINs.
    /// </summary>
    public int CurrentlyLockedPins { get; set; }

    /// <summary>
    /// Gets or sets the number of currently locked IP addresses.
    /// </summary>
    public int CurrentlyLockedIps { get; set; }

    /// <summary>
    /// Gets or sets the number of failed attempts in the last hour.
    /// </summary>
    public int FailedAttemptsLastHour { get; set; }

    /// <summary>
    /// Gets or sets the number of failed attempts in the last day.
    /// </summary>
    public int FailedAttemptsLastDay { get; set; }

    /// <summary>
    /// Gets or sets the number of unique IP addresses with attempts in the last hour.
    /// </summary>
    public int UniqueIpsLastHour { get; set; }

    /// <summary>
    /// Gets or sets the number of unique IP addresses with attempts in the last day.
    /// </summary>
    public int UniqueIpsLastDay { get; set; }
}
