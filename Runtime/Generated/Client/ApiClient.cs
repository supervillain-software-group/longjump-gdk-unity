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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine.Networking;
using UnityEngine;

namespace LongJump.Api.Client
{
    /// <summary>
    /// To Serialize/Deserialize JSON using our custom logic, but only when ContentType is JSON.
    /// </summary>
    internal class CustomJsonCodec
    {
        private readonly IReadableConfiguration _configuration;
        private static readonly string _contentType = "application/json";
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            // OpenAPI generated types generally hide default constructors.
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            DateParseHandling = DateParseHandling.None,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy
                {
                    OverrideSpecifiedNames = false
                }
            }
        };

        public CustomJsonCodec(IReadableConfiguration configuration)
        {
            _configuration = configuration;
        }

        public CustomJsonCodec(JsonSerializerSettings serializerSettings, IReadableConfiguration configuration)
        {
            _serializerSettings = serializerSettings;
            _configuration = configuration;
        }

        /// <summary>
        /// Serialize the object into a JSON string.
        /// </summary>
        /// <param name="obj">Object to be serialized.</param>
        /// <returns>A JSON string.</returns>
        public string Serialize(object obj)
        {
            if (obj != null && obj is LongJump.Api.Model.AbstractOpenAPISchema)
            {
                // the object to be serialized is an oneOf/anyOf schema
                return ((LongJump.Api.Model.AbstractOpenAPISchema)obj).ToJson();
            }
            else
            {
                return JsonConvert.SerializeObject(obj, _serializerSettings);
            }
        }

        public T Deserialize<T>(UnityWebRequest request)
        {
            var result = (T) Deserialize(request, typeof(T));
            return result;
        }

        /// <summary>
        /// Deserialize the JSON string into a proper object.
        /// </summary>
        /// <param name="response">The UnityWebRequest after it has a response.</param>
        /// <param name="type">Object type.</param>
        /// <returns>Object representation of the JSON string.</returns>
        internal object Deserialize(UnityWebRequest request, Type type)
        {
            if (type == typeof(byte[])) // return byte array
            {
                return request.downloadHandler.data;
            }

            // TODO: ? if (type.IsAssignableFrom(typeof(Stream)))
            if (type == typeof(Stream))
            {
                // NOTE: Ignoring Content-Disposition filename support, since not all platforms
                // have a location on disk to write arbitrary data (tvOS, consoles).
                return new MemoryStream(request.downloadHandler.data);
            }

            if (type.Name.StartsWith("System.Nullable`1[[System.DateTime")) // return a datetime object
            {
                return DateTime.Parse(request.downloadHandler.text, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }

            if (type == typeof(string) || type.Name.StartsWith("System.Nullable")) // return primitive type
            {
                return Convert.ChangeType(request.downloadHandler.text, type);
            }

            var contentType = request.GetResponseHeader("Content-Type");

            if (!string.IsNullOrEmpty(contentType) && contentType.Contains("application/json"))
            {
                var text = request.downloadHandler?.text;

                // Generated APIs that don't expect a return value provide System.Object as the type
                if (type == typeof(global::System.Object) && (string.IsNullOrEmpty(text) || text.Trim() == "null"))
                {
                    return null;
                }

                if (request.responseCode >= 200 && request.responseCode < 300)
                {
                    try
                    {
                        // Deserialize as a model
                        return JsonConvert.DeserializeObject(text, type, _serializerSettings);
                    }
                    catch (Exception e)
                    {
                        throw new UnexpectedResponseException(request, type, e.ToString());
                    }
                }
                else
                {
                    throw new ApiException((int)request.responseCode, request.error, text);
                }
            }
            
            if (type != typeof(global::System.Object) && request.responseCode >= 200 && request.responseCode < 300)
            {
                throw new UnexpectedResponseException(request, type);
            }

            return null;

        }

        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }

        public string ContentType
        {
            get { return _contentType; }
            set { throw new InvalidOperationException("Not allowed to set content type."); }
        }
    }
    /// <summary>
    /// Provides a default implementation of an Api client (both synchronous and asynchronous implementations),
    /// encapsulating general REST accessor use cases.
    /// </summary>
    /// <remarks>
    /// The Dispose method will manage the HttpClient lifecycle when not passed by constructor.
    /// </remarks>
    public partial class ApiClient : IDisposable, ISynchronousClient, IAsynchronousClient
    {
        private readonly string _baseUrl;

        /// <summary>
        /// Specifies the settings on a <see cref="JsonSerializer" /> object.
        /// These settings can be adjusted to accommodate custom serialization rules.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; set; } = new JsonSerializerSettings
        {
            // OpenAPI generated types generally hide default constructors.
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            DateParseHandling = DateParseHandling.None,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy
                {
                    OverrideSpecifiedNames = false
                }
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" />, defaulting to the global configurations' base url.
        /// </summary>
        public ApiClient() :
                 this(LongJump.Api.Client.GlobalConfiguration.Instance.BasePath)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClient" />.
        /// </summary>
        /// <param name="basePath">The target service's base path in URL format.</param>
        /// <exception cref="ArgumentException"></exception>
        public ApiClient(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) throw new ArgumentException("basePath cannot be empty");

            _baseUrl = basePath;
        }

        /// <summary>
        /// Disposes resources if they were created by us
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Provides all logic for constructing a new UnityWebRequest.
        /// At this point, all information for querying the service is known. Here, it is simply
        /// mapped into the UnityWebRequest.
        /// </summary>
        /// <param name="method">The http verb.</param>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>[private] A new UnityWebRequest instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private UnityWebRequest NewRequest<T>(
            string method,
            string path,
            RequestOptions options,
            IReadableConfiguration configuration)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (options == null) throw new ArgumentNullException("options");
            if (configuration == null) throw new ArgumentNullException("configuration");

            WebRequestPathBuilder builder = new WebRequestPathBuilder(_baseUrl, path);

            builder.AddPathParameters(options.PathParameters);

            builder.AddQueryParameters(options.QueryParameters);

            string contentType = null;
            if (options.HeaderParameters != null && options.HeaderParameters.ContainsKey("Content-Type"))
            {
                var contentTypes = options.HeaderParameters["Content-Type"];
                contentType = contentTypes.FirstOrDefault();
            }

            var uri = builder.GetFullUri();
            UnityWebRequest request = null;

            if (contentType == "multipart/form-data")
            {
                var formData = new List<IMultipartFormSection>();
                foreach (var formParameter in options.FormParameters)
                {
                    formData.Add(new MultipartFormDataSection(formParameter.Key, formParameter.Value));
                }

                request = UnityWebRequest.Post(uri, formData);
                request.method = method;
            }
            else if (contentType == "application/x-www-form-urlencoded")
            {
                var form = new WWWForm();
                foreach (var kvp in options.FormParameters)
                {
                    form.AddField(kvp.Key, kvp.Value);
                }

                request = UnityWebRequest.Post(uri, form);
                request.method = method;
            }
            else if (contentType == "application/octet-stream")
            {
                if(options.Data is Stream stream)
                {
                    using (var binaryReader = new BinaryReader(stream))
                    {
                        var bytes = binaryReader.ReadBytes((int)stream.Length);
                        request = UnityWebRequest.Put(uri, bytes);
                        request.method = method;
                        request.SetRequestHeader("Content-Type", "application/octet-stream");
                    }
                }
                else
                {
                    throw new InvalidDataException($"{nameof(options.Data)} is not of {nameof(Stream)} type");
                }
            }
            else if (options.Data != null)
            {
                var serializer = new CustomJsonCodec(SerializerSettings, configuration);
                var jsonData = serializer.Serialize(options.Data);

                // Making a post body application/json encoded is whack with UnityWebRequest.
                // See: https://stackoverflow.com/questions/68156230/unitywebrequest-post-not-sending-body
                request = UnityWebRequest.Put(uri, jsonData);
                request.method = method;
                request.SetRequestHeader("Content-Type", "application/json");
            }
            else
            {
                request = new UnityWebRequest(builder.GetFullUri(), method);
            }

            if (request.downloadHandler == null && typeof(T) != typeof(global::System.Object))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
            }

#if UNITY_EDITOR || !UNITY_WEBGL
            if (configuration.UserAgent != null)
            {
                request.SetRequestHeader("User-Agent", configuration.UserAgent);
            }
#endif

            if (configuration.DefaultHeaders != null)
            {
                foreach (var headerParam in configuration.DefaultHeaders)
                {
                    request.SetRequestHeader(headerParam.Key, headerParam.Value);
                }
            }

            if (options.HeaderParameters != null)
            {
                foreach (var headerParam in options.HeaderParameters)
                {
                    foreach (var value in headerParam.Value)
                    {
                        // Todo make content headers actually content headers
                        request.SetRequestHeader(headerParam.Key, value);
                    }
                }
            }

            if (options.Cookies != null && options.Cookies.Count > 0)
            {
                #if UNITY_WEBGL
                throw new System.InvalidOperationException("UnityWebRequest does not support setting cookies in WebGL");
                #else
                if (options.Cookies.Count != 1)
                {
                    UnityEngine.Debug.LogError("Only one cookie supported, ignoring others");
                }

                request.SetRequestHeader("Cookie", options.Cookies[0].ToString());
                #endif
            }

            return request;

        }

        partial void InterceptRequest(UnityWebRequest req, string path, RequestOptions options, IReadableConfiguration configuration);
        partial void InterceptResponse(UnityWebRequest req, string path, RequestOptions options, IReadableConfiguration configuration, ref object responseData);

        private ApiResponse<T> ToApiResponse<T>(UnityWebRequest request, object responseData)
        {
            T result = (T) responseData;

            var transformed = new ApiResponse<T>((HttpStatusCode)request.responseCode, new Multimap<string, string>(), result, request.downloadHandler?.text ?? "")
            {
                ErrorText = request.error,
                Cookies = new List<Cookie>()
            };

            // process response headers, e.g. Access-Control-Allow-Methods
            var responseHeaders = request.GetResponseHeaders();
            if (responseHeaders != null)
            {
                foreach (var responseHeader in request.GetResponseHeaders())
                {
                    transformed.Headers.Add(responseHeader.Key, ClientUtils.ParameterToString(responseHeader.Value));
                }
            }

            return transformed;
        }

        private async Task<ApiResponse<T>> ExecAsync<T>(
            UnityWebRequest request,
            string path,
            RequestOptions options,
            IReadableConfiguration configuration,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var deserializer = new CustomJsonCodec(SerializerSettings, configuration);

            using (request)
            {
                if (configuration.Timeout > TimeSpan.Zero)
                {
                    request.timeout = (int)Math.Ceiling(configuration.Timeout.TotalSeconds);
                }

                if (configuration.Proxy != null)
                {
                    throw new InvalidOperationException("Configuration `Proxy` not supported by UnityWebRequest");
                }

                if (configuration.ClientCertificates != null)
                {
                    // Only Android/iOS/tvOS/Standalone players can support certificates, and this
                    // implementation is intended to work on all platforms.
                    //
                    // TODO: Could optionally allow support for this on these platforms.
                    //
                    // See: https://docs.unity3d.com/ScriptReference/Networking.CertificateHandler.html
                    throw new InvalidOperationException("Configuration `ClientCertificates` not supported by UnityWebRequest on all platforms");
                }

                InterceptRequest(request, path, options, configuration);

        #if UNITY_2020_2_OR_NEWER
                // For Unity 2020.2 and newer, use UnityWebRequest.Result.
                var asyncOp = request.SendWebRequest();
                TaskCompletionSource<UnityWebRequest.Result> tcs = new TaskCompletionSource<UnityWebRequest.Result>();
                asyncOp.completed += (_) => tcs.TrySetResult(request.result);
                using (var tokenRegistration = cancellationToken.Register(request.Abort, true))
                {
                    await tcs.Task;
                }

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    throw new ConnectionException(request);
                }
        #else
                // For Unity 2019 and earlier, await the operation directly.
                var asyncOp = request.SendWebRequest();
                using (var tokenRegistration = cancellationToken.Register(request.Abort, true))
                {
                    await asyncOp;
                }

                if (request.isNetworkError || request.isHttpError)
                {
                    throw new ConnectionException(request);
                }
        #endif

                object responseData = deserializer.Deserialize<T>(request);

                // if the response type is oneOf/anyOf, call FromJSON to deserialize the data
                if (typeof(LongJump.Api.Model.AbstractOpenAPISchema).IsAssignableFrom(typeof(T)))
                {
                    responseData = (T) typeof(T).GetMethod("FromJson").Invoke(null, new object[] { new ByteArrayContent(request.downloadHandler.data) });
                }
                else if (typeof(T).Name == "Stream") // for binary response
                {
                    responseData = (T) (object) new MemoryStream(request.downloadHandler.data);
                }

                InterceptResponse(request, path, options, configuration, ref responseData);

                return ToApiResponse<T>(request, responseData);
            }
        }

        #region IAsynchronousClient
        /// <summary>
        /// Make a HTTP GET request (async).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <param name="cancellationToken">Token that enables callers to cancel the request.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public Task<ApiResponse<T>> GetAsync<T>(string path, RequestOptions options, IReadableConfiguration configuration = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var config = configuration ?? GlobalConfiguration.Instance;
            return ExecAsync<T>(NewRequest<T>("GET", path, options, config), path, options, config, cancellationToken);
        }

        /// <summary>
        /// Make a HTTP POST request (async).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <param name="cancellationToken">Token that enables callers to cancel the request.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public Task<ApiResponse<T>> PostAsync<T>(string path, RequestOptions options, IReadableConfiguration configuration = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var config = configuration ?? GlobalConfiguration.Instance;
            return ExecAsync<T>(NewRequest<T>("POST", path, options, config), path, options, config, cancellationToken);
        }

        /// <summary>
        /// Make a HTTP PUT request (async).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <param name="cancellationToken">Token that enables callers to cancel the request.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public Task<ApiResponse<T>> PutAsync<T>(string path, RequestOptions options, IReadableConfiguration configuration = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var config = configuration ?? GlobalConfiguration.Instance;
            return ExecAsync<T>(NewRequest<T>("PUT", path, options, config), path, options, config, cancellationToken);
        }

        /// <summary>
        /// Make a HTTP DELETE request (async).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <param name="cancellationToken">Token that enables callers to cancel the request.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public Task<ApiResponse<T>> DeleteAsync<T>(string path, RequestOptions options, IReadableConfiguration configuration = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var config = configuration ?? GlobalConfiguration.Instance;
            return ExecAsync<T>(NewRequest<T>("DELETE", path, options, config), path, options, config, cancellationToken);
        }

        /// <summary>
        /// Make a HTTP HEAD request (async).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <param name="cancellationToken">Token that enables callers to cancel the request.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public Task<ApiResponse<T>> HeadAsync<T>(string path, RequestOptions options, IReadableConfiguration configuration = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var config = configuration ?? GlobalConfiguration.Instance;
            return ExecAsync<T>(NewRequest<T>("HEAD", path, options, config), path, options, config, cancellationToken);
        }

        /// <summary>
        /// Make a HTTP OPTION request (async).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <param name="cancellationToken">Token that enables callers to cancel the request.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public Task<ApiResponse<T>> OptionsAsync<T>(string path, RequestOptions options, IReadableConfiguration configuration = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var config = configuration ?? GlobalConfiguration.Instance;
            return ExecAsync<T>(NewRequest<T>("OPTIONS", path, options, config), path, options, config, cancellationToken);
        }

        /// <summary>
        /// Make a HTTP PATCH request (async).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <param name="cancellationToken">Token that enables callers to cancel the request.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public Task<ApiResponse<T>> PatchAsync<T>(string path, RequestOptions options, IReadableConfiguration configuration = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var config = configuration ?? GlobalConfiguration.Instance;
            return ExecAsync<T>(NewRequest<T>("PATCH", path, options, config), path, options, config, cancellationToken);
        }
        #endregion IAsynchronousClient

        #region ISynchronousClient
        /// <summary>
        /// Make a HTTP GET request (synchronous).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public ApiResponse<T> Get<T>(string path, RequestOptions options, IReadableConfiguration configuration = null)
        {
            throw new System.NotImplementedException("UnityWebRequest does not support synchronous operation");
        }

        /// <summary>
        /// Make a HTTP POST request (synchronous).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public ApiResponse<T> Post<T>(string path, RequestOptions options, IReadableConfiguration configuration = null)
        {
            throw new System.NotImplementedException("UnityWebRequest does not support synchronous operation");
        }

        /// <summary>
        /// Make a HTTP PUT request (synchronous).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public ApiResponse<T> Put<T>(string path, RequestOptions options, IReadableConfiguration configuration = null)
        {
            throw new System.NotImplementedException("UnityWebRequest does not support synchronous operation");
        }

        /// <summary>
        /// Make a HTTP DELETE request (synchronous).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public ApiResponse<T> Delete<T>(string path, RequestOptions options, IReadableConfiguration configuration = null)
        {
            throw new System.NotImplementedException("UnityWebRequest does not support synchronous operation");
        }

        /// <summary>
        /// Make a HTTP HEAD request (synchronous).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public ApiResponse<T> Head<T>(string path, RequestOptions options, IReadableConfiguration configuration = null)
        {
            throw new System.NotImplementedException("UnityWebRequest does not support synchronous operation");
        }

        /// <summary>
        /// Make a HTTP OPTION request (synchronous).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public ApiResponse<T> Options<T>(string path, RequestOptions options, IReadableConfiguration configuration = null)
        {
            throw new System.NotImplementedException("UnityWebRequest does not support synchronous operation");
        }

        /// <summary>
        /// Make a HTTP PATCH request (synchronous).
        /// </summary>
        /// <param name="path">The target path (or resource).</param>
        /// <param name="options">The additional request options.</param>
        /// <param name="configuration">A per-request configuration object. It is assumed that any merge with
        /// GlobalConfiguration has been done before calling this method.</param>
        /// <returns>A Task containing ApiResponse</returns>
        public ApiResponse<T> Patch<T>(string path, RequestOptions options, IReadableConfiguration configuration = null)
        {
            throw new System.NotImplementedException("UnityWebRequest does not support synchronous operation");
        }
        #endregion ISynchronousClient
    }
}
