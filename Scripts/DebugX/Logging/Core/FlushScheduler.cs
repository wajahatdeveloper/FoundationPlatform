using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using DebugXLogging;

namespace DebugXLogging
{
    /// <summary>
    /// Periodically flushes file sinks on a background thread so the main thread is not blocked.
    /// </summary>
    public class FlushScheduler : MonoBehaviour
    {
        private static FlushScheduler _instance;
        private static readonly List<FileSink> _fileSinks = new List<FileSink>();
        private static readonly List<JsonFileSink> _jsonFileSinks = new List<JsonFileSink>();
        private static readonly object _sinksLock = new object();
        private static volatile bool _running = true;
        private static Thread _flushThread;
        private const int FlushIntervalMs = 1000;

        public static void EnsureExists()
        {
            if (_instance != null)
                return;

            var go = new GameObject("DebugX FlushScheduler");
            _instance = go.AddComponent<FlushScheduler>();
            DontDestroyOnLoad(go);

            _running = true;
            _flushThread = new Thread(FlushThreadProc)
            {
                Name = "DebugX Flush",
                IsBackground = true
            };
            _flushThread.Start();
        }

        public static void RegisterFileSink(FileSink sink)
        {
            lock (_sinksLock)
            {
                if (sink != null && !_fileSinks.Contains(sink))
                {
                    _fileSinks.Add(sink);
                }
            }
        }

        public static void RegisterJsonFileSink(JsonFileSink sink)
        {
            lock (_sinksLock)
            {
                if (sink != null && !_jsonFileSinks.Contains(sink))
                {
                    _jsonFileSinks.Add(sink);
                }
            }
        }

        private static void FlushThreadProc()
        {
            while (_running)
            {
                Thread.Sleep(FlushIntervalMs);
                if (!_running) break;
                try
                {
                    FlushAllSinksStatic();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[FlushScheduler] Flush failed: {ex.Message}");
                }
            }
        }

        private void OnApplicationQuit()
        {
            _running = false;
            FlushAllSinksStatic();
        }

        private static void FlushAllSinksStatic()
        {
            lock (_sinksLock)
            {
                foreach (var sink in _fileSinks)
                {
                    try
                    {
                        sink.FlushBuffer();
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[FlushScheduler] Failed to flush FileSink: {ex.Message}");
                    }
                }

                foreach (var sink in _jsonFileSinks)
                {
                    try
                    {
                        sink.FlushBuffer();
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[FlushScheduler] Failed to flush JsonFileSink: {ex.Message}");
                    }
                }
            }
        }
    }
}

