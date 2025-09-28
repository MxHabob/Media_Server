using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Events;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Events.Consumers.Pin
{
    /// <summary>
    /// Notifies admin users when a PIN batch is created.
    /// </summary>
    public class PinBatchCreatedNotifier : IEventConsumer<PinBatchCreatedEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinBatchCreatedNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PinBatchCreatedNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(PinBatchCreatedEventArgs eventArgs)
        {
            var data = new
            {
                Name = "PinBatchCreated",
                Arguments = new
                {
                    BatchId = eventArgs.BatchId,
                    BatchName = eventArgs.BatchName,
                    PinCount = eventArgs.PinCount,
                    CreatedDate = eventArgs.CreatedDate
                }
            };

            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.GeneralCommand, data, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
