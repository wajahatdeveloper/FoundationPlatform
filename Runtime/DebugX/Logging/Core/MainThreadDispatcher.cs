using System.Threading;

namespace AetherNexus.FoundationPlatform.Logging
{
    /// <summary>
    /// Main-thread identity for logging. Queued main-thread log actions are drained by the
    /// <see cref="PlatformHost"/> player-loop entry each frame and once more on quit.
    /// </summary>
    public static class MainThreadDispatcher
    {
        /// <summary>
        /// Managed thread ID of Unity's main thread. Used for sync console mode.
        /// </summary>
        public static int MainThreadId { get; private set; }

        /// <summary>True when the calling thread is Unity's main thread (false until captured).</summary>
        public static bool IsMainThread =>
            MainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == MainThreadId;

        /// <summary>
        /// Records the current thread as the main thread. Called from main-thread entry points
        /// (editor load, RuntimeInitializeOnLoad, pipeline start).
        /// </summary>
        public static void CaptureMainThread()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>Kept for the pipeline start path; draining is owned by <see cref="PlatformHost"/>.</summary>
        public static void EnsureExists()
        {
            CaptureMainThread();
        }
    }
}
