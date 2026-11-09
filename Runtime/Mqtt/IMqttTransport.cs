using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LongJump
{
    /// <summary>
    /// The MQTT connection under <see cref="Events"/>. The GDK uses its own MQTT client
    /// unless the game passes another to <see cref="Setup.Initialize"/>, usually an adapter around an
    /// MQTT library the game already ships.
    /// </summary>
    /// <remarks>
    /// The transport only connects, subscribes and passes messages up. Reconnecting, backoff, choosing
    /// topics and parsing messages are the GDK's job, the same whichever transport is used.
    /// </remarks>
    public interface IMqttTransport
    {
        bool IsConnected { get; }

        /// <summary>Connects with a clean session. Throws when it can't.</summary>
        Task ConnectAsync(string host, int port, string clientId, CancellationToken cancellationToken);

        /// <summary>Subscribes at QoS 0 (at most once). Throws when the broker refuses or the connection drops.</summary>
        Task SubscribeAsync(IReadOnlyList<string> topics, CancellationToken cancellationToken);

        /// <summary>Disconnects on purpose. Doesn't raise <see cref="Disconnected"/>, and never throws.</summary>
        Task DisconnectAsync();

        /// <summary>A message arrived: its topic and payload. May be raised on any thread.</summary>
        event Action<string, byte[]> MessageReceived;

        /// <summary>The connection was lost, not closed with <see cref="DisconnectAsync"/>. May be raised on any thread.</summary>
        event Action Disconnected;
    }
}
