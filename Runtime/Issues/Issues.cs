using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LongJump.Api;
using LongJump.Api.Client;
using LongJump.Api.Model;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LongJump
{
    /// <summary>
    /// Reports bug reports, caught errors and crashes to LongJump, with breadcrumbs, the end of the
    /// game's log and recent screenshots.
    /// </summary>
    /// <remarks>
    /// Reporting follows the API's three steps: create the issue listing its files, upload each file
    /// straight to storage, then say they're uploaded. Crashes are unhandled exceptions, caught from
    /// Unity's log; anything logged with Debug.LogException counts as one, so to report an exception
    /// your game handled, call <see cref="ReportException"/> instead of logging it.
    /// </remarks>
    public static class Issues
    {
        const int MaxBreadcrumbs = 100;
        const int MaxScreenshots = 5;
        const int MaxLogBytes = 2 * 1024 * 1024;
        const int MaxScreenshotBytes = 2 * 1024 * 1024;
        const int MaxTextLength = 10_000;
        const int MaxStackTraceLength = 100_000;

        /// <summary>Automatic reports (crashes and errors) sent at most this many times per run.</summary>
        internal static int MaxAutomaticReports { get; set; } = 20;

        /// <summary>Screenshots are scaled down to this width.</summary>
        internal static int ScreenshotWidth { get; set; } = 960;

        /// <summary>Attach the end of the game's log (Application.consoleLogPath) to every issue.</summary>
        public static bool AttachLog { get; set; } = true;

        /// <summary>Breadcrumbs for scene loads and for logged warnings and errors.</summary>
        public static bool AutomaticBreadcrumbs { get; set; } = true;

        /// <summary>Called when building each issue; return any JSON-serializable object to send as its extra data.</summary>
        public static Func<object> Extra { get; set; }

        /// <summary>
        /// Report crashes while running in the Unity editor too. Off by default, so exceptions hit while
        /// developing don't become issues. Set it before <see cref="Setup.Initialize"/>.
        /// </summary>
        public static bool CaptureCrashesInEditor { get; set; }

        static readonly object gate = new object();
        static readonly Queue<Dictionary<string, object>> breadcrumbs = new Queue<Dictionary<string, object>>();
        static readonly Queue<byte[]> screenshots = new Queue<byte[]>();
        static readonly HashSet<string> reportedExceptions = new HashSet<string>();
        static bool captureCrashes;
        static int automaticReports;
        static float lastBreadcrumbScreenshot = float.NegativeInfinity;
        static Task capturing = Task.CompletedTask;

        /// <summary>From <see cref="Setup.Initialize"/>: starts over, and listens to Unity's log and scene loads.</summary>
        internal static void Reset(bool captureUnhandled)
        {
            Application.logMessageReceivedThreaded -= OnLogMessage;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            lock (gate)
            {
                captureCrashes = captureUnhandled && (!Application.isEditor || CaptureCrashesInEditor);
                breadcrumbs.Clear();
                screenshots.Clear();
                reportedExceptions.Clear();
                automaticReports = 0;
            }
            Application.logMessageReceivedThreaded += OnLogMessage;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>When Play starts: stops listening, and puts every option back to its default.</summary>
        internal static void ResetForPlay()
        {
            Application.logMessageReceivedThreaded -= OnLogMessage;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            lock (gate)
            {
                captureCrashes = false;
                breadcrumbs.Clear();
                screenshots.Clear();
                reportedExceptions.Clear();
                automaticReports = 0;
            }
            lastBreadcrumbScreenshot = float.NegativeInfinity;
            capturing = Task.CompletedTask;
            MaxAutomaticReports = 20;
            ScreenshotWidth = 960;
            AttachLog = true;
            AutomaticBreadcrumbs = true;
            Extra = null;
            CaptureCrashesInEditor = false;
        }

        static IssueSettings.ScreenshotModeEnum ScreenshotMode => Session.IssueSettings.ScreenshotMode;

        /// <summary>Records what just happened, to send with the next issue. Safe from any thread.</summary>
        /// <param name="category">Such as "custom", "ui" or "combat".</param>
        /// <param name="data">Any JSON-serializable values.</param>
        public static void Breadcrumb(string message, string category = "custom", IDictionary<string, object> data = null)
        {
            var crumb = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["category"] = category,
                ["message"] = message
            };
            if (data != null) crumb["data"] = data;

            lock (gate)
            {
                breadcrumbs.Enqueue(crumb);
                while (breadcrumbs.Count > MaxBreadcrumbs) breadcrumbs.Dequeue();
            }

            if (ScreenshotMode == IssueSettings.ScreenshotModeEnum.EveryBreadcrumb)
                MainThread.Post(CaptureBreadcrumbScreenshot);
        }

        /// <summary>
        /// Takes a screenshot of this frame to send with the next issue. Does nothing unless the
        /// GameEnvironment's screenshot mode allows it. The last five are kept, at reduced resolution.
        /// </summary>
        /// <remarks>
        /// The screen is read once the frame finishes drawing, so take it before showing a menu over the
        /// game: wait for the result, then show the menu. Works while the game is paused (timeScale 0).
        /// </remarks>
        /// <example>
        /// <code>
        /// IEnumerator Pause() {
        ///     yield return LongJump.Issues.CaptureScreenshotAsync().AsCoroutine();
        ///     pauseMenu.SetActive(true);
        /// }
        /// </code>
        /// </example>
        public static Task CaptureScreenshotAsync()
        {
            if (ScreenshotMode == IssueSettings.ScreenshotModeEnum.Off) return Task.CompletedTask;
            var done = new TaskCompletionSource<bool>();
            capturing = done.Task;
            if (MainThread.IsCurrent) StartCapture(done);
            else MainThread.Post(() => StartCapture(done));
            return done.Task;
        }

        static void StartCapture(TaskCompletionSource<bool> done)
        {
            if (MainThread.Run(CaptureAtEndOfFrame(done)) == null) done.TrySetResult(false);
        }

        /// <summary>Sends a bug report the player wrote.</summary>
        /// <param name="description">What the player wrote. Optional.</param>
        /// <param name="files">Files of your own, such as the current save.</param>
        /// <param name="cloudSaveId">One of the player's cloud saves, left out when the GameEnvironment doesn't store players.</param>
        /// <exception cref="ApiException">The server refused the report.</exception>
        /// <exception cref="IssueUploadException">A file could not be uploaded. The issue is stored, and completed without the file after 4 hours.</exception>
        public static async Task<IssueCreated> ReportBugAsync(string description = null, IEnumerable<IssueFile> files = null, Guid? cloudSaveId = null)
        {
            Setup.RequireInitialized();
            var issue = BuildIssue(Issue.KindEnum.BugReport, cloudSaveId);
            issue.Description = Truncate(description, MaxTextLength);
            return await SendAsync(issue, files?.ToList() ?? new List<IssueFile>());
        }

        /// <summary>
        /// Reports an exception your game caught and handled. Safe from any thread, and never throws;
        /// the result is null when the report could not be sent, or the same exception was already
        /// reported this run.
        /// </summary>
        public static Task<IssueCreated> ReportException(Exception exception)
        {
            if (exception == null || !Setup.IsInitialized) return Task.FromResult<IssueCreated>(null);
            if (MainThread.IsCurrent) return ReportExceptionOnMainThread(exception);

            var result = new TaskCompletionSource<IssueCreated>();
            MainThread.Post(async () => result.TrySetResult(await ReportExceptionOnMainThread(exception)));
            return result.Task;
        }

        static async Task<IssueCreated> ReportExceptionOnMainThread(Exception exception)
        {
            try
            {
                return await BuildAndSendException(exception);
            }
            catch (Exception e)
            {
                Log.Warning($"Couldn't report an exception: {e.Message}");
                return null;
            }
        }

        static async Task<IssueCreated> BuildAndSendException(Exception exception)
        {
            var stackTrace = exception.StackTrace ?? new System.Diagnostics.StackTrace(1, true).ToString();
            if (!ShouldReportAutomatically(exception.GetType().Name, stackTrace)) return null;

            var issue = BuildIssue(Issue.KindEnum.Error, null);
            issue.ExceptionType = exception.GetType().Name;
            issue.ExceptionMessage = Truncate(exception.Message, MaxTextLength);
            issue.StackTrace = Truncate(stackTrace, MaxStackTraceLength);
            if (exception.InnerException != null)
                issue.Extra = new Dictionary<string, object> { ["innerException"] = exception.InnerException.ToString(), ["game"] = issue.Extra };

            return await SendQuietlyAsync(issue);
        }

        static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (condition != null && condition.StartsWith(Log.Prefix)) return;

            if (type == LogType.Exception)
            {
                if (captureCrashes) MainThread.Post(() => ReportCrash(condition, stackTrace));
                return;
            }

            if (AutomaticBreadcrumbs && (type == LogType.Error || type == LogType.Assert || type == LogType.Warning))
                Breadcrumb(Truncate(condition, 1000), type == LogType.Warning ? "warning" : "error");
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (AutomaticBreadcrumbs) Breadcrumb($"loaded {scene.name}", "scene");
        }

        // Unity logs an unhandled exception as "Type: message" with its stack trace.
        static void ReportCrash(string condition, string stackTrace)
        {
            condition ??= "";
            var separator = condition.IndexOf(": ", StringComparison.Ordinal);
            var exceptionType = separator > 0 ? condition.Substring(0, separator) : condition;
            if (!ShouldReportAutomatically(exceptionType, stackTrace)) return;

            var issue = BuildIssue(Issue.KindEnum.Crash, null);
            issue.ExceptionType = Truncate(exceptionType, 255);
            issue.ExceptionMessage = separator > 0 ? Truncate(condition.Substring(separator + 2), MaxTextLength) : null;
            issue.StackTrace = Truncate(stackTrace, MaxStackTraceLength);
            _ = SendQuietlyAsync(issue);
        }

        // The same exception from the same place is reported once per run, so one in Update doesn't
        // send a report every frame.
        static bool ShouldReportAutomatically(string exceptionType, string stackTrace)
        {
            var topFrames = string.Join("\n", (stackTrace ?? "").Split('\n').Take(5));
            lock (gate)
            {
                if (automaticReports >= MaxAutomaticReports) return false;
                if (!reportedExceptions.Add(exceptionType + "\n" + topFrames)) return false;
                automaticReports++;
                return true;
            }
        }

        static Issue BuildIssue(Issue.KindEnum kind, Guid? cloudSaveId)
        {
            List<object> crumbs;
            lock (gate) crumbs = breadcrumbs.Cast<object>().ToList();

            var storePlayer = Session.IssueSettings.StorePlayer;
            return new Issue(kind: kind)
            {
                ClientVersion = Application.version,
                Platform = Application.platform.ToString(),
                Scene = SceneManager.GetActiveScene().name,
                SystemInfo = SystemInfoJson(),
                Breadcrumbs = crumbs,
                Extra = GameExtra(),
                CloudSaveId = storePlayer && cloudSaveId.HasValue ? cloudSaveId.Value : default
            };
        }

        static object GameExtra()
        {
            try
            {
                return Extra?.Invoke();
            }
            catch (Exception e)
            {
                Log.Warning($"Issues.Extra threw, so this issue has no extra data: {e.Message}");
                return null;
            }
        }

        static async Task<IssueCreated> SendQuietlyAsync(Issue issue)
        {
            try
            {
                return await SendAsync(issue, new List<IssueFile>());
            }
            catch (Exception e)
            {
                Log.Warning($"Couldn't report {issue.Kind}: {e.Message}");
                return null;
            }
        }

        static async Task<IssueCreated> SendAsync(Issue issue, List<IssueFile> files)
        {
            var pending = new List<(IssueUploadRequest request, byte[] bytes)>();
            foreach (var file in files)
                pending.Add((UploadRequest(IssueUploadRequest.TypeEnum.File, file.Filename, file.Bytes, file.ContentType, file.Name), file.Bytes));

            // A screenshot asked for this frame belongs with this report. It's ready at the end of the frame.
            await Task.WhenAny(capturing, Task.Delay(1000));
            var shots = ScreenshotsFor(issue.Kind);
            for (var i = 0; i < shots.Length; i++)
                pending.Add((UploadRequest(IssueUploadRequest.TypeEnum.Screenshot, $"screenshot-{i + 1}.jpg", shots[i], "image/jpeg"), shots[i]));

            var log = AttachLog ? ReadLogTail() : null;
            if (log != null)
                pending.Add((UploadRequest(IssueUploadRequest.TypeEnum.Log, Path.GetFileName(Application.consoleLogPath), log, "text/plain"), log));

            issue.Uploads = pending.Select(p => p.request).ToList();
            var created = await Session.CallAsync(configuration =>
                new IssuesApi(configuration).CreateIssueAsync(Setup.GameEnvironmentId, issue));
            if (created.Uploads == null || created.Uploads.Count == 0) return created;

            foreach (var upload in created.Uploads)
            {
                // The server may return a cleaned-up filename (Active Storage replaces characters such as ':' and '/'),
                // so fall back to the type and name, which is unique for files.
                bool SameKind((IssueUploadRequest request, byte[] bytes) p) =>
                    p.request.Type.ToString() == upload.Type.ToString() && p.request.Name == upload.Name;
                var match = pending.FindIndex(p => SameKind(p) && p.request.Filename == upload.Filename);
                if (match < 0) match = pending.FindIndex(SameKind);
                if (match < 0) continue;
                await PutAsync(upload, pending[match].bytes);
                pending.RemoveAt(match);
            }

            var status = await Session.CallAsync(configuration => new IssuesApi(configuration)
                .MarkIssueUploadedAsync(Setup.GameEnvironmentId, created.Id, new IssueUploaded(created.UploadToken)));
            created.Status = status.Status == "new" ? IssueCreated.StatusEnum.New : created.Status;
            if (issue.Kind == Issue.KindEnum.BugReport) Forget(shots);
            return created;
        }

        // Bug reports take the screenshots with them once sent. Automatic reports send copies, so a crash
        // doesn't take the screenshot the player's bug report is about. With on_request a bug report sends
        // only the newest (the one taken for it); with every_breadcrumb, the last five as a trail.
        static byte[][] ScreenshotsFor(Issue.KindEnum kind)
        {
            lock (gate)
            {
                var all = screenshots.ToArray();
                if (kind == Issue.KindEnum.BugReport && ScreenshotMode == IssueSettings.ScreenshotModeEnum.OnRequest && all.Length > 1)
                    return new[] { all[all.Length - 1] };
                return all;
            }
        }

        static void Forget(byte[][] sent)
        {
            lock (gate)
            {
                var kept = screenshots.Where(shot => Array.IndexOf(sent, shot) < 0).ToList();
                screenshots.Clear();
                foreach (var shot in kept) screenshots.Enqueue(shot);
            }
        }

        static IssueUploadRequest UploadRequest(IssueUploadRequest.TypeEnum type, string filename, byte[] bytes, string contentType, string name = null)
        {
            using var md5 = MD5.Create();
            return new IssueUploadRequest(
                type: type,
                name: name,
                filename: filename,
                byteSize: bytes.Length,
                checksum: Convert.ToBase64String(md5.ComputeHash(bytes)),
                contentType: contentType);
        }

        // Storage checks the bytes against the checksum, size and content type the issue declared.
        static async Task PutAsync(IssueUpload upload, byte[] bytes)
        {
            for (var attempt = 1; ; attempt++)
            {
                using var request = new UnityWebRequest(upload.Url, UnityWebRequest.kHttpVerbPUT)
                {
                    uploadHandler = new UploadHandlerRaw(bytes),
                    downloadHandler = new DownloadHandlerBuffer()
                };
                foreach (var header in upload.Headers)
                    request.SetRequestHeader(header.Key, header.Value);
                if (upload.Headers.TryGetValue("Content-Type", out var contentType))
                    request.uploadHandler.contentType = contentType;

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success) return;
                var retry = request.result == UnityWebRequest.Result.ConnectionError || request.responseCode >= 500;
                if (!retry || attempt == 2)
                    throw new IssueUploadException(upload.Filename, request.responseCode, request.downloadHandler?.text ?? request.error);
            }
        }

        static byte[] ReadLogTail()
        {
            var path = Application.consoleLogPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            try
            {
                // Unity keeps the log open for writing.
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var length = (int)Math.Min(stream.Length, MaxLogBytes);
                if (length == 0) return null;
                stream.Seek(-length, SeekOrigin.End);
                var bytes = new byte[length];
                var read = 0;
                while (read < length)
                {
                    var n = stream.Read(bytes, read, length - read);
                    if (n == 0) break;
                    read += n;
                }
                return read == length ? bytes : bytes.Take(read).ToArray();
            }
            catch (Exception e)
            {
                Log.Warning($"Couldn't read the log: {e.Message}");
                return null;
            }
        }

        static void CaptureBreadcrumbScreenshot()
        {
            if (Time.realtimeSinceStartup - lastBreadcrumbScreenshot < 1f) return;
            lastBreadcrumbScreenshot = Time.realtimeSinceStartup;
            MainThread.Run(CaptureAtEndOfFrame(null));
        }

        static IEnumerator CaptureAtEndOfFrame(TaskCompletionSource<bool> done)
        {
            yield return new WaitForEndOfFrame();

            Texture2D full = null;
            try
            {
                full = ScreenCapture.CaptureScreenshotAsTexture();
                var jpg = EncodeScaled(full);
                if (jpg.Length <= MaxScreenshotBytes)
                {
                    lock (gate)
                    {
                        screenshots.Enqueue(jpg);
                        while (screenshots.Count > MaxScreenshots) screenshots.Dequeue();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Couldn't take a screenshot: {e.Message}");
            }
            finally
            {
                if (full != null) UnityEngine.Object.Destroy(full);
                done?.TrySetResult(true);
            }
        }

        static byte[] EncodeScaled(Texture2D source)
        {
            if (source.width <= ScreenshotWidth) return source.EncodeToJPG(75);

            var width = ScreenshotWidth;
            var height = Mathf.RoundToInt(source.height * (float)width / source.width);
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var scaled = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                scaled.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                scaled.Apply();
                return scaled.EncodeToJPG(75);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(scaled);
            }
        }

        static Dictionary<string, object> SystemInfoJson() => new Dictionary<string, object>
        {
            ["operatingSystem"] = SystemInfo.operatingSystem,
            ["deviceModel"] = SystemInfo.deviceModel,
            ["processorType"] = SystemInfo.processorType,
            ["processorCount"] = SystemInfo.processorCount,
            ["systemMemoryMB"] = SystemInfo.systemMemorySize,
            ["graphicsDeviceName"] = SystemInfo.graphicsDeviceName,
            ["graphicsMemoryMB"] = SystemInfo.graphicsMemorySize,
            ["graphicsDeviceType"] = SystemInfo.graphicsDeviceType.ToString(),
            ["screen"] = $"{Screen.width}x{Screen.height}",
            ["unityVersion"] = Application.unityVersion,
            ["gdkVersion"] = Setup.Version
        };

        static string Truncate(string text, int max) =>
            text == null || text.Length <= max ? text : text.Substring(0, max);
    }

    /// <summary>
    /// A file could not be uploaded to storage. The issue is stored and stays incomplete; LongJump
    /// completes it without the missing files after 4 hours.
    /// </summary>
    public class IssueUploadException : Exception
    {
        public string Filename { get; }
        public long StatusCode { get; }

        internal IssueUploadException(string filename, long statusCode, string detail)
            : base($"Uploading {filename} failed ({statusCode}): {detail}")
        {
            Filename = filename;
            StatusCode = statusCode;
        }
    }
}
