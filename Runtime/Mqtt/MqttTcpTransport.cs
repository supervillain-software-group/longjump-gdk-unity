using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LongJump
{
    /// <summary>
    /// LongJump's own MQTT 3.1.1 client: only what <see cref="Events"/> needs. It connects over TCP
    /// with a clean session, subscribes at QoS 0, receives messages and keeps the connection alive.
    /// It never reconnects by itself; the GDK does that.
    /// </summary>
    /// <remarks>Not for WebGL, where games can't open TCP connections.</remarks>
    internal sealed class MqttTcpTransport : IMqttTransport
    {
        internal static readonly TimeSpan KeepAlive = TimeSpan.FromSeconds(60);
        internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
        internal const int MaxPacketBytes = 1024 * 1024;

        readonly object gate = new object();
        Connection current;
        int disconnects;

        public bool IsConnected
        {
            get
            {
                lock (gate) return current != null && current.IsOpen;
            }
        }

        public event Action<string, byte[]> MessageReceived;
        public event Action Disconnected;

        public async Task ConnectAsync(string host, int port, string clientId, CancellationToken cancellationToken)
        {
            await DisconnectAsync();
            int started;
            lock (gate) started = disconnects;
            var connection = new Connection(this);
            await connection.OpenAsync(host, port, clientId, cancellationToken);

            bool stillWanted;
            lock (gate)
            {
                stillWanted = disconnects == started;
                if (stillWanted) current = connection;
            }
            if (!stillWanted)
            {
                // DisconnectAsync ran while this was connecting.
                await connection.CloseAsync();
                throw new OperationCanceledException("Disconnected while connecting.");
            }
            connection.Start();
        }

        public Task SubscribeAsync(IReadOnlyList<string> topics, CancellationToken cancellationToken)
        {
            Connection connection;
            lock (gate) connection = current;
            if (connection == null || !connection.IsOpen) throw new IOException("Not connected.");
            return connection.SubscribeAsync(topics, cancellationToken);
        }

        public async Task DisconnectAsync()
        {
            Connection connection;
            lock (gate)
            {
                connection = current;
                current = null;
                disconnects++;
            }
            if (connection != null) await connection.CloseAsync();
        }

        void OnMessage(string topic, byte[] payload)
        {
            try
            {
                MessageReceived?.Invoke(topic, payload);
            }
            catch (Exception e)
            {
                Log.Warning($"Handling an MQTT message failed: {e.Message}");
            }
        }

        void OnLost(Connection connection)
        {
            lock (gate)
            {
                if (current != connection) return;
                current = null;
            }

            try
            {
                Disconnected?.Invoke();
            }
            catch (Exception e)
            {
                Log.Warning($"Handling an MQTT disconnect failed: {e.Message}");
            }
        }

        /// <summary>One TCP connection. Closed for good once it drops; the transport makes a new one to reconnect.</summary>
        sealed class Connection
        {
            readonly MqttTcpTransport transport;
            readonly CancellationTokenSource stop = new CancellationTokenSource();
            readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
            readonly ConcurrentDictionary<ushort, TaskCompletionSource<byte[]>> pendingSubscriptions =
                new ConcurrentDictionary<ushort, TaskCompletionSource<byte[]>>();
            readonly Stopwatch sinceLastPacket = new Stopwatch();
            TcpClient tcp;
            NetworkStream stream;
            int closed;
            int nextPacketId;

            internal Connection(MqttTcpTransport transport)
            {
                this.transport = transport;
            }

            internal bool IsOpen => stream != null && Volatile.Read(ref closed) == 0;

            internal async Task OpenAsync(string host, int port, string clientId, CancellationToken cancellationToken)
            {
                tcp = new TcpClient { NoDelay = true };
                try
                {
                    await WithTimeout(tcp.ConnectAsync(host, port), cancellationToken);
                    stream = tcp.GetStream();

                    await WriteAsync(Packet(0x10,
                        Str("MQTT"),
                        new byte[] { 0x04, 0x02, (byte)((int)KeepAlive.TotalSeconds >> 8), (byte)((int)KeepAlive.TotalSeconds & 0xFF) },
                        Str(clientId)), cancellationToken);

                    var connack = await WithTimeout(ReadPacketAsync(stop.Token), cancellationToken);
                    if (connack == null || connack.Value.Type != 2 || connack.Value.Body.Length < 2)
                        throw new IOException("The broker didn't acknowledge the connection.");
                    if (connack.Value.Body[1] != 0)
                        throw new IOException($"The broker refused the connection (return code {connack.Value.Body[1]}).");

                    sinceLastPacket.Restart();
                }
                catch
                {
                    Close();
                    throw;
                }
            }

            internal void Start()
            {
                Task.Run(ReadLoopAsync);
                Task.Run(KeepAliveLoopAsync);
            }

            internal async Task SubscribeAsync(IReadOnlyList<string> topics, CancellationToken cancellationToken)
            {
                if (topics.Count == 0) return;

                var id = NextPacketId();
                var result = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                pendingSubscriptions[id] = result;
                try
                {
                    var parts = new List<byte[]> { new[] { (byte)(id >> 8), (byte)(id & 0xFF) } };
                    foreach (var topic in topics)
                    {
                        parts.Add(Str(topic));
                        parts.Add(new byte[] { 0x00 });
                    }
                    await WriteAsync(Packet(0x82, parts.ToArray()), cancellationToken);

                    var codes = await WithTimeout(result.Task, cancellationToken);
                    if (codes.Length != topics.Count) throw new IOException("The broker's subscription answer didn't match the request.");
                    for (var i = 0; i < codes.Length; i++)
                        if (codes[i] == 0x80) throw new IOException($"The broker refused the subscription to {topics[i]}.");
                }
                finally
                {
                    pendingSubscriptions.TryRemove(id, out _);
                }
            }

            internal async Task CloseAsync()
            {
                if (!IsOpen)
                {
                    Close();
                    return;
                }

                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await WriteAsync(new byte[] { 0xE0, 0x00 }, timeout.Token);
                }
                catch
                {
                    // Closing anyway.
                }
                Close();
            }

            /// <summary>Closes the socket. True for the call that actually closed it.</summary>
            bool Close()
            {
                if (Interlocked.Exchange(ref closed, 1) != 0) return false;
                stop.Cancel();
                try
                {
                    tcp?.Dispose();
                }
                catch
                {
                    // Already closed.
                }
                foreach (var pending in pendingSubscriptions.Values)
                    pending.TrySetException(new IOException("The connection closed."));
                return true;
            }

            void Lost()
            {
                if (Close()) transport.OnLost(this);
            }

            async Task ReadLoopAsync()
            {
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        var packet = await ReadPacketAsync(stop.Token);
                        if (packet == null) break;
                        sinceLastPacket.Restart();
                        await HandleAsync(packet.Value);
                    }
                }
                catch (Exception e) when (!(e is OperationCanceledException) && IsOpen)
                {
                    Log.Warning($"The MQTT connection failed: {e.Message}");
                }
                catch
                {
                    // Closed on purpose, or already reported.
                }
                finally
                {
                    Lost();
                }
            }

            async Task KeepAliveLoopAsync()
            {
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromTicks(KeepAlive.Ticks / 2), stop.Token);
                        if (sinceLastPacket.Elapsed > TimeSpan.FromTicks(KeepAlive.Ticks * 3 / 2))
                        {
                            Log.Warning("The MQTT broker stopped answering.");
                            Lost();
                            return;
                        }
                        await WriteAsync(new byte[] { 0xC0, 0x00 }, stop.Token);
                    }
                }
                catch
                {
                    Lost();
                }
            }

            async Task HandleAsync((int Type, int Flags, byte[] Body) packet)
            {
                var body = packet.Body;
                switch (packet.Type)
                {
                    case 3: // PUBLISH
                        if (body.Length < 2) throw new InvalidDataException("A message was too short.");
                        var topicLength = (body[0] << 8) | body[1];
                        if (2 + topicLength > body.Length) throw new InvalidDataException("A message's topic was cut off.");
                        var topic = Encoding.UTF8.GetString(body, 2, topicLength);
                        var offset = 2 + topicLength;
                        var qos = (packet.Flags >> 1) & 0x03;
                        if (qos > 0)
                        {
                            if (offset + 2 > body.Length) throw new InvalidDataException("A message's id was cut off.");
                            var id = (ushort)((body[offset] << 8) | body[offset + 1]);
                            offset += 2;
                            // Subscriptions are QoS 0, so the broker shouldn't send these.
                            if (qos == 1) await WriteAsync(new byte[] { 0x40, 0x02, (byte)(id >> 8), (byte)(id & 0xFF) }, stop.Token);
                            else return;
                        }
                        var payload = new byte[body.Length - offset];
                        Buffer.BlockCopy(body, offset, payload, 0, payload.Length);
                        transport.OnMessage(topic, payload);
                        break;

                    case 9: // SUBACK
                        if (body.Length < 2) throw new InvalidDataException("A subscription answer was too short.");
                        var subscriptionId = (ushort)((body[0] << 8) | body[1]);
                        if (pendingSubscriptions.TryRemove(subscriptionId, out var pending))
                        {
                            var codes = new byte[body.Length - 2];
                            Buffer.BlockCopy(body, 2, codes, 0, codes.Length);
                            pending.TrySetResult(codes);
                        }
                        break;
                }
            }

            /// <summary>Reads one packet, or null when the broker closed the connection.</summary>
            async Task<(int Type, int Flags, byte[] Body)?> ReadPacketAsync(CancellationToken cancellationToken)
            {
                var one = new byte[1];
                if (!await ReadExactlyAsync(one, 1, cancellationToken)) return null;
                var header = one[0];

                var length = 0;
                for (var shift = 0; ; shift += 7)
                {
                    if (shift > 21) throw new InvalidDataException("A packet's length was malformed.");
                    if (!await ReadExactlyAsync(one, 1, cancellationToken)) return null;
                    length |= (one[0] & 0x7F) << shift;
                    if ((one[0] & 0x80) == 0) break;
                }
                if (length > MaxPacketBytes) throw new InvalidDataException($"A packet of {length} bytes is over the limit.");

                var body = new byte[length];
                if (length > 0 && !await ReadExactlyAsync(body, length, cancellationToken)) return null;
                return (header >> 4, header & 0x0F, body);
            }

            async Task<bool> ReadExactlyAsync(byte[] buffer, int count, CancellationToken cancellationToken)
            {
                var read = 0;
                while (read < count)
                {
                    var n = await stream.ReadAsync(buffer, read, count - read, cancellationToken);
                    if (n == 0) return false;
                    read += n;
                }
                return true;
            }

            async Task WriteAsync(byte[] packet, CancellationToken cancellationToken)
            {
                await writeLock.WaitAsync(cancellationToken);
                try
                {
                    await WithTimeout(stream.WriteAsync(packet, 0, packet.Length, cancellationToken), cancellationToken);
                }
                catch (TimeoutException)
                {
                    // A write that may still be in progress leaves the stream unusable.
                    Close();
                    throw;
                }
                finally
                {
                    writeLock.Release();
                }
            }

            ushort NextPacketId()
            {
                var id = (ushort)(Interlocked.Increment(ref nextPacketId) % 65535 + 1);
                return id;
            }

            static async Task WithTimeout(Task task, CancellationToken cancellationToken)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var winner = await Task.WhenAny(task, Task.Delay(Timeout, timeout.Token));
                if (winner != task)
                {
                    _ = task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException("The MQTT broker didn't answer in time.");
                }
                timeout.Cancel();
                await task;
            }

            static async Task<T> WithTimeout<T>(Task<T> task, CancellationToken cancellationToken)
            {
                await WithTimeout((Task)task, cancellationToken);
                return task.Result;
            }

            static byte[] Str(string value)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                if (bytes.Length > 65535) throw new ArgumentException("An MQTT string is limited to 65,535 bytes.");
                var result = new byte[bytes.Length + 2];
                result[0] = (byte)(bytes.Length >> 8);
                result[1] = (byte)(bytes.Length & 0xFF);
                Buffer.BlockCopy(bytes, 0, result, 2, bytes.Length);
                return result;
            }

            static byte[] Packet(byte header, params byte[][] parts)
            {
                var length = 0;
                foreach (var part in parts) length += part.Length;

                var encodedLength = new List<byte>(4);
                var remaining = length;
                do
                {
                    var digit = (byte)(remaining % 128);
                    remaining /= 128;
                    if (remaining > 0) digit |= 0x80;
                    encodedLength.Add(digit);
                } while (remaining > 0);

                var packet = new byte[1 + encodedLength.Count + length];
                packet[0] = header;
                encodedLength.CopyTo(packet, 1);
                var offset = 1 + encodedLength.Count;
                foreach (var part in parts)
                {
                    Buffer.BlockCopy(part, 0, packet, offset, part.Length);
                    offset += part.Length;
                }
                return packet;
            }
        }
    }
}
