// Generated from the API spec by client-libs/generate.sh (templates/unity/calls.mustache). Don't edit.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LongJump.Api.Model;

namespace LongJump.Api
{
    /// <summary>
    /// The PlayerSession endpoints as one-line calls. Each fills in the GameEnvironment from
    /// <c>LongJump.Setup.Initialize</c> and goes through <c>LongJump.Session.CallAsync</c>, which sends the
    /// current token and signs in again once after a 401.
    /// </summary>
    public static class PlayerSession
    {
        /// <summary>Start a play session with Steam</summary>
        public static Task<PlaySession> StartSteamPlaySessionAsync(SteamSignIn steamSignIn, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new PlayerSessionApi(configuration).StartSteamPlaySessionAsync(LongJump.Setup.GameEnvironmentId, steamSignIn, cancellationToken));
    }
}
