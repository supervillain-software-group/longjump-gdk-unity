// Generated from the API spec by client-libs/generate.sh (templates/unity/calls.mustache). Don't edit.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LongJump.Api.Model;

namespace LongJump.Api
{
    /// <summary>
    /// The MQTT endpoints as one-line calls. Each fills in the GameEnvironment from
    /// <c>LongJump.Setup.Initialize</c> and goes through <c>LongJump.Session.CallAsync</c>, which sends the
    /// current token and signs in again once after a 401.
    /// </summary>
    public static class MQTT
    {
        /// <summary>Get the MQTT broker and topics</summary>
        public static Task<MqttTopics> GetMqttTopicsAsync(CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new MQTTApi(configuration).GetMqttTopicsAsync(LongJump.Setup.GameEnvironmentId, cancellationToken));

        /// <summary>Get the player&#39;s own MQTT topic</summary>
        public static Task<PlayerMqttTopics> GetPlayerMqttTopicsAsync(CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new MQTTApi(configuration).GetPlayerMqttTopicsAsync(cancellationToken));
    }
}
