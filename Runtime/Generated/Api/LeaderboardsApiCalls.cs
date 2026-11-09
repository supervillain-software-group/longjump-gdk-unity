// Generated from the API spec by client-libs/generate.sh (templates/unity/calls.mustache). Don't edit.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LongJump.Api.Model;

namespace LongJump.Api
{
    /// <summary>
    /// The Leaderboards endpoints as one-line calls. Each fills in the GameEnvironment from
    /// <c>LongJump.Setup.Initialize</c> and goes through <c>LongJump.Session.CallAsync</c>, which sends the
    /// current token and signs in again once after a 401.
    /// </summary>
    public static class Leaderboards
    {
        /// <summary>Check a name before registering</summary>
        public static Task<LeaderboardParticipantNameCheck> CheckLeaderboardParticipantNameAsync(string slug, string name, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).CheckLeaderboardParticipantNameAsync(LongJump.Setup.GameEnvironmentId, slug, name, cancellationToken));

        /// <summary>Get a leaderboard</summary>
        public static Task<Leaderboard> GetLeaderboardAsync(string slug, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).GetLeaderboardAsync(LongJump.Setup.GameEnvironmentId, slug, cancellationToken));

        /// <summary>Read the ranked entries</summary>
        public static Task<LeaderboardPage> GetLeaderboardEntriesAsync(string slug, string period = default, int? limit = default, string from = default, string cursor = default, string playerIds = default, string participantIds = default, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).GetLeaderboardEntriesAsync(LongJump.Setup.GameEnvironmentId, slug, period, limit, from, cursor, playerIds, participantIds, cancellationToken));

        /// <summary>Read the entries around an owner</summary>
        public static Task<LeaderboardPage> GetLeaderboardEntriesAroundAsync(string slug, string period = default, int? limit = default, Guid? playerId = default, Guid? participantId = default, string me = default, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).GetLeaderboardEntriesAroundAsync(LongJump.Setup.GameEnvironmentId, slug, period, limit, playerId, participantId, me, cancellationToken));

        /// <summary>Get a participant</summary>
        public static Task<LeaderboardParticipant> GetLeaderboardParticipantAsync(string slug, Guid id, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).GetLeaderboardParticipantAsync(LongJump.Setup.GameEnvironmentId, slug, id, cancellationToken));

        /// <summary>List the leaderboards</summary>
        public static Task<List<Leaderboard>> ListLeaderboardsAsync(CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).ListLeaderboardsAsync(LongJump.Setup.GameEnvironmentId, cancellationToken));

        /// <summary>Register a participant</summary>
        public static Task<LeaderboardParticipant> RegisterLeaderboardParticipantAsync(string slug, NewLeaderboardParticipant newLeaderboardParticipant, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).RegisterLeaderboardParticipantAsync(LongJump.Setup.GameEnvironmentId, slug, newLeaderboardParticipant, cancellationToken));

        /// <summary>Post a score</summary>
        public static Task<LeaderboardSubmissionResult> SubmitLeaderboardEntryAsync(string slug, NewLeaderboardEntry newLeaderboardEntry, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new LeaderboardsApi(configuration).SubmitLeaderboardEntryAsync(LongJump.Setup.GameEnvironmentId, slug, newLeaderboardEntry, cancellationToken));
    }
}
