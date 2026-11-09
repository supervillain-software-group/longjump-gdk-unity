using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LongJump.Api;
using LongJump.Api.Client;
using LongJump.Api.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace LongJump
{
    /// <summary>
    /// Watching a leaderboard live, and posting a score safely. Everything else about leaderboards
    /// (listing them, a page of the top, the entries around a player, registering participants) is one
    /// call to the generated <c>LeaderboardsApi</c> through <see cref="Session.CallAsync{T}"/>.
    /// </summary>
    public static class Leaderboards
    {
        static readonly TimeSpan SubmitRetryDelay = TimeSpan.FromSeconds(1);
        static readonly TimeSpan FirstReadRetryDelay = TimeSpan.FromSeconds(1);
        static readonly TimeSpan MaxReadRetryDelay = TimeSpan.FromSeconds(30);
        static readonly TimeSpan AfterPeriodChange = TimeSpan.FromSeconds(1);

        // A change time the server sent that this device's clock already passed: the clock is
        // probably ahead of the server's, so read again shortly, for up to this long after it.
        static readonly TimeSpan ClockAhead = TimeSpan.FromMinutes(2);
        static readonly TimeSpan ClockAheadRetry = TimeSpan.FromSeconds(2);

        static readonly object gate = new object();
        static readonly List<Watcher> watchers = new List<Watcher>();

        /// <summary>
        /// Delivers a leaderboard's standings on the main thread: the current top right away, then each
        /// time it changes (at most about once a second), and the final standings when a period freezes.
        /// It reads the board again after every reconnect, after a period freezes, and when the period
        /// starts, ends, opens or closes, since the server sends nothing at those moments. Live changes
        /// arrive while <see cref="Events"/> is connected, which is once a player or device session has
        /// signed in.
        /// </summary>
        /// <param name="slug">The leaderboard's slug.</param>
        /// <param name="onUpdate">The current standings, and each change to them.</param>
        /// <param name="onPeriodClosed">The final standings of a period that froze. Optional.</param>
        /// <returns>Dispose it to stop.</returns>
        public static IDisposable Watch(string slug, Action<LeaderboardStandings> onUpdate,
            Action<LeaderboardStandings> onPeriodClosed = null)
        {
            if (string.IsNullOrEmpty(slug)) throw new ArgumentException("Required.", nameof(slug));
            if (onUpdate == null) throw new ArgumentNullException(nameof(onUpdate));
            Setup.RequireInitialized();

            var watcher = new Watcher(slug, onUpdate, onPeriodClosed);
            lock (gate) watchers.Add(watcher);
            watcher.Start();
            return watcher;
        }

        /// <summary>
        /// Posts a score. When the entry has no <c>RequestId</c>, the post gets a new one (the entry
        /// itself isn't changed). After a connection error, a timeout or a server error, it sends the
        /// post once more with the same id, so a score is neither lost nor counted twice. A refused post
        /// (any 4xx) isn't retried. If you set <c>RequestId</c> yourself, use a new one for each run: an id
        /// the server already accepted returns that first response.
        /// </summary>
        /// <exception cref="ApiException">The server refused the post; see its ErrorCode and ErrorContent.</exception>
        public static async Task<LeaderboardSubmissionResult> SubmitAsync(string slug, NewLeaderboardEntry entry)
        {
            if (string.IsNullOrEmpty(slug)) throw new ArgumentException("Required.", nameof(slug));
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            // A deep copy: the retry sends this run's values even if the game reuses its entry meanwhile.
            var post = new NewLeaderboardEntry(entry.Score,
                entry.Tiebreakers == null ? null : new Dictionary<string, long>(entry.Tiebreakers),
                CopyAttributes(entry.Attributes), entry.ParticipantId,
                string.IsNullOrEmpty(entry.RequestId) ? Guid.NewGuid().ToString("N") : entry.RequestId);

            Task<LeaderboardSubmissionResult> Post() => Session.CallAsync(configuration =>
                new LeaderboardsApi(configuration).SubmitLeaderboardEntryAsync(Setup.GameEnvironmentId, slug, post));

            try
            {
                return await Post();
            }
            catch (Exception e) when (IsTransient(e))
            {
                Log.Warning($"Posting a score to \"{slug}\" failed, sending it again: {e.Message}");
                await Task.Delay(SubmitRetryDelay);
                return await Post();
            }
        }

        /// <summary>
        /// The attributes as the generated client would send them (its naming, the game's other global
        /// JSON settings), read back unchanged, dates included.
        /// </summary>
        static JToken CopyAttributes(object attributes)
        {
            if (attributes == null) return null;
            var json = JsonConvert.SerializeObject(attributes, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new DefaultNamingStrategy { OverrideSpecifiedNames = false } }
            });
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                return JToken.ReadFrom(reader);
        }

        /// <summary>A connection error, a timeout or a server error: worth trying again.</summary>
        static bool IsTransient(Exception e) => e is ConnectionException || (e is ApiException api && api.ErrorCode >= 500);

        static Watcher[] Current()
        {
            lock (gate) return watchers.ToArray();
        }

        /// <summary>A live message from <see cref="Events"/>, on the main thread.</summary>
        internal static void Deliver(LeaderboardStandings standings)
        {
            foreach (var watcher in Current())
                if (watcher.Topic == standings.Topic) watcher.DeliverLive(standings);
        }

        /// <summary>Events connected or reconnected: messages may have been missed meanwhile.</summary>
        internal static void OnConnected()
        {
            foreach (var watcher in Current()) watcher.Refresh();
        }

        /// <summary><see cref="Setup.Initialize"/> ran again, maybe for another GameEnvironment.</summary>
        internal static void OnInitialized()
        {
            foreach (var watcher in Current()) watcher.Start();
        }

        internal static void ResetForPlay()
        {
            lock (gate)
            {
                foreach (var watcher in watchers) watcher.Disposed = true;
                watchers.Clear();
            }
        }

        sealed class Watcher : IDisposable
        {
            readonly string slug;
            readonly Action<LeaderboardStandings> onUpdate;
            readonly Action<LeaderboardStandings> onPeriodClosed;
            internal string Topic;
            internal bool Disposed;
            int liveMessages;
            bool reading;
            bool readAgain;
            int timerVersion;

            internal Watcher(string slug, Action<LeaderboardStandings> onUpdate, Action<LeaderboardStandings> onPeriodClosed)
            {
                this.slug = slug;
                this.onUpdate = onUpdate;
                this.onPeriodClosed = onPeriodClosed;
            }

            /// <summary>Subscribes to the board's topic in the current GameEnvironment and reads it.</summary>
            internal void Start()
            {
                if (Disposed) return;
                var topic = $"game_environment/{Setup.GameEnvironmentId}/leaderboards/{slug}";
                if (topic != Topic)
                {
                    if (Topic != null) Events.RemoveTopic(Topic);
                    Topic = topic;
                    Events.AddTopic(Topic);
                }
                Refresh();
            }

            internal void DeliverLive(LeaderboardStandings standings)
            {
                if (Disposed) return;
                // A read already running started before this message, so it's older: it's dropped.
                liveMessages++;
                if (standings.IsFinal)
                {
                    Log.Invoke(onPeriodClosed, standings, $"Leaderboards.Watch(\"{slug}\") period closed");
                    // What the board shows now: the next period may have started, or the same one frozen.
                    RefreshAfter(AfterPeriodChange);
                    return;
                }
                Deliver(standings);
            }

            void Deliver(LeaderboardStandings standings)
            {
                Log.Invoke(onUpdate, standings, $"Leaderboards.Watch(\"{slug}\")");
                ScheduleNextChange(standings.Period);
            }

            /// <summary>
            /// Reads again when the period next starts, ends, opens or closes. A change the server
            /// still expects but this device's clock already passed means the clock is ahead: then it
            /// reads again shortly, until the server's answer moves on.
            /// </summary>
            void ScheduleNextChange(StandingsPeriod period)
            {
                var now = DateTime.UtcNow;
                var times = new[] { period?.EndsAt, period?.NextOpenAt, period?.NextCloseAt }
                    .Where(time => time.HasValue)
                    .Select(time => time.Value)
                    .ToList();
                // A board's last period, frozen with nothing after it, keeps its past end for good.
                var ended = period != null && period.Frozen && period.NextOpenAt == null;
                timerVersion++;
                if (!ended && times.Any(time => time <= now && time > now - ClockAhead))
                {
                    RefreshAfter(ClockAheadRetry);
                    return;
                }
                var next = times.Where(time => time > now).DefaultIfEmpty(DateTime.MaxValue).Min();
                if (next != DateTime.MaxValue) RefreshAfter(next - now + AfterPeriodChange);
            }

            void RefreshAfter(TimeSpan delay)
            {
                var version = ++timerVersion;
                _ = RefreshLaterAsync(delay, version);
            }

            async Task RefreshLaterAsync(TimeSpan delay, int version)
            {
                // Task.Delay takes at most about 24 days; a later change is rescheduled by the next read.
                await Task.Delay(TimeSpan.FromTicks(Math.Min(delay.Ticks, TimeSpan.FromDays(24).Ticks)));
                if (version == timerVersion) Refresh();
            }

            /// <summary>Reads the current standings, unless a read is already running (then once more after it).</summary>
            internal void Refresh()
            {
                if (Disposed) return;
                if (reading)
                {
                    readAgain = true;
                    return;
                }
                reading = true;
                _ = ReadAsync();
            }

            /// <summary>
            /// Reads the current top, retrying connection and server errors with backoff. A live message
            /// that arrives during the read is newer, so the read is then dropped.
            /// </summary>
            async Task ReadAsync()
            {
                var delay = FirstReadRetryDelay;
                try
                {
                    while (!Disposed)
                    {
                        var liveBefore = liveMessages;
                        readAgain = false;
                        try
                        {
                            var environment = Setup.GameEnvironmentId;
                            var leaderboard = await Session.CallAsync(configuration =>
                                new LeaderboardsApi(configuration).GetLeaderboardAsync(environment, slug));
                            var page = await Session.CallAsync(configuration =>
                                new LeaderboardsApi(configuration).GetLeaderboardEntriesAsync(environment, slug,
                                    limit: Math.Max(1, leaderboard.LiveTopCount)));
                            if (!Disposed && liveMessages == liveBefore && environment == Setup.GameEnvironmentId)
                                Deliver(LeaderboardStandings.From(Topic, leaderboard, page));
                            if (!readAgain) return;
                        }
                        catch (ApiException e) when (e.ErrorCode == 404)
                        {
                            Log.Warning($"There's no leaderboard \"{slug}\" in this GameEnvironment, so watching it shows nothing.");
                            return;
                        }
                        catch (Exception e) when (IsTransient(e))
                        {
                            if (Disposed) return;
                            Log.Warning($"Couldn't read leaderboard \"{slug}\", retrying in {delay.TotalSeconds:0}s: {e.Message}");
                            await Task.Delay(delay);
                            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxReadRetryDelay.Ticks));
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"Couldn't read leaderboard \"{slug}\", and won't retry: {e.Message}");
                            return;
                        }
                    }
                }
                finally
                {
                    reading = false;
                }
            }

            public void Dispose()
            {
                lock (gate)
                {
                    if (Disposed) return;
                    Disposed = true;
                    watchers.Remove(this);
                }
                timerVersion++;
                if (Topic != null) Events.RemoveTopic(Topic);
            }
        }
    }
}
