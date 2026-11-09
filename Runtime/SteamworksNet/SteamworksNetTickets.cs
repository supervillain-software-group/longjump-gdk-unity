// This compiles only when the project has Steamworks.NET 20.2.0 or newer: as a package (the assembly
// definition's version rule) or copied in (LONGJUMP_STEAMWORKS_NET, which the GDK's editor code sets in
// Player Settings > Scripting Define Symbols, Standalone). If it fails to compile because Steamworks.NET
// was removed while the editor was closed, remove LONGJUMP_STEAMWORKS_NET there.

// Steamworks.NET's types exist only where it compiles them; this mirrors its own switch.
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_ANDROID || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace LongJump
{
    /// <summary>
    /// Gets Steam tickets through Steamworks.NET for <c>Session.SignInWithSteamAsync()</c>. It compiles only
    /// when the GDK's editor code finds Steamworks.NET in the project (the LONGJUMP_STEAMWORKS_NET define).
    /// </summary>
    static class SteamworksNetTickets
    {
        static readonly Dictionary<HAuthTicket, (string identity, TaskCompletionSource<SteamTicket> result)> pending =
            new Dictionary<HAuthTicket, (string, TaskCompletionSource<SteamTicket>)>();
        static Callback<GetTicketForWebApiResponse_t> responses;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            pending.Clear();
            responses?.Dispose();
            responses = null;
            Session.UseSteamLibrary("Steamworks.NET", RequestAsync);
        }

        static Task<SteamTicket> RequestAsync(string identity)
        {
            var result = new TaskCompletionSource<SteamTicket>();
            if (MainThread.IsCurrent) Request(identity, result);
            else MainThread.Post(() => Request(identity, result));
            return result.Task;
        }

        /// <summary>Asks Steam for a ticket. Steam answers later, through <see cref="OnResponse"/>.</summary>
        internal static void Request(string identity, TaskCompletionSource<SteamTicket> result)
        {
            try
            {
                // Steam sends its answer to every listener, the game's own included, so OnResponse takes
                // only the answers to the GDK's own requests.
                if (responses == null) responses = Callback<GetTicketForWebApiResponse_t>.Create(OnResponse);
                var handle = SteamUser.GetAuthTicketForWebApi(identity);
                if (handle == HAuthTicket.Invalid) throw new InvalidOperationException("Steam didn't start the ticket request.");
                pending[handle] = (identity, result);
            }
            catch (Exception e)
            {
                result.TrySetException(new InvalidOperationException(
                    "Couldn't get a Steam ticket through Steamworks.NET. Is Steam running, and has the game called SteamAPI.Init? " + e.Message, e));
            }
        }

        internal static void OnResponse(GetTicketForWebApiResponse_t response)
        {
            if (!pending.TryGetValue(response.m_hAuthTicket, out var request)) return;
            pending.Remove(response.m_hAuthTicket);
            if (response.m_eResult != EResult.k_EResultOK)
                request.result.TrySetException(new InvalidOperationException($"Steam couldn't provide a ticket: {response.m_eResult}."));
            else
                request.result.TrySetResult(SteamTicket.FromBytes(request.identity, response.m_rgubTicket, response.m_cubTicket));
        }
    }
}
#endif
