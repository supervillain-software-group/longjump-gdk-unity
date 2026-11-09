using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LongJump.Api;
using LongJump.Api.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LongJump
{
    /// <summary>
    /// Real-time player events. The GDK connects once a player or device session signs in, stays
    /// connected, and calls your handlers on the main thread. Live leaderboards
    /// (<see cref="Leaderboards.Watch"/>) arrive over the same connection.
    /// </summary>
    /// <example>
    /// <code>
    /// LongJump.Events.On("item_get", e => ShowToast($"{e.Player.DisplayName} found {e.Data["item_name"]}"));
    /// </code>
    /// </example>
    public static class Events
    {
        /// <summary>Messages waiting for the main thread. When full, the oldest are dropped.</summary>
        const int MaxQueuedMessages = 1000;

        const int MaxDispatchedPerFrame = 200;
        static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(1);
        static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
        const float StableConnectionSeconds = 60f;

        static readonly object gate = new object();
        static readonly List<Handler> handlers = new List<Handler>();

        /// <summary>Topics subscribed besides the GameEnvironment's, with how many watchers want each. Under gate.</summary>
        static readonly Dictionary<string, int> extraTopics = new Dictionary<string, int>();
        static readonly ConcurrentQueue<object> received = new ConcurrentQueue<object>();
        static int receivedCount;
        static readonly RateLimitedWarning droppedWarning = new RateLimitedWarning();
        static readonly RateLimitedWarning malformedWarning = new RateLimitedWarning();
        static readonly string clientId = "longjump-" + Guid.NewGuid().ToString("N").Substring(0, 16);

        static IMqttTransport transport;
        static CancellationTokenSource run;
        static bool wanted;
        static bool loopRunning;
        static bool connected;
        static float connectedAt = float.NegativeInfinity;
        static TimeSpan retryDelay = FirstRetryDelay;
        static List<string> topics;
        static bool refreshTopics;
        static string brokerHost;
        static int brokerPort;

        /// <summary>Connected and subscribed.</summary>
        public static bool IsConnected => connected;

        /// <summary>Connected and subscribed, including after each reconnect. On the main thread.</summary>
        public static event Action Connected;

        /// <summary>The connection was lost; the GDK is reconnecting. On the main thread.</summary>
        public static event Action Disconnected;

        /// <summary>Calls <paramref name="handler"/> on the main thread for each player event named <paramref name="name"/>.</summary>
        /// <returns>Dispose it to stop.</returns>
        public static IDisposable On(string name, Action<GameEvent> handler)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Required.", nameof(name));
            return Add(new Handler(name, handler ?? throw new ArgumentNullException(nameof(handler))));
        }

        /// <summary>Calls <paramref name="handler"/> on the main thread for every player event.</summary>
        /// <returns>Dispose it to stop.</returns>
        public static IDisposable OnAny(Action<GameEvent> handler) =>
            Add(new Handler(null, handler ?? throw new ArgumentNullException(nameof(handler))));

        static IDisposable Add(Handler handler)
        {
            lock (gate) handlers.Add(handler);
            return handler;
        }

        sealed class Handler : IDisposable
        {
            internal readonly string Name;
            internal readonly Action<GameEvent> Callback;

            internal Handler(string name, Action<GameEvent> callback)
            {
                Name = name;
                Callback = callback;
            }

            public void Dispose()
            {
                lock (gate) handlers.Remove(this);
            }
        }

        // Topics beyond the GameEnvironment's

        /// <summary>
        /// Subscribes to a topic as well as the GameEnvironment's, now if connected and after every
        /// reconnect. Main thread.
        /// </summary>
        internal static void AddTopic(string topic)
        {
            bool first;
            lock (gate)
            {
                extraTopics.TryGetValue(topic, out var count);
                extraTopics[topic] = count + 1;
                first = count == 0;
            }
            if (first && connected) _ = SubscribeNowAsync(topic);
        }

        /// <summary>
        /// Stops wanting a topic. The transport can't unsubscribe, so messages on it may keep arriving
        /// until the next reconnect; with no watcher they go nowhere.
        /// </summary>
        internal static void RemoveTopic(string topic)
        {
            lock (gate)
            {
                if (!extraTopics.TryGetValue(topic, out var count)) return;
                if (count <= 1) extraTopics.Remove(topic);
                else extraTopics[topic] = count - 1;
            }
        }

        static List<string> TopicsToSubscribe()
        {
            lock (gate) return (topics ?? new List<string>()).Concat(extraTopics.Keys).Distinct().ToList();
        }

        static async Task SubscribeNowAsync(string topic)
        {
            var current = transport;
            var token = run?.Token ?? CancellationToken.None;
            try
            {
                await current.SubscribeAsync(new[] { topic }, token);
            }
            catch (Exception e) when (!token.IsCancellationRequested)
            {
                // A dropped connection reconnects and subscribes to every topic again.
                Log.Warning($"Couldn't subscribe to {topic}; it's subscribed again after the next reconnect: {e.Message}");
            }
            catch (Exception)
            {
                // Stopped meanwhile.
            }
        }

        // Connection

        internal static void Reset(IMqttTransport newTransport)
        {
            Stop();
            if (transport != null)
            {
                transport.MessageReceived -= OnMessage;
                transport.Disconnected -= OnTransportDisconnected;
            }
            transport = newTransport;
            topics = null;
            brokerHost = null;
            transport.MessageReceived += OnMessage;
            transport.Disconnected += OnTransportDisconnected;
            while (received.TryDequeue(out _)) Interlocked.Decrement(ref receivedCount);
        }

        /// <summary>After each sign-in: connect if not connected, and fetch the topics again before the next connect.</summary>
        internal static void Start()
        {
            refreshTopics = true;
            if (!wanted)
            {
                wanted = true;
                run = new CancellationTokenSource();
                retryDelay = FirstRetryDelay;
                EnsureLoop(waitFirst: false);
            }
            else if (!connected)
            {
                EnsureLoop(waitFirst: false);
            }
        }

        internal static void Stop()
        {
            wanted = false;
            run?.Cancel();
            var wasConnected = connected;
            connected = false;
            if (transport != null) _ = transport.DisconnectAsync();
            if (wasConnected) Log.Invoke(Disconnected, "Events.Disconnected");
        }

        static void OnTransportDisconnected() => MainThread.Post(() =>
        {
            var wasConnected = connected;
            connected = false;
            if (wasConnected) Log.Invoke(Disconnected, "Events.Disconnected");
            if (!wanted) return;

            // The wait starts over only after a connection that held for a while. A failed connect attempt
            // or a connection that drops right away keeps the backoff growing.
            if (wasConnected && Time.realtimeSinceStartup - connectedAt >= StableConnectionSeconds) retryDelay = FirstRetryDelay;
            EnsureLoop(waitFirst: true);
        });

        /// <summary>Starts the connect loop unless it's running. Only one runs at a time. Main thread.</summary>
        static void EnsureLoop(bool waitFirst)
        {
            if (loopRunning || !wanted) return;
            loopRunning = true;
            _ = RunLoopAsync(run, waitFirst);
        }

        static async Task RunLoopAsync(CancellationTokenSource loopRun, bool waitFirst)
        {
            var token = loopRun.Token;
            var loopTransport = transport;
            try
            {
                while (wanted && !token.IsCancellationRequested)
                {
                    if (waitFirst)
                    {
                        await Task.Delay(retryDelay, token);
                        retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, MaxRetryDelay.Ticks));
                    }
                    waitFirst = true;

                    try
                    {
                        if (topics == null || refreshTopics)
                        {
                            try
                            {
                                var answer = await Session.CallAsync(configuration =>
                                    new MQTTApi(configuration).GetMqttTopicsAsync(Setup.GameEnvironmentId, token));
                                // A fetch that finishes after Stop belongs to the old session.
                                token.ThrowIfCancellationRequested();
                                brokerHost = answer.Broker.Host;
                                brokerPort = answer.Broker.Port;
                                topics = answer.Topics?.ToList() ?? new List<string>();
                                refreshTopics = false;
                            }
                            catch (Exception e) when (!token.IsCancellationRequested && topics != null)
                            {
                                // Receiving needs no token, so an expired session or a server problem
                                // doesn't stop a reconnect: use the broker and topics from last time.
                                Log.Warning($"Couldn't fetch the real-time topics, using the last ones: {e.Message}");
                            }
                        }
                        token.ThrowIfCancellationRequested();

                        await loopTransport.ConnectAsync(brokerHost, brokerPort, clientId, token);
                        var subscribed = TopicsToSubscribe();
                        await loopTransport.SubscribeAsync(subscribed, token);
                        if (!wanted || token.IsCancellationRequested)
                        {
                            await DisconnectQuietlyAsync(loopTransport);
                            return;
                        }
                        // A drop reported while subscribing was handled before this loop said it connected.
                        if (!loopTransport.IsConnected) throw new IOException("The connection dropped while subscribing.");

                        connected = true;
                        connectedAt = Time.realtimeSinceStartup;
                        // Topics watched while this connection was subscribing.
                        foreach (var added in TopicsToSubscribe().Except(subscribed)) _ = SubscribeNowAsync(added);
                        Log.Invoke(Connected, "Events.Connected");
                        // Messages sent while disconnected are gone: watched boards read again.
                        Leaderboards.OnConnected();
                        return;
                    }
                    catch (Exception) when (token.IsCancellationRequested)
                    {
                        // Stopped while connecting: close whatever the connect finished opening.
                        await DisconnectQuietlyAsync(loopTransport);
                        return;
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Couldn't connect for real-time events, retrying in {retryDelay.TotalSeconds:0}s: {e.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Stopped.
            }
            finally
            {
                loopRunning = false;
                // Stop and Start may have run while this loop was finishing with the old run.
                if (wanted && !connected && loopRun != run) EnsureLoop(waitFirst: false);
            }
        }

        static async Task DisconnectQuietlyAsync(IMqttTransport connection)
        {
            try
            {
                await connection.DisconnectAsync();
            }
            catch (Exception e)
            {
                Log.Warning($"Disconnecting real-time events failed: {e.Message}");
            }
        }

        /// <summary>
        /// When Play starts: forgets the last Play session's handlers, queue and connection. Unity keeps
        /// static state between Play sessions when the project turns domain reload off.
        /// </summary>
        internal static void ResetForPlay()
        {
            Connected = null;
            Disconnected = null;
            Stop();
            if (transport != null)
            {
                transport.MessageReceived -= OnMessage;
                transport.Disconnected -= OnTransportDisconnected;
                transport = null;
            }
            lock (gate)
            {
                handlers.Clear();
                extraTopics.Clear();
            }
            while (received.TryDequeue(out _)) Interlocked.Decrement(ref receivedCount);
            run = null;
            connectedAt = float.NegativeInfinity;
            retryDelay = FirstRetryDelay;
            topics = null;
            brokerHost = null;
        }

        // Messages

        /// <summary>From the transport, on any thread. Parses, then queues for the main thread.</summary>
        static void OnMessage(string topic, byte[] payload)
        {
            object message;
            try
            {
                message = Parse(topic, payload);
            }
            catch (Exception e)
            {
                malformedWarning.Warn($"Dropped a real-time message that couldn't be read: {e.Message}");
                return;
            }
            if (message == null) return;

            received.Enqueue(message);
            if (Interlocked.Increment(ref receivedCount) > MaxQueuedMessages && received.TryDequeue(out _))
            {
                Interlocked.Decrement(ref receivedCount);
                droppedWarning.Warn($"More than {MaxQueuedMessages} real-time messages were waiting; dropped the oldest.");
            }
        }

        /// <summary>A GameEvent or LeaderboardStandings, or null for a type this version doesn't know.</summary>
        static object Parse(string topic, byte[] payload)
        {
            JObject json;
            var text = Encoding.UTF8.GetString(payload);
            using (var reader = new JsonTextReader(new StringReader(text)) { DateParseHandling = DateParseHandling.None })
                json = JObject.Load(reader);

            var type = (string)json["type"];
            var message = json["message"] as JObject;
            switch (type)
            {
                case "player_event":
                    var name = message == null ? null : (string)message["name"];
                    if (string.IsNullOrEmpty(name)) throw new InvalidDataException("A player event had no name.");
                    return new GameEvent(name, message["data"] as JObject, PlayerInfo.From(message["player"]), text);

                case "leaderboard_update":
                    return LeaderboardStandings.Parse(topic, false, message);

                case "leaderboard_period_closed":
                    return LeaderboardStandings.Parse(topic, true, message);

                default:
                    return null;
            }
        }

        /// <summary>Every frame on the main thread: hands queued messages to the game's handlers.</summary>
        internal static void Pump()
        {
            for (var i = 0; i < MaxDispatchedPerFrame && received.TryDequeue(out var message); i++)
            {
                Interlocked.Decrement(ref receivedCount);
                switch (message)
                {
                    case GameEvent gameEvent:
                        Handler[] current;
                        lock (gate) current = handlers.ToArray();
                        foreach (var handler in current)
                            if (handler.Name == null || handler.Name == gameEvent.Name)
                                Log.Invoke(handler.Callback, gameEvent, $"Events.On(\"{handler.Name}\")");
                        break;

                    case LeaderboardStandings standings:
                        Leaderboards.Deliver(standings);
                        break;
                }
            }
        }

        /// <summary>A warning logged at most once every 10 seconds, with how many were skipped.</summary>
        sealed class RateLimitedWarning
        {
            long lastTicks;
            int skipped;

            internal void Warn(string message)
            {
                var now = DateTime.UtcNow.Ticks;
                var last = Interlocked.Read(ref lastTicks);
                if (now - last < TimeSpan.TicksPerSecond * 10 || Interlocked.CompareExchange(ref lastTicks, now, last) != last)
                {
                    Interlocked.Increment(ref skipped);
                    return;
                }
                var count = Interlocked.Exchange(ref skipped, 0);
                Log.Warning(count > 0 ? $"{message} ({count} more like this not shown)" : message);
            }
        }
    }
}
