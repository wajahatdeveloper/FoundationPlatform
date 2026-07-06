using System.Collections.Generic;
using DebugXLogging;

namespace DebugXLogging
{
    /// <summary>
    /// Fluent builder for log pipeline configuration
    /// </summary>
    public class LogPipelineConfig
    {
        internal List<ILogSink> Sinks { get; } = new List<ILogSink>();
        internal LogLevel MinimumLevel { get; set; } = LogLevel.Debug;
        internal HashSet<string> ExcludedChannels { get; set; }

        public LogPipelineConfig SetMinimumLevel(LogLevel level)
        {
            MinimumLevel = level;
            return this;
        }

        public LogPipelineConfig AddSink(ILogSink sink)
        {
            Sinks.Add(sink);
            return this;
        }

        public LogPipelineConfig ExcludeChannels(params string[] channels)
        {
            if (ExcludedChannels == null)
                ExcludedChannels = new HashSet<string>();
            
            foreach (var channel in channels)
            {
                if (!string.IsNullOrEmpty(channel))
                    ExcludedChannels.Add(channel);
            }
            
            return this;
        }
    }
}

