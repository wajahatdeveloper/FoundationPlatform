using System.Threading;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// MonoBehaviour that dispatches actions to Unity's main thread
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;

        /// <summary>
        /// Managed thread ID of Unity's main thread. Set in Awake. Used for sync console mode.
        /// </summary>
        public static int MainThreadId { get; private set; }

        /// <summary>True when the calling thread is Unity's main thread (false until captured).</summary>
        public static bool IsMainThread =>
            MainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == MainThreadId;

        /// <summary>
        /// Records the current thread as the main thread. Called from main-thread entry points that run
        /// before the dispatcher GameObject exists (editor load, RuntimeInitializeOnLoad).
        /// </summary>
        public static void CaptureMainThread()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private void Awake()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static void EnsureExists()
        {
            if (_instance != null)
                return;

            var go = new GameObject("DebugX MainThreadDispatcher");
            _instance = go.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            // Process queued main thread actions
            LogQueue.ProcessMainThreadActions();
        }

        private void OnApplicationQuit()
        {
            // Ensure all actions are processed before shutdown
            LogQueue.ProcessMainThreadActions();
        }
    }
}

