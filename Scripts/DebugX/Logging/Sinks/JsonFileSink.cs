using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using DebugXLogging;

namespace DebugXLogging
{
    /// <summary>
    /// JSON structured log sink with buffering
    /// </summary>
    public class JsonFileSink : LogSinkBase, IDisposable
    {
        private readonly string _basePath;
        private readonly bool _useArrayFormat;
        private readonly long _maxFileSize;
        private readonly int _bufferThreshold;
        private readonly float _flushIntervalSeconds;
        private StreamWriter _writer;
        private string _currentFilePath;
        private int _fileIndex = 0;
        private bool _isArrayStarted = false;
        private readonly List<string> _pendingWrites = new List<string>();
        private readonly object _bufferLock = new object();
        private DateTime _lastFlushTime;

        public JsonFileSink(string basePath, LogLevel minimumLevel = LogLevel.Debug,
            bool useArrayFormat = false, long maxFileSizeMB = 10, int bufferThreshold = 50, float flushIntervalSeconds = 1f)
        {
            _basePath = basePath;
            MinimumLevel = minimumLevel;
            _useArrayFormat = useArrayFormat;
            _maxFileSize = maxFileSizeMB * 1024 * 1024;
            _bufferThreshold = bufferThreshold;
            _flushIntervalSeconds = flushIntervalSeconds;
            _lastFlushTime = DateTime.Now;
            OpenFile();
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_writer == null) return;

            try
            {
                string json = SerializeToJson(logEvent);
                bool shouldFlush = false;

                lock (_bufferLock)
                {
                    _pendingWrites.Add(json);

                    // Check if we should flush
                    shouldFlush = _pendingWrites.Count >= _bufferThreshold ||
                                  logEvent.Level >= LogLevel.Error ||
                                  (DateTime.Now - _lastFlushTime).TotalSeconds >= _flushIntervalSeconds;
                }

                if (shouldFlush)
                {
                    FlushBuffer();
                }

                // Check file size and roll if needed (after flush).
                // Must hold _bufferLock so RollFile's writer disposal/reopen cannot
                // interleave with FlushBuffer's writes on the background flush thread.
                lock (_bufferLock)
                {
                    if (_writer != null && _writer.BaseStream.Length > _maxFileSize)
                    {
                        RollFile();
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[JsonFileSink] Failed to write log: {ex.Message}");
            }
        }

        public void FlushBuffer()
        {
            if (_writer == null) return;

            lock (_bufferLock)
            {
                if (_pendingWrites.Count == 0)
                    return;

                try
                {
                    foreach (var json in _pendingWrites)
                    {
                        if (_useArrayFormat)
                        {
                            if (!_isArrayStarted)
                            {
                                _writer.Write("[");
                                _isArrayStarted = true;
                            }
                            else
                            {
                                _writer.Write(",");
                            }
                        }

                        _writer.WriteLine(json); // NDJSON format (newline delimited)
                    }
                    _pendingWrites.Clear();
                    _writer.Flush();
                    _lastFlushTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[JsonFileSink] Failed to flush buffer: {ex.Message}");
                }
            }
        }

        private string SerializeToJson(LogEvent logEvent)
        {
            // Simple manual JSON serialization (AOT-safe)
            var sb = new StringBuilder();
            sb.Append("{");

            // Timestamp
            sb.Append($"\"timestamp\":\"{logEvent.Timestamp:O}\",");

            // Level
            sb.Append($"\"level\":\"{logEvent.Level}\",");

            // Message
            sb.Append($"\"message\":{JsonEscape(logEvent.Message)},");

            // Message template
            if (!string.IsNullOrEmpty(logEvent.MessageTemplate))
                sb.Append($"\"messageTemplate\":{JsonEscape(logEvent.MessageTemplate)},");

            // Channel
            if (!string.IsNullOrEmpty(logEvent.Channel))
                sb.Append($"\"channel\":{JsonEscape(logEvent.Channel)},");

            // Source context
            if (!string.IsNullOrEmpty(logEvent.SourceContext))
                sb.Append($"\"sourceContext\":{JsonEscape(logEvent.SourceContext)},");

            // Properties
            if (logEvent.Properties != null && logEvent.Properties.Length > 0)
            {
                sb.Append("\"properties\":{");
                for (int i = 0; i < logEvent.Properties.Length; i++)
                {
                    if (i > 0) sb.Append(",");
                    var prop = logEvent.Properties[i];
                    sb.Append($"\"{prop.Key}\":{JsonValue(prop.Value)}");
                }
                sb.Append("},");
            }

            // Caller info
            if (!logEvent.Caller.IsEmpty)
            {
                sb.Append("\"caller\":{");
                sb.Append($"\"memberName\":{JsonEscape(logEvent.Caller.MemberName)},");
                sb.Append($"\"filePath\":{JsonEscape(logEvent.Caller.FilePath)},");
                sb.Append($"\"lineNumber\":{logEvent.Caller.LineNumber}");
                sb.Append("},");
            }

            // Exception
            if (logEvent.Exception != null)
            {
                sb.Append($"\"exception\":{JsonEscape(logEvent.Exception.ToString())},");
            }

            // Remove trailing comma
            if (sb[sb.Length - 1] == ',')
                sb.Length--;

            sb.Append("}");
            return sb.ToString();
        }

        private string JsonEscape(string value)
        {
            if (value == null) return "null";
            return "\"" + value.Replace("\\", "\\\\")
                               .Replace("\"", "\\\"")
                               .Replace("\n", "\\n")
                               .Replace("\r", "\\r")
                               .Replace("\t", "\\t") + "\"";
        }

        private string JsonValue(object value)
        {
            if (value == null) return "null";
            if (value is string s) return JsonEscape(s);
            if (value is bool b) return b ? "true" : "false";
            if (value is int || value is long || value is float || value is double)
                return value.ToString();

            // Try JsonUtility for Unity-serializable types
            try
            {
                var type = value.GetType();
                if (type.IsDefined(typeof(System.SerializableAttribute), false))
                {
                    string json = JsonUtility.ToJson(value);
                    if (!string.IsNullOrEmpty(json) && json != "{}")
                    {
                        return json;
                    }
                }
            }
            catch
            {
                // Fall through to ToString
            }

            // Fallback: use ToString
            return JsonEscape(value.ToString());
        }

        private void OpenFile()
        {
            try
            {
                int sessionNumber = SessionCounter.GetSessionNumber(_basePath);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string extension = _useArrayFormat ? ".json" : ".ndjson";
                _currentFilePath = Path.Combine(_basePath, $"log-{sessionNumber:D3}-{timestamp}-{_fileIndex:D3}{extension}");

                Directory.CreateDirectory(_basePath);
                // Array format must own the file for a single writer lifecycle: appending to an
                // existing/partial array file (e.g. a crashed run that wrote '[' but never ']')
                // produces unbalanced brackets / invalid JSON. NDJSON tolerates append.
                _writer = new StreamWriter(_currentFilePath, append: !_useArrayFormat);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[JsonFileSink] Failed to open log file: {ex.Message}");
                _writer = null;
            }
        }

        private void RollFile()
        {
            if (_writer != null)
            {
                // Close array format if needed
                if (_useArrayFormat && _isArrayStarted)
                {
                    _writer.Write("]");
                    _writer.Flush();
                }
                _writer.Dispose();
            }
            _fileIndex++;
            _isArrayStarted = false; // Reset for new file
            OpenFile();
        }

        public void Dispose()
        {
            FlushBuffer();

            if (_useArrayFormat && _isArrayStarted && _writer != null)
            {
                _writer.Write("]");
                _writer.Flush();
            }

            _writer?.Dispose();
            _writer = null;
        }
    }
}

