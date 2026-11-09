using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace LongJump
{
    /// <summary>
    /// Runs <see cref="Events"/> over MQTTnet 2.x from com.github.asus4.mqtt-unity, for games that already
    /// ship it. This assembly only compiles when that package is installed.
    /// </summary>
    /// <remarks>
    /// A lost connection is whatever MQTTnet reports through its Disconnected event, nothing more. MQTTnet
    /// 2.x doesn't notice a connection that dies silently (a network drop with no reset): it keeps waiting
    /// for its keepalive answer, so the connection stays open but receives nothing until the operating
    /// system gives up on the socket. A new client is used for each connection, so an old client finishing
    /// its disconnect late can't touch the new connection.
    /// </remarks>
    /// <example><c>LongJump.Setup.Initialize(environmentId, mqttTransport: new MqttNetTransport());</c></example>
    public sealed class MqttNetTransport : IMqttTransport
    {
        readonly Func<IMqttClient> createClient;
        readonly object gate = new object();
        IMqttClient current;
        bool opened;

        public MqttNetTransport() : this(() => new MqttFactory().CreateMqttClient()) { }

        /// <param name="createClient">Makes a new MQTTnet client; called for each connection.</param>
        internal MqttNetTransport(Func<IMqttClient> createClient)
        {
            this.createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
        }

        public bool IsConnected
        {
            get
            {
                lock (gate) return opened && current != null && current.IsConnected;
            }
        }

        public event Action<string, byte[]> MessageReceived;
        public event Action Disconnected;

        public async Task ConnectAsync(string host, int port, string clientId, CancellationToken cancellationToken)
        {
            await DisconnectAsync();
            cancellationToken.ThrowIfCancellationRequested();

            var client = createClient();
            client.ApplicationMessageReceived += OnMessage;
            client.Disconnected += OnDisconnected;
            lock (gate)
            {
                current = client;
                opened = false;
            }

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(host, port)
                .WithClientId(clientId)
                .WithCleanSession()
                .WithKeepAlivePeriod(MqttTcpTransport.KeepAlive)
                .WithCommunicationTimeout(MqttTcpTransport.Timeout)
                .Build();
            try
            {
                await client.ConnectAsync(options);
            }
            catch
            {
                Detach(client);
                throw;
            }

            bool stillWanted;
            lock (gate)
            {
                stillWanted = current == client;
                if (stillWanted) opened = true;
            }
            if (!stillWanted)
            {
                // DisconnectAsync ran while this was connecting.
                Detach(client);
                Quietly(client.DisconnectAsync());
                throw new OperationCanceledException("Disconnected while connecting.");
            }
        }

        public async Task SubscribeAsync(IReadOnlyList<string> topics, CancellationToken cancellationToken)
        {
            if (topics.Count == 0) return;
            cancellationToken.ThrowIfCancellationRequested();
            IMqttClient client;
            lock (gate) client = opened ? current : null;
            if (client == null || !client.IsConnected) throw new InvalidOperationException("Not connected.");
            var filters = topics.Select(topic => new TopicFilterBuilder().WithTopic(topic).WithAtMostOnceQoS().Build()).ToList();
            var results = await client.SubscribeAsync(filters);
            var refused = results?.Where(result => result.ReturnCode == MqttSubscribeReturnCode.Failure).Select(result => result.TopicFilter.Topic).ToList();
            if (refused != null && refused.Count > 0)
                throw new InvalidOperationException($"The broker refused the subscription to {string.Join(", ", refused)}.");
        }

        public async Task DisconnectAsync()
        {
            IMqttClient client;
            lock (gate) client = current;
            if (client == null) return;
            Detach(client);

            try
            {
                if (client.IsConnected)
                {
                    var disconnect = client.DisconnectAsync();
                    await Task.WhenAny(disconnect, Task.Delay(MqttTcpTransport.Timeout));
                    Quietly(disconnect);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Disconnecting MQTT failed: {e.Message}");
            }
        }

        /// <summary>Stops listening to a client and forgets it, so nothing it does later reaches the GDK.</summary>
        void Detach(IMqttClient client)
        {
            client.ApplicationMessageReceived -= OnMessage;
            client.Disconnected -= OnDisconnected;
            lock (gate)
            {
                if (current != client) return;
                current = null;
                opened = false;
            }
        }

        static void Quietly(Task task) => task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);

        void OnMessage(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                MessageReceived?.Invoke(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload ?? Array.Empty<byte>());
            }
            catch (Exception exception)
            {
                Log.Warning($"Handling an MQTT message failed: {exception.Message}");
            }
        }

        // MQTTnet also raises Disconnected when a connect attempt fails; ConnectAsync reports that by
        // throwing, so only a connection that was open counts as lost.
        void OnDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            if (!(sender is IMqttClient client)) return;
            lock (gate)
            {
                if (current != client || !opened) return;
            }
            Detach(client);

            try
            {
                Disconnected?.Invoke();
            }
            catch (Exception exception)
            {
                Log.Warning($"Handling an MQTT disconnect failed: {exception.Message}");
            }
        }
    }
}
