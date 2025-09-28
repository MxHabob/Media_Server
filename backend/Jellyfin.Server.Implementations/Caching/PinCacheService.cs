using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.PinGeneration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Caching
{
    /// <summary>
    /// Service for caching PIN-related data to improve performance.
    /// </summary>
    public class PinCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<PinCacheService> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps;
        private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _batchCacheExpiration = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _statisticsCacheExpiration = TimeSpan.FromMinutes(5);

        // Cache key constants
        private const string BatchKeyPrefix = "pin_batch_";
        private const string BatchListKeyPrefix = "pin_batches_";
        private const string StatisticsKeyPrefix = "pin_stats_";
        private const string UserPinKeyPrefix = "user_pin_";

        /// <summary>
        /// Initializes a new instance of the <see cref="PinCacheService"/> class.
        /// </summary>
        /// <param name="memoryCache">The memory cache.</param>
        /// <param name="logger">The logger.</param>
        public PinCacheService(IMemoryCache memoryCache, ILogger<PinCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _cacheTimestamps = new ConcurrentDictionary<string, DateTime>();
        }

        /// <summary>
        /// Gets a PIN batch from cache.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>The cached batch or null if not found.</returns>
        public PinBatch? GetBatch(Guid batchId)
        {
            var key = $"{BatchKeyPrefix}{batchId}";
            return _memoryCache.Get<PinBatch>(key);
        }

        /// <summary>
        /// Caches a PIN batch.
        /// </summary>
        /// <param name="batch">The batch to cache.</param>
        public void SetBatch(PinBatch batch)
        {
            var key = $"{BatchKeyPrefix}{batch.Id}";
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _batchCacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(key, batch, options);
            _cacheTimestamps[key] = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a list of PIN batches from cache.
        /// </summary>
        /// <param name="status">Optional status filter.</param>
        /// <param name="subscriptionType">Optional subscription type filter.</param>
        /// <param name="createdByUserId">Optional creator filter.</param>
        /// <returns>The cached batch list or null if not found.</returns>
        public List<PinBatch>? GetBatchList(BatchStatus? status, SubscriptionType? subscriptionType, Guid? createdByUserId)
        {
            var key = GenerateBatchListKey(status, subscriptionType, createdByUserId);
            return _memoryCache.Get<List<PinBatch>>(key);
        }

        /// <summary>
        /// Caches a list of PIN batches.
        /// </summary>
        /// <param name="batches">The batches to cache.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="subscriptionType">Optional subscription type filter.</param>
        /// <param name="createdByUserId">Optional creator filter.</param>
        public void SetBatchList(List<PinBatch> batches, BatchStatus? status, SubscriptionType? subscriptionType, Guid? createdByUserId)
        {
            var key = GenerateBatchListKey(status, subscriptionType, createdByUserId);
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _batchCacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(key, batches, options);
            _cacheTimestamps[key] = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets batch statistics from cache.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <returns>The cached statistics or null if not found.</returns>
        public BatchStatistics? GetBatchStatistics(Guid batchId)
        {
            var key = $"{StatisticsKeyPrefix}{batchId}";
            return _memoryCache.Get<BatchStatistics>(key);
        }

        /// <summary>
        /// Caches batch statistics.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="statistics">The statistics to cache.</param>
        public void SetBatchStatistics(Guid batchId, BatchStatistics statistics)
        {
            var key = $"{StatisticsKeyPrefix}{batchId}";
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _statisticsCacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Priority = CacheItemPriority.High
            };

            _memoryCache.Set(key, statistics, options);
            _cacheTimestamps[key] = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets PIN batch users from cache.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="includeInactive">Whether to include inactive PINs.</param>
        /// <param name="includeExpired">Whether to include expired PINs.</param>
        /// <returns>The cached PIN batch users or null if not found.</returns>
        public List<PinBatchUser>? GetBatchPins(Guid batchId, bool includeInactive, bool includeExpired)
        {
            var key = $"{BatchKeyPrefix}{batchId}_pins_{includeInactive}_{includeExpired}";
            return _memoryCache.Get<List<PinBatchUser>>(key);
        }

        /// <summary>
        /// Caches PIN batch users.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="pins">The PINs to cache.</param>
        /// <param name="includeInactive">Whether to include inactive PINs.</param>
        /// <param name="includeExpired">Whether to include expired PINs.</param>
        public void SetBatchPins(Guid batchId, List<PinBatchUser> pins, bool includeInactive, bool includeExpired)
        {
            var key = $"{BatchKeyPrefix}{batchId}_pins_{includeInactive}_{includeExpired}";
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultCacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(key, pins, options);
            _cacheTimestamps[key] = DateTime.UtcNow;
        }

        /// <summary>
        /// Invalidates cache entries for a specific batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        public void InvalidateBatch(Guid batchId)
        {
            var keysToRemove = new List<string>
            {
                $"{BatchKeyPrefix}{batchId}",
                $"{StatisticsKeyPrefix}{batchId}"
            };

            // Remove batch-specific cache entries
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheTimestamps.TryRemove(key, out _);
            }

            // Remove batch list caches (they may contain this batch)
            InvalidateBatchLists();

            _logger.LogDebug("Invalidated cache for batch {BatchId}", batchId);
        }

        /// <summary>
        /// Invalidates all batch list caches.
        /// </summary>
        public void InvalidateBatchLists()
        {
            var keysToRemove = _cacheTimestamps.Keys
                .Where(k => k.StartsWith(BatchListKeyPrefix, StringComparison.Ordinal))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheTimestamps.TryRemove(key, out _);
            }

            _logger.LogDebug("Invalidated all batch list caches");
        }

        /// <summary>
        /// Clears all PIN-related cache entries.
        /// </summary>
        public void ClearAll()
        {
            var keysToRemove = _cacheTimestamps.Keys
                .Where(k => k.StartsWith(BatchKeyPrefix, StringComparison.Ordinal) || 
                           k.StartsWith(BatchListKeyPrefix, StringComparison.Ordinal) || 
                           k.StartsWith(StatisticsKeyPrefix, StringComparison.Ordinal) ||
                           k.StartsWith(UserPinKeyPrefix, StringComparison.Ordinal))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheTimestamps.TryRemove(key, out _);
            }

            _logger.LogInformation("Cleared all PIN-related cache entries");
        }

        /// <summary>
        /// Gets cache statistics for monitoring.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        public PinCacheStatistics GetCacheStatistics()
        {
            var now = DateTime.UtcNow;
            var totalEntries = _cacheTimestamps.Count;
            var expiredEntries = _cacheTimestamps.Count(kvp => 
                now - kvp.Value > _defaultCacheExpiration);

            return new PinCacheStatistics
            {
                TotalCacheEntries = totalEntries,
                ExpiredCacheEntries = expiredEntries,
                ActiveCacheEntries = totalEntries - expiredEntries,
                BatchCacheEntries = _cacheTimestamps.Count(k => k.Key.StartsWith(BatchKeyPrefix, StringComparison.Ordinal)),
                StatisticsCacheEntries = _cacheTimestamps.Count(k => k.Key.StartsWith(StatisticsKeyPrefix, StringComparison.Ordinal)),
                BatchListCacheEntries = _cacheTimestamps.Count(k => k.Key.StartsWith(BatchListKeyPrefix, StringComparison.Ordinal))
            };
        }

        /// <summary>
        /// Performs cache cleanup of expired entries.
        /// </summary>
        public void CleanupExpiredEntries()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cacheTimestamps
                .Where(kvp => now - kvp.Value > _defaultCacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _memoryCache.Remove(key);
                _cacheTimestamps.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
        }

        /// <summary>
        /// Generates a cache key for batch lists.
        /// </summary>
        /// <param name="status">Optional status filter.</param>
        /// <param name="subscriptionType">Optional subscription type filter.</param>
        /// <param name="createdByUserId">Optional creator filter.</param>
        /// <returns>The cache key.</returns>
        private static string GenerateBatchListKey(BatchStatus? status, SubscriptionType? subscriptionType, Guid? createdByUserId)
        {
            var parts = new List<string> { BatchListKeyPrefix };
            
            if (status.HasValue)
            {
                parts.Add($"status_{status.Value}");
            }
            
            if (subscriptionType.HasValue)
            {
                parts.Add($"type_{subscriptionType.Value}");
            }
            
            if (createdByUserId.HasValue)
            {
                parts.Add($"user_{createdByUserId.Value}");
            }

            return string.Join("_", parts);
        }
    }

    /// <summary>
    /// Contains PIN cache statistics.
    /// </summary>
    public class PinCacheStatistics
    {
        /// <summary>
        /// Gets or sets the total number of cache entries.
        /// </summary>
        public int TotalCacheEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of expired cache entries.
        /// </summary>
        public int ExpiredCacheEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of active cache entries.
        /// </summary>
        public int ActiveCacheEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of batch cache entries.
        /// </summary>
        public int BatchCacheEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of statistics cache entries.
        /// </summary>
        public int StatisticsCacheEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of batch list cache entries.
        /// </summary>
        public int BatchListCacheEntries { get; set; }
    }
}
