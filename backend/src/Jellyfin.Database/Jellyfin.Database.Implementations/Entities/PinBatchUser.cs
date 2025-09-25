using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// Represents the relationship between a PIN batch and a user.
    /// </summary>
    public class PinBatchUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PinBatchUser"/> class.
        /// </summary>
        public PinBatchUser()
        {
            CreatedDate = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Gets or sets the unique identifier for this relationship.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the batch ID.
        /// </summary>
        [Required]
        public Guid BatchId { get; set; }

        /// <summary>
        /// Gets or sets the user ID. Can be null if the PIN hasn't been used yet.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the actual PIN code (hashed).
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string PinCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original PIN code (for reference, encrypted).
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string? OriginalPin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this PIN is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the date when this PIN was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the date when this PIN was first used.
        /// </summary>
        public DateTime? FirstUsedDate { get; set; }

        /// <summary>
        /// Gets or sets the date when this PIN was last used.
        /// </summary>
        public DateTime? LastUsedDate { get; set; }

        /// <summary>
        /// Gets or sets the number of times this PIN has been used.
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Gets or sets the date when this PIN expires.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the date when this PIN was deactivated.
        /// </summary>
        public DateTime? DeactivatedDate { get; set; }

        /// <summary>
        /// Gets or sets the reason for deactivation.
        /// </summary>
        [MaxLength(500)]
        [StringLength(500)]
        public string? DeactivationReason { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for this PIN.
        /// </summary>
        [MaxLength(1000)]
        [StringLength(1000)]
        public string? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the last login.
        /// </summary>
        [MaxLength(45)]
        [StringLength(45)]
        public string? LastLoginIp { get; set; }

        /// <summary>
        /// Gets or sets the device name of the last login.
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string? LastLoginDevice { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the batch.
        /// </summary>
        [ForeignKey(nameof(BatchId))]
        public virtual PinBatch? Batch { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
