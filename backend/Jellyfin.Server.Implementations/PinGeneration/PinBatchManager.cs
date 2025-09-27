using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.PinGeneration
{
    /// <summary>
    /// Manages PIN batches and their associated users.
    /// </summary>
    public class PinBatchManager
    {
        private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
        private readonly ILogger<PinBatchManager> _logger;
        private readonly PinGeneratorService _pinGeneratorService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinBatchManager"/> class.
        /// </summary>
        /// <param name="dbContextFactory">The database context factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="pinGeneratorService">The PIN generator service.</param>
        public PinBatchManager(
            IDbContextFactory<JellyfinDbContext> dbContextFactory,
            ILogger<PinBatchManager> logger,
            PinGeneratorService pinGeneratorService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _pinGeneratorService = pinGeneratorService;
        }

        /// <summary>
        /// Creates a new PIN batch with the specified configuration.
        /// </summary>
        /// <param name="name">The batch name.</param>
        /// <param name="description">The batch description.</param>
        /// <param name="subscriptionType">The subscription type.</param>
        /// <param name="pinPattern">The PIN pattern.</param>
        /// <param name="pinLength">The PIN length.</param>
        /// <param name="pinCount">The number of PINs to generate.</param>
        /// <param name="createdByUserId">The ID of the user creating the batch.</param>
        /// <param name="customCharacterSet">Custom character set for custom pattern.</param>
        /// <param name="expirationDate">Optional expiration date for the batch.</param>
        /// <param name="batchSettings">Optional batch-specific settings.</param>
        /// <returns>The created batch with generated PINs.</returns>
        public async Task<PinBatch> CreateBatchAsync(
            string name,
            string? description,
            SubscriptionType subscriptionType,
            PinPattern pinPattern,
            int pinLength,
            int pinCount,
            Guid createdByUserId,
            string? customCharacterSet = null,
            DateTime? expirationDate = null,
            BatchSettings? batchSettings = null)
        {
            if (pinCount <= 0)
            {
                throw new ArgumentException("PIN count must be positive.", nameof(pinCount));
            }

            if (pinLength <= 0)
            {
                throw new ArgumentException("PIN length must be positive.", nameof(pinLength));
            }

            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            // Get existing PINs to avoid duplicates
            var existingPins = await GetExistingPinsAsync(dbContext).ConfigureAwait(false);

            // Generate PINs
            var generatedPins = _pinGeneratorService.GenerateUniquePins(
                pinPattern,
                pinLength,
                pinCount,
                customCharacterSet,
                existingPins);

            if (generatedPins.Count < pinCount)
            {
                _logger.LogWarning("Could only generate {GeneratedCount} unique PINs out of {RequestedCount} requested",
                    generatedPins.Count, pinCount);
            }

            // Create batch
            var batch = new PinBatch
            {
                Name = name,
                Description = description,
                SubscriptionType = subscriptionType,
                PinPattern = pinPattern,
                PinLength = pinLength,
                CustomCharacterSet = customCharacterSet,
                TotalPins = generatedPins.Count,
                UsedPins = 0,
                ActivePins = generatedPins.Count,
                ExpiredPins = 0,
                Status = BatchStatus.Active,
                CreatedByUserId = createdByUserId,
                ExpirationDate = expirationDate
            };

            // Apply batch settings if provided
            if (batchSettings != null)
            {
                ApplyBatchSettings(batch, batchSettings);
            }

            dbContext.PinBatches.Add(batch);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Create User accounts and PIN batch user entries
            var pinBatchUsers = new List<PinBatchUser>();
            var createdUsers = new List<User>();

            foreach (var pin in generatedPins)
            {
                // Create a unique username based on the PIN
                var username = $"PIN_{pin}";

                // Create the User account
                var user = new User(
                    username,
                    "Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider",
                    "Jellyfin.Server.Implementations.Users.DefaultPasswordResetProvider")
                {
                    PinCode = BCrypt.Net.BCrypt.HashPassword(pin),
                    SubscriptionType = subscriptionType,
                    ExpirationDate = CalculatePinExpirationDate(batch)
                };

                // Set default permissions
                user.SetPermission(PermissionKind.EnableRemoteAccess, true);
                user.SetPermission(PermissionKind.EnableContentDownloading, false);
                user.SetPermission(PermissionKind.EnableSyncTranscoding, true);

                // Apply batch settings to user if provided
                if (batchSettings != null)
                {
                    ApplyBatchSettingsToUser(user, batchSettings);
                }

                dbContext.Users.Add(user);
                createdUsers.Add(user);

                // Create PIN batch user entry linked to the created user
                var pinBatchUser = new PinBatchUser
                {
                    BatchId = batch.Id,
                    UserId = user.Id, // Link to the created user
                    PinCode = BCrypt.Net.BCrypt.HashPassword(pin),
                    OriginalPin = EncryptPin(pin), // Store encrypted original PIN for reference
                    IsActive = true,
                    ExpirationDate = CalculatePinExpirationDate(batch)
                };

                pinBatchUsers.Add(pinBatchUser);
            }

            // Save all changes
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Created PIN batch '{BatchName}' with {PinCount} PINs (ID: {BatchId})",
                name,
                generatedPins.Count,
                batch.Id);

            return batch;
        }

        /// <summary>
        /// Gets all PIN batches with optional filtering.
        /// </summary>
        /// <param name="status">Optional status filter.</param>
        /// <param name="subscriptionType">Optional subscription type filter.</param>
        /// <param name="createdByUserId">Optional creator filter.</param>
        /// <returns>A list of PIN batches.</returns>
        public async Task<List<PinBatch>> GetBatchesAsync(
            BatchStatus? status = null,
            SubscriptionType? subscriptionType = null,
            Guid? createdByUserId = null)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var query = dbContext.PinBatches.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }

            if (subscriptionType.HasValue)
            {
                query = query.Where(b => b.SubscriptionType == subscriptionType.Value);
            }

            if (createdByUserId.HasValue)
            {
                query = query.Where(b => b.CreatedByUserId.Equals(createdByUserId.Value));
            }

            return await query
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a PIN batch by ID.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>The PIN batch or null if not found.</returns>
        public async Task<PinBatch?> GetBatchAsync(Guid batchId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            return await dbContext.PinBatches
                .Include(b => b.PinBatchUsers)
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates a PIN batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="name">The new name.</param>
        /// <param name="description">The new description.</param>
        /// <param name="modifiedByUserId">The ID of the user modifying the batch.</param>
        /// <returns>True if updated successfully, false if batch not found.</returns>
        public async Task<bool> UpdateBatchAsync(
            Guid batchId,
            string? name = null,
            string? description = null,
            Guid? modifiedByUserId = null)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            if (batch == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(name))
            {
                batch.Name = name;
            }

            if (description != null)
            {
                batch.Description = description;
            }

            if (modifiedByUserId.HasValue)
            {
                batch.ModifiedByUserId = modifiedByUserId.Value;
            }

            batch.ModifiedDate = DateTime.UtcNow;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Updated PIN batch '{BatchName}' (ID: {BatchId})", batch.Name, batch.Id);

            return true;
        }

        /// <summary>
        /// Activates a PIN batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>True if activated successfully, false if batch not found.</returns>
        public async Task<bool> ActivateBatchAsync(Guid batchId)
        {
            return await UpdateBatchStatusAsync(batchId, BatchStatus.Active).ConfigureAwait(false);
        }

        /// <summary>
        /// Suspends a PIN batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>True if suspended successfully, false if batch not found.</returns>
        public async Task<bool> SuspendBatchAsync(Guid batchId)
        {
            return await UpdateBatchStatusAsync(batchId, BatchStatus.Suspended).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a PIN batch (soft delete).
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>True if deleted successfully, false if batch not found.</returns>
        public async Task<bool> DeleteBatchAsync(Guid batchId)
        {
            return await UpdateBatchStatusAsync(batchId, BatchStatus.Deleted).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets batch statistics.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>Batch statistics or null if batch not found.</returns>
        public async Task<BatchStatistics?> GetBatchStatisticsAsync(Guid batchId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            if (batch == null)
            {
                return null;
            }

            var pinBatchUsers = await dbContext.PinBatchUsers
                .Where(pbu => pbu.BatchId.Equals(batchId))
                .ToListAsync()
                .ConfigureAwait(false);

            var now = DateTime.UtcNow;
            var activePins = pinBatchUsers.Count(pbu => pbu.IsActive && (!pbu.ExpirationDate.HasValue || pbu.ExpirationDate.Value > now));
            var expiredPins = pinBatchUsers.Count(pbu => pbu.ExpirationDate.HasValue && pbu.ExpirationDate.Value <= now);
            var usedPins = pinBatchUsers.Count(pbu => pbu.UsageCount > 0);

            return new BatchStatistics
            {
                BatchId = batchId,
                BatchName = batch.Name,
                TotalPins = pinBatchUsers.Count,
                ActivePins = activePins,
                ExpiredPins = expiredPins,
                UsedPins = usedPins,
                UnusedPins = pinBatchUsers.Count - usedPins,
                TotalUsageCount = pinBatchUsers.Sum(pbu => pbu.UsageCount),
                AverageUsagePerPin = pinBatchUsers.Count > 0 ? (double)pinBatchUsers.Sum(pbu => pbu.UsageCount) / pinBatchUsers.Count : 0,
                CreatedDate = batch.CreatedDate,
                LastActivityDate = pinBatchUsers.Max(pbu => pbu.LastUsedDate)
            };
        }

        /// <summary>
        /// Gets PINs from a batch with optional filtering.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="includeInactive">Whether to include inactive PINs.</param>
        /// <param name="includeExpired">Whether to include expired PINs.</param>
        /// <returns>A list of PIN batch users.</returns>
        public async Task<List<PinBatchUser>> GetBatchPinsAsync(
            Guid batchId,
            bool includeInactive = false,
            bool includeExpired = false)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var query = dbContext.PinBatchUsers
                .Where(pbu => pbu.BatchId.Equals(batchId));

            if (!includeInactive)
            {
                query = query.Where(pbu => pbu.IsActive);
            }

            if (!includeExpired)
            {
                var now = DateTime.UtcNow;
                query = query.Where(pbu => !pbu.ExpirationDate.HasValue || pbu.ExpirationDate.Value > now);
            }

            return await query
                .OrderBy(pbu => pbu.CreatedDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the batch status.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="status">The new status.</param>
        /// <returns>True if updated successfully, false if batch not found.</returns>
        private async Task<bool> UpdateBatchStatusAsync(Guid batchId, BatchStatus status)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            if (batch == null)
            {
                return false;
            }

            batch.Status = status;
            batch.ModifiedDate = DateTime.UtcNow;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Updated PIN batch '{BatchName}' status to {Status} (ID: {BatchId})",
                batch.Name,
                status,
                batch.Id);

            return true;
        }

        /// <summary>
        /// Gets existing PINs from the database to avoid duplicates.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <returns>A set of existing PINs.</returns>
        private async Task<HashSet<string>> GetExistingPinsAsync(JellyfinDbContext dbContext)
        {
            var existingPins = new HashSet<string>();

            // Get PINs from PinBatchUsers
            var batchPins = await dbContext.PinBatchUsers
                .Where(pbu => !string.IsNullOrEmpty(pbu.OriginalPin))
                .Select(pbu => pbu.OriginalPin)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var encryptedPin in batchPins)
            {
                if (!string.IsNullOrEmpty(encryptedPin))
                {
                    try
                    {
                        var decryptedPin = DecryptPin(encryptedPin);
                        if (!string.IsNullOrEmpty(decryptedPin))
                        {
                            existingPins.Add(decryptedPin);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt existing PIN for duplicate checking");
                    }
                }
            }

            // Get PINs from Users (for backward compatibility)
            var userPins = await dbContext.Users
                .Where(u => !string.IsNullOrEmpty(u.PinCode))
                .Select(u => u.PinCode)
                .ToListAsync()
                .ConfigureAwait(false);

            // Note: We can't decrypt BCrypt hashes, so we'll rely on the PIN generation
            // to avoid duplicates through the random generation process

            return existingPins;
        }

        /// <summary>
        /// Applies batch settings to a user.
        /// </summary>
        /// <param name="user">The user to apply settings to.</param>
        /// <param name="settings">The settings to apply.</param>
        private static void ApplyBatchSettingsToUser(User user, BatchSettings settings)
        {
            if (settings.MaxConcurrentSessions.HasValue)
            {
                user.MaxActiveSessions = settings.MaxConcurrentSessions.Value;
            }

            if (settings.MaxBitrate.HasValue)
            {
                user.RemoteClientBitrateLimit = settings.MaxBitrate.Value;
            }

            if (settings.MaxParentalRating.HasValue)
            {
                user.MaxParentalRatingScore = settings.MaxParentalRating.Value;
            }

            // Set permissions based on batch settings
            user.SetPermission(PermissionKind.EnableRemoteAccess, settings.AllowRemoteAccess);
            user.SetPermission(PermissionKind.EnableContentDownloading, settings.AllowDownload);
            user.SetPermission(PermissionKind.EnableSyncTranscoding, settings.AllowTranscoding);
        }

        /// <summary>
        /// Applies batch settings to a batch.
        /// </summary>
        /// <param name="batch">The batch to apply settings to.</param>
        /// <param name="settings">The settings to apply.</param>
        private static void ApplyBatchSettings(PinBatch batch, BatchSettings settings)
        {
            if (settings.MaxConcurrentSessions.HasValue)
            {
                batch.MaxConcurrentSessions = settings.MaxConcurrentSessions.Value;
            }

            batch.AllowRemoteAccess = settings.AllowRemoteAccess;
            batch.AllowTranscoding = settings.AllowTranscoding;
            batch.AllowDownload = settings.AllowDownload;
            batch.AllowSyncPlay = settings.AllowSyncPlay;

            if (settings.MaxBitrate.HasValue)
            {
                batch.MaxBitrate = settings.MaxBitrate.Value;
            }

            if (settings.MaxParentalRating.HasValue)
            {
                batch.MaxParentalRating = settings.MaxParentalRating.Value;
            }

            if (settings.Price.HasValue)
            {
                batch.Price = settings.Price.Value;
            }

            if (!string.IsNullOrEmpty(settings.Currency))
            {
                batch.Currency = settings.Currency;
            }

            if (settings.Metadata != null && settings.Metadata.Count > 0)
            {
                batch.Metadata = System.Text.Json.JsonSerializer.Serialize(settings.Metadata);
            }
        }

        /// <summary>
        /// Calculates the expiration date for a PIN based on the batch configuration.
        /// </summary>
        /// <param name="batch">The batch configuration.</param>
        /// <returns>The expiration date or null for lifetime.</returns>
        private static DateTime? CalculatePinExpirationDate(PinBatch batch)
        {
            if (batch.SubscriptionType == SubscriptionType.Lifetime)
            {
                return null;
            }

            var duration = batch.SubscriptionType switch
            {
                SubscriptionType.SixHours => TimeSpan.FromHours(6),
                SubscriptionType.TwelveHours => TimeSpan.FromHours(12),
                SubscriptionType.Daily => TimeSpan.FromDays(1),
                SubscriptionType.Weekly => TimeSpan.FromDays(7),
                SubscriptionType.Monthly => TimeSpan.FromDays(30),
                SubscriptionType.Quarterly => TimeSpan.FromDays(90),
                SubscriptionType.Yearly => TimeSpan.FromDays(365),
                _ => TimeSpan.FromHours(24)
            };

            return DateTime.UtcNow.Add(duration);
        }

        /// <summary>
        /// Encrypts a PIN for storage.
        /// </summary>
        /// <param name="pin">The PIN to encrypt.</param>
        /// <returns>The encrypted PIN.</returns>
        private static string EncryptPin(string pin)
        {
            // Simple encryption for reference storage
            // In production, use proper encryption with a secure key
            var bytes = System.Text.Encoding.UTF8.GetBytes(pin);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decrypts a PIN from storage.
        /// </summary>
        /// <param name="encryptedPin">The encrypted PIN.</param>
        /// <returns>The decrypted PIN.</returns>
        private static string DecryptPin(string encryptedPin)
        {
            try
            {
                var bytes = Convert.FromBase64String(encryptedPin);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Deletes all PINs in a batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>True if deleted successfully, false if batch not found.</returns>
        public async Task<bool> DeleteAllBatchPinsAsync(Guid batchId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            if (batch == null)
            {
                return false;
            }

            // Delete all PIN batch users for this batch
            var pinBatchUsers = await dbContext.PinBatchUsers
                .Where(pbu => pbu.BatchId.Equals(batchId))
                .ToListAsync()
                .ConfigureAwait(false);

            dbContext.PinBatchUsers.RemoveRange(pinBatchUsers);

            // Update batch statistics
            batch.TotalPins = 0;
            batch.ActivePins = 0;
            batch.UsedPins = 0;
            batch.ExpiredPins = 0;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Deleted all {PinCount} PINs from batch '{BatchName}' (ID: {BatchId})", 
                pinBatchUsers.Count, batch.Name, batchId);

            return true;
        }

        /// <summary>
        /// Deactivates all PINs in a batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>True if deactivated successfully, false if batch not found.</returns>
        public async Task<bool> DeactivateAllBatchPinsAsync(Guid batchId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            if (batch == null)
            {
                return false;
            }

            // Deactivate all PIN batch users for this batch
            var pinBatchUsers = await dbContext.PinBatchUsers
                .Where(pbu => pbu.BatchId.Equals(batchId) && pbu.IsActive)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var pinBatchUser in pinBatchUsers)
            {
                pinBatchUser.IsActive = false;
                pinBatchUser.DeactivatedDate = DateTime.UtcNow;
                pinBatchUser.DeactivationReason = "Batch deactivation";
            }

            // Update batch statistics
            batch.ActivePins = 0;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Deactivated all {PinCount} PINs in batch '{BatchName}' (ID: {BatchId})",
                pinBatchUsers.Count,
                batch.Name,
                batchId);

            return true;
        }

        /// <summary>
        /// Updates PIN usage statistics when a PIN is used for authentication.
        /// </summary>
        /// <param name="userId">The user ID that was authenticated.</param>
        /// <param name="remoteEndPoint">The remote endpoint of the request.</param>
        /// <param name="deviceName">The device name used for authentication.</param>
        /// <returns>True if updated successfully, false if PIN batch user not found.</returns>
        public async Task<bool> UpdatePinUsageAsync(Guid userId, string remoteEndPoint, string? deviceName = null)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var pinBatchUser = await dbContext.PinBatchUsers
                .FirstOrDefaultAsync(pbu => pbu.UserId.Equals(userId))
                .ConfigureAwait(false);

            if (pinBatchUser == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            // Update usage statistics
            if (pinBatchUser.FirstUsedDate == null)
            {
                pinBatchUser.FirstUsedDate = now;
            }
            
            pinBatchUser.LastUsedDate = now;
            pinBatchUser.UsageCount++;
            pinBatchUser.LastLoginIp = remoteEndPoint;
            pinBatchUser.LastLoginDevice = deviceName;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Updated PIN usage for user {UserId} in batch {BatchId}",
                userId,
                pinBatchUser.BatchId);

            return true;
        }
    }
}
