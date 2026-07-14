using System;
using System.Runtime.CompilerServices;

namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Zero-allocation logger for a channel. Check ShouldEmit first; no heap alloc when filtered.
    /// </summary>
    public readonly struct DebugXLogger
    {
        private readonly string _channel;

        internal DebugXLogger(LogChannel channel)
        {
            _channel = channel.Name;
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitCore(LogLevel level, string messageTemplate, object[] propertyValues, Exception exception)
        {
            if (level == LogLevel.Error && exception != null && ExplicitErrorDedupe.ShouldSkipErrorLog(exception))
            {
                return;
            }

            var callerInfo = CallerInfoHelper.GetCallerInfo();
            var (renderedMessage, templateProperties) = MessageTemplateParser.Parse(messageTemplate, propertyValues);

            string stackTrace = null;
            if (level == LogLevel.Error || level == LogLevel.Fatal || DebugXBuilder.EnableFullStackTraces)
                stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();

            var logEvent = new LogEvent(
                level,
                messageTemplate,
                renderedMessage,
                templateProperties,
                _channel,
                null,
                callerInfo,
                exception,
                null,
                stackTrace
            );
            LogPipeline.Emit(logEvent);

            if (level == LogLevel.Error && exception == null)
            {
                ExplicitErrorDedupe.RegisterExplicitFailure(templateProperties);
            }
        }

        #region Verbose

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose(string messageTemplate)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Verbose, _channel)) return;
            EmitCore(LogLevel.Verbose, messageTemplate, null, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose<T1>(string messageTemplate, T1 arg1)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Verbose, _channel)) return;
            EmitCore(LogLevel.Verbose, messageTemplate, new object[] { arg1 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose<T1, T2>(string messageTemplate, T1 arg1, T2 arg2)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Verbose, _channel)) return;
            EmitCore(LogLevel.Verbose, messageTemplate, new object[] { arg1, arg2 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Verbose, _channel)) return;
            EmitCore(LogLevel.Verbose, messageTemplate, new object[] { arg1, arg2, arg3 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Verbose, _channel)) return;
            EmitCore(LogLevel.Verbose, messageTemplate, new object[] { arg1, arg2, arg3, arg4 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Verbose<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Verbose, _channel)) return;
            EmitCore(LogLevel.Verbose, messageTemplate, new object[] { arg1, arg2, arg3, arg4, arg5 }, null);
        }

        [UnityEngine.HideInCallstack]
        public void Verbose(string messageTemplate, params object[] propertyValues)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Verbose, _channel)) return;
            EmitCore(LogLevel.Verbose, messageTemplate, propertyValues, null);
        }

        #endregion

        #region Debug

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string messageTemplate)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Debug, _channel)) return;
            EmitCore(LogLevel.Debug, messageTemplate, null, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T1>(string messageTemplate, T1 arg1)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Debug, _channel)) return;
            EmitCore(LogLevel.Debug, messageTemplate, new object[] { arg1 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T1, T2>(string messageTemplate, T1 arg1, T2 arg2)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Debug, _channel)) return;
            EmitCore(LogLevel.Debug, messageTemplate, new object[] { arg1, arg2 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Debug, _channel)) return;
            EmitCore(LogLevel.Debug, messageTemplate, new object[] { arg1, arg2, arg3 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Debug, _channel)) return;
            EmitCore(LogLevel.Debug, messageTemplate, new object[] { arg1, arg2, arg3, arg4 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Debug, _channel)) return;
            EmitCore(LogLevel.Debug, messageTemplate, new object[] { arg1, arg2, arg3, arg4, arg5 }, null);
        }

        [UnityEngine.HideInCallstack]
        public void Debug(string messageTemplate, params object[] propertyValues)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Debug, _channel)) return;
            EmitCore(LogLevel.Debug, messageTemplate, propertyValues, null);
        }

        #endregion

        #region Info

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string messageTemplate)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, _channel)) return;
            EmitCore(LogLevel.Information, messageTemplate, null, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info<T1>(string messageTemplate, T1 arg1)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, _channel)) return;
            EmitCore(LogLevel.Information, messageTemplate, new object[] { arg1 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info<T1, T2>(string messageTemplate, T1 arg1, T2 arg2)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, _channel)) return;
            EmitCore(LogLevel.Information, messageTemplate, new object[] { arg1, arg2 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, _channel)) return;
            EmitCore(LogLevel.Information, messageTemplate, new object[] { arg1, arg2, arg3 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, _channel)) return;
            EmitCore(LogLevel.Information, messageTemplate, new object[] { arg1, arg2, arg3, arg4 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, _channel)) return;
            EmitCore(LogLevel.Information, messageTemplate, new object[] { arg1, arg2, arg3, arg4, arg5 }, null);
        }

        [UnityEngine.HideInCallstack]
        public void Info(string messageTemplate, params object[] propertyValues)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Information, _channel)) return;
            EmitCore(LogLevel.Information, messageTemplate, propertyValues, null);
        }

        #endregion

        #region Warning

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning(string messageTemplate)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Warning, _channel)) return;
            EmitCore(LogLevel.Warning, messageTemplate, null, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning<T1>(string messageTemplate, T1 arg1)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Warning, _channel)) return;
            EmitCore(LogLevel.Warning, messageTemplate, new object[] { arg1 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning<T1, T2>(string messageTemplate, T1 arg1, T2 arg2)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Warning, _channel)) return;
            EmitCore(LogLevel.Warning, messageTemplate, new object[] { arg1, arg2 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Warning, _channel)) return;
            EmitCore(LogLevel.Warning, messageTemplate, new object[] { arg1, arg2, arg3 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Warning, _channel)) return;
            EmitCore(LogLevel.Warning, messageTemplate, new object[] { arg1, arg2, arg3, arg4 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warning<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Warning, _channel)) return;
            EmitCore(LogLevel.Warning, messageTemplate, new object[] { arg1, arg2, arg3, arg4, arg5 }, null);
        }

        [UnityEngine.HideInCallstack]
        public void Warning(string messageTemplate, params object[] propertyValues)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Warning, _channel)) return;
            EmitCore(LogLevel.Warning, messageTemplate, propertyValues, null);
        }

        #endregion

        #region Error

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string messageTemplate)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, null, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1>(string messageTemplate, T1 arg1)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2>(string messageTemplate, T1 arg1, T2 arg2)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2, arg3 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2, arg3, arg4 }, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2, arg3, arg4, arg5 }, null);
        }

        [UnityEngine.HideInCallstack]
        public void Error(string messageTemplate, params object[] propertyValues)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, propertyValues, null);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(Exception exception, string messageTemplate)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, null, exception);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1>(Exception exception, string messageTemplate, T1 arg1)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1 }, exception);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2>(Exception exception, string messageTemplate, T1 arg1, T2 arg2)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2 }, exception);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2, T3>(Exception exception, string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2, arg3 }, exception);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2, T3, T4>(Exception exception, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2, arg3, arg4 }, exception);
        }

        [UnityEngine.HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error<T1, T2, T3, T4, T5>(Exception exception, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, new object[] { arg1, arg2, arg3, arg4, arg5 }, exception);
        }

        [UnityEngine.HideInCallstack]
        public void Error(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            if (!LogPipeline.ShouldEmit(LogLevel.Error, _channel)) return;
            EmitCore(LogLevel.Error, messageTemplate, propertyValues, exception);
        }

        #endregion

        #region Watch

        /// <summary>
        /// Tracks a variable in the DebugX Console's Watch panel (one live row per name, updated in
        /// place). Safe to call every frame; editor-only, no-op in builds. Call on the main thread.
        /// </summary>
        [UnityEngine.HideInCallstack]
        public void Watch(string name, object value)
        {
#if UNITY_EDITOR
            ConsoleLogStore.SetWatch(name, value != null ? value.ToString() : "null");
#endif
        }

        #endregion
    }
}
