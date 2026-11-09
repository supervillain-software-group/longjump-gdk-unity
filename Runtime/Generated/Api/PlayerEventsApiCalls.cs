// Generated from the API spec by client-libs/generate.sh (templates/unity/calls.mustache). Don't edit.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LongJump.Api.Model;

namespace LongJump.Api
{
    /// <summary>
    /// The PlayerEvents endpoints as one-line calls. Each fills in the GameEnvironment from
    /// <c>LongJump.Setup.Initialize</c> and goes through <c>LongJump.Session.CallAsync</c>, which sends the
    /// current token and signs in again once after a 401.
    /// </summary>
    public static class PlayerEvents
    {
        /// <summary>Broadcast a player event</summary>
        public static Task<PlayerEventMessage> BroadcastPlayerEventAsync(NewPlayerEvent newPlayerEvent, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new PlayerEventsApi(configuration).BroadcastPlayerEventAsync(LongJump.Setup.GameEnvironmentId, newPlayerEvent, cancellationToken));
    }
}
