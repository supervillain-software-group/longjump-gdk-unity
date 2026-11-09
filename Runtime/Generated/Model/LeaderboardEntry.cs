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
    /// LeaderboardEntry
    /// </summary>
    [DataContract(Name = "LeaderboardEntry")]
    public partial class LeaderboardEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardEntry" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected LeaderboardEntry() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardEntry" /> class.
        /// </summary>
        /// <param name="rank">1 is the best. Ranks never tie (required).</param>
        /// <param name="score">score (required).</param>
        /// <param name="tiebreakers">tiebreakers (required).</param>
        /// <param name="attributes">Public details of the run that holds the rank (required).</param>
        /// <param name="achievedAt">achievedAt (required).</param>
        /// <param name="owner">owner (required).</param>
        public LeaderboardEntry(int rank = default, long score = default, Dictionary<string, long> tiebreakers = default, Object attributes = default, DateTime achievedAt = default, LeaderboardOwner owner = default)
        {
            this.Rank = rank;
            this.Score = score;
            // to ensure "tiebreakers" is required (not null)
            if (tiebreakers == null)
            {
                throw new ArgumentNullException("tiebreakers is a required property for LeaderboardEntry and cannot be null");
            }
            this.Tiebreakers = tiebreakers;
            // to ensure "attributes" is required (not null)
            if (attributes == null)
            {
                throw new ArgumentNullException("attributes is a required property for LeaderboardEntry and cannot be null");
            }
            this.Attributes = attributes;
            this.AchievedAt = achievedAt;
            // to ensure "owner" is required (not null)
            if (owner == null)
            {
                throw new ArgumentNullException("owner is a required property for LeaderboardEntry and cannot be null");
            }
            this.Owner = owner;
        }

        /// <summary>
        /// 1 is the best. Ranks never tie
        /// </summary>
        /// <value>1 is the best. Ranks never tie</value>
        [DataMember(Name = "rank", IsRequired = true, EmitDefaultValue = true)]
        public int Rank { get; set; }

        /// <summary>
        /// Gets or Sets Score
        /// </summary>
        [DataMember(Name = "score", IsRequired = true, EmitDefaultValue = true)]
        public long Score { get; set; }

        /// <summary>
        /// Gets or Sets Tiebreakers
        /// </summary>
        /*
        <example>{&quot;time&quot;:61300}</example>
        */
        [DataMember(Name = "tiebreakers", IsRequired = true, EmitDefaultValue = true)]
        public Dictionary<string, long> Tiebreakers { get; set; }

        /// <summary>
        /// Public details of the run that holds the rank
        /// </summary>
        /// <value>Public details of the run that holds the rank</value>
        /*
        <example>{&quot;character&quot;:&quot;Ryu&quot;}</example>
        */
        [DataMember(Name = "attributes", IsRequired = true, EmitDefaultValue = true)]
        public Object Attributes { get; set; }

        /// <summary>
        /// Gets or Sets AchievedAt
        /// </summary>
        /*
        <example>2026-10-21T22:11:50.678Z</example>
        */
        [DataMember(Name = "achievedAt", IsRequired = true, EmitDefaultValue = true)]
        public DateTime AchievedAt { get; set; }

        /// <summary>
        /// Gets or Sets Owner
        /// </summary>
        [DataMember(Name = "owner", IsRequired = true, EmitDefaultValue = true)]
        public LeaderboardOwner Owner { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class LeaderboardEntry {\n");
            sb.Append("  Rank: ").Append(Rank).Append("\n");
            sb.Append("  Score: ").Append(Score).Append("\n");
            sb.Append("  Tiebreakers: ").Append(Tiebreakers).Append("\n");
            sb.Append("  Attributes: ").Append(Attributes).Append("\n");
            sb.Append("  AchievedAt: ").Append(AchievedAt).Append("\n");
            sb.Append("  Owner: ").Append(Owner).Append("\n");
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
