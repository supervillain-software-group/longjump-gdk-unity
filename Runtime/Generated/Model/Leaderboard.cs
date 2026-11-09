/*
 * LongJump GDK API
 *
 * The LongJump Game Dev Kit (GDK) gives your game player sign-in, real-time events between players, leaderboards, cloud saves, and bug reports, errors and crashes. Call this API from your game client.  ## Games and environments  Your Team owns your Games, and each Game has one or more GameEnvironments, such as development, staging and production. Almost everything in the GDK belongs to a GameEnvironment rather than to the Game, which is why most endpoints start with a GameEnvironment id. Environments keep both configuration and data apart:  - **Configuration.** Each GameEnvironment has its own settings, starting with its Steam App ID, so a playtest, a demo and the full release can each be an environment of the same Game. - **Data.** Play sessions, player events, leaderboards and their entries, cloud saves and the real-time topics all belong to one GameEnvironment. Testing in staging never reaches production players' leaderboards or broadcasts.  A few things are shared by all of a Game's environments:  - **Players.** A player is one Steam account, the same player in every game and environment. - **Having played the Game.** Endpoints that require the player to have played the Game accept them in all of its environments once they've started a play session in any one. Posting a leaderboard score is the exception: it needs a play session in that GameEnvironment.  ## Player authentication  Most endpoints need a Bearer token. Your game gets one by [starting a play session](#tag/player-session/post/api/v1/game_environments/{game_environment_id}/steam_players) with a Steam auth ticket when it launches, then sends it on every request as `Authorization: Bearer <token>`.  Player tokens last two days. If a request answers `401`, start a new play session to get a fresh token.  New to leaderboards? The [Leaderboards guide](#tag/leaderboards) walks from a first board to daily resets, events, kiosks and live updates.  Games whose players have no account, such as a kiosk at an event, can instead [create a device session](#tag/device-sessions/post/api/v1/game_environments/{game_environment_id}/device_sessions) once their GameEnvironment allows it. A device session only works on the leaderboard endpoints, where it posts scores for participants it registers, and on the MQTT endpoint, to watch leaderboards live.  ## Real-time events  The GDK delivers real-time events over [MQTT](https://mqtt.org/), so your game needs an MQTT client such as [MQTTnet](https://github.com/dotnet/MQTTnet). After signing in, [fetch the broker and topics](#tag/mqtt/get/api/v1/game_environments/{game_environment_id}/mqtt) and subscribe to them. Always connect to the broker host from that response rather than a hardcoded one, so your game keeps working if the broker moves.  Every message is JSON with an `id`, a `type` and a `message`:  - `player_event`: another player [broadcast an event](#tag/player-events/post/api/v1/game_environments/{game_environment_id}/player_events). `message` has the event `name`, its `data` and the `player` who sent it.  Each leaderboard also has its own topic, `game_environment/{game_environment_id}/leaderboards/{slug}` (its `liveTopic`). Subscribe to it only on screens that show that leaderboard:  - `leaderboard_update`: the top of the current period changed. `message` has the `leaderboard` (`slug` and `name`), the `period`, the `total` number of entries, and the top `entries`, as many as the leaderboard's `liveTopCount`. Updates come at most about once a second per leaderboard, so a burst of scores becomes one message. - `leaderboard_period_closed`: a period froze. `message` has the same fields, with the final standings.  Messages aren't kept for late subscribers, and nothing is sent when a new period starts or a daily window opens or closes. So a screen reads the standings itself (`GET …/entries?limit=` its `liveTopCount`) when it starts, after every reconnect, after `leaderboard_period_closed`, and at the times its `period` gives (`endsAt`, `nextOpenAt`, `nextCloseAt`). `total` changes in messages only along with the top.  ## Errors  Errors answer with an HTTP status and a JSON body like `{\"error\": \"Not found\"}`. A `422` also includes `details`, the messages for each invalid request field. Rate-limited requests answer `429` with a `Retry-After` header, in seconds.  Every request body is limited to 2MB, and a larger one answers `413`. Files never go through this API: [issues](#tag/issues) upload theirs straight to storage.  ## Quick start  1. Make sure your Team has your Steam Publisher Web API key and your GameEnvironment has your Steam App ID. Note the GameEnvironment `id`. 2. When your game launches, call [ISteamUser::GetAuthTicketForWebApi](https://partner.steamgames.com/doc/api/ISteamUser#GetAuthTicketForWebApi) from the Steam SDK with an `identity` string of your choosing (5 to 30 characters), and hex-encode the ticket it returns. 3. [Start a play session](#tag/player-session/post/api/v1/game_environments/{game_environment_id}/steam_players) with the GameEnvironment `id`, your `identity` and the `ticket`. Keep `accessToken.token` from the response for every later request. 4. [Fetch the MQTT broker and topics](#tag/mqtt/get/api/v1/game_environments/{game_environment_id}/mqtt), connect to `broker.host` and subscribe to each topic. 5. Handle incoming messages by `type`. For `player_event`, decide how your game reacts to what other players did. 6. When your player does something worth sharing, [broadcast a player event](#tag/player-events/post/api/v1/game_environments/{game_environment_id}/player_events). You choose what goes in its `data`. 
 *
 * The version of the OpenAPI document: 1.0.0
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OpenAPIDateConverter = LongJump.Api.Client.OpenAPIDateConverter;

namespace LongJump.Api.Model
{
    /// <summary>
    /// Leaderboard
    /// </summary>
    [DataContract(Name = "Leaderboard")]
    public partial class Leaderboard
    {
        /// <summary>
        /// &#x60;desc&#x60;: higher is better. &#x60;asc&#x60;: lower is better
        /// </summary>
        /// <value>&#x60;desc&#x60;: higher is better. &#x60;asc&#x60;: lower is better</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ScoreOrderEnum
        {
            /// <summary>
            /// Enum Desc for value: desc
            /// </summary>
            [EnumMember(Value = "desc")]
            Desc = 1,

            /// <summary>
            /// Enum Asc for value: asc
            /// </summary>
            [EnumMember(Value = "asc")]
            Asc = 2
        }


        /// <summary>
        /// &#x60;desc&#x60;: higher is better. &#x60;asc&#x60;: lower is better
        /// </summary>
        /// <value>&#x60;desc&#x60;: higher is better. &#x60;asc&#x60;: lower is better</value>
        [DataMember(Name = "scoreOrder", IsRequired = true, EmitDefaultValue = true)]
        public ScoreOrderEnum ScoreOrder { get; set; }
        /// <summary>
        /// &#x60;best&#x60; keeps the better result, &#x60;latest&#x60; overwrites
        /// </summary>
        /// <value>&#x60;best&#x60; keeps the better result, &#x60;latest&#x60; overwrites</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum WriteModeEnum
        {
            /// <summary>
            /// Enum Best for value: best
            /// </summary>
            [EnumMember(Value = "best")]
            Best = 1,

            /// <summary>
            /// Enum Latest for value: latest
            /// </summary>
            [EnumMember(Value = "latest")]
            Latest = 2
        }


        /// <summary>
        /// &#x60;best&#x60; keeps the better result, &#x60;latest&#x60; overwrites
        /// </summary>
        /// <value>&#x60;best&#x60; keeps the better result, &#x60;latest&#x60; overwrites</value>
        [DataMember(Name = "writeMode", IsRequired = true, EmitDefaultValue = true)]
        public WriteModeEnum WriteMode { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Leaderboard" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Leaderboard() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Leaderboard" /> class.
        /// </summary>
        /// <param name="slug">slug (required).</param>
        /// <param name="name">name (required).</param>
        /// <param name="scoreOrder">&#x60;desc&#x60;: higher is better. &#x60;asc&#x60;: lower is better (required).</param>
        /// <param name="writeMode">&#x60;best&#x60; keeps the better result, &#x60;latest&#x60; overwrites (required).</param>
        /// <param name="tiebreakers">Compared in order when scores tie. A full tie goes to whoever reached the result first (required).</param>
        /// <param name="minScore">minScore (required).</param>
        /// <param name="maxScore">maxScore (required).</param>
        /// <param name="resetSchedule">Five-field cron in UTC. Null: never resets (required).</param>
        /// <param name="opensAt">Daily window opening, UTC. Null: always open (required).</param>
        /// <param name="closesAt">Daily window close, UTC (required).</param>
        /// <param name="startsAt">Accepts nothing before this (required).</param>
        /// <param name="endsAt">Accepts nothing after this (required).</param>
        /// <param name="maxSubmissionsPerPeriod">Posts allowed per owner per period. Null: unlimited (required).</param>
        /// <param name="participantsResetEachPeriod">participantsResetEachPeriod (required).</param>
        /// <param name="liveTopCount">How many entries live messages carry (required).</param>
        /// <param name="liveTopic">This leaderboard&#39;s MQTT topic (required).</param>
        /// <param name="period">period (required).</param>
        public Leaderboard(string slug = default, string name = default, ScoreOrderEnum scoreOrder = default, WriteModeEnum writeMode = default, List<LeaderboardTiebreaker> tiebreakers = default, long? minScore = default, long? maxScore = default, string resetSchedule = default, string opensAt = default, string closesAt = default, DateTime? startsAt = default, DateTime? endsAt = default, int? maxSubmissionsPerPeriod = default, bool participantsResetEachPeriod = default, int liveTopCount = default, string liveTopic = default, LeaderboardPeriod period = default)
        {
            // to ensure "slug" is required (not null)
            if (slug == null)
            {
                throw new ArgumentNullException("slug is a required property for Leaderboard and cannot be null");
            }
            this.Slug = slug;
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for Leaderboard and cannot be null");
            }
            this.Name = name;
            this.ScoreOrder = scoreOrder;
            this.WriteMode = writeMode;
            // to ensure "tiebreakers" is required (not null)
            if (tiebreakers == null)
            {
                throw new ArgumentNullException("tiebreakers is a required property for Leaderboard and cannot be null");
            }
            this.Tiebreakers = tiebreakers;
            // to ensure "minScore" is required (not null)
            if (minScore == null)
            {
                throw new ArgumentNullException("minScore is a required property for Leaderboard and cannot be null");
            }
            this.MinScore = minScore;
            // to ensure "maxScore" is required (not null)
            if (maxScore == null)
            {
                throw new ArgumentNullException("maxScore is a required property for Leaderboard and cannot be null");
            }
            this.MaxScore = maxScore;
            // to ensure "resetSchedule" is required (not null)
            if (resetSchedule == null)
            {
                throw new ArgumentNullException("resetSchedule is a required property for Leaderboard and cannot be null");
            }
            this.ResetSchedule = resetSchedule;
            // to ensure "opensAt" is required (not null)
            if (opensAt == null)
            {
                throw new ArgumentNullException("opensAt is a required property for Leaderboard and cannot be null");
            }
            this.OpensAt = opensAt;
            // to ensure "closesAt" is required (not null)
            if (closesAt == null)
            {
                throw new ArgumentNullException("closesAt is a required property for Leaderboard and cannot be null");
            }
            this.ClosesAt = closesAt;
            // to ensure "startsAt" is required (not null)
            if (startsAt == null)
            {
                throw new ArgumentNullException("startsAt is a required property for Leaderboard and cannot be null");
            }
            this.StartsAt = startsAt;
            // to ensure "endsAt" is required (not null)
            if (endsAt == null)
            {
                throw new ArgumentNullException("endsAt is a required property for Leaderboard and cannot be null");
            }
            this.EndsAt = endsAt;
            // to ensure "maxSubmissionsPerPeriod" is required (not null)
            if (maxSubmissionsPerPeriod == null)
            {
                throw new ArgumentNullException("maxSubmissionsPerPeriod is a required property for Leaderboard and cannot be null");
            }
            this.MaxSubmissionsPerPeriod = maxSubmissionsPerPeriod;
            this.ParticipantsResetEachPeriod = participantsResetEachPeriod;
            this.LiveTopCount = liveTopCount;
            // to ensure "liveTopic" is required (not null)
            if (liveTopic == null)
            {
                throw new ArgumentNullException("liveTopic is a required property for Leaderboard and cannot be null");
            }
            this.LiveTopic = liveTopic;
            // to ensure "period" is required (not null)
            if (period == null)
            {
                throw new ArgumentNullException("period is a required property for Leaderboard and cannot be null");
            }
            this.Period = period;
        }

        /// <summary>
        /// Gets or Sets Slug
        /// </summary>
        /*
        <example>daily-fight</example>
        */
        [DataMember(Name = "slug", IsRequired = true, EmitDefaultValue = true)]
        public string Slug { get; set; }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        /*
        <example>Daily fight</example>
        */
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// Compared in order when scores tie. A full tie goes to whoever reached the result first
        /// </summary>
        /// <value>Compared in order when scores tie. A full tie goes to whoever reached the result first</value>
        [DataMember(Name = "tiebreakers", IsRequired = true, EmitDefaultValue = true)]
        public List<LeaderboardTiebreaker> Tiebreakers { get; set; }

        /// <summary>
        /// Gets or Sets MinScore
        /// </summary>
        [DataMember(Name = "minScore", IsRequired = true, EmitDefaultValue = true)]
        public long? MinScore { get; set; }

        /// <summary>
        /// Gets or Sets MaxScore
        /// </summary>
        [DataMember(Name = "maxScore", IsRequired = true, EmitDefaultValue = true)]
        public long? MaxScore { get; set; }

        /// <summary>
        /// Five-field cron in UTC. Null: never resets
        /// </summary>
        /// <value>Five-field cron in UTC. Null: never resets</value>
        /*
        <example>0 5 * * *</example>
        */
        [DataMember(Name = "resetSchedule", IsRequired = true, EmitDefaultValue = true)]
        public string ResetSchedule { get; set; }

        /// <summary>
        /// Daily window opening, UTC. Null: always open
        /// </summary>
        /// <value>Daily window opening, UTC. Null: always open</value>
        /*
        <example>14:00</example>
        */
        [DataMember(Name = "opensAt", IsRequired = true, EmitDefaultValue = true)]
        public string OpensAt { get; set; }

        /// <summary>
        /// Daily window close, UTC
        /// </summary>
        /// <value>Daily window close, UTC</value>
        /*
        <example>22:00</example>
        */
        [DataMember(Name = "closesAt", IsRequired = true, EmitDefaultValue = true)]
        public string ClosesAt { get; set; }

        /// <summary>
        /// Accepts nothing before this
        /// </summary>
        /// <value>Accepts nothing before this</value>
        /*
        <example>2026-10-21T22:11:50.678Z</example>
        */
        [DataMember(Name = "startsAt", IsRequired = true, EmitDefaultValue = true)]
        public DateTime? StartsAt { get; set; }

        /// <summary>
        /// Accepts nothing after this
        /// </summary>
        /// <value>Accepts nothing after this</value>
        /*
        <example>2026-10-21T22:11:50.678Z</example>
        */
        [DataMember(Name = "endsAt", IsRequired = true, EmitDefaultValue = true)]
        public DateTime? EndsAt { get; set; }

        /// <summary>
        /// Posts allowed per owner per period. Null: unlimited
        /// </summary>
        /// <value>Posts allowed per owner per period. Null: unlimited</value>
        [DataMember(Name = "maxSubmissionsPerPeriod", IsRequired = true, EmitDefaultValue = true)]
        public int? MaxSubmissionsPerPeriod { get; set; }

        /// <summary>
        /// Gets or Sets ParticipantsResetEachPeriod
        /// </summary>
        [DataMember(Name = "participantsResetEachPeriod", IsRequired = true, EmitDefaultValue = true)]
        public bool ParticipantsResetEachPeriod { get; set; }

        /// <summary>
        /// How many entries live messages carry
        /// </summary>
        /// <value>How many entries live messages carry</value>
        [DataMember(Name = "liveTopCount", IsRequired = true, EmitDefaultValue = true)]
        public int LiveTopCount { get; set; }

        /// <summary>
        /// This leaderboard&#39;s MQTT topic
        /// </summary>
        /// <value>This leaderboard&#39;s MQTT topic</value>
        [DataMember(Name = "liveTopic", IsRequired = true, EmitDefaultValue = true)]
        public string LiveTopic { get; set; }

        /// <summary>
        /// Gets or Sets Period
        /// </summary>
        [DataMember(Name = "period", IsRequired = true, EmitDefaultValue = true)]
        public LeaderboardPeriod Period { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Leaderboard {\n");
            sb.Append("  Slug: ").Append(Slug).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  ScoreOrder: ").Append(ScoreOrder).Append("\n");
            sb.Append("  WriteMode: ").Append(WriteMode).Append("\n");
            sb.Append("  Tiebreakers: ").Append(Tiebreakers).Append("\n");
            sb.Append("  MinScore: ").Append(MinScore).Append("\n");
            sb.Append("  MaxScore: ").Append(MaxScore).Append("\n");
            sb.Append("  ResetSchedule: ").Append(ResetSchedule).Append("\n");
            sb.Append("  OpensAt: ").Append(OpensAt).Append("\n");
            sb.Append("  ClosesAt: ").Append(ClosesAt).Append("\n");
            sb.Append("  StartsAt: ").Append(StartsAt).Append("\n");
            sb.Append("  EndsAt: ").Append(EndsAt).Append("\n");
            sb.Append("  MaxSubmissionsPerPeriod: ").Append(MaxSubmissionsPerPeriod).Append("\n");
            sb.Append("  ParticipantsResetEachPeriod: ").Append(ParticipantsResetEachPeriod).Append("\n");
            sb.Append("  LiveTopCount: ").Append(LiveTopCount).Append("\n");
            sb.Append("  LiveTopic: ").Append(LiveTopic).Append("\n");
            sb.Append("  Period: ").Append(Period).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

    }

}
