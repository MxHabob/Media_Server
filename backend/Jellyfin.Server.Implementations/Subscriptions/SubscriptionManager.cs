using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Data;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Subscriptions
{
    /// <summary>
    /// Manages subscription configurations and user subscriptions.
    /// </summary>
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
        private readonly ILogger<SubscriptionManager> _logger;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionManager"/> class.
        /// </summary>
        /// <param name="dbContextFactory">The database context factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userManager">The user manager.</param>
        public SubscriptionManager(
            IDbContextFactory<JellyfinDbContext> dbContextFactory,
            ILogger<SubscriptionManager> logger,
            IUserManager userManager)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SubscriptionConfiguration>> GetActiveConfigurationsAsync()
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await dbContext.SubscriptionConfigurations
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<SubscriptionConfiguration?> GetConfigurationAsync(Guid id)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            return await dbContext.SubscriptionConfigurations
                .FirstOrDefaultAsync(c => c.Id == id)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<SubscriptionConfiguration> CreateConfigurationAsync(SubscriptionConfiguration configuration, Guid createdByUserId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            
            configuration.CreatedByUserId = createdByUserId;
            configuration.CreatedDate = DateTime.UtcNow;
            
            dbContext.SubscriptionConfigurations.Add(configuration);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            
            _logger.LogInformation("Created subscription configuration: {Name} (ID: {Id})", configuration.Name, configuration.Id);
            return configuration;
        }

        /// <inheritdoc />
        public async Task<SubscriptionConfiguration> UpdateConfigurationAsync(SubscriptionConfiguration configuration, Guid modifiedByUserId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            
            var existing = await dbContext.SubscriptionConfigurations
                .FirstOrDefaultAsync(c => c.Id == configuration.Id)
                .ConfigureAwait(false);
            
            if (existing == null)
            {
                throw new InvalidOperationException($"Subscription configuration with ID {configuration.Id} not found.");
            }

            existing.Name = configuration.Name;
            existing.Description = configuration.Description;
            existing.SubscriptionType = configuration.SubscriptionType;
            existing.CustomDurationHours = configuration.CustomDurationHours;
            existing.MaxConcurrentSessions = configuration.MaxConcurrentSessions;
            existing.AllowRemoteAccess = configuration.AllowRemoteAccess;
            existing.MaxBitrate = configuration.MaxBitrate;
            existing.AllowTranscoding = configuration.AllowTranscoding;
            existing.MaxParentalRating = configuration.MaxParentalRating;
            existing.AllowDownload = configuration.AllowDownload;
            existing.AllowSyncPlay = configuration.AllowSyncPlay;
            existing.Price = configuration.Price;
            existing.Currency = configuration.Currency;
            existing.IsActive = configuration.IsActive;
            existing.SortOrder = configuration.SortOrder;
            existing.Metadata = configuration.Metadata;
            existing.ModifiedByUserId = modifiedByUserId;
            existing.ModifiedDate = DateTime.UtcNow;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            
            _logger.LogInformation("Updated subscription configuration: {Name} (ID: {Id})", configuration.Name, configuration.Id);
            return existing;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteConfigurationAsync(Guid id)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            
            var configuration = await dbContext.SubscriptionConfigurations
                .FirstOrDefaultAsync(c => c.Id == id)
                .ConfigureAwait(false);
            
            if (configuration == null)
            {
                return false;
            }

            // Check if any users are using this configuration
            var usersWithThisConfig = await dbContext.Users
                .Where(u => u.SubscriptionType == configuration.SubscriptionType)
                .CountAsync()
                .ConfigureAwait(false);

            if (usersWithThisConfig > 0)
            {
                _logger.LogWarning("Cannot delete subscription configuration {Name} (ID: {Id}) as {Count} users are using it", 
                    configuration.Name, configuration.Id, usersWithThisConfig);
                return false;
            }

            dbContext.SubscriptionConfigurations.Remove(configuration);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            
            _logger.LogInformation("Deleted subscription configuration: {Name} (ID: {Id})", configuration.Name, configuration.Id);
            return true;
        }

        /// <inheritdoc />
        public async Task<(User user, string pin)> CreateUserWithSubscriptionAsync(string username, Guid configurationId, int? customDurationHours = null)
        {
            var configuration = await GetConfigurationAsync(configurationId).ConfigureAwait(false);
            if (configuration == null)
            {
                throw new InvalidOperationException($"Subscription configuration with ID {configurationId} not found.");
            }

            if (!configuration.IsActive)
            {
                throw new InvalidOperationException($"Subscription configuration {configuration.Name} is not active.");
            }

            // Generate PIN
            var pin = GenerateSecurePin(6);
            
            // Calculate expiration date
            var expirationDate = CalculateExpirationDate(configuration, customDurationHours);
            
            // Create user with subscription
            var user = await _userManager.CreateUserWithSubscriptionAsync(username, pin, configuration, expirationDate).ConfigureAwait(false);
            
            _logger.LogInformation("Created user {Username} with subscription {SubscriptionName} (PIN: {Pin})", 
                username, configuration.Name, pin);
            
            return (user, pin);
        }

        /// <inheritdoc />
        public async Task<User> UpdateUserSubscriptionAsync(Guid userId, Guid configurationId, bool extendExisting = false)
        {
            var configuration = await GetConfigurationAsync(configurationId).ConfigureAwait(false);
            if (configuration == null)
            {
                throw new InvalidOperationException($"Subscription configuration with ID {configurationId} not found.");
            }

            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found.");
            }

            DateTime? newExpirationDate;
            if (extendExisting && user.ExpirationDate.HasValue)
            {
                var additionalDuration = CalculateDuration(configuration);
                newExpirationDate = user.ExpirationDate.Value.Add(additionalDuration);
            }
            else
            {
                newExpirationDate = CalculateExpirationDate(configuration);
            }

            user.SubscriptionType = configuration.SubscriptionType;
            user.ExpirationDate = newExpirationDate;
            
            // Apply configuration settings to user
            ApplyConfigurationToUser(user, configuration);
            
            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
            
            _logger.LogInformation("Updated subscription for user {Username} to {SubscriptionName}", 
                user.Username, configuration.Name);
            
            return user;
        }

        /// <inheritdoc />
        public async Task<User> ExtendUserSubscriptionAsync(Guid userId, int additionalHours)
        {
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found.");
            }

            if (user.ExpirationDate.HasValue)
            {
                user.ExpirationDate = user.ExpirationDate.Value.AddHours(additionalHours);
            }
            else
            {
                // If no expiration date (lifetime), set one based on additional hours
                user.ExpirationDate = DateTime.UtcNow.AddHours(additionalHours);
            }

            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
            
            _logger.LogInformation("Extended subscription for user {Username} by {Hours} hours", 
                user.Username, additionalHours);
            
            return user;
        }

        /// <inheritdoc />
        public async Task<SubscriptionStatistics> GetStatisticsAsync()
        {
            var users = _userManager.Users.ToList();
            var configurations = await GetActiveConfigurationsAsync().ConfigureAwait(false);
            
            var statistics = new SubscriptionStatistics
            {
                TotalActiveSubscriptions = users.Count(u => !u.ExpirationDate.HasValue || u.ExpirationDate.Value >= DateTime.UtcNow),
                TotalExpiredSubscriptions = users.Count(u => u.ExpirationDate.HasValue && u.ExpirationDate.Value < DateTime.UtcNow),
                TotalLifetimeSubscriptions = users.Count(u => u.SubscriptionType == SubscriptionType.Lifetime),
                SubscriptionsByType = users.GroupBy(u => u.SubscriptionType)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Calculate total revenue if pricing is configured
            statistics.TotalRevenue = configurations
                .Where(c => c.Price.HasValue)
                .Sum(c => c.Price!.Value * statistics.SubscriptionsByType.GetValueOrDefault(c.SubscriptionType, 0));

            // Calculate average duration
            var usersWithExpiration = users.Where(u => u.ExpirationDate.HasValue).ToList();
            if (usersWithExpiration.Any())
            {
                statistics.AverageDurationHours = usersWithExpiration
                    .Average(u => (u.ExpirationDate!.Value - DateTime.UtcNow).TotalHours);
            }

            return statistics;
        }

        /// <inheritdoc />
        public async Task<bool> IsSubscriptionValidAsync(Guid userId)
        {
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return false;
            }

            // Check if user has a PIN (subscription-based user)
            if (string.IsNullOrEmpty(user.PinCode))
            {
                return true; // Regular user without subscription restrictions
            }

            // Check expiration
            if (user.ExpirationDate.HasValue && user.ExpirationDate.Value < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<User>> GetUsersWithExpiringSubscriptionsAsync(int hoursBeforeExpiration = 24)
        {
            var expirationThreshold = DateTime.UtcNow.AddHours(hoursBeforeExpiration);
            
            return _userManager.Users
                .Where(u => u.ExpirationDate.HasValue && 
                           u.ExpirationDate.Value <= expirationThreshold && 
                           u.ExpirationDate.Value > DateTime.UtcNow)
                .ToList();
        }

        /// <summary>
        /// Generates a secure random PIN.
        /// </summary>
        /// <param name="length">The length of the PIN.</param>
        /// <returns>A secure random PIN.</returns>
        private static string GenerateSecurePin(int length)
        {
            const string chars = "0123456789";
            var bytes = RandomNumberGenerator.GetBytes(length);
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            return new string(result);
        }

        /// <summary>
        /// Calculates the expiration date based on subscription configuration.
        /// </summary>
        /// <param name="configuration">The subscription configuration.</param>
        /// <param name="customDurationHours">Custom duration in hours.</param>
        /// <returns>The expiration date or null for lifetime.</returns>
        private static DateTime? CalculateExpirationDate(SubscriptionConfiguration configuration, int? customDurationHours = null)
        {
            if (configuration.SubscriptionType == SubscriptionType.Lifetime)
            {
                return null;
            }

            var duration = CalculateDuration(configuration, customDurationHours);
            return DateTime.UtcNow.Add(duration);
        }

        /// <summary>
        /// Calculates the duration based on subscription configuration.
        /// </summary>
        /// <param name="configuration">The subscription configuration.</param>
        /// <param name="customDurationHours">Custom duration in hours.</param>
        /// <returns>The duration.</returns>
        private static TimeSpan CalculateDuration(SubscriptionConfiguration configuration, int? customDurationHours = null)
        {
            return configuration.SubscriptionType switch
            {
                SubscriptionType.SixHours => TimeSpan.FromHours(6),
                SubscriptionType.TwelveHours => TimeSpan.FromHours(12),
                SubscriptionType.Daily => TimeSpan.FromDays(1),
                SubscriptionType.Weekly => TimeSpan.FromDays(7),
                SubscriptionType.Monthly => TimeSpan.FromDays(30),
                SubscriptionType.Quarterly => TimeSpan.FromDays(90),
                SubscriptionType.Yearly => TimeSpan.FromDays(365),
                SubscriptionType.Custom => TimeSpan.FromHours(customDurationHours ?? configuration.CustomDurationHours ?? 24),
                _ => TimeSpan.Zero
            };
        }

        /// <summary>
        /// Applies configuration settings to a user.
        /// </summary>
        /// <param name="user">The user to apply settings to.</param>
        /// <param name="configuration">The configuration to apply.</param>
        private static void ApplyConfigurationToUser(User user, SubscriptionConfiguration configuration)
        {
            user.MaxActiveSessions = configuration.MaxConcurrentSessions;
            
            if (configuration.MaxParentalRating.HasValue)
            {
                user.MaxParentalRatingScore = configuration.MaxParentalRating.Value;
            }
            
            if (configuration.MaxBitrate.HasValue)
            {
                user.RemoteClientBitrateLimit = configuration.MaxBitrate.Value;
            }

            // Set permissions based on configuration
            if (!configuration.AllowRemoteAccess)
            {
                user.RemovePermission(PermissionKind.EnableRemoteAccess);
            }
            else
            {
                user.AddPermission(PermissionKind.EnableRemoteAccess);
            }

            if (!configuration.AllowDownload)
            {
                user.RemovePermission(PermissionKind.EnableContentDownloading);
            }
            else
            {
                user.AddPermission(PermissionKind.EnableContentDownloading);
            }

            if (!configuration.AllowSyncPlay)
            {
                user.SyncPlayAccess = SyncPlayUserAccessType.None;
            }
            else
            {
                user.SyncPlayAccess = SyncPlayUserAccessType.CreateAndJoinGroups;
            }
        }
    }
}
