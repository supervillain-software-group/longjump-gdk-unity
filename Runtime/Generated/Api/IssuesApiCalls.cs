// Generated from the API spec by client-libs/generate.sh (templates/unity/calls.mustache). Don't edit.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LongJump.Api.Model;

namespace LongJump.Api
{
    /// <summary>
    /// The Issues endpoints as one-line calls. Each fills in the GameEnvironment from
    /// <c>LongJump.Setup.Initialize</c> and goes through <c>LongJump.Session.CallAsync</c>, which sends the
    /// current token and signs in again once after a 401.
    /// </summary>
    public static class Issues
    {
        /// <summary>Report a bug, error or crash</summary>
        public static Task<IssueCreated> CreateIssueAsync(Issue issue, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new IssuesApi(configuration).CreateIssueAsync(LongJump.Setup.GameEnvironmentId, issue, cancellationToken));

        /// <summary>Say an issue&#39;s files are uploaded</summary>
        public static Task<IssueStatus> MarkIssueUploadedAsync(Guid id, IssueUploaded issueUploaded, CancellationToken cancellationToken = default) =>
            LongJump.Session.CallAsync(configuration => new IssuesApi(configuration).MarkIssueUploadedAsync(LongJump.Setup.GameEnvironmentId, id, issueUploaded, cancellationToken));
    }
}
