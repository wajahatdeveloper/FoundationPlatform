using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DebugXLogging
{
    /// <summary>
    /// Manages persistent session counter for log file naming
    /// </summary>
    public static class SessionCounter
    {
        private static readonly object _lock = new object();
        private static readonly string _counterFileName = "session.counter";

        // Cache the resolved session number per directory for the process lifetime so that
        // multiple sinks pointing at the same directory (e.g. FileSink + JsonFileSink) agree
        // on the session number and do not each increment the persisted counter.
        private static readonly Dictionary<string, int> _resolvedSessions = new Dictionary<string, int>();

        /// <summary>
        /// Gets the current session number for the given log directory.
        /// If the counter file doesn't exist, creates it with value 1.
        /// If it exists, reads the value, increments it, and writes it back.
        /// </summary>
        /// <param name="logDirectory">The log directory path</param>
        /// <returns>The session number (1-based)</returns>
        public static int GetSessionNumber(string logDirectory)
        {
            lock (_lock)
            {
                // Normalize the key so equivalent directory paths resolve to the same cached entry.
                string cacheKey;
                try
                {
                    cacheKey = Path.GetFullPath(logDirectory);
                }
                catch
                {
                    cacheKey = logDirectory ?? string.Empty;
                }

                if (_resolvedSessions.TryGetValue(cacheKey, out int cached))
                {
                    return cached;
                }

                try
                {
                    string counterFilePath = Path.Combine(logDirectory, _counterFileName);
                    int sessionNumber = 1;

                    // Ensure directory exists
                    Directory.CreateDirectory(logDirectory);

                    // Read existing counter or create new one
                    if (File.Exists(counterFilePath))
                    {
                        string content = File.ReadAllText(counterFilePath).Trim();
                        if (int.TryParse(content, out int existingValue) && existingValue > 0)
                        {
                            sessionNumber = existingValue + 1;
                        }
                    }

                    // Write incremented value back
                    File.WriteAllText(counterFilePath, sessionNumber.ToString());

                    _resolvedSessions[cacheKey] = sessionNumber;
                    return sessionNumber;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[SessionCounter] Failed to get session number: {ex.Message}");
                    // Return 1 as fallback
                    return 1;
                }
            }
        }
    }
}

