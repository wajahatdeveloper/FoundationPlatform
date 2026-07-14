using System.Collections.Concurrent;
using System.Threading;

namespace AetherNexus.FoundationPlatform.DebugX
{
    internal struct QueuedLog
    {
        public LogEvent Event;
        public bool BackgroundOnly;
    }

    /// <summary>
    /// Thread-safe queue for async log processing. The worker thread sleeps on a semaphore and is
    /// woken per enqueue (no polling). Overflow drops the oldest events but is never silent: a
    /// synthetic warning reporting the drop count is emitted with the next processed batch.
    /// </summary>
    public static class LogQueue
    {
        private static readonly ConcurrentQueue<QueuedLog> _queue = new ConcurrentQueue<QueuedLog>();
        private static readonly ConcurrentQueue<System.Action> _mainThreadActions = new ConcurrentQueue<System.Action>();
        private static readonly SemaphoreSlim _workSignal = new SemaphoreSlim(0);
        private static Thread _workerThread;
        private static volatile bool _isRunning = false;
        private static readonly object _shutdownLock = new object();
        private static readonly int _batchSize = 50;
        private static readonly int _maxQueueSize = 10000;
        private static readonly int _maxMainThreadActions = 5000;
        private static int _approxCount = 0;
        private static int _approxMainThreadCount = 0;
        private static int _droppedCount = 0;

        public static void Start()
        {
            if (_isRunning)
                return;

            lock (_shutdownLock)
            {
                if (_isRunning)
                    return;

                _isRunning = true;
                _workerThread = new Thread(WorkerThreadProc)
                {
                    Name = "DebugX Log Worker",
                    IsBackground = true
                };
                _workerThread.Start();
            }
        }

        public static void Stop()
        {
            if (!_isRunning)
                return;

            lock (_shutdownLock)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                _workSignal.Release(); // wake the worker so it can observe _isRunning and exit

                // Wait for worker thread to finish
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(2000); // Wait up to 2 seconds
                }

                // Process any remaining items synchronously
                ProcessRemainingItems();
            }
        }

        public static void Enqueue(LogEvent logEvent, bool backgroundOnly = false)
        {
            // Prevent queue overflow using an approximate, atomically-tracked size.
            // Drop oldest items until we are back under the cap; the drop is reported via a
            // synthetic warning so data loss is never silent.
            while (Interlocked.CompareExchange(ref _approxCount, 0, 0) >= _maxQueueSize)
            {
                if (_queue.TryDequeue(out _))
                {
                    Interlocked.Decrement(ref _approxCount);
                    Interlocked.Increment(ref _droppedCount);
                }
                else
                {
                    break;
                }
            }

            _queue.Enqueue(new QueuedLog { Event = logEvent, BackgroundOnly = backgroundOnly });
            Interlocked.Increment(ref _approxCount);
            _workSignal.Release();
        }

        public static void EnqueueMainThreadAction(System.Action action)
        {
            // Same drop-oldest cap as the event queue: if the main thread stalls (imports, modal
            // dialogs) this queue must not grow without bound.
            while (Interlocked.CompareExchange(ref _approxMainThreadCount, 0, 0) >= _maxMainThreadActions)
            {
                if (_mainThreadActions.TryDequeue(out _))
                {
                    Interlocked.Decrement(ref _approxMainThreadCount);
                    Interlocked.Increment(ref _droppedCount);
                }
                else
                {
                    break;
                }
            }

            _mainThreadActions.Enqueue(action);
            Interlocked.Increment(ref _approxMainThreadCount);
        }

        public static void ProcessMainThreadActions()
        {
            int processed = 0;
            while (processed < _batchSize && _mainThreadActions.TryDequeue(out var action))
            {
                Interlocked.Decrement(ref _approxMainThreadCount);
                try
                {
                    action?.Invoke();
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[LogQueue] Main thread action failed: {ex.Message}");
                }
                processed++;
            }
        }

        private static void WorkerThreadProc()
        {
            while (_isRunning)
            {
                try
                {
                    // Sleep until an enqueue wakes us (1s timeout so shutdown/drop reporting can't stall).
                    _workSignal.Wait(1000);
                    ProcessBatch();
                    ReportDrops();
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[LogQueue] Worker thread error: {ex.Message}");
                    Thread.Sleep(100); // Longer delay on error
                }
            }
        }

        private static void ProcessBatch()
        {
            int count = 0;
            while (count < _batchSize && _queue.TryDequeue(out var queued))
            {
                Interlocked.Decrement(ref _approxCount);
                LogPipeline.ProcessLogEvent(queued.Event, queued.BackgroundOnly);
                count++;
            }
        }

        /// <summary>Emits one synthetic warning summarizing any events dropped since the last report.</summary>
        private static void ReportDrops()
        {
            int dropped = Interlocked.Exchange(ref _droppedCount, 0);
            if (dropped == 0)
                return;

            var warning = new LogEvent(
                LogLevel.Warning,
                "DebugX: {Count} log events dropped (queue overflow)",
                $"DebugX: {dropped} log events dropped (queue overflow)",
                channel: "DebugX");
            LogPipeline.ProcessLogEvent(warning, backgroundOnly: false);
        }

        private static void ProcessRemainingItems()
        {
            // Process all remaining items synchronously
            while (_queue.TryDequeue(out var queued))
            {
                Interlocked.Decrement(ref _approxCount);
                LogPipeline.ProcessLogEvent(queued.Event, queued.BackgroundOnly);
            }
        }
    }
}
