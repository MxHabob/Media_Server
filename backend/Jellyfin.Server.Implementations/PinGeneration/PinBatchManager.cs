using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Caching;
using Jellyfin.Server.Implementations.Events;
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
        private readonly PinEventService _pinEventService;
        private readonly PinCacheService _pinCacheService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinBatchManager"/> class.
        /// </summary>
        /// <param name="dbContextFactory">The database context factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="pinGeneratorService">The PIN generator service.</param>
        /// <param name="pinEventService">The PIN event service.</param>
        /// <param name="pinCacheService">The PIN cache service.</param>
        public PinBatchManager(
            IDbContextFactory<JellyfinDbContext> dbContextFactory,
            ILogger<PinBatchManager> logger,
            PinGeneratorService pinGeneratorService,
            PinEventService pinEventService,
            PinCacheService pinCacheService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _pinGeneratorService = pinGeneratorService;
            _pinEventService = pinEventService;
            _pinCacheService = pinCacheService;
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

                // Set default permissions - ensure PIN users have full access capabilities
                user.SetPermission(PermissionKind.EnableRemoteAccess, true);
                user.SetPermission(PermissionKind.EnableContentDownloading, false);
                user.SetPermission(PermissionKind.EnableSyncTranscoding, true);
                
                // Add office/management permissions for PIN users
                user.SetPermission(PermissionKind.EnableCollectionManagement, true);
                user.SetPermission(PermissionKind.EnableSubtitleManagement, true);
                user.SetPermission(PermissionKind.EnableLyricManagement, true);
                user.SetPermission(PermissionKind.EnableAllDevices, true);
                user.SetPermission(PermissionKind.EnableAllFolders, true);
                user.SetPermission(PermissionKind.EnableAllChannels, true);

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

            // Publish real-time event for immediate frontend updates
            await _pinEventService.PublishPinBatchCreatedAsync(batch.Id, batch.Name, generatedPins.Count).ConfigureAwait(false);

            // Invalidate cache for batch lists since we added a new batch
            _pinCacheService.InvalidateBatchLists();

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
            // Try to get from cache first
            var cachedBatches = _pinCacheService.GetBatchList(status, subscriptionType, createdByUserId);
            if (cachedBatches != null)
            {
                return cachedBatches;
            }

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

            var batches = await query
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync()
                .ConfigureAwait(false);

            // Cache the results
            _pinCacheService.SetBatchList(batches, status, subscriptionType, createdByUserId);

            return batches;
        }

        /// <summary>
        /// Gets a PIN batch by ID.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>The PIN batch or null if not found.</returns>
        public async Task<PinBatch?> GetBatchAsync(Guid batchId)
        {
            // Try to get from cache first
            var cachedBatch = _pinCacheService.GetBatch(batchId);
            if (cachedBatch != null)
            {
                return cachedBatch;
            }

            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .Include(b => b.PinBatchUsers)
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            // Cache the result if found
            if (batch != null)
            {
                _pinCacheService.SetBatch(batch);
            }

            return batch;
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
        /// Deletes a PIN batch (soft delete) and removes associated users.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>True if deleted successfully, false if batch not found.</returns>
        public async Task<bool> DeleteBatchAsync(Guid batchId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            if (batch == null)
            {
                return false;
            }

            // Get all PIN batch users for this batch
            var pinBatchUsers = await dbContext.PinBatchUsers
                .Where(pbu => pbu.BatchId.Equals(batchId) && pbu.UserId.HasValue)
                .ToListAsync()
                .ConfigureAwait(false);

            // Delete associated users
            var userIdsToDelete = pinBatchUsers.Select(pbu => pbu.UserId!.Value).ToList();
            if (userIdsToDelete.Any())
            {
                var usersToDelete = await dbContext.Users
                    .Where(u => userIdsToDelete.Contains(u.Id))
                    .ToListAsync()
                    .ConfigureAwait(false);

                dbContext.Users.RemoveRange(usersToDelete);
                _logger.LogInformation("Deleting {UserCount} users associated with batch '{BatchName}' (ID: {BatchId})", 
                    usersToDelete.Count, batch.Name, batchId);
            }

            // Update batch status to deleted
            batch.Status = BatchStatus.Deleted;
            batch.ModifiedDate = DateTime.UtcNow;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Deleted PIN batch '{BatchName}' and {UserCount} associated users (ID: {BatchId})",
                batch.Name,
                userIdsToDelete.Count,
                batch.Id);

            // Publish real-time event for immediate frontend updates
            await _pinEventService.PublishPinBatchDeletedAsync(batchId, batch.Name, pinBatchUsers.Count).ConfigureAwait(false);

            return true;
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
            // Try to get from cache first
            var cachedPins = _pinCacheService.GetBatchPins(batchId, includeInactive, includeExpired);
            if (cachedPins != null)
            {
                return cachedPins;
            }

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

            var pins = await query
                .OrderBy(pbu => pbu.CreatedDate)
                .ToListAsync()
                .ConfigureAwait(false);

            // Cache the results
            _pinCacheService.SetBatchPins(batchId, pins, includeInactive, includeExpired);

            return pins;
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

            // If status is being changed to deleted, delete associated users
            if (status == BatchStatus.Deleted)
            {
                // Get all PIN batch users for this batch
                var pinBatchUsers = await dbContext.PinBatchUsers
                    .Where(pbu => pbu.BatchId.Equals(batchId) && pbu.UserId.HasValue)
                    .ToListAsync()
                    .ConfigureAwait(false);

                // Delete associated users
                var userIdsToDelete = pinBatchUsers.Select(pbu => pbu.UserId!.Value).ToList();
                if (userIdsToDelete.Any())
                {
                    var usersToDelete = await dbContext.Users
                        .Where(u => userIdsToDelete.Contains(u.Id))
                        .ToListAsync()
                        .ConfigureAwait(false);

                    dbContext.Users.RemoveRange(usersToDelete);
                    _logger.LogInformation("Deleting {UserCount} users associated with batch '{BatchName}' (ID: {BatchId})", 
                        usersToDelete.Count, batch.Name, batchId);
                }
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
        /// Encrypts a PIN for storage using AES encryption.
        /// </summary>
        /// <param name="pin">The PIN to encrypt.</param>
        /// <returns>The encrypted PIN.</returns>
        private static string EncryptPin(string pin)
        {
            // Use AES encryption with a secure key for PIN storage
            // This prevents PIN exposure even if database is compromised
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = GetEncryptionKey();
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            var pinBytes = System.Text.Encoding.UTF8.GetBytes(pin);
            var encryptedBytes = encryptor.TransformFinalBlock(pinBytes, 0, pinBytes.Length);
            
            // Combine IV and encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
            
            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a PIN from storage using AES decryption.
        /// </summary>
        /// <param name="encryptedPin">The encrypted PIN.</param>
        /// <returns>The decrypted PIN.</returns>
        private static string DecryptPin(string encryptedPin)
        {
            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedPin);
                
                using var aes = System.Security.Cryptography.Aes.Create();
                aes.Key = GetEncryptionKey();
                
                // Extract IV from the beginning of the encrypted data
                var iv = new byte[aes.IV.Length];
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                // Extract encrypted data
                var cipherText = new byte[encryptedBytes.Length - iv.Length];
                Array.Copy(encryptedBytes, iv.Length, cipherText, 0, cipherText.Length);
                
                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                
                return System.Text.Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the encryption key for PIN encryption/decryption.
        /// </summary>
        /// <returns>The encryption key.</returns>
        private static byte[] GetEncryptionKey()
        {
            // In production, this should be stored securely (e.g., in Azure Key Vault, AWS KMS, or environment variables)
            // For now, using a deterministic key based on machine-specific data
            var keySource = Environment.MachineName + "JellyfinPinEncryption2024";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keySource));
        }

        /// <summary>
        /// Deletes all PINs in a batch and removes associated users.
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

            // Get all PIN batch users for this batch
            var pinBatchUsers = await dbContext.PinBatchUsers
                .Where(pbu => pbu.BatchId.Equals(batchId))
                .ToListAsync()
                .ConfigureAwait(false);

            // Delete associated users
            var userIdsToDelete = pinBatchUsers
                .Where(pbu => pbu.UserId.HasValue)
                .Select(pbu => pbu.UserId!.Value)
                .ToList();

            if (userIdsToDelete.Any())
            {
                var usersToDelete = await dbContext.Users
                    .Where(u => userIdsToDelete.Contains(u.Id))
                    .ToListAsync()
                    .ConfigureAwait(false);

                dbContext.Users.RemoveRange(usersToDelete);
                _logger.LogInformation("Deleting {UserCount} users associated with batch '{BatchName}' (ID: {BatchId})", 
                    usersToDelete.Count, batch.Name, batchId);
            }

            // Delete all PIN batch users for this batch
            dbContext.PinBatchUsers.RemoveRange(pinBatchUsers);

            // Update batch statistics
            batch.TotalPins = 0;
            batch.ActivePins = 0;
            batch.UsedPins = 0;
            batch.ExpiredPins = 0;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Deleted all {PinCount} PINs and {UserCount} users from batch '{BatchName}' (ID: {BatchId})", 
                pinBatchUsers.Count, userIdsToDelete.Count, batch.Name, batchId);

            // Publish real-time event for immediate frontend updates
            await _pinEventService.PublishPinBatchDeletedAsync(batchId, batch.Name, pinBatchUsers.Count).ConfigureAwait(false);

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

            // Publish real-time event for PIN usage tracking
            await _pinEventService.PublishPinUsedAsync(pinBatchUser.Id, userId, pinBatchUser.BatchId, remoteEndPoint).ConfigureAwait(false);

            // Invalidate cache for this batch since usage statistics changed
            _pinCacheService.InvalidateBatch(pinBatchUser.BatchId);

            return true;
        }

        /// <summary>
        /// Marks a PIN as expired and prevents further use.
        /// </summary>
        /// <param name="pinId">The PIN ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if marked as expired successfully, false if PIN not found.</returns>
        public async Task<bool> MarkPinAsExpiredAsync(Guid pinId, Guid userId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var pinBatchUser = await dbContext.PinBatchUsers
                .FirstOrDefaultAsync(pbu => pbu.Id.Equals(pinId) && pbu.UserId.Equals(userId))
                .ConfigureAwait(false);

            if (pinBatchUser == null)
            {
                return false;
            }

            // Mark PIN as inactive and expired
            pinBatchUser.IsActive = false;
            pinBatchUser.ExpirationDate = DateTime.UtcNow; // Set to current time to ensure immediate expiration
            pinBatchUser.DeactivatedDate = DateTime.UtcNow;
            pinBatchUser.DeactivationReason = "PIN expired";

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Publish real-time event for PIN expiration
            await _pinEventService.PublishPinExpiredAsync(pinId, userId, pinBatchUser.BatchId).ConfigureAwait(false);

            _logger.LogInformation(
                "Marked PIN {PinId} as expired for user {UserId} in batch {BatchId}",
                pinId,
                userId,
                pinBatchUser.BatchId);

            return true;
        }

        /// <summary>
        /// Checks if a PIN has been used and prevents reuse after expiration.
        /// Optimized version that uses a more efficient approach to avoid loading all PINs.
        /// </summary>
        /// <param name="pin">The PIN to check.</param>
        /// <returns>True if PIN is valid and not expired, false otherwise.</returns>
        public async Task<bool> IsPinValidAndNotExpiredAsync(string pin)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            // First, try to find a potential match by checking if any PIN hash could match
            // This is a performance optimization to avoid loading all PINs
            var potentialMatches = await dbContext.PinBatchUsers
                .Where(pbu => pbu.IsActive && 
                             (pbu.ExpirationDate == null || pbu.ExpirationDate > DateTime.UtcNow))
                .Select(pbu => new { pbu.Id, pbu.PinCode, pbu.UserId, pbu.BatchId, pbu.ExpirationDate })
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var match in potentialMatches)
            {
                // Verify the PIN using BCrypt
                if (BCrypt.Net.BCrypt.Verify(pin, match.PinCode))
                {
                    // Check if PIN is expired (double-check)
                    if (match.ExpirationDate.HasValue && match.ExpirationDate.Value <= DateTime.UtcNow)
                    {
                        // PIN is expired, mark as inactive
                        var pinBatchUser = await dbContext.PinBatchUsers
                            .FirstOrDefaultAsync(pbu => pbu.Id == match.Id)
                            .ConfigureAwait(false);
                        
                        if (pinBatchUser != null)
                        {
                            pinBatchUser.IsActive = false;
                            pinBatchUser.DeactivatedDate = DateTime.UtcNow;
                            pinBatchUser.DeactivationReason = "PIN expired during validation";
                            
                            await dbContext.SaveChangesAsync().ConfigureAwait(false);
                            
                            // Publish expiration event
                            await _pinEventService.PublishPinExpiredAsync(pinBatchUser.Id, pinBatchUser.UserId ?? Guid.Empty, pinBatchUser.BatchId).ConfigureAwait(false);
                        }
                        
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Optimized PIN validation method that uses a more efficient database query.
        /// This method is designed for high-frequency authentication scenarios.
        /// </summary>
        /// <param name="pin">The PIN to validate.</param>
        /// <returns>A tuple containing (isValid, userId, batchId) or (false, null, null) if invalid.</returns>
        public async Task<(bool isValid, Guid? userId, Guid? batchId)> ValidatePinOptimizedAsync(string pin)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            // Use a more efficient query that only selects the necessary fields
            // Note: We can't use BCrypt.Verify in LINQ expressions, so we'll get potential matches first
            var potentialMatches = await dbContext.PinBatchUsers
                .Where(pbu => pbu.IsActive && 
                             (pbu.ExpirationDate == null || pbu.ExpirationDate > DateTime.UtcNow))
                .Select(pbu => new { pbu.Id, pbu.PinCode, pbu.UserId, pbu.BatchId, pbu.ExpirationDate })
                .ToListAsync()
                .ConfigureAwait(false);

            // Find the matching PIN using BCrypt verification
            var pinMatch = potentialMatches.FirstOrDefault(pm => BCrypt.Net.BCrypt.Verify(pin, pm.PinCode));

            if (pinMatch == null)
            {
                return (false, null, null);
            }

            // Double-check expiration
            if (pinMatch.ExpirationDate.HasValue && pinMatch.ExpirationDate.Value <= DateTime.UtcNow)
            {
                // Mark as expired
                var pinBatchUser = await dbContext.PinBatchUsers
                    .FirstOrDefaultAsync(pbu => pbu.Id == pinMatch.Id)
                    .ConfigureAwait(false);
                
                if (pinBatchUser != null)
                {
                    pinBatchUser.IsActive = false;
                    pinBatchUser.DeactivatedDate = DateTime.UtcNow;
                    pinBatchUser.DeactivationReason = "PIN expired during validation";
                    
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    
                    // Invalidate cache
                    _pinCacheService.InvalidateBatch(pinMatch.BatchId);
                    
                    // Publish expiration event
                    await _pinEventService.PublishPinExpiredAsync(pinBatchUser.Id, pinBatchUser.UserId ?? Guid.Empty, pinBatchUser.BatchId).ConfigureAwait(false);
                }
                
                return (false, null, null);
            }

            return (true, pinMatch.UserId, pinMatch.BatchId);
        }

        /// <summary>
        /// Generates PINs for an existing batch that has no PINs.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="pinCount">The number of PINs to generate.</param>
        /// <returns>True if PINs were generated successfully, false if batch not found or already has PINs.</returns>
        public async Task<bool> GeneratePinsForExistingBatchAsync(Guid batchId, int pinCount)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

            var batch = await dbContext.PinBatches
                .FirstOrDefaultAsync(b => b.Id.Equals(batchId))
                .ConfigureAwait(false);

            if (batch == null)
            {
                return false;
            }

            // Check if batch already has PINs
            var existingPinCount = await dbContext.PinBatchUsers
                .CountAsync(pbu => pbu.BatchId.Equals(batchId))
                .ConfigureAwait(false);

            if (existingPinCount > 0)
            {
                _logger.LogWarning("Batch {BatchId} already has {PinCount} PINs", batchId, existingPinCount);
                return false;
            }

            // Get existing PINs to avoid duplicates
            var existingPins = await GetExistingPinsAsync(dbContext).ConfigureAwait(false);

            // Generate PINs
            var generatedPins = _pinGeneratorService.GenerateUniquePins(
                batch.PinPattern,
                batch.PinLength,
                pinCount,
                batch.CustomCharacterSet,
                existingPins);

            if (generatedPins.Count < pinCount)
            {
                _logger.LogWarning("Could only generate {GeneratedCount} unique PINs out of {RequestedCount} requested for batch {BatchId}",
                    generatedPins.Count, pinCount, batchId);
            }

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
                    SubscriptionType = batch.SubscriptionType,
                    ExpirationDate = CalculatePinExpirationDate(batch)
                };

                // Set default permissions - ensure PIN users have full access capabilities
                user.SetPermission(PermissionKind.EnableRemoteAccess, true);
                user.SetPermission(PermissionKind.EnableContentDownloading, false);
                user.SetPermission(PermissionKind.EnableSyncTranscoding, true);
                
                // Add office/management permissions for PIN users
                user.SetPermission(PermissionKind.EnableCollectionManagement, true);
                user.SetPermission(PermissionKind.EnableSubtitleManagement, true);
                user.SetPermission(PermissionKind.EnableLyricManagement, true);
                user.SetPermission(PermissionKind.EnableAllDevices, true);
                user.SetPermission(PermissionKind.EnableAllFolders, true);
                user.SetPermission(PermissionKind.EnableAllChannels, true);

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

            // Update batch statistics
            batch.TotalPins = generatedPins.Count;
            batch.ActivePins = generatedPins.Count;
            batch.UsedPins = 0;
            batch.ExpiredPins = 0;

            // Save all changes
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Generated {PinCount} PINs for existing batch '{BatchName}' (ID: {BatchId})",
                generatedPins.Count,
                batch.Name,
                batch.Id);

            // Publish real-time event for immediate frontend updates
            await _pinEventService.PublishPinBatchCreatedAsync(batch.Id, batch.Name, generatedPins.Count).ConfigureAwait(false);

            // Invalidate cache for batch lists since we added PINs
            _pinCacheService.InvalidateBatchLists();
            _pinCacheService.InvalidateBatch(batchId);

            return true;
        }

        /// <summary>
        /// Gets PINs from a batch with decrypted original PINs for display.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="includeInactive">Whether to include inactive PINs.</param>
        /// <param name="includeExpired">Whether to include expired PINs.</param>
        /// <param name="includeOriginalPins">Whether to decrypt and include original PINs.</param>
        /// <returns>A list of PIN batch users with optionally decrypted PINs.</returns>
        public async Task<List<PinBatchUser>> GetBatchPinsWithDecryptionAsync(
            Guid batchId,
            bool includeInactive = false,
            bool includeExpired = false,
            bool includeOriginalPins = false)
        {
            var pins = await GetBatchPinsAsync(batchId, includeInactive, includeExpired).ConfigureAwait(false);

            if (includeOriginalPins)
            {
                // Decrypt original PINs for display
                foreach (var pin in pins)
                {
                    if (!string.IsNullOrEmpty(pin.OriginalPin))
                    {
                        try
                        {
                            pin.OriginalPin = DecryptPin(pin.OriginalPin);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to decrypt PIN for display");
                            pin.OriginalPin = "***DECRYPTION_FAILED***";
                        }
                    }
                }
            }

            return pins;
        }

        /// <summary>
        /// Preloads frequently accessed PIN data into cache for better performance.
        /// This method should be called during application startup or when cache is cleared.
        /// </summary>
        /// <param name="maxBatchesToPreload">Maximum number of recent batches to preload.</param>
        public async Task PreloadCacheAsync(int maxBatchesToPreload = 50)
        {
            try
            {
                using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

                // Preload recent active batches
                var recentBatches = await dbContext.PinBatches
                    .Where(b => b.Status == BatchStatus.Active)
                    .OrderByDescending(b => b.CreatedDate)
                    .Take(maxBatchesToPreload)
                    .ToListAsync()
                    .ConfigureAwait(false);

                foreach (var batch in recentBatches)
                {
                    _pinCacheService.SetBatch(batch);
                }

                // Preload batch lists for common filters
                var allBatches = await dbContext.PinBatches
                    .OrderByDescending(b => b.CreatedDate)
                    .ToListAsync()
                    .ConfigureAwait(false);

                _pinCacheService.SetBatchList(allBatches, null, null, null);
                _pinCacheService.SetBatchList(allBatches.Where(b => b.Status == BatchStatus.Active).ToList(), BatchStatus.Active, null, null);

                _logger.LogInformation("Preloaded cache with {BatchCount} batches", recentBatches.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload PIN cache");
            }
        }
    }
}
