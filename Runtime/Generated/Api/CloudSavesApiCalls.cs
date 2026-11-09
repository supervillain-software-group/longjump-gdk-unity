// Generated from the API spec by client-libs/generate.sh (templates/unity/calls.mustache). Don't edit.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LongJump.Api.Model;

namespace LongJump.Api
{
    /// <summary>
    /// The CloudSaves endpoints as one-line calls. Each fills in the GameEnvironment from
    /// <c>LongJump.Setup.Initialize</c> and goes through <c>LongJump.Session.CallAsync</c>, which sends the
    /// current token and signs in again once after a 401.
    /// </summary>
    public static class CloudSaves
    {
        /// <summary>Create a cloud save</summary>
        public static Task<CloudSaveSummary> CreateCloudSaveAsync(NewCloudSave newCloudSave, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new CloudSavesApi(configuration).CreateCloudSaveAsync(LongJump.Setup.GameEnvironmentId, newCloudSave, cancellationToken));

        /// <summary>Get a cloud save</summary>
        public static Task<CloudSave> GetCloudSaveAsync(Guid id, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new CloudSavesApi(configuration).GetCloudSaveAsync(id, cancellationToken));

        /// <summary>List the player&#39;s cloud saves</summary>
        public static Task<CloudSavePage> ListCloudSavesAsync(int? page = default, string name = default, string autosave = default, string varPublic = default, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new CloudSavesApi(configuration).ListCloudSavesAsync(LongJump.Setup.GameEnvironmentId, page, name, autosave, varPublic, cancellationToken));
    }
}
