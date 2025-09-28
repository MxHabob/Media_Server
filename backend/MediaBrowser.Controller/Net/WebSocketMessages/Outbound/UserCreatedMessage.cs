using System;
using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// User created message.
/// </summary>
public class UserCreatedMessage : OutboundWebSocketMessage<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreatedMessage"/> class.
    /// </summary>
    /// <param name="data">The user id.</param>
    public UserCreatedMessage(Guid data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.UserCreated)]
    public override SessionMessageType MessageType => SessionMessageType.UserCreated;
}
