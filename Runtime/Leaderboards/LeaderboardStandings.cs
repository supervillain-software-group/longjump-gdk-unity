using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LongJump.Api.Model;
using Newtonsoft.Json.Linq;

namespace LongJump
{
    /// <summary>
    /// The top of a leaderboard's period, as <see cref="Leaderboards.Watch"/> delivers it: first the
    /// current standings, then each live update, and the final standings when a period freezes.
    /// </summary>
    public sealed class LeaderboardStandings
    {
        /// <summary>The leaderboard's slug.</summary>
        public string Slug { get; }

        /// <summary>The leaderboard's name.</summary>
        public string Name { get; }

        /// <summary>True for the final standings of a period that froze: its rank 1 is the winner.</summary>
        public bool IsFinal { get; }

        /// <summary>The period these standings are from.</summary>
        public StandingsPeriod Period { get; }

        /// <summary>How many visible entries the period has, beyond the ones listed.</summary>
        public int Total { get; }

        /// <summary>The top entries, best first, as many as the leaderboard's live top count.</summary>
        public IReadOnlyList<StandingsEntry> Entries { get; }

        internal string Topic { get; }

        LeaderboardStandings(string topic, string slug, string name, bool isFinal, StandingsPeriod period, int total,
            IReadOnlyList<StandingsEntry> entries)
        {
            Topic = topic;
            Slug = slug;
            Name = name;
            IsFinal = isFinal;
            Period = period;
            Total = total;
            Entries = entries;
        }

        /// <summary>A live message's <c>message</c>. Throws when it isn't a leaderboard's standings.</summary>
        internal static LeaderboardStandings Parse(string topic, bool isFinal, JObject message)
        {
            var leaderboard = message?["leaderboard"] as JObject;
            var slug = leaderboard == null ? null : (string)leaderboard["slug"];
            if (string.IsNullOrEmpty(slug)) throw new InvalidDataException("A leaderboard message had no leaderboard.");

            var entries = (message["entries"] as JArray ?? new JArray())
                .OfType<JObject>()
                .Select(StandingsEntry.Parse)
                .ToList();
            return new LeaderboardStandings(topic, slug, (string)leaderboard["name"], isFinal,
                StandingsPeriod.Parse(message["period"] as JObject), (int?)message["total"] ?? entries.Count, entries);
        }

        /// <summary>Standings read through the API, for the first delivery of a watch.</summary>
        internal static LeaderboardStandings From(string topic, Leaderboard leaderboard, LeaderboardPage page) =>
            new LeaderboardStandings(topic, leaderboard.Slug, leaderboard.Name, false, StandingsPeriod.From(page.Period),
                page.Total, (page.Entries ?? new List<LeaderboardEntry>()).Select(StandingsEntry.From).ToList());

        internal static DateTime? ReadTime(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type == JTokenType.Date) return ((DateTime)token).ToUniversalTime();
            return DateTime.TryParse((string)token, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var time) ? time : (DateTime?)null;
        }
    }

    /// <summary>One run of a leaderboard between resets. Times are UTC; null when they don't apply.</summary>
    public sealed class StandingsPeriod
    {
        /// <summary>Null for a leaderboard that never resets.</summary>
        public DateTime? StartsAt { get; }

        /// <summary>Null for a period that never ends.</summary>
        public DateTime? EndsAt { get; }

        /// <summary>Whether scores are accepted now.</summary>
        public bool Open { get; }

        /// <summary>Closed for good: these are the final results.</summary>
        public bool Frozen { get; }

        /// <summary>When the period freezes, if it will.</summary>
        public DateTime? FreezesAt { get; }

        /// <summary>When a closed period opens next, if it will.</summary>
        public DateTime? NextOpenAt { get; }

        /// <summary>When an open period stops accepting scores next, if it will.</summary>
        public DateTime? NextCloseAt { get; }

        StandingsPeriod(DateTime? startsAt, DateTime? endsAt, bool open, bool frozen, DateTime? freezesAt, DateTime? nextOpenAt,
            DateTime? nextCloseAt)
        {
            StartsAt = startsAt;
            EndsAt = endsAt;
            Open = open;
            Frozen = frozen;
            FreezesAt = freezesAt;
            NextOpenAt = nextOpenAt;
            NextCloseAt = nextCloseAt;
        }

        internal static StandingsPeriod Parse(JObject period) =>
            period == null
                ? new StandingsPeriod(null, null, false, false, null, null, null)
                : new StandingsPeriod(LeaderboardStandings.ReadTime(period["startsAt"]), LeaderboardStandings.ReadTime(period["endsAt"]),
                    (bool?)period["open"] ?? false, (bool?)period["frozen"] ?? false, LeaderboardStandings.ReadTime(period["freezesAt"]),
                    LeaderboardStandings.ReadTime(period["nextOpenAt"]), LeaderboardStandings.ReadTime(period["nextCloseAt"]));

        internal static StandingsPeriod From(LeaderboardPeriod period) =>
            period == null
                ? Parse(null)
                : new StandingsPeriod(Utc(period.StartsAt), Utc(period.EndsAt), period.Open, period.Frozen, Utc(period.FreezesAt),
                    Utc(period.NextOpenAt), Utc(period.NextCloseAt));

        static DateTime? Utc(DateTime? time) => time?.ToUniversalTime();
    }

    /// <summary>An entry in <see cref="LeaderboardStandings"/>.</summary>
    public sealed class StandingsEntry
    {
        /// <summary>1 is the best. Ranks never tie.</summary>
        public int Rank { get; }

        public long Score { get; }

        /// <summary>The tie-breaker values, by name.</summary>
        public IReadOnlyDictionary<string, long> Tiebreakers { get; }

        /// <summary>The attributes posted with the run that holds the rank. <c>Attributes.ToObject&lt;T&gt;()</c> reads them as your own type.</summary>
        public JObject Attributes { get; }

        /// <summary>When the result was reached, UTC.</summary>
        public DateTime? AchievedAt { get; }

        public StandingsOwner Owner { get; }

        StandingsEntry(int rank, long score, IReadOnlyDictionary<string, long> tiebreakers, JObject attributes, DateTime? achievedAt,
            StandingsOwner owner)
        {
            Rank = rank;
            Score = score;
            Tiebreakers = tiebreakers;
            Attributes = attributes;
            AchievedAt = achievedAt;
            Owner = owner;
        }

        internal static StandingsEntry Parse(JObject entry)
        {
            var tiebreakers = new Dictionary<string, long>();
            if (entry["tiebreakers"] is JObject values)
                foreach (var value in values.Properties())
                    if (value.Value.Type == JTokenType.Integer) tiebreakers[value.Name] = (long)value.Value;
            return new StandingsEntry((int?)entry["rank"] ?? 0, (long?)entry["score"] ?? 0, tiebreakers,
                entry["attributes"] as JObject ?? new JObject(), LeaderboardStandings.ReadTime(entry["achievedAt"]),
                StandingsOwner.Parse(entry["owner"] as JObject));
        }

        internal static StandingsEntry From(LeaderboardEntry entry) =>
            new StandingsEntry(entry.Rank, entry.Score, entry.Tiebreakers ?? new Dictionary<string, long>(),
                entry.Attributes == null ? new JObject() : JObject.FromObject(entry.Attributes), entry.AchievedAt.ToUniversalTime(),
                StandingsOwner.From(entry.Owner));
    }

    /// <summary>Who holds an entry: a signed-in player, or a participant a kiosk registered by name.</summary>
    public sealed class StandingsOwner
    {
        /// <summary>The player's or participant's id.</summary>
        public string Id { get; }

        /// <summary>True for a participant, false for a player.</summary>
        public bool IsParticipant { get; }

        /// <summary>The player's display name, or the participant's name.</summary>
        public string Name { get; }

        /// <summary>The player's avatar URL. Null for participants.</summary>
        public string AvatarUrl { get; }

        StandingsOwner(string id, bool isParticipant, string name, string avatarUrl)
        {
            Id = id;
            IsParticipant = isParticipant;
            Name = name;
            AvatarUrl = avatarUrl;
        }

        internal static StandingsOwner Parse(JObject owner)
        {
            if (owner == null) return new StandingsOwner(null, false, null, null);
            var isParticipant = (string)owner["type"] == "participant";
            return new StandingsOwner((string)owner["id"], isParticipant,
                isParticipant ? (string)owner["name"] : (string)owner["displayName"], (string)owner["avatarUrl"]);
        }

        internal static StandingsOwner From(LeaderboardOwner owner)
        {
            if (owner == null) return new StandingsOwner(null, false, null, null);
            var isParticipant = owner.Type == LeaderboardOwner.TypeEnum.Participant;
            return new StandingsOwner(owner.Id.ToString(), isParticipant, isParticipant ? owner.Name : owner.DisplayName, owner.AvatarUrl);
        }
    }
}
