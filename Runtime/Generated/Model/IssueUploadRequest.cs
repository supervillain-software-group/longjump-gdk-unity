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
    /// A file you are about to upload. Storage refuses the upload unless the file matches &#x60;byteSize&#x60;, &#x60;checksum&#x60; and &#x60;contentType&#x60; exactly.
    /// </summary>
    [DataContract(Name = "IssueUploadRequest")]
    public partial class IssueUploadRequest
    {
        /// <summary>
        /// &#x60;log&#x60;: the end of your log file (&#x60;Application.consoleLogPath&#x60;), at most 2MB. &#x60;screenshot&#x60;: at most 2MB, the last one is shown in the email. &#x60;file&#x60;: any file of your own, such as a save, at most 10MB.
        /// </summary>
        /// <value>&#x60;log&#x60;: the end of your log file (&#x60;Application.consoleLogPath&#x60;), at most 2MB. &#x60;screenshot&#x60;: at most 2MB, the last one is shown in the email. &#x60;file&#x60;: any file of your own, such as a save, at most 10MB.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            /// <summary>
            /// Enum Log for value: log
            /// </summary>
            [EnumMember(Value = "log")]
            Log = 1,

            /// <summary>
            /// Enum Screenshot for value: screenshot
            /// </summary>
            [EnumMember(Value = "screenshot")]
            Screenshot = 2,

            /// <summary>
            /// Enum File for value: file
            /// </summary>
            [EnumMember(Value = "file")]
            File = 3
        }


        /// <summary>
        /// &#x60;log&#x60;: the end of your log file (&#x60;Application.consoleLogPath&#x60;), at most 2MB. &#x60;screenshot&#x60;: at most 2MB, the last one is shown in the email. &#x60;file&#x60;: any file of your own, such as a save, at most 10MB.
        /// </summary>
        /// <value>&#x60;log&#x60;: the end of your log file (&#x60;Application.consoleLogPath&#x60;), at most 2MB. &#x60;screenshot&#x60;: at most 2MB, the last one is shown in the email. &#x60;file&#x60;: any file of your own, such as a save, at most 10MB.</value>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IssueUploadRequest" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected IssueUploadRequest() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="IssueUploadRequest" /> class.
        /// </summary>
        /// <param name="type">&#x60;log&#x60;: the end of your log file (&#x60;Application.consoleLogPath&#x60;), at most 2MB. &#x60;screenshot&#x60;: at most 2MB, the last one is shown in the email. &#x60;file&#x60;: any file of your own, such as a save, at most 10MB. (required).</param>
        /// <param name="name">Required for &#x60;file&#x60;, unique within the issue, such as &#x60;save&#x60;.</param>
        /// <param name="filename">filename (required).</param>
        /// <param name="byteSize">byteSize (required).</param>
        /// <param name="checksum">Base64 of the MD5 digest of the file (24 characters ending in &#x60;&#x3D;&#x3D;&#x60;) (required).</param>
        /// <param name="contentType">Defaults to application/octet-stream.</param>
        public IssueUploadRequest(TypeEnum type = default, string name = default, string filename = default, int byteSize = default, string checksum = default, string contentType = default)
        {
            this.Type = type;
            // to ensure "filename" is required (not null)
            if (filename == null)
            {
                throw new ArgumentNullException("filename is a required property for IssueUploadRequest and cannot be null");
            }
            this.Filename = filename;
            this.ByteSize = byteSize;
            // to ensure "checksum" is required (not null)
            if (checksum == null)
            {
                throw new ArgumentNullException("checksum is a required property for IssueUploadRequest and cannot be null");
            }
            this.Checksum = checksum;
            this.Name = name;
            this.ContentType = contentType;
        }

        /// <summary>
        /// Required for &#x60;file&#x60;, unique within the issue, such as &#x60;save&#x60;
        /// </summary>
        /// <value>Required for &#x60;file&#x60;, unique within the issue, such as &#x60;save&#x60;</value>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Filename
        /// </summary>
        /*
        <example>Player.log</example>
        */
        [DataMember(Name = "filename", IsRequired = true, EmitDefaultValue = true)]
        public string Filename { get; set; }

        /// <summary>
        /// Gets or Sets ByteSize
        /// </summary>
        /*
        <example>48213</example>
        */
        [DataMember(Name = "byteSize", IsRequired = true, EmitDefaultValue = true)]
        public int ByteSize { get; set; }

        /// <summary>
        /// Base64 of the MD5 digest of the file (24 characters ending in &#x60;&#x3D;&#x3D;&#x60;)
        /// </summary>
        /// <value>Base64 of the MD5 digest of the file (24 characters ending in &#x60;&#x3D;&#x3D;&#x60;)</value>
        /*
        <example>xyU7ZEEbOqSFkk785klLtQ&#x3D;&#x3D;</example>
        */
        [DataMember(Name = "checksum", IsRequired = true, EmitDefaultValue = true)]
        public string Checksum { get; set; }

        /// <summary>
        /// Defaults to application/octet-stream
        /// </summary>
        /// <value>Defaults to application/octet-stream</value>
        /*
        <example>text/plain</example>
        */
        [DataMember(Name = "contentType", EmitDefaultValue = false)]
        public string ContentType { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class IssueUploadRequest {\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Filename: ").Append(Filename).Append("\n");
            sb.Append("  ByteSize: ").Append(ByteSize).Append("\n");
            sb.Append("  Checksum: ").Append(Checksum).Append("\n");
            sb.Append("  ContentType: ").Append(ContentType).Append("\n");
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
