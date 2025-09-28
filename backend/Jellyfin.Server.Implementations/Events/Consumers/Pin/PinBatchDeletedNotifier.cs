using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Events;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Events.Consumers.Pin
{
    /// <summary>
    /// Notifies admin users when a PIN batch is deleted.
    /// </summary>
    public class PinBatchDeletedNotifier : IEventConsumer<PinBatchDeletedEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinBatchDeletedNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PinBatchDeletedNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(PinBatchDeletedEventArgs eventArgs)
        {
            var data = new
            {
                Name = "PinBatchDeleted",
                Arguments = new
                {
                    BatchId = eventArgs.BatchId,
                    BatchName = eventArgs.BatchName,
                    PinCount = eventArgs.PinCount,
                    DeletedDate = eventArgs.DeletedDate
                }
            };

            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.GeneralCommand, data, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
