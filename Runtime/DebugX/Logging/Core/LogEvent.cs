using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Represents a single log event with all metadata
    /// AOT-safe, no reflection required
    /// </summary>
    public readonly struct LogEvent
    {
        public readonly DateTime Timestamp;
        public readonly LogLevel Level;
        public readonly string Message;
        public readonly string MessageTemplate; // For structured logging
        public readonly LogProperty[] Properties;
        public readonly string Channel;
        public readonly string SourceContext; // Class name
        public readonly CallerInfo Caller;
        public readonly Exception Exception;
        public readonly UnityEngine.Object UnityContext;
        public readonly string StackTrace;

        /// <summary>Time.frameCount at log time when created on the main thread, otherwise -1.</summary>
        public readonly int FrameCount;

        public LogEvent(
            LogLevel level,
            string messageTemplate,
            string renderedMessage,
            LogProperty[] properties,
            string channel,
            string sourceContext,
            CallerInfo caller,
            Exception exception,
            UnityEngine.Object unityContext,
            string stackTrace)
        {
            Timestamp = DateTime.Now;
            Level = level;
            MessageTemplate = messageTemplate;
            Message = renderedMessage;
            Properties = properties ?? Array.Empty<LogProperty>();
            Channel = channel;
            SourceContext = sourceContext;
            Caller = caller;
            Exception = exception;
            UnityContext = unityContext;
            StackTrace = stackTrace;
            FrameCount = MainThreadDispatcher.IsMainThread ? Time.frameCount : -1;
        }

        /// <summary>Convenience overload for simple channel-tagged diagnostic events (e.g. queue-overflow warnings).</summary>
        public LogEvent(LogLevel level, string messageTemplate, string renderedMessage, string channel)
            : this(level, messageTemplate, renderedMessage, null, channel, null, default, null, null, null)
        {
        }
    }
}

