using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Security
{
    /// <summary>
    /// Service for managing PIN security features including rate limiting and audit logging.
    /// </summary>
    public class PinSecurityService
    {
        private readonly ILogger<PinSecurityService> _logger;
        private readonly ConcurrentDictionary<string, List<DateTime>> _pinAttempts;
        private readonly ConcurrentDictionary<string, List<DateTime>> _ipAttempts;
        private readonly ConcurrentDictionary<string, DateTime> _lockedPins;
        private readonly ConcurrentDictionary<string, DateTime> _lockedIps;

        // Configuration constants
        private const int MaxPinAttemptsPerMinute = 5;
        private const int MaxIpAttemptsPerMinute = 20;
        private const int LockoutDurationMinutes = 15;
        private const int MaxConsecutiveFailures = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinSecurityService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public PinSecurityService(ILogger<PinSecurityService> logger)
        {
            _logger = logger;
            _pinAttempts = new ConcurrentDictionary<string, List<DateTime>>();
            _ipAttempts = new ConcurrentDictionary<string, List<DateTime>>();
            _lockedPins = new ConcurrentDictionary<string, DateTime>();
            _lockedIps = new ConcurrentDictionary<string, DateTime>();
        }

        /// <summary>
        /// Records a PIN authentication attempt.
        /// </summary>
        /// <param name="pin">The PIN that was attempted.</param>
        /// <param name="ipAddress">The IP address of the attempt.</param>
        /// <param name="success">Whether the attempt was successful.</param>
        /// <param name="userId">The user ID if successful.</param>
        /// <returns>True if the attempt is allowed, false if rate limited.</returns>
        public async Task<bool> RecordPinAttemptAsync(string pin, string ipAddress, bool success, Guid? userId = null)
        {
            var now = DateTime.UtcNow;
            var pinKey = HashPin(pin);
            var ipKey = ipAddress;

            // Check if PIN or IP is currently locked
            if (IsLocked(pinKey, ipKey, now))
            {
                _logger.LogWarning("PIN authentication attempt blocked - PIN or IP is locked (IP: {IP})", ipAddress);
                return false;
            }

            // Record the attempt
            RecordAttempt(pinKey, now);
            RecordAttempt(ipKey, now);

            // Log the attempt
            await LogPinAttemptAsync(pin, ipAddress, success, userId).ConfigureAwait(false);

            if (success)
            {
                // Clear failed attempts on successful authentication
                ClearFailedAttempts(pinKey, ipKey);
                _logger.LogInformation("Successful PIN authentication (IP: {IP}, UserId: {UserId})", ipAddress, userId);
                return true;
            }

            // Check for rate limiting
            if (IsRateLimited(pinKey, ipKey, now))
            {
                LockPinOrIp(pinKey, ipKey, now);
                _logger.LogWarning("PIN authentication rate limited and locked (IP: {IP})", ipAddress);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a PIN is currently locked.
        /// </summary>
        /// <param name="pin">The PIN to check.</param>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if locked, false otherwise.</returns>
        public bool IsPinLocked(string pin, string ipAddress)
        {
            var now = DateTime.UtcNow;
            var pinKey = HashPin(pin);
            var ipKey = ipAddress;

            return IsLocked(pinKey, ipKey, now);
        }

        /// <summary>
        /// Gets security statistics for monitoring.
        /// </summary>
        /// <returns>Security statistics.</returns>
        public PinSecurityStatistics GetSecurityStatistics()
        {
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);
            var oneDayAgo = now.AddDays(-1);

            return new PinSecurityStatistics
            {
                CurrentlyLockedPins = _lockedPins.Count(kvp => kvp.Value > now),
                CurrentlyLockedIps = _lockedIps.Count(kvp => kvp.Value > now),
                FailedAttemptsLastHour = _pinAttempts.Values
                    .SelectMany(attempts => attempts)
                    .Count(attempt => attempt > oneHourAgo),
                FailedAttemptsLastDay = _pinAttempts.Values
                    .SelectMany(attempts => attempts)
                    .Count(attempt => attempt > oneDayAgo),
                UniqueIpsLastHour = _ipAttempts.Keys.Count(ip =>
                    _ipAttempts[ip].Any(attempt => attempt > oneHourAgo)),
                UniqueIpsLastDay = _ipAttempts.Keys.Count(ip =>
                    _ipAttempts[ip].Any(attempt => attempt > oneDayAgo))
            };
        }

        /// <summary>
        /// Clears all security data (for testing or maintenance).
        /// </summary>
        public void ClearAllSecurityData()
        {
            _pinAttempts.Clear();
            _ipAttempts.Clear();
            _lockedPins.Clear();
            _lockedIps.Clear();
            _logger.LogInformation("All PIN security data cleared");
        }

        /// <summary>
        /// Unlocks a specific PIN.
        /// </summary>
        /// <param name="pin">The PIN to unlock.</param>
        /// <returns>True if unlocked, false if not found.</returns>
        public bool UnlockPin(string pin)
        {
            var pinKey = HashPin(pin);
            var removed = _lockedPins.TryRemove(pinKey, out _);
            if (removed)
            {
                _logger.LogInformation("PIN manually unlocked");
            }

            return removed;
        }

        /// <summary>
        /// Unlocks a specific IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address to unlock.</param>
        /// <returns>True if unlocked, false if not found.</returns>
        public bool UnlockIp(string ipAddress)
        {
            var removed = _lockedIps.TryRemove(ipAddress, out _);
            if (removed)
            {
                _logger.LogInformation("IP address {IP} manually unlocked", ipAddress);
            }

            return removed;
        }

        /// <summary>
        /// Checks if a PIN or IP is currently locked.
        /// </summary>
        /// <param name="pinKey">The hashed PIN key.</param>
        /// <param name="ipKey">The IP key.</param>
        /// <param name="now">The current time.</param>
        /// <returns>True if locked, false otherwise.</returns>
        private bool IsLocked(string pinKey, string ipKey, DateTime now)
        {
            // Check PIN lock
            if (_lockedPins.TryGetValue(pinKey, out var pinLockTime) && pinLockTime > now)
            {
                return true;
            }

            // Check IP lock
            if (_lockedIps.TryGetValue(ipKey, out var ipLockTime) && ipLockTime > now)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Records an authentication attempt.
        /// </summary>
        /// <param name="key">The key (PIN or IP).</param>
        /// <param name="attemptTime">The time of the attempt.</param>
        private void RecordAttempt(string key, DateTime attemptTime)
        {
            _pinAttempts.AddOrUpdate(key,
                new List<DateTime> { attemptTime },
                (k, existing) =>
                {
                    existing.Add(attemptTime);
                    // Keep only recent attempts (last hour)
                    var cutoff = attemptTime.AddHours(-1);
                    existing.RemoveAll(t => t < cutoff);
                    return existing;
                });
        }

        /// <summary>
        /// Checks if a PIN or IP is rate limited.
        /// </summary>
        /// <param name="pinKey">The hashed PIN key.</param>
        /// <param name="ipKey">The IP key.</param>
        /// <param name="now">The current time.</param>
        /// <returns>True if rate limited, false otherwise.</returns>
        private bool IsRateLimited(string pinKey, string ipKey, DateTime now)
        {
            var oneMinuteAgo = now.AddMinutes(-1);

            // Check PIN rate limiting
            if (_pinAttempts.TryGetValue(pinKey, out var pinAttempts))
            {
                var recentPinAttempts = pinAttempts.Count(t => t > oneMinuteAgo);
                if (recentPinAttempts >= MaxPinAttemptsPerMinute)
                {
                    return true;
                }
            }

            // Check IP rate limiting
            if (_ipAttempts.TryGetValue(ipKey, out var ipAttempts))
            {
                var recentIpAttempts = ipAttempts.Count(t => t > oneMinuteAgo);
                if (recentIpAttempts >= MaxIpAttemptsPerMinute)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Locks a PIN or IP address.
        /// </summary>
        /// <param name="pinKey">The hashed PIN key.</param>
        /// <param name="ipKey">The IP key.</param>
        /// <param name="now">The current time.</param>
        private void LockPinOrIp(string pinKey, string ipKey, DateTime now)
        {
            var lockUntil = now.AddMinutes(LockoutDurationMinutes);

            // Lock PIN if it has too many attempts
            if (_pinAttempts.TryGetValue(pinKey, out var pinAttempts))
            {
                var recentPinAttempts = pinAttempts.Count(t => t > now.AddMinutes(-1));
                if (recentPinAttempts >= MaxPinAttemptsPerMinute)
                {
                    _lockedPins.AddOrUpdate(pinKey, lockUntil, (k, existing) => lockUntil);
                }
            }

            // Lock IP if it has too many attempts
            if (_ipAttempts.TryGetValue(ipKey, out var ipAttempts))
            {
                var recentIpAttempts = ipAttempts.Count(t => t > now.AddMinutes(-1));
                if (recentIpAttempts >= MaxIpAttemptsPerMinute)
                {
                    _lockedIps.AddOrUpdate(ipKey, lockUntil, (k, existing) => lockUntil);
                }
            }
        }

        /// <summary>
        /// Clears failed attempts for a PIN and IP.
        /// </summary>
        /// <param name="pinKey">The hashed PIN key.</param>
        /// <param name="ipKey">The IP key.</param>
        private void ClearFailedAttempts(string pinKey, string ipKey)
        {
            _pinAttempts.TryRemove(pinKey, out _);
            _ipAttempts.TryRemove(ipKey, out _);
        }

        /// <summary>
        /// Hashes a PIN for storage (one-way hash).
        /// </summary>
        /// <param name="pin">The PIN to hash.</param>
        /// <returns>The hashed PIN.</returns>
        private static string HashPin(string pin)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pin));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Logs a PIN authentication attempt.
        /// </summary>
        /// <param name="pin">The PIN that was attempted.</param>
        /// <param name="ipAddress">The IP address of the attempt.</param>
        /// <param name="success">Whether the attempt was successful.</param>
        /// <param name="userId">The user ID if successful.</param>
        private async Task LogPinAttemptAsync(string pin, string ipAddress, bool success, Guid? userId)
        {
            var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            var message = success
                ? "Successful PIN authentication attempt (IP: {IP}, UserId: {UserId})"
                : "Failed PIN authentication attempt (IP: {IP})";

            _logger.Log(logLevel, message, ipAddress, userId);

            // In a real implementation, you might want to store this in a database
            // for audit purposes and compliance requirements
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

}
