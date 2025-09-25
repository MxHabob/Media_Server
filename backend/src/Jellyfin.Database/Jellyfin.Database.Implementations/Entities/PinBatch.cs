using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// Enum to define PIN generation patterns.
    /// </summary>
    public enum PinPattern
    {
        /// <summary>
        /// Numeric only (0-9).
        /// </summary>
        Numeric,

        /// <summary>
        /// Alphanumeric (0-9, A-Z).
        /// </summary>
        Alphanumeric,

        /// <summary>
        /// Alphanumeric with lowercase (0-9, A-Z, a-z).
        /// </summary>
        AlphanumericMixed,

        /// <summary>
        /// Custom pattern with specific characters.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Enum to define batch status.
    /// </summary>
    public enum BatchStatus
    {
        /// <summary>
        /// Batch is active and PINs can be used.
        /// </summary>
        Active,

        /// <summary>
        /// Batch is suspended, PINs cannot be used.
        /// </summary>
        Suspended,

        /// <summary>
        /// Batch is expired, PINs cannot be used.
        /// </summary>
        Expired,

        /// <summary>
        /// Batch is deleted.
        /// </summary>
        Deleted
    }

    /// <summary>
    /// Represents a batch of PIN codes with common properties and management.
    /// </summary>
    public class PinBatch
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PinBatch"/> class.
        /// </summary>
        public PinBatch()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.UtcNow;
            Status = BatchStatus.Active;
            PinBatchUsers = new HashSet<PinBatchUser>();
        }

        /// <summary>
        /// Gets or sets the unique identifier for the batch.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the batch name.
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the batch description.
        /// </summary>
        [MaxLength(1000)]
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the subscription type for this batch.
        /// </summary>
        public SubscriptionType SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets the PIN pattern for this batch.
        /// </summary>
        public PinPattern PinPattern { get; set; }

        /// <summary>
        /// Gets or sets the PIN length.
        /// </summary>
        public int PinLength { get; set; } = 6;

        /// <summary>
        /// Gets or sets the custom character set for custom PIN pattern.
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string? CustomCharacterSet { get; set; }

        /// <summary>
        /// Gets or sets the total number of PINs in this batch.
        /// </summary>
        public int TotalPins { get; set; }

        /// <summary>
        /// Gets or sets the number of PINs that have been used.
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
        /// Gets or sets the date when the batch was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the batch expires.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the batch was last modified.
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created this batch.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who last modified this batch.
        /// </summary>
        public Guid? ModifiedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the maximum concurrent sessions allowed for PINs in this batch.
        /// </summary>
        public int? MaxConcurrentSessions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether remote access is allowed for PINs in this batch.
        /// </summary>
        public bool AllowRemoteAccess { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum bitrate for PINs in this batch.
        /// </summary>
        public int? MaxBitrate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether transcoding is allowed for PINs in this batch.
        /// </summary>
        public bool AllowTranscoding { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum parental rating for PINs in this batch.
        /// </summary>
        public int? MaxParentalRating { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether downloads are allowed for PINs in this batch.
        /// </summary>
        public bool AllowDownload { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether sync play is allowed for PINs in this batch.
        /// </summary>
        public bool AllowSyncPlay { get; set; } = true;

        /// <summary>
        /// Gets or sets the price per PIN in this batch.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency for the price.
        /// </summary>
        [MaxLength(3)]
        [StringLength(3)]
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for the batch.
        /// </summary>
        [MaxLength(4000)]
        [StringLength(4000)]
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets the collection of PIN batch users.
        /// </summary>
        public virtual ICollection<PinBatchUser> PinBatchUsers { get; private set; }
    }
}
