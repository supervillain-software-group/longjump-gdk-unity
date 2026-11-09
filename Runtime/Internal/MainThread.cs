using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace LongJump
{
    /// <summary>
    /// An object named "LongJump", kept across scene loads, that runs work on Unity's main thread.
    /// UnityWebRequest and screen capture only work there, but crashes can be logged and real-time
    /// messages arrive on any thread. Each frame it also renews the session when due and hands
    /// real-time messages to the game.
    /// </summary>
    internal class MainThread : MonoBehaviour
    {
        static MainThread instance;
        static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        static int mainThreadId = -1;

        internal static bool IsCurrent => System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;

        /// <summary>Call from the main thread.</summary>
        internal static void Ensure()
        {
            mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (instance != null) return;

            var go = new GameObject("LongJump");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MainThread>();
        }

        /// <summary>When Play starts: drops the last Play session's object and queued work.</summary>
        internal static void ResetForPlay()
        {
            if (instance != null) DestroyImmediate(instance.gameObject);
            instance = null;
            while (queue.TryDequeue(out _)) { }
        }

        /// <summary>Runs the action on the main thread during the next Update. Safe from any thread.</summary>
        internal static void Post(Action action) => queue.Enqueue(action);

        internal static Coroutine Run(IEnumerator routine) => instance != null ? instance.StartCoroutine(routine) : null;

        void Update()
        {
            while (queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    // A warning, not an exception, so the crash handler doesn't report it.
                    Log.Warning(e.ToString());
                }
            }

            try
            {
                Session.Tick();
                Events.Pump();
            }
            catch (Exception e)
            {
                Log.Warning(e.ToString());
            }
        }

        void OnApplicationQuit()
        {
            Events.Stop();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
