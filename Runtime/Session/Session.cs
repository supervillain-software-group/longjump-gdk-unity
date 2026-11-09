using System;
using System.Threading.Tasks;
using LongJump.Api;
using LongJump.Api.Client;
using LongJump.Api.Model;

namespace LongJump
{
    /// <summary>
    /// The session: Steam sign-in for a player, or a device session for a game whose players have no
    /// account (a kiosk), the token every request sends, and renewing it.
    /// </summary>
    /// <remarks>
    /// Tokens last two days. Steam sign-in takes a ticket provider, a function that gets a fresh Steam
    /// ticket, and the GDK signs in again on its own shortly before the token expires, or when the server
    /// refuses it. A device session renews itself the same way and stays the same device. When the GDK
    /// can't renew, it raises <see cref="Expired"/>.
    /// </remarks>
    public static class Session
    {
        static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(10);
        static readonly TimeSpan TicketTimeout = TimeSpan.FromSeconds(30);
        const float RenewRetrySeconds = 60f;

        static readonly IssueSettings DefaultIssueSettings =
            new IssueSettings(IssueSettings.ScreenshotModeEnum.Off, storePlayer: true);

        static Func<Task<SteamTicket>> ticketProvider;
        static DeviceSession device;
        static string deviceLabel;
        static Task<bool> renewal;
        static bool renewalRefused;
        static float nextRenewalAttempt;

        /// <summary>
        /// Changes on every sign-in, sign-out and Play start, so a sign-in or renewal that was
        /// already running when it changed finishes without touching the new session.
        /// </summary>
        static int generation;

        /// <summary>The generation whose sign-in the current session came from.</summary>
        static int established;

        /// <summary>
        /// A sign-in is running. The session from before it isn't renewed meanwhile, so a renewal
        /// can't finish after the new sign-in and replace it.
        /// </summary>
        static bool SignInPending => generation != established;

        /// <summary>The current play session, or null when not signed in.</summary>
        internal static PlaySession Current { get; private set; }

        /// <summary>The signed-in player, or null (also with a device session).</summary>
        public static Player Player => Current?.Player;

        /// <summary>Signed in, as a player or as a device.</summary>
        public static bool IsSignedIn => Current != null || device != null;

        /// <summary>When the token expires, in UTC.</summary>
        internal static DateTime? ExpiresAt => Current?.AccessToken?.ExpiresAt ?? device?.AccessToken?.ExpiresAt;

        static bool CanRenew => ticketProvider != null || device != null;

        /// <summary>How the GameEnvironment wants issues reported. Defaults apply before sign-in.</summary>
        internal static IssueSettings IssueSettings => Current?.IssueSettings ?? DefaultIssueSettings;

        /// <summary>
        /// The session ended and the GDK couldn't renew it; sign in again. Events stays connected and keeps
        /// receiving, but nothing that needs the token works until then. On the main thread.
        /// </summary>
        public static event Action Expired;

        internal static string Token => Current?.AccessToken?.Token ?? device?.AccessToken?.Token;

        /// <summary>Signs in, and signs in again with a fresh ticket whenever the session needs renewing.</summary>
        /// <param name="ticketProvider">Gets a fresh ticket from SteamUser.GetAuthTicketForWebApi.</param>
        /// <returns>The signed-in player.</returns>
        /// <exception cref="ApiException">The server refused the sign-in; see its ErrorCode and ErrorContent.</exception>
        public static async Task<Player> SignInWithSteamAsync(Func<Task<SteamTicket>> ticketProvider)
        {
            if (ticketProvider == null) throw new ArgumentNullException(nameof(ticketProvider));
            Setup.RequireInitialized();
            return await SignInAsync(ticketProvider, StartNewSession());
        }

        /// <summary>A new sign-in: anything still running for the last session stops counting.</summary>
        static int StartNewSession()
        {
            generation++;
            renewal = null;
            return generation;
        }

        /// <summary>
        /// Signs in, and signs in again whenever the session needs renewing. For Steam's callbacks:
        /// the provider asks Steam for a ticket and calls back with it when Steam answers, or with null
        /// when Steam fails. Sign-in fails if no answer comes within 30 seconds.
        /// </summary>
        public static async Task<Player> SignInWithSteamAsync(Action<Action<SteamTicket>> ticketProvider)
        {
            if (ticketProvider == null) throw new ArgumentNullException(nameof(ticketProvider));
            return await SignInWithSteamAsync(FromCallback(ticketProvider));
        }

        /// <summary>
        /// Signs in without a player, for a game whose players have no account, such as a kiosk at an
        /// event. The GameEnvironment must allow device sessions. A device session registers
        /// participants and posts their scores (see the README), and the GDK renews it before it expires,
        /// keeping the same device, so it keeps seeing the participants' metadata. Each call is a new
        /// device.
        /// </summary>
        /// <param name="deviceId">A label for this device in the LongJump admin, such as "booth-1".</param>
        /// <exception cref="ApiException">The server refused; a 403 means the GameEnvironment doesn't allow device sessions.</exception>
        public static async Task SignInAsDeviceAsync(string deviceId = null)
        {
            Setup.RequireInitialized();
            var started = StartNewSession();
            DeviceSession session;
            try
            {
                session = await new DeviceSessionsApi(SignInConfiguration())
                    .CreateDeviceSessionAsync(Setup.GameEnvironmentId, new NewDeviceSession(deviceId));
                if (generation != started) throw new OperationCanceledException("Signed out or signed in again while signing in.");
                RequireToken(session?.AccessToken);
            }
            catch (Exception) when (generation == started)
            {
                SignInFailed(started);
                throw;
            }

            established = started;
            ticketProvider = null;
            renewalRefused = false;
            Current = null;
            device = session;
            deviceLabel = deviceId;
            Events.Start();
        }

        /// <summary>Forgets the session and disconnects <see cref="Events"/>. Issues are still reported, without a player.</summary>
        public static void SignOut()
        {
            generation++;
            established = generation;
            renewal = null;
            ticketProvider = null;
            renewalRefused = false;
            Current = null;
            device = null;
            deviceLabel = null;
            Events.Stop();
        }

        static Configuration SignInConfiguration() => new Configuration { BasePath = Setup.BaseUrl, UserAgent = Setup.UserAgent };

        /// <summary>An IL2CPP build stripped above Minimal parses every response empty (see the README).</summary>
        static void RequireToken(AccessToken token)
        {
            if (string.IsNullOrEmpty(token?.Token))
                throw new InvalidOperationException("Sign-in returned no token. In an IL2CPP build, check Managed Stripping Level (see the LongJump README).");
        }

        static async Task<Player> SignInAsync(Func<Task<SteamTicket>> provider, int started)
        {
            PlaySession session;
            try
            {
                var ticket = await GetTicketAsync(provider);
                session = await new PlayerSessionApi(SignInConfiguration())
                    .StartSteamPlaySessionAsync(Setup.GameEnvironmentId, new SteamSignIn(ticket.Identity, ticket.TicketHex));

                // The game signed out or in again (or quit, or stopped Play) while this was waiting on Steam or the server.
                if (generation != started) throw new OperationCanceledException("Signed out or signed in again while signing in.");
                RequireToken(session?.AccessToken);
            }
            catch (Exception) when (generation == started)
            {
                SignInFailed(started);
                throw;
            }

            established = started;
            ticketProvider = provider;
            renewalRefused = false;
            device = null;
            deviceLabel = null;
            Current = session;
            Events.Start();
            return session.Player;
        }

        /// <summary>A sign-in failed: the session from before it, if any, stays and renews again.</summary>
        static void SignInFailed(int started) => established = started;

        /// <summary>
        /// Makes a call to a generated API class with the session's token. When the server refuses the
        /// token, renews the session once and repeats the call. Build the API class inside the function,
        /// from the configuration it's given. A device session's token that isn't due to expire isn't
        /// renewed on a 401: that endpoint doesn't take device sessions, and the ApiException comes back as
        /// it is. Games rarely need this: the one-line calls (<c>LongJump.Api.Leaderboards</c> and so on)
        /// already go through it.
        /// </summary>
        /// <example><c>var page = await Session.CallAsync(c =&gt; new LeaderboardsApi(c).GetLeaderboardEntriesAsync(Setup.GameEnvironmentId, "daily"));</c></example>
        /// <exception cref="SessionExpiredException">The token was refused and the session couldn't be renewed.</exception>
        public static async Task<T> CallAsync<T>(Func<Configuration, Task<T>> call)
        {
            if (call == null) throw new ArgumentNullException(nameof(call));
            var token = Token;
            try
            {
                return await call(Setup.Configuration);
            }
            catch (ApiException e) when (e.ErrorCode == 401 && token != null)
            {
                if (device != null && Token == token && ExpiresAt > DateTime.UtcNow + RenewBefore) throw;

                // Another call, or a new sign-in, may have replaced the token meanwhile.
                if (Token == token) await RenewAsync(tokenRejected: true);
                if (Token == null || Token == token) throw new SessionExpiredException(e);
                return await call(Setup.Configuration);
            }
        }

        /// <summary>Signs in again with the ticket provider, or renews the device session. Only one renewal runs at a time.</summary>
        static Task<bool> RenewAsync(bool tokenRejected)
        {
            if (renewal != null && !renewal.IsCompleted) return renewal;
            if (SignInPending) return Task.FromResult(false);
            renewal = RenewOnceAsync(tokenRejected);
            return renewal;
        }

        static async Task<bool> RenewOnceAsync(bool tokenRejected)
        {
            if (!CanRenew || renewalRefused)
            {
                if (tokenRejected) Expire();
                return false;
            }

            var started = generation;
            try
            {
                if (device != null) await RenewDeviceAsync(started);
                else await SignInAsync(ticketProvider, started);
                return true;
            }
            catch (Exception) when (generation != started)
            {
                // Signed out or in again meanwhile: this renewal belongs to a session that's gone.
                return false;
            }
            catch (ApiException e) when (e.ErrorCode >= 400 && e.ErrorCode < 500)
            {
                // A refused sign-in isn't retried on a timer: retrying can't fix a bad ticket or app setup.
                renewalRefused = true;
                Log.Warning($"Renewing the session was refused ({e.ErrorCode}): {e.ErrorContent}");
                if (tokenRejected) Expire();
                return false;
            }
            catch (Exception e)
            {
                Log.Warning($"Couldn't renew the session, retrying in {RenewRetrySeconds}s: {e.Message}");
                nextRenewalAttempt = UnityEngine.Time.realtimeSinceStartup + RenewRetrySeconds;
                return false;
            }
        }

        /// <summary>
        /// A new token for the same device, so it keeps seeing its participants' metadata. A token that
        /// already expired can't be renewed: then it starts a new device session, a new device.
        /// </summary>
        static async Task RenewDeviceAsync(int started)
        {
            DeviceSession renewed;
            try
            {
                renewed = await new DeviceSessionsApi(Setup.Configuration).RenewDeviceSessionAsync(Setup.GameEnvironmentId);
            }
            catch (ApiException e) when (e.ErrorCode == 401)
            {
                Log.Warning("The device session expired before it could be renewed, so this is now a new device: it can't see the metadata of participants registered before.");
                renewed = await new DeviceSessionsApi(SignInConfiguration())
                    .CreateDeviceSessionAsync(Setup.GameEnvironmentId, new NewDeviceSession(deviceLabel));
            }
            if (generation != started) throw new OperationCanceledException("Signed out or signed in again while renewing.");
            RequireToken(renewed?.AccessToken);

            device = renewed;
            Events.Start();
        }

        /// <summary>Ends the session. Events stays connected: receiving needs no token, only broadcasting does.</summary>
        static void Expire()
        {
            if (!IsSignedIn) return;
            Current = null;
            device = null;
            Log.Invoke(Expired, "Session.Expired");
        }

        /// <summary>When Play starts: forgets the last Play session's sign-in and the game's handlers.</summary>
        internal static void ResetForPlay()
        {
            generation++;
            established = generation;
            ticketProvider = null;
            device = null;
            deviceLabel = null;
            renewal = null;
            renewalRefused = false;
            nextRenewalAttempt = 0f;
            Current = null;
            Expired = null;
        }

        /// <summary>Called every frame: renews shortly before the token expires, and expires the session when it does.</summary>
        internal static void Tick()
        {
            if (!IsSignedIn || ExpiresAt == null) return;

            var now = DateTime.UtcNow;
            var expiresAt = ExpiresAt.Value.ToUniversalTime();
            if (now >= expiresAt && (!CanRenew || renewalRefused))
            {
                Expire();
                return;
            }

            if (!CanRenew || renewalRefused || now < expiresAt - RenewBefore) return;
            if (SignInPending || (renewal != null && !renewal.IsCompleted)) return;
            if (UnityEngine.Time.realtimeSinceStartup < nextRenewalAttempt) return;

            nextRenewalAttempt = UnityEngine.Time.realtimeSinceStartup + RenewRetrySeconds;
            _ = RenewAsync(tokenRejected: now >= expiresAt);
        }

        /// <summary>Asks the provider for a ticket, giving up after 30 seconds so a ticket Steam never sends can't stall sign-in.</summary>
        static async Task<SteamTicket> GetTicketAsync(Func<Task<SteamTicket>> provider)
        {
            var request = provider() ?? throw new InvalidOperationException("The ticket provider returned no task.");
            if (await Task.WhenAny(request, Task.Delay(TicketTimeout)) != request)
            {
                _ = request.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException("Steam didn't provide a ticket within 30 seconds.");
            }
            return await request ?? throw new InvalidOperationException("The ticket provider returned no ticket.");
        }

        static Func<Task<SteamTicket>> FromCallback(Action<Action<SteamTicket>> provider) => () =>
        {
            var result = new TaskCompletionSource<SteamTicket>();
            provider(ticket => result.TrySetResult(ticket));
            return result.Task;
        };
    }

    /// <summary>A Steam auth ticket from SteamUser.GetAuthTicketForWebApi.</summary>
    public sealed class SteamTicket
    {
        internal string Identity { get; }

        internal string TicketHex { get; }

        /// <param name="identity">The identity passed to GetAuthTicketForWebApi.</param>
        /// <param name="ticketHex">The ticket it returned, hex-encoded.</param>
        public SteamTicket(string identity, string ticketHex)
        {
            Identity = identity;
            TicketHex = ticketHex;
        }

        /// <summary>Hex-encodes the first <paramref name="length"/> bytes of a ticket buffer.</summary>
        public static SteamTicket FromBytes(string identity, byte[] ticket, int length)
        {
            var hex = new System.Text.StringBuilder(length * 2);
            for (var i = 0; i < length; i++) hex.Append(ticket[i].ToString("X2"));
            return new SteamTicket(identity, hex.ToString());
        }
    }

    /// <summary>The server refused the player's token and the session couldn't be renewed. Sign in again.</summary>
    public class SessionExpiredException : Exception
    {
        internal SessionExpiredException(Exception inner) : base("The LongJump session expired; sign in again.", inner) { }
    }
}
