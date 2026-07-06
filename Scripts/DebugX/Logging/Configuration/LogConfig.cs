using System.Collections.Generic;

namespace DebugXLogging
{
    /// <summary>
    /// Central configuration for log filtering - checked BEFORE expensive log processing
    /// </summary>
    public static class LogConfig
    {
        private static readonly HashSet<string> _disabledChannels = new HashSet<string>();
        private static LogLevel _minimumLevel = LogLevel.Debug;

        public static LogLevel MinimumLevel
        {
            get => _minimumLevel;
            set => _minimumLevel = value;
        }

        /// <summary>
        /// Check if logging is enabled for given level
        /// </summary>
        public static bool IsEnabled(LogLevel level) => level >= _minimumLevel;

        /// <summary>
        /// Check if channel is enabled (null channel = always enabled)
        /// </summary>
        public static bool IsChannelEnabled(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return true;
            return !_disabledChannels.Contains(channel);
        }

        /// <summary>
        /// Combined check for level + channel
        /// </summary>
        public static bool IsEnabled(LogLevel level, string channel)
        {
            return level >= _minimumLevel && IsChannelEnabled(channel);
        }

        public static void DisableChannel(string channel)
        {
            if (!string.IsNullOrEmpty(channel))
                _disabledChannels.Add(channel);
        }

        public static void EnableChannel(string channel)
        {
            if (!string.IsNullOrEmpty(channel))
                _disabledChannels.Remove(channel);
        }

        public static void DisableAllChannels(params string[] channels)
        {
            foreach (var channel in channels)
                DisableChannel(channel);
        }

        public static void EnableAllChannels()
        {
            _disabledChannels.Clear();
        }

        public static bool IsChannelDisabled(string channel) => _disabledChannels.Contains(channel);
    }
}
