using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Events.Consumers.Users
{
    /// <summary>
    /// Notifies all sessions when a user is created.
    /// </summary>
    public class UserCreatedNotifier : IEventConsumer<UserCreatedEventArgs>
    {
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCreatedNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public UserCreatedNotifier(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(UserCreatedEventArgs eventArgs)
        {
            // Send to all admin sessions to notify about new user creation
            await _sessionManager.SendMessageToAdminSessions(
                SessionMessageType.UserCreated,
                eventArgs.Argument.Id.ToString("N", CultureInfo.InvariantCulture),
                CancellationToken.None).ConfigureAwait(false);
        }
    }
}
