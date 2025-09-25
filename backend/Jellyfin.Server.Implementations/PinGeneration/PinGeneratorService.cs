using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.PinGeneration
{
    /// <summary>
    /// Service for generating PIN codes with various patterns and configurations.
    /// </summary>
    public class PinGeneratorService
    {
        private readonly ILogger<PinGeneratorService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinGeneratorService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public PinGeneratorService(ILogger<PinGeneratorService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generates a single PIN with the specified pattern and length.
        /// </summary>
        /// <param name="pattern">The PIN pattern to use.</param>
        /// <param name="length">The length of the PIN.</param>
        /// <param name="customCharacterSet">Custom character set for custom pattern.</param>
        /// <returns>A generated PIN.</returns>
        public string GeneratePin(PinPattern pattern, int length, string? customCharacterSet = null)
        {
            if (length <= 0)
            {
                throw new ArgumentException("PIN length must be positive.", nameof(length));
            }

            var characterSet = GetCharacterSet(pattern, customCharacterSet);
            if (string.IsNullOrEmpty(characterSet))
            {
                throw new ArgumentException("Invalid character set for PIN generation.", nameof(pattern));
            }

            var bytes = RandomNumberGenerator.GetBytes(length);
            var result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(characterSet[bytes[i] % characterSet.Length]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Generates multiple unique PINs with the specified pattern and length.
        /// </summary>
        /// <param name="pattern">The PIN pattern to use.</param>
        /// <param name="length">The length of the PIN.</param>
        /// <param name="count">The number of PINs to generate.</param>
        /// <param name="customCharacterSet">Custom character set for custom pattern.</param>
        /// <param name="existingPins">Existing PINs to avoid duplicates.</param>
        /// <returns>A list of unique generated PINs.</returns>
        public List<string> GenerateUniquePins(
            PinPattern pattern, 
            int length, 
            int count, 
            string? customCharacterSet = null,
            HashSet<string>? existingPins = null)
        {
            if (count <= 0)
            {
                throw new ArgumentException("Count must be positive.", nameof(count));
            }

            var characterSet = GetCharacterSet(pattern, customCharacterSet);
            if (string.IsNullOrEmpty(characterSet))
            {
                throw new ArgumentException("Invalid character set for PIN generation.", nameof(pattern));
            }

            var generatedPins = new HashSet<string>();
            var existing = existingPins ?? new HashSet<string>();
            var maxAttempts = count * 10; // Prevent infinite loops
            var attempts = 0;

            while (generatedPins.Count < count && attempts < maxAttempts)
            {
                var pin = GeneratePin(pattern, length, customCharacterSet);
                
                if (!existing.Contains(pin) && !generatedPins.Contains(pin))
                {
                    generatedPins.Add(pin);
                }
                
                attempts++;
            }

            if (generatedPins.Count < count)
            {
                _logger.LogWarning("Could only generate {GeneratedCount} unique PINs out of {RequestedCount} requested", 
                    generatedPins.Count, count);
            }

            return generatedPins.ToList();
        }

        /// <summary>
        /// Generates PINs for a batch with the specified configuration.
        /// </summary>
        /// <param name="batch">The batch configuration.</param>
        /// <param name="count">The number of PINs to generate.</param>
        /// <param name="existingPins">Existing PINs to avoid duplicates.</param>
        /// <returns>A list of generated PINs with their metadata.</returns>
        public List<GeneratedPinInfo> GeneratePinsForBatch(
            PinBatch batch, 
            int count, 
            HashSet<string>? existingPins = null)
        {
            var pins = GenerateUniquePins(
                batch.PinPattern, 
                batch.PinLength, 
                count, 
                batch.CustomCharacterSet, 
                existingPins);

            var result = new List<GeneratedPinInfo>();
            var expirationDate = CalculateExpirationDate(batch);

            foreach (var pin in pins)
            {
                result.Add(new GeneratedPinInfo
                {
                    Pin = pin,
                    BatchId = batch.Id,
                    SubscriptionType = batch.SubscriptionType,
                    ExpirationDate = expirationDate,
                    MaxConcurrentSessions = batch.MaxConcurrentSessions,
                    AllowRemoteAccess = batch.AllowRemoteAccess,
                    MaxBitrate = batch.MaxBitrate,
                    AllowTranscoding = batch.AllowTranscoding,
                    MaxParentalRating = batch.MaxParentalRating,
                    AllowDownload = batch.AllowDownload,
                    AllowSyncPlay = batch.AllowSyncPlay,
                    CreatedDate = DateTime.UtcNow
                });
            }

            return result;
        }

        /// <summary>
        /// Validates a PIN against the specified pattern.
        /// </summary>
        /// <param name="pin">The PIN to validate.</param>
        /// <param name="pattern">The expected pattern.</param>
        /// <param name="length">The expected length.</param>
        /// <param name="customCharacterSet">Custom character set for custom pattern.</param>
        /// <returns>True if the PIN is valid, false otherwise.</returns>
        public bool ValidatePin(string pin, PinPattern pattern, int length, string? customCharacterSet = null)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length != length)
            {
                return false;
            }

            var characterSet = GetCharacterSet(pattern, customCharacterSet);
            if (string.IsNullOrEmpty(characterSet))
            {
                return false;
            }

            return pin.All(c => characterSet.Contains(c));
        }

        /// <summary>
        /// Gets the character set for the specified pattern.
        /// </summary>
        /// <param name="pattern">The PIN pattern.</param>
        /// <param name="customCharacterSet">Custom character set for custom pattern.</param>
        /// <returns>The character set.</returns>
        private static string GetCharacterSet(PinPattern pattern, string? customCharacterSet = null)
        {
            return pattern switch
            {
                PinPattern.Numeric => "0123456789",
                PinPattern.Alphanumeric => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                PinPattern.AlphanumericMixed => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
                PinPattern.Custom => customCharacterSet ?? "0123456789",
                _ => "0123456789"
            };
        }

        /// <summary>
        /// Calculates the expiration date based on the batch configuration.
        /// </summary>
        /// <param name="batch">The batch configuration.</param>
        /// <returns>The expiration date or null for lifetime.</returns>
        private static DateTime? CalculateExpirationDate(PinBatch batch)
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
    }

    /// <summary>
    /// Contains information about a generated PIN.
    /// </summary>
    public class GeneratedPinInfo
    {
        /// <summary>
        /// Gets or sets the generated PIN.
        /// </summary>
        public string Pin { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the batch ID.
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Gets or sets the subscription type.
        /// </summary>
        public SubscriptionType SubscriptionType { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

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
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
