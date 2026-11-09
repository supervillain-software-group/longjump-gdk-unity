# LongJump GDK for Unity

The Unity client for the LongJump Game Dev Kit. It needs Unity 2022.3 or newer and `com.unity.nuget.newtonsoft-json`, and makes every request with `UnityWebRequest`. Everything is in namespace `LongJump`; every callback and event runs on the main thread.

This package offers what games do (sign in, listen for player events, watch a leaderboard and post to it, load a save slot, report bugs); for everything else, every endpoint is a one-line generated call in `LongJump.Api` (see "The rest of the API").

Calls that wait return a `Task`, so `await` them. To use one in a coroutine instead, add `.AsCoroutine()` and `yield return` it, then check its `Error` and read its `Result`; it never throws into the coroutine:

```csharp
var report = Issues.ReportBugAsync(description).AsCoroutine();
yield return report;
if (report.Error != null) Debug.LogWarning(report.Error.Message);
```

## Setup and sign-in

Call `Setup.Initialize` once when the game starts, with the GameEnvironment id from the LongJump admin, then sign in when Steam is ready. The GDK gets Steam tickets through the game's Steam library, and signs in again by itself before the two-day token expires, or when the server refuses it:

```csharp
using LongJump;

Setup.Initialize(new Guid("428b32bc-d75a-4232-bbe7-cb8445c8bd25"));

try
{
    var player = await Session.SignInWithSteamAsync();
}
catch (Exception e) { Debug.LogWarning(e.Message); }
```

That works with Steamworks.NET (20.2.0 and newer, which Heathen's Steamworks toolkit also uses) and Facepunch.Steamworks. The GDK's editor code finds the library whenever scripts reload and turns on its support, logging a line that starts with `[LongJump]`:

- **Installed as a package:** nothing is needed. Removing the package turns the support off.
- **Copied into the project, or as DLLs:** the GDK adds `LONGJUMP_STEAMWORKS_NET` or `LONGJUMP_FACEPUNCH` to Player Settings > Scripting Define Symbols (Standalone), and removes it when you delete the library in the editor. If you remove the library while the editor is closed, remove the define yourself.
- **Steamworks.NET copied in without its `.asmdef` file** compiles into your own scripts, where the GDK can't reach it: keep the `.asmdef`, or install Steamworks.NET as a package.

Your game still starts Steam itself (`SteamAPI.Init` or `SteamClient.Init`), and decides when to sign in. Signing in before Steam has started fails with a message saying so. `SignInWithSteamAsync("my-identity")` uses a ticket identity of your own (5 to 30 characters); the default is `longjump`.

With any other Steam library, give sign-in a ticket provider: a function that gets a fresh ticket from `GetAuthTicketForWebApi`. The GDK calls it at sign-in and at each renewal:

```csharp
var player = await Session.SignInWithSteamAsync(reply =>
{
    pendingTicketReply = reply;   // your Steam library's ticket callback calls
    RequestWebApiTicket("longjump");
});

// When Steam answers:
pendingTicketReply(SteamTicket.FromBytes("longjump", ticketBytes, ticketLength));
```

`Session.SignInWithSteamAsync(Func<Task<SteamTicket>>)` takes a provider that returns a `Task` instead. `Session.SignInWithSteamAsync(ticket)` signs in with one ticket you already have, but a ticket works only once, so that session can't renew: when it runs out, `Session.Expired` is raised.

Sign-in returns the player, and `Session.Player` and `Session.IsSignedIn` stay current. When a session ends and can't be renewed, `Session.Expired` is raised: sign in again. `Setup.Initialize(id, "http://localhost:3000")` points at a local server.

### Without a player: device sessions

A game whose players have no account, such as a kiosk at an event, signs in as a device instead, once an admin has turned on device sessions for the GameEnvironment:

```csharp
await Session.SignInAsDeviceAsync("booth-1");   // the label shows in the LongJump admin
```

A device session registers participants by name and posts their scores (see Leaderboards). `Session.Player` stays null. The GDK renews it before its two-day token expires and keeps it the same device, so it keeps seeing the private metadata of the participants it registered. A device session works on leaderboards and real-time events, and reports issues without a player; endpoints that need a player (cloud saves, player events) answer `401`. Each call to `SignInAsDeviceAsync` is a new device, and so is a session whose token had already expired when the GDK tried to renew it (after the kiosk was off for two days), which then can't see earlier participants' metadata. The admin console and its CSV export always have it.

## Real-time events

Once signed in, the GDK connects to LongJump's real-time server and stays connected, reconnecting after drops. Listen for player events by name, and broadcast your own with the generated API:

```csharp
using LongJump.Api.Model;   // the generated models

var subscription = Events.On("item_get", e =>
{
    if (!e.IsMine) ShowToast($"{e.Player.DisplayName} found {e.Data["item_name"]}");
});

await LongJump.Api.PlayerEvents.BroadcastPlayerEventAsync(
    new NewPlayerEvent("item_get", new { item_name = "Ember Blade", quality = 0 }));

subscription.Dispose();   // stop listening
```

`e.Data` is the JSON the sender broadcast (`e.Data.ToObject<MyType>()` converts it), with the field names the sender's type used. Players receive their own broadcasts too; `e.IsMine` tells them apart. `Events.IsConnected`, `Events.Connected` and `Events.Disconnected` report the connection.

The GDK uses its own MQTT client. A game that already ships MQTTnet from `com.github.asus4.mqtt-unity` can keep it: the package then includes `MqttNetTransport`, and `Setup.Initialize(id, mqttTransport: new MqttNetTransport())` uses it. Any other library works through an adapter implementing `IMqttTransport`.

## Cloud saves

Saves go in named slots. Saving to a slot adds a new save, and `CloudSaves.LoadAsync` returns a slot's newest one as your own type, matching the slot name exactly (the server's search matches parts of names):

```csharp
await LongJump.Api.CloudSaves.CreateCloudSaveAsync(new NewCloudSave("slot1", saveData));

var save = await CloudSaves.LoadAsync<SaveData>("slot1");   // null when the slot is empty
if (save != null) Apply(save);
```

## Leaderboards

Leaderboards are set up in the LongJump admin and addressed by slug. The API docs' Leaderboards guide explains what each setting does. Post a score with `Leaderboards.SubmitAsync`, which gives the post a `RequestId` and sends it once more after a connection error, a timeout or a server error, so a score is neither lost nor counted twice:

```csharp
var result = await Leaderboards.SubmitAsync("daily", new NewLeaderboardEntry(
    score: 4200,
    tiebreakers: new Dictionary<string, long> { ["time"] = 61300 },   // one per tie-breaker the board has
    attributes: new { character = "Ryu" }));                          // shown with the entry
Debug.Log($"Rank {result.Entry.Rank}{(result.Improved ? ", a new best" : "")}");
```

A refused post isn't retried: the `ApiException`'s `ErrorCode` says why (`409` the board is closed, with `nextOpenAt` in `ErrorContent`; `422` invalid or out of attempts; `429` too many posts). `SubmitAsync` doesn't change your entry, so reusing one object for each run is fine. If you set `RequestId` yourself, use a new one for each run: an id the server already accepted returns the first response and drops the new score.

Watch a board to keep a screen current. The handler gets the current top right away, then every change (at most about once a second), on the main thread, and `onPeriodClosed` gets the final standings when a period freezes:

```csharp
var watch = Leaderboards.Watch("daily",
    standings => ShowTop(standings.Entries),   // each has Rank, Score, Tiebreakers, Attributes, Owner.Name
    final => ShowWinner(final.Entries[0]));
watch.Dispose();   // stop
```

Live changes arrive once a player or device session has signed in. The server sends nothing when a new period starts or a daily window opens or closes, and messages sent while a screen was disconnected are gone, so `Watch` reads the board again at those moments and after every reconnect; your handler just shows what it's given.

Everything else is a one-line generated call in `LongJump.Api.Leaderboards`. Reads need no sign-in:

```csharp
var boards = await LongJump.Api.Leaderboards.ListLeaderboardsAsync();
var board = await LongJump.Api.Leaderboards.GetLeaderboardAsync("daily");   // settings, and whether it's open
var top = await LongJump.Api.Leaderboards.GetLeaderboardEntriesAsync("daily", limit: 10);
var more = await LongJump.Api.Leaderboards.GetLeaderboardEntriesAsync("daily", limit: 10, cursor: top.NextCursor);
var yesterday = await LongJump.Api.Leaderboards.GetLeaderboardEntriesAsync("daily", period: "previous");
var bottom = await LongJump.Api.Leaderboards.GetLeaderboardEntriesAsync("daily", from: "bottom", limit: 4);   // the last 4, best first, with their real ranks
var aroundMe = await LongJump.Api.Leaderboards.GetLeaderboardEntriesAroundAsync("daily", limit: 5, me: "true");
var chosen = await LongJump.Api.Leaderboards.GetLeaderboardEntriesAsync("daily", playerIds: string.Join(",", ids));
```

At a kiosk, signed in as a device, register each visitor and post for them with `ParticipantId`:

```csharp
var alex = await LongJump.Api.Leaderboards.RegisterLeaderboardParticipantAsync("conference",
    new NewLeaderboardParticipant("Alex", new { email = "alex@example.com" }));   // metadata stays private
if (alex.Existing) { /* someone registered this name already: ask if it's them */ }
await Leaderboards.SubmitAsync("conference", new NewLeaderboardEntry(4200, participantId: alex.Id));
var again = await LongJump.Api.Leaderboards.GetLeaderboardParticipantAsync("conference", alex.Id);
```

Either order works. Registering straight away is enough when the kiosk asks for the name and the details together, since `Existing` says the name was taken. To ask "are you the Alex who played earlier?" before asking for anything else, check the name first; it registers nothing:

```csharp
var check = await LongJump.Api.Leaderboards.CheckLeaderboardParticipantNameAsync("conference", "Alex");
if (check.Taken) { /* resume as check.Participant.Id, or ask for another name */ }
else { /* ask for the email, then register as above, still handling Existing */ }
```

A name the server refuses (offensive, blank, too long) answers `422` from the check too, so the kiosk can ask for another before collecting the rest.

## Issues

Crashes (unhandled exceptions) are reported on their own once `Setup.Initialize` has run, each distinct one once per run. The rest is up to the game:

```csharp
// What just happened, sent with the next issue. Scene loads, warnings and errors are added for you.
Issues.Breadcrumb("entered the boss fight", "combat");

// An exception the game handled. Never throws.
catch (Exception e) { Issues.ReportException(e); }

// When the player pauses, before the pause menu appears, so the shot shows the game:
await Issues.CaptureScreenshotAsync();
pauseMenu.SetActive(true);

// When they send it. IssueFile refuses an empty file or one over IssueFile.MaxBytes (10MB).
await Issues.ReportBugAsync(description,
    new[] { new IssueFile("save", "save.json", saveBytes, "application/json") }, cloudSaveId);
```

Every issue carries the breadcrumbs, the end of the game's log (`Issues.AttachLog`), system info, the scene and `Application.version`, and anything `Issues.Extra` returns. Screenshots follow the GameEnvironment's screenshot mode from sign-in: none when it is off (and before sign-in), the ones the game asks for with `on_request`, and one per breadcrumb (at most one a second) with `every_breadcrumb`. The last five are kept at 960 pixels wide. A bug report takes them (with `on_request`, only the newest); crash and error reports send copies, so they never take the screenshot a bug report is about.

In the Unity editor, crashes aren't reported unless `Issues.CaptureCrashesInEditor` is set before `Setup.Initialize`, so exceptions hit while developing don't become issues. Everything else works in Play mode, and the GDK starts fresh each time Play is pressed, including in projects that turn off domain reload.

Anything logged with `Debug.LogException` counts as a crash. For an exception the game handled, call `ReportException` instead of logging it.

## The rest of the API

Every endpoint is a one-line call, generated from the API spec: a static class per area of the API in `LongJump.Api` (`LongJump.Api.Leaderboards`, `LongJump.Api.CloudSaves`, `LongJump.Api.PlayerEvents` and so on), with a method named after the endpoint's operation in the API docs, and models in `LongJump.Api.Model`. Each call fills in the GameEnvironment from `Setup.Initialize`, sends the current token, and renews the session and retries once when the server refuses the token. Calls that need no player work before sign-in:

```csharp
using LongJump.Api.Model;

var saves = await LongJump.Api.CloudSaves.ListCloudSavesAsync();
await LongJump.Api.PlayerEvents.BroadcastPlayerEventAsync(new NewPlayerEvent("level_up", data));
```

`LongJump.Api.Leaderboards` and `LongJump.Leaderboards` (and the same for `CloudSaves` and `Issues`) share a short name, so a file that has both `using LongJump;` and `using LongJump.Api;` must spell one of them out. These examples spell out the generated ones.

Behind each one-line call is a generated class (`LeaderboardsApi` and so on) that takes the server address and token as a `Configuration`. To call one yourself, build it inside `Session.CallAsync`, which supplies the configuration and does the renewal: `Session.CallAsync(c => new LongJump.Api.LeaderboardsApi(c).GetLeaderboardAsync(Setup.GameEnvironmentId, "daily"))`. Use the `Async` methods; the others throw `NotImplementedException`.

Game data (event data, saves, issue extra data) keeps the field names your types use. The server checks every request and answers a bad one with `422` and its reasons, in the `ApiException`.

## IL2CPP code stripping

At Managed Stripping Level Minimal, IL2CPP builds work as they are. At Low or higher, Unity removes the parts of `LongJump.Api` that Newtonsoft fills by reflection, and every server response arrives empty, in builds only, never in the editor. Unity reads `link.xml` files only from a project's `Assets` folder, so copy this package's `Runtime/link.xml` into `Assets` (for example `Assets/LongJump/link.xml`) before raising the level. At High, your own classes that only Newtonsoft creates, such as the type you pass to `CloudSaves.LoadAsync<T>` or `e.Data.ToObject<T>()`, can be stripped the same way, so keep those in your own `link.xml` too.

## Where this comes from

This package lives in the LongJump repo under `client-libs/unity/`, and is copied into games. Don't edit a game's copy: change it in the LongJump repo and copy it again. `Runtime/Generated` is generated from the API spec; everything else in `Runtime` is written by hand, following `docs/client-libs-spec.md` there.
