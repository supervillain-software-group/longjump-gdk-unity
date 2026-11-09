using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

namespace LongJump.Editor
{
    /// <summary>
    /// Turns on the GDK's adapter for the Steam library the game has, so <c>Session.SignInWithSteamAsync()</c>
    /// gets tickets by itself. Runs whenever scripts reload. A library is recognized by the members the
    /// adapter calls, not by an upper version limit, so a newer version keeps working. Each adapter
    /// compiles only when its define is on. For a library installed as a package, the adapter's assembly
    /// definition sets it, so removing the package turns it off. For a library copied into the project,
    /// this sets it in Player Settings (Standalone) and turns it off when the library's files are deleted,
    /// logging each change.
    /// </summary>
    [InitializeOnLoad]
    static class SteamLibraries
    {
        sealed class Library
        {
            public string Name;
            public string Define;
            public string AdapterAssembly;
            public Func<Assembly, bool> Matches;
            public Func<Assembly, bool> HasMembers;
            public Func<string, bool> IsLibraryFile;
            public string TooOld;
        }

        static readonly Library[] Libraries =
        {
            new Library
            {
                Name = "Steamworks.NET",
                Define = "LONGJUMP_STEAMWORKS_NET",
                AdapterAssembly = "LongJump.Gdk.SteamworksNet",
                Matches = a => a.GetName().Name == "com.rlabrecque.steamworks.net",
                HasMembers = a =>
                    HasMethod(a, "Steamworks.SteamUser", "GetAuthTicketForWebApi", typeof(string)) &&
                    a.GetType("Steamworks.GetTicketForWebApiResponse_t") != null &&
                    a.GetType("Steamworks.Callback`1") != null,
                IsLibraryFile = path => path.EndsWith("/com.rlabrecque.steamworks.net.asmdef", StringComparison.Ordinal),
                TooOld = "SteamUser.GetAuthTicketForWebApi (Steamworks.NET 20.2.0 and newer)",
            },
            new Library
            {
                Name = "Facepunch.Steamworks",
                Define = "LONGJUMP_FACEPUNCH",
                AdapterAssembly = "LongJump.Gdk.Facepunch",
                Matches = a => a.GetName().Name.StartsWith("Facepunch.Steamworks", StringComparison.Ordinal),
                HasMembers = a =>
                    HasMethod(a, "Steamworks.SteamUser", "GetAuthTicketForWebApiAsync", typeof(string), typeof(double)) &&
                    a.GetType("Steamworks.SteamClient")?.GetProperty("IsValid", BindingFlags.Public | BindingFlags.Static) != null,
                IsLibraryFile = path =>
                {
                    var file = path.Substring(path.LastIndexOf('/') + 1);
                    return file.StartsWith("Facepunch.Steamworks.", StringComparison.Ordinal) && file.EndsWith(".dll", StringComparison.Ordinal);
                },
                TooOld = "SteamUser.GetAuthTicketForWebApiAsync (Facepunch.Steamworks from mid-2023 on)",
            },
        };

        static SteamLibraries()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompiled;
            Check();
        }

        static void Check()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var supported = false;
            foreach (var library in Libraries)
            {
                var found = assemblies.FirstOrDefault(library.Matches);
                var usable = found != null && library.HasMembers(found);
                if (found != null && !usable)
                    LogOnce($"{library.Name} in this project doesn't have {library.TooOld}, so Session.SignInWithSteamAsync() can't use it. Update it, or pass a ticket provider to Session.SignInWithSteamAsync (see the LongJump README).", warning: true);
                // A package turns the adapter on through its assembly definition's version rule instead.
                var package = found != null && UnityEditor.PackageManager.PackageInfo.FindForAssembly(found) != null;
                if (usable && package) LogOnce($"Found the {library.Name} package: Session.SignInWithSteamAsync() gets Steam tickets through it.", warning: false);
                SetDefine(library, usable && !package);
                supported |= usable;
            }

            // Steamworks.NET copied in without its assembly definition compiles into the game's own scripts,
            // which no other assembly can reference.
            var loose = assemblies.FirstOrDefault(a => a.GetName().Name.StartsWith("Assembly-CSharp", StringComparison.Ordinal) && a.GetType("Steamworks.SteamAPI") != null);
            if (loose != null)
                LogOnce($"Steamworks.NET is compiled into {loose.GetName().Name}, without its com.rlabrecque.steamworks.net assembly definition, so LongJump can't use it. Keep its .asmdef file, or install it as a package. Meanwhile, pass a ticket provider to Session.SignInWithSteamAsync.", warning: true);

            if (supported || loose != null) return;
            foreach (var other in assemblies.Where(LooksLikeSteam))
                LogOnce($"{other.GetName().Name} looks like a Steam library, which LongJump has no support for. Pass a ticket provider to Session.SignInWithSteamAsync (see the LongJump README).", warning: true);
        }

        static bool LooksLikeSteam(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            if (name.StartsWith("LongJump", StringComparison.Ordinal) || name.StartsWith("com.rlabrecque.steamworks.net", StringComparison.Ordinal)) return false;
            return name.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool HasMethod(Assembly assembly, string type, string method, params Type[] parameters) =>
            assembly.GetType(type)?.GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, parameters, null) != null;

        /// <summary>Turns a copied-in library's adapter off when its files are deleted, before scripts recompile without it.</summary>
        internal static void OnDeleted(string[] paths)
        {
            foreach (var library in Libraries)
                if (paths.Any(library.IsLibraryFile)) SetDefine(library, false);
        }

        static void SetDefine(Library library, bool on)
        {
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out var defines);
            if (defines.Contains(library.Define) == on) return;
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone,
                on ? defines.Append(library.Define).ToArray() : defines.Where(d => d != library.Define).ToArray());
            AssetDatabase.SaveAssets();
            Debug.Log(on
                ? $"[LongJump] Found {library.Name}: turned on Steam sign-in through it ({library.Define} in Player Settings, Standalone)."
                : $"[LongJump] Removed {library.Define} from Player Settings (Standalone): no copy of {library.Name} is in the project's own files.");
        }

        // When a library is removed, its adapter stops compiling, and scripts don't reload until it does,
        // so Check can't run. This turns the adapter off instead.
        static void OnCompiled(string path, CompilerMessage[] messages)
        {
            var library = Libraries.FirstOrDefault(l => path.EndsWith(l.AdapterAssembly + ".dll", StringComparison.Ordinal));
            if (library == null || !messages.Any(m => m.type == CompilerMessageType.Error)) return;
            Debug.LogWarning($"[LongJump] LongJump's {library.Name} support didn't compile, so it's turned off: {messages.First(m => m.type == CompilerMessageType.Error).message}");
            SetDefine(library, false);
        }

        static void LogOnce(string message, bool warning)
        {
            var key = "LongJump.SteamLibraries." + message.GetHashCode();
            if (SessionState.GetBool(key, false)) return;
            SessionState.SetBool(key, true);
            if (warning) Debug.LogWarning("[LongJump] " + message);
            else Debug.Log("[LongJump] " + message);
        }
    }

    /// <summary>Sees a Steam library's files deleted in the editor, before scripts recompile without them.</summary>
    sealed class SteamLibraryDeletions : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) =>
            SteamLibraries.OnDeleted(deleted.Concat(movedFrom).ToArray());
    }
}
