// This compiles only when the project has Facepunch.Steamworks (LONGJUMP_FACEPUNCH, which the GDK's editor
// code sets in Player Settings > Scripting Define Symbols, Standalone). If it fails to compile because
// Facepunch.Steamworks was removed while the editor was closed, remove LONGJUMP_FACEPUNCH there.
using System;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace LongJump
{
    /// <summary>
    /// Gets Steam tickets through Facepunch.Steamworks for <c>Session.SignInWithSteamAsync()</c>. It compiles
    /// only when the GDK's editor code finds Facepunch.Steamworks in the project (the LONGJUMP_FACEPUNCH define).
    /// </summary>
    static class FacepunchTickets
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() => Session.UseSteamLibrary("Facepunch.Steamworks", RequestAsync);

        static Task<SteamTicket> RequestAsync(string identity)
        {
            var result = new TaskCompletionSource<SteamTicket>();
            if (MainThread.IsCurrent) Request(identity, result);
            else MainThread.Post(() => Request(identity, result));
            return result.Task;
        }

        /// <summary>Facepunch.Steamworks waits for Steam's answer to its own request itself.</summary>
        internal static async void Request(string identity, TaskCompletionSource<SteamTicket> result)
        {
            try
            {
                if (!SteamClient.IsValid)
                    throw new InvalidOperationException("Is Steam running, and has the game called SteamClient.Init?");
                var ticket = await SteamUser.GetAuthTicketForWebApiAsync(identity);
                if (ticket?.Data == null) throw new InvalidOperationException("Steam didn't provide a ticket.");
                result.TrySetResult(SteamTicket.FromBytes(identity, ticket.Data, ticket.Data.Length));
            }
            catch (Exception e)
            {
                result.TrySetException(new InvalidOperationException("Couldn't get a Steam ticket through Facepunch.Steamworks. " + e.Message, e));
            }
        }
    }
}
