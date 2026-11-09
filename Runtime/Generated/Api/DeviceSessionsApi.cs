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
    public interface IDeviceSessionsApiSync : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// Create a device session
        /// </summary>
        /// <remarks>
        /// Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <returns>DeviceSession</returns>
        DeviceSession CreateDeviceSession(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default);

        /// <summary>
        /// Create a device session
        /// </summary>
        /// <remarks>
        /// Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <returns>ApiResponse of DeviceSession</returns>
        ApiResponse<DeviceSession> CreateDeviceSessionWithHttpInfo(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default);
        /// <summary>
        /// Renew a device session
        /// </summary>
        /// <remarks>
        /// Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>DeviceSession</returns>
        DeviceSession RenewDeviceSession(Guid gameEnvironmentId);

        /// <summary>
        /// Renew a device session
        /// </summary>
        /// <remarks>
        /// Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>ApiResponse of DeviceSession</returns>
        ApiResponse<DeviceSession> RenewDeviceSessionWithHttpInfo(Guid gameEnvironmentId);
        #endregion Synchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IDeviceSessionsApiAsync : IApiAccessor
    {
        #region Asynchronous Operations
        /// <summary>
        /// Create a device session
        /// </summary>
        /// <remarks>
        /// Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of DeviceSession</returns>
        System.Threading.Tasks.Task<DeviceSession> CreateDeviceSessionAsync(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a device session
        /// </summary>
        /// <remarks>
        /// Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (DeviceSession)</returns>
        System.Threading.Tasks.Task<ApiResponse<DeviceSession>> CreateDeviceSessionWithHttpInfoAsync(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default, System.Threading.CancellationToken cancellationToken = default);
        /// <summary>
        /// Renew a device session
        /// </summary>
        /// <remarks>
        /// Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of DeviceSession</returns>
        System.Threading.Tasks.Task<DeviceSession> RenewDeviceSessionAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Renew a device session
        /// </summary>
        /// <remarks>
        /// Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </remarks>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (DeviceSession)</returns>
        System.Threading.Tasks.Task<ApiResponse<DeviceSession>> RenewDeviceSessionWithHttpInfoAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default);
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IDeviceSessionsApi : IDeviceSessionsApiSync, IDeviceSessionsApiAsync
    {

    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class DeviceSessionsApi : IDisposable, IDeviceSessionsApi
    {
        private LongJump.Api.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSessionsApi"/> class.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <returns></returns>
        public DeviceSessionsApi() : this((string)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSessionsApi"/> class.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <param name="basePath">The target service's base path in URL format.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public DeviceSessionsApi(string basePath)
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
        /// Initializes a new instance of the <see cref="DeviceSessionsApi"/> class using Configuration object.
        /// **IMPORTANT** This will also create an instance of HttpClient, which is less than ideal.
        /// It's better to reuse the <see href="https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#issues-with-the-original-httpclient-class-available-in-net">HttpClient and HttpClientHandler</see>.
        /// </summary>
        /// <param name="configuration">An instance of Configuration.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public DeviceSessionsApi(LongJump.Api.Client.Configuration configuration)
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
        /// Initializes a new instance of the <see cref="DeviceSessionsApi"/> class
        /// using a Configuration object and client instance.
        /// </summary>
        /// <param name="client">The client interface for synchronous API access.</param>
        /// <param name="asyncClient">The client interface for asynchronous API access.</param>
        /// <param name="configuration">The configuration object.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DeviceSessionsApi(LongJump.Api.Client.ISynchronousClient client, LongJump.Api.Client.IAsynchronousClient asyncClient, LongJump.Api.Client.IReadableConfiguration configuration)
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
        /// Create a device session Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <returns>DeviceSession</returns>
        public DeviceSession CreateDeviceSession(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default)
        {
            LongJump.Api.Client.ApiResponse<DeviceSession> localVarResponse = CreateDeviceSessionWithHttpInfo(gameEnvironmentId, newDeviceSession);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Create a device session Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <returns>ApiResponse of DeviceSession</returns>
        public LongJump.Api.Client.ApiResponse<DeviceSession> CreateDeviceSessionWithHttpInfo(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default)
        {
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
            localVarRequestOptions.Data = newDeviceSession;


            // make the HTTP request
            var localVarResponse = this.Client.Post<DeviceSession>("/api/v1/game_environments/{game_environment_id}/device_sessions", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("CreateDeviceSession", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Create a device session Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of DeviceSession</returns>
        public async System.Threading.Tasks.Task<DeviceSession> CreateDeviceSessionAsync(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = CreateDeviceSessionWithHttpInfoAsync(gameEnvironmentId, newDeviceSession, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<DeviceSession> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<DeviceSession> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Create a device session Signs in a client that has no player, for games whose players have no account, such as a kiosk at an event. The GameEnvironment must allow device sessions (an admin setting, off by default). No credential is needed.  The token works only on the leaderboard endpoints of this GameEnvironment, where it registers participants and posts their scores, and on its MQTT endpoint. It lasts two days. Renew it before then with &#x60;renewDeviceSession&#x60;, which keeps the device and so the participants it registered; a new session is a new device. Limited to 10 per minute per IP address.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="newDeviceSession"> (optional)</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (DeviceSession)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<DeviceSession>> CreateDeviceSessionWithHttpInfoAsync(Guid gameEnvironmentId, NewDeviceSession newDeviceSession = default, System.Threading.CancellationToken cancellationToken = default)
        {

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
            localVarRequestOptions.Data = newDeviceSession;


            // make the HTTP request

            var task = this.AsynchronousClient.PostAsync<DeviceSession>("/api/v1/game_environments/{game_environment_id}/device_sessions", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("CreateDeviceSession", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Renew a device session Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>DeviceSession</returns>
        public DeviceSession RenewDeviceSession(Guid gameEnvironmentId)
        {
            LongJump.Api.Client.ApiResponse<DeviceSession> localVarResponse = RenewDeviceSessionWithHttpInfo(gameEnvironmentId);
            return localVarResponse.Data;
        }

        /// <summary>
        /// Renew a device session Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <returns>ApiResponse of DeviceSession</returns>
        public LongJump.Api.Client.ApiResponse<DeviceSession> RenewDeviceSessionWithHttpInfo(Guid gameEnvironmentId)
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

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request
            var localVarResponse = this.Client.Post<DeviceSession>("/api/v1/game_environments/{game_environment_id}/device_sessions/renew", localVarRequestOptions, this.Configuration);

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("RenewDeviceSession", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

        /// <summary>
        /// Renew a device session Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of DeviceSession</returns>
        public async System.Threading.Tasks.Task<DeviceSession> RenewDeviceSessionAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default)
        {
            var task = RenewDeviceSessionWithHttpInfoAsync(gameEnvironmentId, cancellationToken);
#if UNITY_EDITOR || !UNITY_WEBGL
            LongJump.Api.Client.ApiResponse<DeviceSession> localVarResponse = await task.ConfigureAwait(false);
#else
            LongJump.Api.Client.ApiResponse<DeviceSession> localVarResponse = await task;
#endif
            return localVarResponse.Data;
        }

        /// <summary>
        /// Renew a device session Returns a new two-day token for the device the current token belongs to, so the device keeps seeing the metadata of the participants it registered. Call it before the current token expires; the current token keeps working until then. Once it has expired, this answers &#x60;401&#x60;: create a new session instead, which is a new device.
        /// </summary>
        /// <exception cref="LongJump.Api.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="gameEnvironmentId">GameEnvironment id</param>
        /// <param name="cancellationToken">Cancellation Token to cancel the request.</param>
        /// <returns>Task of ApiResponse (DeviceSession)</returns>
        public async System.Threading.Tasks.Task<LongJump.Api.Client.ApiResponse<DeviceSession>> RenewDeviceSessionWithHttpInfoAsync(Guid gameEnvironmentId, System.Threading.CancellationToken cancellationToken = default)
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

            // authentication (bearer_auth) required
            // bearer authentication required
            if (!string.IsNullOrEmpty(this.Configuration.AccessToken) && !localVarRequestOptions.HeaderParameters.ContainsKey("Authorization"))
            {
                localVarRequestOptions.HeaderParameters.Add("Authorization", "Bearer " + this.Configuration.AccessToken);
            }

            // make the HTTP request

            var task = this.AsynchronousClient.PostAsync<DeviceSession>("/api/v1/game_environments/{game_environment_id}/device_sessions/renew", localVarRequestOptions, this.Configuration, cancellationToken);

#if UNITY_EDITOR || !UNITY_WEBGL
            var localVarResponse = await task.ConfigureAwait(false);
#else
            var localVarResponse = await task;
#endif

            if (this.ExceptionFactory != null)
            {
                Exception _exception = this.ExceptionFactory("RenewDeviceSession", localVarResponse);
                if (_exception != null) throw _exception;
            }

            return localVarResponse;
        }

    }
}
