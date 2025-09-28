using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Events;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Events.Consumers.Pin
{
    /// <summary>
    /// Notifies admin users when a PIN is used.
    /// </summary>
    public class PinUsedNotifier : IEventConsumer<PinUsedEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinUsedNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PinUsedNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(PinUsedEventArgs eventArgs)
        {
            var data = new
            {
                Name = "PinUsed",
                Arguments = new
                {
                    PinId = eventArgs.PinId,
                    UserId = eventArgs.UserId,
                    BatchId = eventArgs.BatchId,
                    RemoteEndPoint = eventArgs.RemoteEndPoint,
                    UsedDate = eventArgs.UsedDate
                }
            };

            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.GeneralCommand, data, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
