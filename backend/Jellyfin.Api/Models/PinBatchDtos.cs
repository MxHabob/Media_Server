using System;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Api.Models.PinBatchDtos
{
    /// <summary>
    /// Request to create a new PIN batch.
    /// </summary>
    public class CreatePinBatchRequest
    {
        /// <summary>
        /// Gets or sets the batch name.
        /// </summary>
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the batch description.
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the subscription type.
        /// </summary>
        [Required]
        public SubscriptionType SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets the PIN pattern.
        /// </summary>
        [Required]
        public PinPattern PinPattern { get; set; }

        /// <summary>
        /// Gets or sets the PIN length.
        /// </summary>
        [Range(4, 20)]
        public int PinLength { get; set; } = 6;

        /// <summary>
        /// Gets or sets the number of PINs to generate.
        /// </summary>
        [Range(1, 10000)]
        public int PinCount { get; set; }

        /// <summary>
        /// Gets or sets the custom character set for custom pattern.
        /// </summary>
        [StringLength(255)]
        public string? CustomCharacterSet { get; set; }

        /// <summary>
        /// Gets or sets the expiration date for the batch.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent sessions.
        /// </summary>
        [Range(1, 100)]
        public int? MaxConcurrentSessions { get; set; }

        /// <summary>
        /// Gets or sets whether remote access is allowed.
        /// </summary>
        public bool AllowRemoteAccess { get; set; } = true;

        /// <summary>
        /// Gets or sets whether transcoding is allowed.
        /// </summary>
        public bool AllowTranscoding { get; set; } = true;

        /// <summary>
        /// Gets or sets whether downloads are allowed.
        /// </summary>
        public bool AllowDownload { get; set; } = false;

        /// <summary>
        /// Gets or sets whether sync play is allowed.
        /// </summary>
        public bool AllowSyncPlay { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum bitrate.
        /// </summary>
        [Range(1, 100000000)]
        public int? MaxBitrate { get; set; }

        /// <summary>
        /// Gets or sets the maximum parental rating.
        /// </summary>
        [Range(0, 18)]
        public int? MaxParentalRating { get; set; }

        /// <summary>
        /// Gets or sets the price per PIN.
        /// </summary>
        [Range(0, 999999.99)]
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        [StringLength(3)]
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        [StringLength(4000)]
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Request to update a PIN batch.
    /// </summary>
    public class UpdatePinBatchRequest
    {
        /// <summary>
        /// Gets or sets the batch name.
        /// </summary>
        [StringLength(255, MinimumLength = 1)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the batch description.
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// PIN batch information.
    /// </summary>
    public class PinBatchDto
    {
        /// <summary>
        /// Gets or sets the batch ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the batch name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the batch description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the subscription type.
        /// </summary>
        public SubscriptionType SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets the PIN pattern.
        /// </summary>
        public PinPattern PinPattern { get; set; }

        /// <summary>
        /// Gets or sets the PIN length.
        /// </summary>
        public int PinLength { get; set; }

        /// <summary>
        /// Gets or sets the custom character set.
        /// </summary>
        public string? CustomCharacterSet { get; set; }

        /// <summary>
        /// Gets or sets the total number of PINs.
        /// </summary>
        public int TotalPins { get; set; }

        /// <summary>
        /// Gets or sets the number of used PINs.
        /// </summary>
        public int UsedPins { get; set; }

        /// <summary>
        /// Gets or sets the number of active PINs.
        /// </summary>
        public int ActivePins { get; set; }

        /// <summary>
        /// Gets or sets the number of expired PINs.
        /// </summary>
        public int ExpiredPins { get; set; }

        /// <summary>
        /// Gets or sets the batch status.
        /// </summary>
        public BatchStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the creator user ID.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the modifier user ID.
        /// </summary>
        public Guid? ModifiedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent sessions.
        /// </summary>
        public int? MaxConcurrentSessions { get; set; }

        /// <summary>
        /// Gets or sets whether remote access is allowed.
        /// </summary>
        public bool AllowRemoteAccess { get; set; }

        /// <summary>
        /// Gets or sets the maximum bitrate.
        /// </summary>
        public int? MaxBitrate { get; set; }

        /// <summary>
        /// Gets or sets whether transcoding is allowed.
        /// </summary>
        public bool AllowTranscoding { get; set; }

        /// <summary>
        /// Gets or sets the maximum parental rating.
        /// </summary>
        public int? MaxParentalRating { get; set; }

        /// <summary>
        /// Gets or sets whether downloads are allowed.
        /// </summary>
        public bool AllowDownload { get; set; }

        /// <summary>
        /// Gets or sets whether sync play is allowed.
        /// </summary>
        public bool AllowSyncPlay { get; set; }

        /// <summary>
        /// Gets or sets the price per PIN.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// PIN batch user information.
    /// </summary>
    public class PinBatchUserDto
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the batch ID.
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Gets or sets the user ID. Can be null if the PIN hasn't been used yet.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets whether the PIN is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the first used date.
        /// </summary>
        public DateTime? FirstUsedDate { get; set; }

        /// <summary>
        /// Gets or sets the last used date.
        /// </summary>
        public DateTime? LastUsedDate { get; set; }

        /// <summary>
        /// Gets or sets the usage count.
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the deactivated date.
        /// </summary>
        public DateTime? DeactivatedDate { get; set; }

        /// <summary>
        /// Gets or sets the deactivation reason.
        /// </summary>
        public string? DeactivationReason { get; set; }

        /// <summary>
        /// Gets or sets additional metadata.
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the last login IP.
        /// </summary>
        public string? LastLoginIp { get; set; }

        /// <summary>
        /// Gets or sets the last login device.
        /// </summary>
        public string? LastLoginDevice { get; set; }
    }

    /// <summary>
    /// Batch statistics information.
    /// </summary>
    public class BatchStatisticsDto
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
}
