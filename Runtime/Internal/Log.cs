using System;
using UnityEngine;

namespace LongJump
{
    /// <summary>
    /// The GDK's logging. Only warnings, prefixed [LongJump]: crash capture listens to errors and
    /// exceptions in Unity's log and skips these lines, so the GDK never reports itself.
    /// </summary>
    internal static class Log
    {
        internal const string Prefix = "[LongJump]";

        internal static void Warning(string message) => Debug.LogWarning($"{Prefix} {message}");

        /// <summary>Calls a game's handler, so that one throwing never stops the GDK or other handlers.</summary>
        internal static void Invoke(Action handler, string what)
        {
            if (handler == null) return;
            foreach (Action single in handler.GetInvocationList())
            {
                try
                {
                    single();
                }
                catch (Exception e)
                {
                    Warning($"A {what} handler threw: {e}");
                }
            }
        }

        internal static void Invoke<T>(Action<T> handler, T value, string what)
        {
            if (handler == null) return;
            foreach (Action<T> single in handler.GetInvocationList())
            {
                try
                {
                    single(value);
                }
                catch (Exception e)
                {
                    Warning($"A {what} handler threw: {e}");
                }
            }
        }
    }
}
