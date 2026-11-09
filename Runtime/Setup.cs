using System;
using LongJump.Api.Client;
using UnityEngine;

namespace LongJump
{
    /// <summary>
    /// The game's connection to LongJump. Call <see cref="Initialize"/> once at startup, before
    /// anything else, then sign in with <see cref="Session"/> when Steam is ready.
    /// </summary>
    /// <remarks>
    /// The generated classes in LongJump.Api cover every endpoint, for anything this layer doesn't
    /// wrap. Call them through <see cref="Session.CallAsync{T}"/>, which gives them the current token
    /// and renews the session when the server refuses it.
    /// </remarks>
    public static class Setup
    {
        public const string Version = "0.8.0";
        internal const string ProductionUrl = "https://app.longjump.dev";

        public static Guid GameEnvironmentId { get; private set; }

        internal static string BaseUrl { get; private set; }

        internal static bool IsInitialized { get; private set; }

        internal static string UserAgent => $"LongJumpGdk/{Version} Unity/{Application.unityVersion}";

        /// <summary>A new configuration with the base URL and, once signed in, the player's token.</summary>
        internal static Configuration Configuration
        {
            get
            {
                RequireInitialized();
                return new Configuration
                {
                    BasePath = BaseUrl,
                    UserAgent = UserAgent,
                    AccessToken = Session.Token
                };
            }
        }

        /// <param name="gameEnvironmentId">The GameEnvironment id from the LongJump admin.</param>
        /// <param name="baseUrl">The LongJump server, such as http://localhost:3000 in development.</param>
        /// <param name="captureCrashes">Report unhandled exceptions as crashes.</param>
        /// <param name="mqttTransport">
        /// The MQTT client for <see cref="Events"/>. By default, LongJump's own. Pass an adapter to keep
        /// an MQTT library your game already ships, such as <c>MqttNetTransport</c> for MQTTnet.
        /// </param>
        public static void Initialize(Guid gameEnvironmentId, string baseUrl = ProductionUrl, bool captureCrashes = true,
            IMqttTransport mqttTransport = null)
        {
            if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentException("Required.", nameof(baseUrl));

            MainThread.Ensure();
            if (IsInitialized) Session.SignOut();

            GameEnvironmentId = gameEnvironmentId;
            BaseUrl = baseUrl.TrimEnd('/');
            IsInitialized = true;

            Events.Reset(mqttTransport ?? new MqttTcpTransport());
            Issues.Reset(captureCrashes);
            Leaderboards.OnInitialized();
        }

        /// <summary>
        /// Runs each time Play starts, before any scene loads. A project that turns off domain reload keeps
        /// static state from the last Play session; this starts the GDK fresh, as a new run of the game would.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay()
        {
            Session.ResetForPlay();
            Events.ResetForPlay();
            Issues.ResetForPlay();
            Leaderboards.ResetForPlay();
            MainThread.ResetForPlay();
            IsInitialized = false;
            GameEnvironmentId = Guid.Empty;
            BaseUrl = null;
        }

        internal static void RequireInitialized()
        {
            if (!IsInitialized) throw new InvalidOperationException("Call LongJump.Setup.Initialize first.");
        }
    }
}
