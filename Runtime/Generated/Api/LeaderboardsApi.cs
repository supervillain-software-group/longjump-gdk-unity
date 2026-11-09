/*
 * LongJump GDK API
 *
 * The LongJump Game Dev Kit (GDK) gives your game player sign-in, real-time events between players, leaderboards, cloud saves, and bug reports, errors and crashes. Call this API from your game client.  ## Games and environments  Your Team owns your Games, and each Game has one or more GameEnvironments, such as development, staging and production. Almost everything in the GDK belongs to a GameEnvironment rather than to the Game, which is why most endpoints start with a GameEnvironment id. Environments keep both configuration and data apart:  - **Configuration.** Each GameEnvironment has its own settings, starting with its Steam App ID, so a playtest, a demo and the full release can each be an environment of the same Game. - **Data.** Play sessions, player events, leaderboards and their entries, cloud saves and the real-time topics all belong to one GameEnvironment. Testing in staging never reaches production players' leaderboards or broadcasts.  A few things are shared by all of a Game's environments:  - **Players.** A player is one Steam account, the same player in every game and environment. - **Having played the Game.** Endpoints that require the player to have played the Game accept them in all of its environments once they've started a play session in any one. Posting a leaderboard score is the exception: it needs a play session in that GameEnvironment.  ## Player authentication  Most endpoints need a Bearer token. Your game gets one by [starting a play session](#tag/player-session/post/api/v1/game_environments/{game_environment_id}/steam_players) with a Steam auth ticket when it launches, then sends it on every request as `Authorization: Bearer <token>`.  Player tokens last two days. If a request answers `401`, start a new play session to get a fresh token.  New to leaderboards? The [Leaderboards guide](#tag/leaderboards) walks from a first board to daily resets, events, kiosks and live updates.  Games whose players have no account, such as a kiosk at an event, can instead [create a device session](#tag/device-sessions/post/api/v1/game_environments/{game_environment_id}/device_sessions) once their GameEnvironment allows it. A device session only works on the leaderboard endpoints, where it posts scores for participants it registers, and on the MQTT endpoint, to watch leaderboards live.  ## Real-time events  The GDK delivers real-time events over [MQTT](https://mqtt.org/), so your game needs an MQTT client such as [MQTTnet](https://github.com/dotnet/MQTTnet). After signing in, [fetch the broker and topics](#tag/mqtt/get/api/v1/game_environments/{game_environment_id}/mqtt) and subscribe to them. Always connect to the broker host from that response rather than a hardcoded one, so your game keeps working if the broker moves.  Every message is JSON with an `id`, a `type` and a `message`:  - `player_event`: another player [broadcast an event](#tag/player-events/post/api/v1/game_environments/{game_environment_id}/player_events). `message` has the event `name`, its `data` and the `player` who sent it.  Each leaderboard also has its own topic, `game_environment/{game_environment_id}/leaderboards/{slug}` (its `liveTopic`). Subscribe to it only on screens that show that leaderboard:  - `leaderboard_update`: the top of the current period changed. `message` has the `leaderboard` (`slug` and `name`), the `period`, the `total` number of entries, and the top `entries`, as many as the leaderboard's `liveTopCount`. Updates come at most about once a second per leaderboard, so a burst of scores becomes one message. - `leaderboard_period_closed`: a period froze. `message` has the same fields, with the final standings.  Messages aren't kept for late subscribers, and nothing is sent when a new period starts or a daily window opens or closes. So a screen reads the standings itself (`GET …/entries?limit=` its `liveTopCount`) when it starts, after every reconnect, after `leaderboard_period_closed`, and at the times its `period` gives (`endsAt`, `nextOpenAt`, `nextCloseAt`). `total` changes in messages only along with the top.  ## Errors  Errors answer with an HTTP status and a JSON body like `{\"error\": \"Not found\"}`. A `422` also includes `details`, the messages for each invalid request field. Rate-limited requests answer `429` with a `Retry-After` header, in seconds.  Every request body is limited to 2MB, and a larger one answers `413`. Files never go through this API: [issues](#tag/issues) upload theirs straight to storage.  ## Quick start  1. Make sure your Team has your Steam Publisher Web API key and your GameEnvironment has your Steam App ID. Note the GameEnvironment `id`. 2. When your game launches, call [ISteamUser::GetAuthTicketForWebApi](https://partner.steamgames.com/doc/api/ISteamUser#GetAuthTicketForWebApi) from the Steam SDK with an `identity` string of your choosing (5 to 30 characters), and hex-encode the ticket it returns. 3. [Start a play session](#tag/player-session/post/api/v1/game_environments/{game_environment_id}/steam_players) with the GameEnvironment `id`, your `identity` and the `ticket`. Keep `accessToken.token` from the response for every later request. 4. [Fetch the MQTT broker and topics](#tag/mqtt/get/api/v1/game_environments/{game_environment_id}/mqtt), connect to `broker.host` and subscribe to each topic. 5. Handle incoming messages by `type`. For `player_event`, decide how your game reacts to what other players did. 6. When your player does something worth sharing, [broadcast a player event](#tag/player-events/post/api/v1/game_environments/{game_environment_id}/player_events). You choose what goes in its `data`. 
 *
 * The version of the OpenAPI document: 1.0.0
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Mime;
using LongJump.Api.Client;
using LongJump.Api.Model;

namespace LongJump.Api
{

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface ILeaderboardsApiSync : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// Check a name before registering
        /// </summary>
        /// <remarks>
        /// Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <returns>LeaderboardParticipantNameCheck</returns>
        LeaderboardParticipantNameCheck CheckLeaderboardParticipantName(Guid gameEnvironmentId, string slug, string name);

        /// <summary>
        /// Check a name before registering
        /// </summary>
        /// <remarks>
        /// Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <returns>ApiResponse of LeaderboardParticipantNameCheck</returns>
        ApiResponse<LeaderboardParticipantNameCheck> CheckLeaderboardParticipantNameWithHttpInfo(Guid gameEnvironmentId, string slug, string name);
        /// <summary>
        /// Get a leaderboard
        /// </summary>
        /// <remarks>
        /// Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <returns>Leaderboard</returns>
        Leaderboard GetLeaderboard(Guid gameEnvironmentId, string slug);

        /// <summary>
        /// Get a leaderboard
        /// </summary>
        /// <remarks>
        /// Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <returns>ApiResponse of Leaderboard</returns>
        ApiResponse<Leaderboard> GetLeaderboardWithHttpInfo(Guid gameEnvironmentId, string slug);
        /// <summary>
        /// Read the ranked entries
        /// </summary>
        /// <remarks>
        /// Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <returns>LeaderboardPage</returns>
        LeaderboardPage GetLeaderboardEntries(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default);

        /// <summary>
        /// Read the ranked entries
        /// </summary>
        /// <remarks>
        /// Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <returns>ApiResponse of LeaderboardPage</returns>
        ApiResponse<LeaderboardPage> GetLeaderboardEntriesWithHttpInfo(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default);
        /// <summary>
        /// Read the entries around an owner
        /// </summary>
        /// <remarks>
        /// Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <returns>LeaderboardPage</returns>
        LeaderboardPage GetLeaderboardEntriesAround(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default);

        /// <summary>
        /// Read the entries around an owner
        /// </summary>
        /// <remarks>
        /// Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <returns>ApiResponse of LeaderboardPage</returns>
        ApiResponse<LeaderboardPage> GetLeaderboardEntriesAroundWithHttpInfo(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default);
        /// <summary>
        /// Get a participant
        /// </summary>
        /// <remarks>
        /// Returns a participant with their metadata, to the device that registered them only.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <returns>LeaderboardParticipant</returns>
        LeaderboardParticipant GetLeaderboardParticipant(Guid gameEnvironmentId, string slug, Guid id);

        /// <summary>
        /// Get a participant
        /// </summary>
        /// <remarks>
        /// Returns a participant with their metadata, to the device that registered them only.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <returns>ApiResponse of LeaderboardParticipant</returns>
        ApiResponse<LeaderboardParticipant> GetLeaderboardParticipantWithHttpInfo(Guid gameEnvironmentId, string slug, Guid id);
        /// <summary>
        /// List the leaderboards
        /// </summary>
        /// <remarks>
        /// Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>List&lt;Leaderboard&gt;</returns>
        List<Leaderboard> ListLeaderboards(Guid gameEnvironmentId);

        /// <summary>
        /// List the leaderboards
        /// </summary>
        /// <remarks>
        /// Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>ApiResponse of List&lt;Leaderboard&gt;</returns>
        ApiResponse<List<Leaderboard>> ListLeaderboardsWithHttpInfo(Guid gameEnvironmentId);
        /// <summary>
        /// Register a participant
        /// </summary>
        /// <remarks>
        /// Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <returns>LeaderboardParticipant</returns>
        LeaderboardParticipant RegisterLeaderboardParticipant(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant);

        /// <summary>
        /// Register a participant
        /// </summary>
        /// <remarks>
        /// Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <returns>ApiResponse of LeaderboardParticipant</returns>
        ApiResponse<LeaderboardParticipant> RegisterLeaderboardParticipantWithHttpInfo(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant);
        /// <summary>
        /// Post a score
        /// </summary>
        /// <remarks>
        /// Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <returns>LeaderboardSubmissionResult</returns>
        LeaderboardSubmissionResult SubmitLeaderboardEntry(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry);

        /// <summary>
        /// Post a score
        /// </summary>
        /// <remarks>
        /// Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <returns>ApiResponse of LeaderboardSubmissionResult</returns>
        ApiResponse<LeaderboardSubmissionResult> SubmitLeaderboardEntryWithHttpInfo(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry);
        #endregion Synchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface ILeaderboardsApiAsync : IApiAccessor
    {
        #region Asynchronous Operations
        /// <summary>
        /// Check a name before registering
        /// </summary>
        /// <remarks>
        /// Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardParticipantNameCheck</returns>
        System.Threading.Tasks.Task<LeaderboardParticipantNameCheck> CheckLeaderboardParticipantNameAsync(Guid gameEnvironmentId, string slug, string name, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Check a name before registering
        /// </summary>
        /// <remarks>
        /// Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardParticipantNameCheck)</returns>
        System.Threading.Tasks.Task<ApiResponse<LeaderboardParticipantNameCheck>> CheckLeaderboardParticipantNameWithHttpInfoAsync(Guid gameEnvironmentId, string slug, string name, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// Get a leaderboard
        /// </summary>
        /// <remarks>
        /// Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of Leaderboard</returns>
        System.Threading.Tasks.Task<Leaderboard> GetLeaderboardAsync(Guid gameEnvironmentId, string slug, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a leaderboard
        /// </summary>
        /// <remarks>
        /// Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (Leaderboard)</returns>
        System.Threading.Tasks.Task<ApiResponse<Leaderboard>> GetLeaderboardWithHttpInfoAsync(Guid gameEnvironmentId, string slug, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// Read the ranked entries
        /// </summary>
        /// <remarks>
        /// Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardPage</returns>
        System.Threading.Tasks.Task<LeaderboardPage> GetLeaderboardEntriesAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Read the ranked entries
        /// </summary>
        /// <remarks>
        /// Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardPage)</returns>
        System.Threading.Tasks.Task<ApiResponse<LeaderboardPage>> GetLeaderboardEntriesWithHttpInfoAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// Read the entries around an owner
        /// </summary>
        /// <remarks>
        /// Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardPage</returns>
        System.Threading.Tasks.Task<LeaderboardPage> GetLeaderboardEntriesAroundAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Read the entries around an owner
        /// </summary>
        /// <remarks>
        /// Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardPage)</returns>
        System.Threading.Tasks.Task<ApiResponse<LeaderboardPage>> GetLeaderboardEntriesAroundWithHttpInfoAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// Get a participant
        /// </summary>
        /// <remarks>
        /// Returns a participant with their metadata, to the device that registered them only.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardParticipant</returns>
        System.Threading.Tasks.Task<LeaderboardParticipant> GetLeaderboardParticipantAsync(Guid gameEnvironmentId, string slug, Guid id, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a participant
        /// </summary>
        /// <remarks>
        /// Returns a participant with their metadata, to the device that registered them only.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardParticipant)</returns>
        System.Threading.Tasks.Task<ApiResponse<LeaderboardParticipant>> GetLeaderboardParticipantWithHttpInfoAsync(Guid gameEnvironmentId, string slug, Guid id, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// List the leaderboards
        /// </summary>
        /// <remarks>
        /// Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of List&lt;Leaderboard&gt;</returns>
        System.Threading.Tasks.Task<List<Leaderboard>> ListLeaderboardsAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// List the leaderboards
        /// </summary>
        /// <remarks>
        /// Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (List&lt;Leaderboard&gt;)</returns>
        System.Threading.Tasks.Task<ApiResponse<List<Leaderboard>>> ListLeaderboardsWithHttpInfoAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// Register a participant
        /// </summary>
        /// <remarks>
        /// Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardParticipant</returns>
        System.Threading.Tasks.Task<LeaderboardParticipant> RegisterLeaderboardParticipantAsync(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Register a participant
        /// </summary>
        /// <remarks>
        /// Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardParticipant)</returns>
        System.Threading.Tasks.Task<ApiResponse<LeaderboardParticipant>> RegisterLeaderboardParticipantWithHttpInfoAsync(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// Post a score
        /// </summary>
        /// <remarks>
        /// Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardSubmissionResult</returns>
        System.Threading.Tasks.Task<LeaderboardSubmissionResult> SubmitLeaderboardEntryAsync(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Post a score
        /// </summary>
        /// <remarks>
        /// Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardSubmissionResult)</returns>
        System.Threading.Tasks.Task<ApiResponse<LeaderboardSubmissionResult>> SubmitLeaderboardEntryWithHttpInfoAsync(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry, System.Threading.CancellationToken cancellationToken = default);
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface ILeaderboardsApi : ILeaderboardsApiSync, ILeaderboardsApiAsync
    {

    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class LeaderboardsApi : IDisposable, ILeaderboardsApi
    {
        private LongJump.Api.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardsApi"/> class.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <returns></returns>
        public LeaderboardsApi() : this((string)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardsApi"/> class.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <param name="basePath">The target service's base path in URL format.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public LeaderboardsApi(string basePath)
        {
            this.Configuration = LongJump.Api.Client.Configuration.MergeConfigurations(
                LongJump.Api.Client.GlobalConfiguration.Instance,
                new LongJump.Api.Client.Configuration { BasePath = basePath }
            );
            this.ApiClient = new LongJump.Api.Client.ApiClient(this.Configuration.BasePath);
            this.Client =  this.ApiClient;
            this.AsynchronousClient = this.ApiClient;
            this.ExceptionFactory = LongJump.Api.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardsApi"/> class using Configuration object.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <param name="configuration">An instance of Configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public LeaderboardsApi(LongJump.Api.Client.Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Configuration = LongJump.Api.Client.Configuration.MergeConfigurations(
                LongJump.Api.Client.GlobalConfiguration.Instance,
                configuration
            );
            this.ApiClient = new LongJump.Api.Client.ApiClient(this.Configuration.BasePath);
            this.Client = this.ApiClient;
            this.AsynchronousClient = this.ApiClient;
            ExceptionFactory = LongJump.Api.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LeaderboardsApi"/> class
        /// using a Configuration object and client instance.
        /// </summary>
        /// <param name="client">The client interface for synchronous API access.</param>
        /// <param name="asyncClient">The client interface for asynchronous API access.</param>
        /// <param name="configuration">The configuration object.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public LeaderboardsApi(LongJump.Api.Client.ISynchronousClient client, LongJump.Api.Client.IAsynchronousClient asyncClient, LongJump.Api.Client.IReadableConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (asyncClient == null) throw new ArgumentNullException("asyncClient");
            if (configuration == null) throw new ArgumentNullException("configuration");

            this.Client = client;
            this.AsynchronousClient = asyncClient;
            this.Configuration = configuration;
            this.ExceptionFactory = LongJump.Api.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Disposes resources if they were created by us
        /// </summary>
        public void Dispose()
        {
            this.ApiClient?.Dispose();
        }

        /// <summary>
        /// Holds the ApiClient if created
        /// </summary>
        public LongJump.Api.Client.ApiClient ApiClient { get; set; } = null;

        /// <summary>
        /// The client for accessing this underlying API asynchronously.
        /// </summary>
        public LongJump.Api.Client.IAsynchronousClient AsynchronousClient { get; set; }

        /// <summary>
        /// The client for accessing this underlying API synchronously.
        /// </summary>
        public LongJump.Api.Client.ISynchronousClient Client { get; set; }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public string GetBasePath()
        {
            return this.Configuration.BasePath;
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public LongJump.Api.Client.IReadableConfiguration Configuration { get; set; }

        /// <summary>
        /// Provides a factory method hook for the creation of exceptions.
        /// </summary>
        public LongJump.Api.Client.ExceptionFactory ExceptionFactory
        {
            get
            {
                if (_exceptionFactory != null && _exceptionFactory.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException("Multicast delegate for ExceptionFactory is unsupported.");
                }
                return _exceptionFactory;
            }
            set { _exceptionFactory = value; }
        }

        /// <summary>
        /// Check a name before registering Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <returns>LeaderboardParticipantNameCheck</returns>
        public LeaderboardParticipantNameCheck CheckLeaderboardParticipantName(Guid gameEnvironmentId, string slug, string name)
        {
            LongJump.Api.Client.ApiResponse<LeaderboardParticipantNameCheck> localVarResponse = CheckLeaderboardParticipantNameWithHttpInfo(gameEnvironmentId, slug, name);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Check a name before registering Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <returns>ApiResponse of LeaderboardParticipantNameCheck</returns>
        public LongJump.Api.Client.ApiResponse<LeaderboardParticipantNameCheck> CheckLeaderboardParticipantNameWithHttpInfo(Guid gameEnvironmentId, string slug, string name)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->CheckLeaderboardParticipantName");

            // verify the required parameter 'name' is set
            if (name == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'name' when calling LeaderboardsApi->CheckLeaderboardParticipantName");

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "name", name));

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request
            var localVarResponse = this.Client.Get<LeaderboardParticipantNameCheck>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("CheckLeaderboardParticipantName", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Check a name before registering Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardParticipantNameCheck</returns>
        public async System.Threading.Tasks.Task<LeaderboardParticipantNameCheck> CheckLeaderboardParticipantNameAsync(Guid gameEnvironmentId, string slug, string name, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = CheckLeaderboardParticipantNameWithHttpInfoAsync(gameEnvironmentId, slug, name, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<LeaderboardParticipantNameCheck> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<LeaderboardParticipantNameCheck> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Check a name before registering Checks whether a name is already registered, without registering it. Optional: [registering](#tag/leaderboards/post/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) a taken name already answers with that participant and &#x60;existing: true&#x60;. Check first when the game must know before it asks for anything else, for example to ask \&quot;are you the Alex who played earlier, or pick another name?\&quot; before collecting an email.  A taken name comes back with &#x60;taken: true&#x60; and the participant, with their metadata only for the device that registered them. A free name answers &#x60;taken: false&#x60;. Names match as when registering, ignoring case and extra spaces, and a name registering would refuse (offensive, blank or too long) answers &#x60;422&#x60; the same way. The name stays free until the game registers it, so registering can still answer &#x60;existing: true&#x60; if another kiosk took the name in between. Only device sessions check names. Limited to 60 checks a minute per session per leaderboard and 240 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="name"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardParticipantNameCheck)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<LeaderboardParticipantNameCheck>> CheckLeaderboardParticipantNameWithHttpInfoAsync(Guid gameEnvironmentId, string slug, string name, System.Threading.CancellationToken cancellationToken = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->CheckLeaderboardParticipantName");

            // verify the required parameter 'name' is set
            if (name == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'name' when calling LeaderboardsApi->CheckLeaderboardParticipantName");


            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "name", name));

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request

            var task = this.AsynchronousClient.GetAsync<LeaderboardParticipantNameCheck>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("CheckLeaderboardParticipantName", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Get a leaderboard Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <returns>Leaderboard</returns>
        public Leaderboard GetLeaderboard(Guid gameEnvironmentId, string slug)
        {
            LongJump.Api.Client.ApiResponse<Leaderboard> localVarResponse = GetLeaderboardWithHttpInfo(gameEnvironmentId, slug);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Get a leaderboard Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <returns>ApiResponse of Leaderboard</returns>
        public LongJump.Api.Client.ApiResponse<Leaderboard> GetLeaderboardWithHttpInfo(Guid gameEnvironmentId, string slug)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboard");

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter


            // make the HTTP request
            var localVarResponse = this.Client.Get<Leaderboard>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboard", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Get a leaderboard Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of Leaderboard</returns>
        public async System.Threading.Tasks.Task<Leaderboard> GetLeaderboardAsync(Guid gameEnvironmentId, string slug, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = GetLeaderboardWithHttpInfoAsync(gameEnvironmentId, slug, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<Leaderboard> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<Leaderboard> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Get a leaderboard Returns the leaderboard, its settings, and whether its current period is open and when that changes. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (Leaderboard)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<Leaderboard>> GetLeaderboardWithHttpInfoAsync(Guid gameEnvironmentId, string slug, System.Threading.CancellationToken cancellationToken = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboard");


            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter


            // make the HTTP request

            var task = this.AsynchronousClient.GetAsync<Leaderboard>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboard", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Read the ranked entries Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <returns>LeaderboardPage</returns>
        public LeaderboardPage GetLeaderboardEntries(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default)
        {
            LongJump.Api.Client.ApiResponse<LeaderboardPage> localVarResponse = GetLeaderboardEntriesWithHttpInfo(gameEnvironmentId, slug, period, limit, from, cursor, playerIds, participantIds);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Read the ranked entries Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <returns>ApiResponse of LeaderboardPage</returns>
        public LongJump.Api.Client.ApiResponse<LeaderboardPage> GetLeaderboardEntriesWithHttpInfo(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboardEntries");

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            if (period != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "period", period));
            }
            if (limit != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "limit", limit));
            }
            if (from != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "from", from));
            }
            if (cursor != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "cursor", cursor));
            }
            if (playerIds != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "playerIds", playerIds));
            }
            if (participantIds != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "participantIds", participantIds));
            }

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request
            var localVarResponse = this.Client.Get<LeaderboardPage>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/entries", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboardEntries", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Read the ranked entries Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardPage</returns>
        public async System.Threading.Tasks.Task<LeaderboardPage> GetLeaderboardEntriesAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = GetLeaderboardEntriesWithHttpInfoAsync(gameEnvironmentId, slug, period, limit, from, cursor, playerIds, participantIds, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<LeaderboardPage> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<LeaderboardPage> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Read the ranked entries Returns a page of entries from the top, best first, with the total and a cursor for the next page. With &#x60;from&#x3D;bottom&#x60;, returns the last &#x60;limit&#x60; entries instead, still best first and with their ranks on the whole board (the last 4 of 19 are ranks 16 to 19), for example to show a new player the entries they&#39;re about to pass; the bottom is one page, so there&#39;s no cursor. With &#x60;playerIds&#x60; or &#x60;participantIds&#x60;, returns just those owners&#39; entries with their ranks on the whole board instead.  No token needed, and participant metadata is never included. A valid token lets a player see their own entry even if an admin hid it, as if it were visible: in the top (with ranks counted around it, for them only), around and by id. A device sees a hidden participant it registered only when the read names them with &#x60;participantIds&#x60;. An expired or unknown token is ignored.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="from">&#x60;top&#x60; (the default) or &#x60;bottom&#x60;: the last &#x60;limit&#x60; entries. Not with &#x60;cursor&#x60;, &#x60;playerIds&#x60; or &#x60;participantIds&#x60; (optional, default to top)</param>
        /// <param name="cursor">&#x60;nextCursor&#x60; from the previous page (optional)</param>
        /// <param name="playerIds">Player ids, separated by commas (up to 100 ids with participantIds) (optional)</param>
        /// <param name="participantIds">Participant ids, separated by commas (up to 100 ids with playerIds) (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardPage)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<LeaderboardPage>> GetLeaderboardEntriesWithHttpInfoAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default, System.Threading.CancellationToken cancellationToken = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboardEntries");


            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            if (period != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "period", period));
            }
            if (limit != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "limit", limit));
            }
            if (from != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "from", from));
            }
            if (cursor != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "cursor", cursor));
            }
            if (playerIds != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "playerIds", playerIds));
            }
            if (participantIds != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "participantIds", participantIds));
            }

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request

            var task = this.AsynchronousClient.GetAsync<LeaderboardPage>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/entries", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboardEntries", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Read the entries around an owner Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <returns>LeaderboardPage</returns>
        public LeaderboardPage GetLeaderboardEntriesAround(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default)
        {
            LongJump.Api.Client.ApiResponse<LeaderboardPage> localVarResponse = GetLeaderboardEntriesAroundWithHttpInfo(gameEnvironmentId, slug, period, limit, playerId, participantId, me);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Read the entries around an owner Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <returns>ApiResponse of LeaderboardPage</returns>
        public LongJump.Api.Client.ApiResponse<LeaderboardPage> GetLeaderboardEntriesAroundWithHttpInfo(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboardEntriesAround");

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            if (period != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "period", period));
            }
            if (limit != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "limit", limit));
            }
            if (playerId != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "playerId", playerId));
            }
            if (participantId != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "participantId", participantId));
            }
            if (me != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "me", me));
            }

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request
            var localVarResponse = this.Client.Get<LeaderboardPage>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/entries/around", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboardEntriesAround", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Read the entries around an owner Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardPage</returns>
        public async System.Threading.Tasks.Task<LeaderboardPage> GetLeaderboardEntriesAroundAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = GetLeaderboardEntriesAroundWithHttpInfoAsync(gameEnvironmentId, slug, period, limit, playerId, participantId, me, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<LeaderboardPage> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<LeaderboardPage> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Read the entries around an owner Returns up to &#x60;limit&#x60; entries centered on one owner: give exactly one of &#x60;playerId&#x60;, &#x60;participantId&#x60; or &#x60;me&#x3D;true&#x60; (the signed-in player). Empty when the owner has no entry. No token needed except for &#x60;me&#x60;.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="period">&#x60;current&#x60; (the default: the latest period to start), &#x60;previous&#x60;, or any ISO 8601 time inside the period to read. Encode a &#x60;+&#x60; in the time as &#x60;%2B&#x60;, or use &#x60;Z&#x60; (optional)</param>
        /// <param name="limit">Entries to return, 1 to 100 (default 20) (optional)</param>
        /// <param name="playerId"> (optional)</param>
        /// <param name="participantId"> (optional)</param>
        /// <param name="me">&#x60;true&#x60; for the signed-in player. Any other value is a 422 (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardPage)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<LeaderboardPage>> GetLeaderboardEntriesAroundWithHttpInfoAsync(Guid gameEnvironmentId, string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default, System.Threading.CancellationToken cancellationToken = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboardEntriesAround");


            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            if (period != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "period", period));
            }
            if (limit != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "limit", limit));
            }
            if (playerId != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "playerId", playerId));
            }
            if (participantId != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "participantId", participantId));
            }
            if (me != null)
            {
                localVarRequestOptions.QueryParameters.Add(LongJump.Api.Client.ClientUtils.ParameterToMultiMap("", "me", me));
            }

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request

            var task = this.AsynchronousClient.GetAsync<LeaderboardPage>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/entries/around", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboardEntriesAround", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Get a participant Returns a participant with their metadata, to the device that registered them only.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <returns>LeaderboardParticipant</returns>
        public LeaderboardParticipant GetLeaderboardParticipant(Guid gameEnvironmentId, string slug, Guid id)
        {
            LongJump.Api.Client.ApiResponse<LeaderboardParticipant> localVarResponse = GetLeaderboardParticipantWithHttpInfo(gameEnvironmentId, slug, id);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Get a participant Returns a participant with their metadata, to the device that registered them only.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <returns>ApiResponse of LeaderboardParticipant</returns>
        public LongJump.Api.Client.ApiResponse<LeaderboardParticipant> GetLeaderboardParticipantWithHttpInfo(Guid gameEnvironmentId, string slug, Guid id)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboardParticipant");

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.PathParameters.Add("id", LongJump.Api.Client.ClientUtils.ParameterToString(id)); // path parameter

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request
            var localVarResponse = this.Client.Get<LeaderboardParticipant>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants/{id}", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboardParticipant", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Get a participant Returns a participant with their metadata, to the device that registered them only.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardParticipant</returns>
        public async System.Threading.Tasks.Task<LeaderboardParticipant> GetLeaderboardParticipantAsync(Guid gameEnvironmentId, string slug, Guid id, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = GetLeaderboardParticipantWithHttpInfoAsync(gameEnvironmentId, slug, id, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<LeaderboardParticipant> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<LeaderboardParticipant> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Get a participant Returns a participant with their metadata, to the device that registered them only.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="id"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardParticipant)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<LeaderboardParticipant>> GetLeaderboardParticipantWithHttpInfoAsync(Guid gameEnvironmentId, string slug, Guid id, System.Threading.CancellationToken cancellationToken = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->GetLeaderboardParticipant");


            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.PathParameters.Add("id", LongJump.Api.Client.ClientUtils.ParameterToString(id)); // path parameter

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request

            var task = this.AsynchronousClient.GetAsync<LeaderboardParticipant>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants/{id}", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("GetLeaderboardParticipant", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// List the leaderboards Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>List&lt;Leaderboard&gt;</returns>
        public List<Leaderboard> ListLeaderboards(Guid gameEnvironmentId)
        {
            LongJump.Api.Client.ApiResponse<List<Leaderboard>> localVarResponse = ListLeaderboardsWithHttpInfo(gameEnvironmentId);
            return localVarResponse.Data;
        }

        /// <summary>
        /// List the leaderboards Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>ApiResponse of List&lt;Leaderboard&gt;</returns>
        public LongJump.Api.Client.ApiResponse<List<Leaderboard>> ListLeaderboardsWithHttpInfo(Guid gameEnvironmentId)
        {
            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter


            // make the HTTP request
            var localVarResponse = this.Client.Get<List<Leaderboard>>("/api/v1/game_environments/{game_environment_id}/leaderboards", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ListLeaderboards", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// List the leaderboards Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of List&lt;Leaderboard&gt;</returns>
        public async System.Threading.Tasks.Task<List<Leaderboard>> ListLeaderboardsAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = ListLeaderboardsWithHttpInfoAsync(gameEnvironmentId, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<List<Leaderboard>> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<List<Leaderboard>> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// List the leaderboards Lists the GameEnvironment&#39;s leaderboards with their settings and current period. No token needed.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (List&lt;Leaderboard&gt;)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<List<Leaderboard>>> ListLeaderboardsWithHttpInfoAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default)
        {

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter


            // make the HTTP request

            var task = this.AsynchronousClient.GetAsync<List<Leaderboard>>("/api/v1/game_environments/{game_environment_id}/leaderboards", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("ListLeaderboards", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Register a participant Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <returns>LeaderboardParticipant</returns>
        public LeaderboardParticipant RegisterLeaderboardParticipant(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant)
        {
            LongJump.Api.Client.ApiResponse<LeaderboardParticipant> localVarResponse = RegisterLeaderboardParticipantWithHttpInfo(gameEnvironmentId, slug, newLeaderboardParticipant);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Register a participant Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <returns>ApiResponse of LeaderboardParticipant</returns>
        public LongJump.Api.Client.ApiResponse<LeaderboardParticipant> RegisterLeaderboardParticipantWithHttpInfo(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->RegisterLeaderboardParticipant");

            // verify the required parameter 'newLeaderboardParticipant' is set
            if (newLeaderboardParticipant == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'newLeaderboardParticipant' when calling LeaderboardsApi->RegisterLeaderboardParticipant");

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
                "application/json"
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.Data = newLeaderboardParticipant;

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request
            var localVarResponse = this.Client.Post<LeaderboardParticipant>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("RegisterLeaderboardParticipant", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Register a participant Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardParticipant</returns>
        public async System.Threading.Tasks.Task<LeaderboardParticipant> RegisterLeaderboardParticipantAsync(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = RegisterLeaderboardParticipantWithHttpInfoAsync(gameEnvironmentId, slug, newLeaderboardParticipant, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<LeaderboardParticipant> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<LeaderboardParticipant> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Register a participant Registers someone with no account by name, for example a walk-up player at a kiosk, so the game can post scores for them. Only device sessions register participants. Names match ignoring case and extra spaces: registering a name that exists returns that participant with &#x60;existing: true&#x60;, so the game can ask whether it&#39;s the same person. To ask before collecting anything else, [check the name](#tag/leaderboards/get/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants) first. Offensive names are refused.  &#x60;metadata&#x60; (at most 4 KB) is private: only this device (through any of its sessions), the admin console and its exports see it, never public reads. When the name is already registered, the &#x60;metadata&#x60; sent is ignored and the participant comes back as first registered. When the leaderboard&#39;s participants reset each period, registrations last for the current period only, so register again after each reset. Limited to 30 registrations a minute per session per leaderboard and 120 per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardParticipant"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardParticipant)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<LeaderboardParticipant>> RegisterLeaderboardParticipantWithHttpInfoAsync(Guid gameEnvironmentId, string slug, NewLeaderboardParticipant newLeaderboardParticipant, System.Threading.CancellationToken cancellationToken = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->RegisterLeaderboardParticipant");

            // verify the required parameter 'newLeaderboardParticipant' is set
            if (newLeaderboardParticipant == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'newLeaderboardParticipant' when calling LeaderboardsApi->RegisterLeaderboardParticipant");


            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
                "application/json"
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.Data = newLeaderboardParticipant;

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request

            var task = this.AsynchronousClient.PostAsync<LeaderboardParticipant>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/participants", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("RegisterLeaderboardParticipant", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Post a score Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <returns>LeaderboardSubmissionResult</returns>
        public LeaderboardSubmissionResult SubmitLeaderboardEntry(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry)
        {
            LongJump.Api.Client.ApiResponse<LeaderboardSubmissionResult> localVarResponse = SubmitLeaderboardEntryWithHttpInfo(gameEnvironmentId, slug, newLeaderboardEntry);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Post a score Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <returns>ApiResponse of LeaderboardSubmissionResult</returns>
        public LongJump.Api.Client.ApiResponse<LeaderboardSubmissionResult> SubmitLeaderboardEntryWithHttpInfo(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->SubmitLeaderboardEntry");

            // verify the required parameter 'newLeaderboardEntry' is set
            if (newLeaderboardEntry == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'newLeaderboardEntry' when calling LeaderboardsApi->SubmitLeaderboardEntry");

            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
                "application/json"
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };

            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.Data = newLeaderboardEntry;

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request
            var localVarResponse = this.Client.Post<LeaderboardSubmissionResult>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/entries", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("SubmitLeaderboardEntry", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Post a score Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of LeaderboardSubmissionResult</returns>
        public async System.Threading.Tasks.Task<LeaderboardSubmissionResult> SubmitLeaderboardEntryAsync(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = SubmitLeaderboardEntryWithHttpInfoAsync(gameEnvironmentId, slug, newLeaderboardEntry, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<LeaderboardSubmissionResult> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<LeaderboardSubmissionResult> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Post a score Posts a score for the signed-in player. A device session instead posts for a participant it names with &#x60;participantId&#x60; (a player session can&#39;t). The score goes to the current period; the game never names it. Players need a play session in this GameEnvironment.  With &#x60;writeMode&#x60; &#x60;best&#x60;, the entry changes only when the result ranks higher as a whole (score, then each tie-breaker in its own order). &#x60;attributes&#x60; change with it, so they always describe the run that holds the rank. With &#x60;latest&#x60;, every post replaces the entry.  A post that arrives as the period closes either makes the final standings or gets &#x60;409&#x60;: once a winner is announced, no score changes it.  Every post is logged. Send a new &#x60;requestId&#x60; with each run to retry safely: retrying an accepted post returns its first response without applying it again; a rejected post isn&#39;t remembered, so a retry is checked again. Reusing an id for a different run returns the old response and drops the new score. Limited to 2 posts per second per session per leaderboard, and 50 per second per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="slug">Leaderboard slug, unique in the GameEnvironment</param>
        /// <param name="newLeaderboardEntry"></param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (LeaderboardSubmissionResult)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<LeaderboardSubmissionResult>> SubmitLeaderboardEntryWithHttpInfoAsync(Guid gameEnvironmentId, string slug, NewLeaderboardEntry newLeaderboardEntry, System.Threading.CancellationToken cancellationToken = default)
        {
            // verify the required parameter 'slug' is set
            if (slug == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'slug' when calling LeaderboardsApi->SubmitLeaderboardEntry");

            // verify the required parameter 'newLeaderboardEntry' is set
            if (newLeaderboardEntry == null)
                throw new LongJump.Api.Client.ApiException(400, "Missing required parameter 'newLeaderboardEntry' when calling LeaderboardsApi->SubmitLeaderboardEntry");


            LongJump.Api.Client.RequestOptions localVarRequestOptions = new LongJump.Api.Client.RequestOptions();

            string[] _contentTypes = new string[] {
                "application/json"
            };

            // to determine the Accept header
            string[] _accepts = new string[] {
                "application/json"
            };


            var localVarContentType = LongJump.Api.Client.ClientUtils.SelectHeaderContentType(_contentTypes);
            if (localVarContentType != null) localVarRequestOptions.HeaderParameters.Add("Content-Type", localVarContentType);

            var localVarAccept = LongJump.Api.Client.ClientUtils.SelectHeaderAccept(_accepts);
            if (localVarAccept != null) localVarRequestOptions.HeaderParameters.Add("Accept", localVarAccept);

            localVarRequestOptions.PathParameters.Add("game_environment_id", LongJump.Api.Client.ClientUtils.ParameterToString(gameEnvironmentId)); // path parameter
            localVarRequestOptions.PathParameters.Add("slug", LongJump.Api.Client.ClientUtils.ParameterToString(slug)); // path parameter
            localVarRequestOptions.Data = newLeaderboardEntry;

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request

            var task = this.AsynchronousClient.PostAsync<LeaderboardSubmissionResult>("/api/v1/game_environments/{game_environment_id}/leaderboards/{slug}/entries", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("SubmitLeaderboardEntry", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

    }
}
