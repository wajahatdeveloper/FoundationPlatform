using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Simple rolling file sink with buffering
    /// </summary>
    public class FileSink : LogSinkBase, IDisposable
    {
        private readonly string _basePath;
        private readonly long _maxFileSize;
        private readonly int _bufferThreshold;
        private readonly float _flushIntervalSeconds;
        private StreamWriter _writer;
        private string _currentFilePath;
        private int _fileIndex = 0;
        private readonly List<string> _pendingWrites = new List<string>();
        private readonly object _bufferLock = new object();
        private DateTime _lastFlushTime;

        public FileSink(string basePath, LogLevel minimumLevel,
            long maxFileSizeMB, int bufferThreshold, float flushIntervalSeconds)
        {
            _basePath = basePath;
            MinimumLevel = minimumLevel;
            _maxFileSize = maxFileSizeMB * 1024 * 1024;
            _bufferThreshold = bufferThreshold;
            _flushIntervalSeconds = flushIntervalSeconds;
            _lastFlushTime = DateTime.Now;
            OpenNewFile();
        }

        public FileSink(string basePath, LogLevel minimumLevel)
            : this(basePath, minimumLevel, maxFileSizeMB: 10, bufferThreshold: 50, flushIntervalSeconds: 1f)
        {
        }

        public override void Emit(LogEvent logEvent)
        {
            if (_writer == null) return;

            try
            {
                string line = FormatLine(logEvent);
                bool shouldFlush = false;

                lock (_bufferLock)
                {
                    _pendingWrites.Add(line);

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
                // Serialize under _bufferLock so the flush thread cannot be mid-WriteLine
                // on a StreamWriter that RollFile is disposing/reopening.
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
                UnityEngine.Debug.LogError($"[FileSink] Failed to write log: {ex.Message}");
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
                    foreach (var line in _pendingWrites)
                    {
                        _writer.WriteLine(line);
                    }
                    _pendingWrites.Clear();
                    _writer.Flush();
                    _lastFlushTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[FileSink] Failed to flush buffer: {ex.Message}");
                }
            }
        }

        private string FormatLine(LogEvent logEvent)
        {
            var sb = new StringBuilder();
            sb.Append($"{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff} ");
            sb.Append($"[{logEvent.Level}] ");
            sb.Append($"[{logEvent.Channel ?? "Default"}] ");
            if (!string.IsNullOrEmpty(logEvent.SourceContext))
                sb.Append($"[{logEvent.SourceContext}] ");
            sb.Append(logEvent.Message);

            if (logEvent.Exception != null)
            {
                sb.Append($"\n{logEvent.Exception}");
            }

            if (!string.IsNullOrEmpty(logEvent.StackTrace))
            {
                sb.Append($"\n{logEvent.StackTrace}");
            }

            return sb.ToString();
        }

        private void OpenNewFile()
        {
            try
            {
                int sessionNumber = SessionCounter.GetSessionNumber(_basePath);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                _currentFilePath = Path.Combine(_basePath, $"log-{sessionNumber:D3}-{timestamp}-{_fileIndex:D3}.txt");

                Directory.CreateDirectory(_basePath);

                _writer = new StreamWriter(_currentFilePath, append: true);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[FileSink] Failed to open log file: {ex.Message}");
                _writer = null;
            }
        }

        private void RollFile()
        {
            _writer?.Dispose();
            _fileIndex++;
            OpenNewFile();
        }

        public void Dispose()
        {
            FlushBuffer();
            _writer?.Dispose();
            _writer = null;
        }
    }
}

