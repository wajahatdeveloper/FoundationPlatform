using System;
using System.Collections.Generic;

namespace DebugXLogging
{
    internal static class LoggerFactory
    {
        private static readonly Dictionary<string, IDebugLogger> _loggerCache = new Dictionary<string, IDebugLogger>();
        
        internal static string GlobalClassLoggerFormat = "[{0}] {1}";

        internal static IDebugLogger GetOrCreateLogger(string context)
        {
            if (!_loggerCache.TryGetValue(context, out var logger))
            {
                logger = new DebugLogger(context);
                _loggerCache[context] = logger;
            }
            return logger;
        }

        internal static IDebugLogger GetOrCreateLogger(Type type)
        {
            return GetOrCreateLogger(type.Name);
        }

        internal static void SetGlobalFormat(string format)
        {
            GlobalClassLoggerFormat = format;
        }
    }
}
