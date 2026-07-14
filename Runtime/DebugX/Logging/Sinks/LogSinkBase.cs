using System.Collections.Generic;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Base class with common filtering logic
    /// </summary>
    public abstract class LogSinkBase : ILogSink
    {
        protected LogLevel MinimumLevel { get; set; } = LogLevel.Debug;
        protected HashSet<string> ExcludedChannels { get; set; }

        public virtual bool RequiresMainThread => false;

        /// <summary>
        /// Set channel filters for this sink (excluded only; all channels allowed by default)
        /// </summary>
        public void SetChannelFilters(HashSet<string> excludedChannels)
        {
            ExcludedChannels = excludedChannels != null ? new HashSet<string>(excludedChannels) : null;
        }

        public virtual bool ShouldEmit(LogEvent logEvent)
        {
            // Level filter
            if (logEvent.Level < MinimumLevel)
                return false;

            // Channel filter: exclude only
            if (!string.IsNullOrEmpty(logEvent.Channel) && ExcludedChannels != null && ExcludedChannels.Contains(logEvent.Channel))
                return false;

            return true;
        }

        public abstract void Emit(LogEvent logEvent);
    }
}

