using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Helper for adaptively detecting caller information by skipping internal logging methods.
    /// Uses caching to optimize performance by avoiding repeated StackTrace analysis.
    /// </summary>
    public static class CallerInfoHelper
    {
        private const int MaxCacheSize = 100;
        private static readonly ConcurrentDictionary<string, int> _skipCountCache = new ConcurrentDictionary<string, int>();
        private static readonly object _cacheLock = new object();
        private static int _cacheHits = 0;
        private static int _cacheMisses = 0;

        // Logging pipeline namespace — every frame under this prefix is skipped when resolving the call site.
        private const string DebugXNamespace = "AetherNexus.FoundationPlatform.DebugX";
        private const string DebugXPathMarker = "/DebugX/";

        private static bool _skipMethodsContainingLog = false;

        /// <summary>
        /// If true, methods whose names contain 'Log' (case-insensitive) will be skipped as internal methods.
        /// Useful for skipping wrapper methods like LogIfEnabled, LogDebug, etc.
        /// Configure this via DebugXInitializer.Initialize().
        /// Changing this value clears the cache to ensure fresh analysis.
        /// </summary>
        public static bool SkipMethodsContainingLog
        {
            get => _skipMethodsContainingLog;
            set
            {
                if (_skipMethodsContainingLog != value)
                {
                    _skipMethodsContainingLog = value;
                    // Clear cache when toggle changes to ensure fresh analysis
                    lock (_cacheLock)
                    {
                        _skipCountCache.Clear();
                        _cacheHits = 0;
                        _cacheMisses = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Gets caller information by adaptively skipping internal logging methods.
        /// Uses cached skip counts for performance.
        /// </summary>
        public static CallerInfo GetCallerInfo()
        {
            try
            {
                // Get the calling method that invoked GetCallerInfo
                // Frame 0 = GetCallerInfo, Frame 1 = calling method (Info/Debug/etc), Frame 2+ = actual caller
                var callingMethod = GetCallingMethod();
                if (callingMethod == null)
                {
                    return default;
                }

                string cacheKey = GetCacheKey(callingMethod);
                
                // Try cache first
                if (_skipCountCache.TryGetValue(cacheKey, out int cachedSkipCount))
                {
                    _cacheHits++;
                    return GetCallerInfoWithSkip(cachedSkipCount);
                }

                // Cache miss - perform adaptive analysis
                _cacheMisses++;
                int skipCount = FindOptimalSkipCount(callingMethod);
                
                // Cache the result (with size limit)
                CacheSkipCount(cacheKey, skipCount);
                
                return GetCallerInfoWithSkip(skipCount);
            }
            catch
            {
                // Graceful degradation - return empty if StackTrace unavailable
                return default;
            }
        }

        private static MethodBase GetCallingMethod()
        {
            var stackTrace = new StackTrace(skipFrames: 1, fNeedFileInfo: false);
            var frame = stackTrace.GetFrame(0);
            return frame?.GetMethod();
        }

        private static string GetCacheKey(MethodBase method)
        {
            if (method == null) return "";
            
            var declaringType = method.DeclaringType;
            string ns = declaringType?.Namespace ?? "";
            string typeName = declaringType?.Name ?? "";
            string methodName = method.Name ?? "";
            
            return $"{ns}.{typeName}.{methodName}";
        }

        private static int FindOptimalSkipCount(MethodBase callingMethod)
        {
            // StackTrace is taken inside FindOptimalSkipCount, so frame 0 is this method.
            // The returned skip is passed to GetCallerInfoWithSkip (same depth: +0 vs GetCallerInfoWithSkip).
            int maxFrames = 20;
            var stackTrace = new StackTrace(fNeedFileInfo: false);

            for (int skip = 0; skip < maxFrames && skip < stackTrace.FrameCount; skip++)
            {
                var frame = stackTrace.GetFrame(skip);
                if (frame == null) break;

                var method = frame.GetMethod();
                if (method == null) continue;

                if (!IsInternalMethod(method))
                    return skip;
            }

            // Conservative: FindOptimalSkipCount + GetCallerInfo + Write/EmitCore + Info/Error + caller
            return 4;
        }

        private static bool IsInternalMethod(MethodBase method)
        {
            if (method == null) return false;

            var declaringType = method.DeclaringType;
            if (declaringType == null) return false;

            string methodName = method.Name ?? "";
            string ns = declaringType.Namespace ?? "";

            // Entire DebugX logging pipeline (GetCallerInfo, WriteStructured, Logger.Info, etc.)
            if (ns == DebugXNamespace || ns.StartsWith(DebugXNamespace + ".", StringComparison.Ordinal))
                return true;

            if (SkipMethodsContainingLog && methodName.IndexOf("Log", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static bool IsDebugXInternalPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.Replace('\\', '/').IndexOf(DebugXPathMarker, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static CallerInfo GetCallerInfoWithSkip(int skipCount)
        {
            try
            {
                var stackTrace = new StackTrace(skipFrames: skipCount, fNeedFileInfo: true);
                var frame = stackTrace.GetFrame(0);
                
                if (frame != null)
                {
                    var method = frame.GetMethod();
                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();

                    // Extract actual method name from async state machines
                    string methodName = ExtractAsyncMethodName(method);

#if UNITY_EDITOR
                    // System.Diagnostics returns no file info for some assemblies (frames render as
                    // "<GUID>:0"), or may land on DebugX internals when skip is wrong. Fall back to
                    // Unity's extractor for the first non-DebugX (at Assets/..:N) frame.
                    if (string.IsNullOrEmpty(fileName) || lineNumber <= 0 || IsDebugXInternalPath(fileName))
                    {
                        var (uFile, uLine) = TryUnityCaller();
                        if (!string.IsNullOrEmpty(uFile))
                        {
                            fileName = uFile;
                            lineNumber = uLine;
                        }
                    }
#endif

                    return new CallerInfo(
                        methodName,
                        fileName ?? "",
                        lineNumber > 0 ? lineNumber : 0
                    );
                }
            }
            catch
            {
                // Graceful degradation
            }
            
            return default;
        }

        /// <summary>
        /// Extracts the actual method name from async/await state machines.
        /// When method is MoveNext, parses the state machine type name (ClassName+<MethodName>d__X) to get the real method name.
        /// </summary>
        private static string ExtractAsyncMethodName(MethodBase method)
        {
            if (method == null) return "";
            
            string methodName = method.Name ?? "";
            
            // Check if this is a MoveNext method (async state machine)
            if (methodName != "MoveNext")
            {
                return methodName;
            }
            
            // Try to extract method name from state machine type name
            // Pattern: ClassName+<MethodName>d__X where X is a number
            var declaringType = method.DeclaringType;
            if (declaringType != null)
            {
                // Use ToString() to get full nested type name including parent class
                // For nested types, Name only gives the nested part, but ToString() gives the full name
                // Example: "SceneLifecycle.SceneInitializationCoordinator+<Start>d__42"
                string typeName = declaringType.ToString();
                
                // Remove namespace if present (everything before the last dot before the +)
                // We want just the class name part: "SceneInitializationCoordinator+<Start>d__42"
                int plusIndex = typeName.IndexOf('+');
                if (plusIndex >= 0)
                {
                    // Find the last dot before the plus sign
                    string beforePlus = typeName.Substring(0, plusIndex);
                    int lastDot = beforePlus.LastIndexOf('.');
                    if (lastDot >= 0)
                    {
                        typeName = typeName.Substring(lastDot + 1);
                    }
                }
                
                // Match pattern: ClassName+<MethodName>d__X
                // Example: SceneInitializationCoordinator+<Start>d__42
                var match = Regex.Match(typeName, @"^(.+)\+<(.+)>d__\d+$");
                if (match.Success && match.Groups.Count >= 3)
                {
                    string extractedMethodName = match.Groups[2].Value;
                    if (!string.IsNullOrEmpty(extractedMethodName))
                    {
                        return extractedMethodName;
                    }
                }
            }
            
            // Fallback: return original method name if extraction failed
            return methodName;
        }

#if UNITY_EDITOR
        private static readonly Regex UnityAtFrame = new Regex(@"\(at (.+\.cs):(\d+)\)", RegexOptions.Compiled);

        /// <summary>
        /// Resolves the first non-DebugX caller frame from Unity's stack extractor. Editor-only fallback
        /// for assemblies where System.Diagnostics provides no file/line.
        /// </summary>
        private static (string file, int line) TryUnityCaller()
        {
            try
            {
                string stack = UnityEngine.StackTraceUtility.ExtractStackTrace();
                if (string.IsNullOrEmpty(stack)) return (null, 0);

                var lines = stack.Split('\n');
                foreach (var line in lines)
                {
                    string trimmed = line.TrimStart();

                    if (trimmed.StartsWith("AetherNexus.FoundationPlatform.DebugX.", StringComparison.Ordinal)) continue;
                    if (trimmed.StartsWith("at AetherNexus.FoundationPlatform.DebugX.", StringComparison.Ordinal)) continue;
                    if (trimmed.StartsWith("FoundationPlatform.DebugX.", StringComparison.Ordinal)) continue;
                    if (trimmed.StartsWith("DebugX", StringComparison.Ordinal)) continue;
                    if (trimmed.StartsWith("UnityEngine.StackTraceUtility", StringComparison.Ordinal)) continue;
                    if (trimmed.StartsWith("UnityEngine.Debug:ExtractStackTrace", StringComparison.Ordinal)) continue;

                    var m = UnityAtFrame.Match(line);
                    if (!m.Success || !int.TryParse(m.Groups[2].Value, out int ln))
                        continue;

                    string file = m.Groups[1].Value;
                    if (IsDebugXInternalPath(file))
                        continue;

                    return (file, ln);
                }
            }
            catch { /* best effort */ }
            return (null, 0);
        }
#endif

        private static void CacheSkipCount(string key, int skipCount)
        {
            if (string.IsNullOrEmpty(key)) return;
            
            // Simple size limit: clear cache if it gets too large
            // This is a simple approach - could use LRU but this is sufficient for most cases
            lock (_cacheLock)
            {
                if (_skipCountCache.Count >= MaxCacheSize)
                {
                    _skipCountCache.Clear();
                    _cacheHits = 0;
                    _cacheMisses = 0;
                }
                
                _skipCountCache.TryAdd(key, skipCount);
            }
        }
    }
}

