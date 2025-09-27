using System;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Events
{
    /// <summary>
    /// Service for publishing PIN-related events for real-time updates.
    /// </summary>
    public class PinEventService
    {
        private readonly IEventManager _eventManager;
        private readonly ILogger<PinEventService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinEventService"/> class.
        /// </summary>
        /// <param name="eventManager">The event manager.</param>
        /// <param name="logger">The logger.</param>
        public PinEventService(IEventManager eventManager, ILogger<PinEventService> logger)
        {
            _eventManager = eventManager;
            _logger = logger;
        }

        /// <summary>
        /// Publishes a PIN batch created event.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="batchName">The batch name.</param>
        /// <param name="pinCount">The number of PINs created.</param>
        public async Task PublishPinBatchCreatedAsync(Guid batchId, string batchName, int pinCount)
        {
            try
            {
                var eventArgs = new PinBatchCreatedEventArgs
                {
                    BatchId = batchId,
                    BatchName = batchName,
                    PinCount = pinCount,
                    CreatedDate = DateTime.UtcNow
                };

                await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);
                _logger.LogInformation("Published PIN batch created event for batch '{BatchName}' (ID: {BatchId})", batchName, batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PIN batch created event for batch '{BatchName}' (ID: {BatchId})", batchName, batchId);
            }
        }

        /// <summary>
        /// Publishes a PIN batch deleted event.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="batchName">The batch name.</param>
        /// <param name="pinCount">The number of PINs deleted.</param>
        public async Task PublishPinBatchDeletedAsync(Guid batchId, string batchName, int pinCount)
        {
            try
            {
                var eventArgs = new PinBatchDeletedEventArgs
                {
                    BatchId = batchId,
                    BatchName = batchName,
                    PinCount = pinCount,
                    DeletedDate = DateTime.UtcNow
                };

                await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);
                _logger.LogInformation("Published PIN batch deleted event for batch '{BatchName}' (ID: {BatchId})", batchName, batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PIN batch deleted event for batch '{BatchName}' (ID: {BatchId})", batchName, batchId);
            }
        }

        /// <summary>
        /// Publishes a PIN used event.
        /// </summary>
        /// <param name="pinId">The PIN ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="remoteEndPoint">The remote endpoint.</param>
        public async Task PublishPinUsedAsync(Guid pinId, Guid userId, Guid batchId, string remoteEndPoint)
        {
            try
            {
                var eventArgs = new PinUsedEventArgs
                {
                    PinId = pinId,
                    UserId = userId,
                    BatchId = batchId,
                    RemoteEndPoint = remoteEndPoint,
                    UsedDate = DateTime.UtcNow
                };

                await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);
                _logger.LogInformation("Published PIN used event for PIN {PinId} in batch {BatchId}", pinId, batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PIN used event for PIN {PinId} in batch {BatchId}", pinId, batchId);
            }
        }

        /// <summary>
        /// Publishes a PIN expired event.
        /// </summary>
        /// <param name="pinId">The PIN ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="batchId">The batch ID.</param>
        public async Task PublishPinExpiredAsync(Guid pinId, Guid userId, Guid batchId)
        {
            try
            {
                var eventArgs = new PinExpiredEventArgs
                {
                    PinId = pinId,
                    UserId = userId,
                    BatchId = batchId,
                    ExpiredDate = DateTime.UtcNow
                };

                await _eventManager.PublishAsync(eventArgs).ConfigureAwait(false);
                _logger.LogInformation("Published PIN expired event for PIN {PinId} in batch {BatchId}", pinId, batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PIN expired event for PIN {PinId} in batch {BatchId}", pinId, batchId);
            }
        }
    }

    /// <summary>
    /// Event arguments for PIN batch created events.
    /// </summary>
    public class PinBatchCreatedEventArgs : GenericEventArgs<object>
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
        /// Gets or sets the number of PINs created.
        /// </summary>
        public int PinCount { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Event arguments for PIN batch deleted events.
    /// </summary>
    public class PinBatchDeletedEventArgs : GenericEventArgs<object>
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
        /// Gets or sets the number of PINs deleted.
        /// </summary>
        public int PinCount { get; set; }

        /// <summary>
        /// Gets or sets the deletion date.
        /// </summary>
        public DateTime DeletedDate { get; set; }
    }

    /// <summary>
    /// Event arguments for PIN used events.
    /// </summary>
    public class PinUsedEventArgs : GenericEventArgs<object>
    {
        /// <summary>
        /// Gets or sets the PIN ID.
        /// </summary>
        public Guid PinId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the batch ID.
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Gets or sets the remote endpoint.
        /// </summary>
        public string RemoteEndPoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the usage date.
        /// </summary>
        public DateTime UsedDate { get; set; }
    }

    /// <summary>
    /// Event arguments for PIN expired events.
    /// </summary>
    public class PinExpiredEventArgs : GenericEventArgs<object>
    {
        /// <summary>
        /// Gets or sets the PIN ID.
        /// </summary>
        public Guid PinId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the batch ID.
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime ExpiredDate { get; set; }
    }
}
