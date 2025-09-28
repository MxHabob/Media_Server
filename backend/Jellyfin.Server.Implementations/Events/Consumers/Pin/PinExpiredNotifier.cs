using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Events;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Events.Consumers.Pin
{
    /// <summary>
    /// Notifies admin users when a PIN expires.
    /// </summary>
    public class PinExpiredNotifier : IEventConsumer<PinExpiredEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinExpiredNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public PinExpiredNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(PinExpiredEventArgs eventArgs)
        {
            var data = new
            {
                Name = "PinExpired",
                Arguments = new
                {
                    PinId = eventArgs.PinId,
                    UserId = eventArgs.UserId,
                    BatchId = eventArgs.BatchId,
                    ExpiredDate = eventArgs.ExpiredDate
                }
            };

            await _sessionManager.SendMessageToAdminSessions(SessionMessageType.GeneralCommand, data, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
