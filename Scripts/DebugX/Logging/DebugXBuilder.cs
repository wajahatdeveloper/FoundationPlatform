using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using DebugXLogging;

namespace DebugXLogging
{
    /// <summary>
    /// Fluent builder implementation for DebugX structured logging
    /// </summary>
    internal class DebugXBuilder : IDebugXBuilder
    {
        private string _channel;
        private string _sourceContext;
        private List<LogProperty> _properties;
        private UnityEngine.Object _unityContext;

        public IDebugXBuilder WithChannel(LogChannel channel)
        {
            _channel = channel.Name;
            return this;
        }

        public IDebugXBuilder WithSourceContext(string context)
        {
            _sourceContext = context;
            return this;
        }

        public IDebugXBuilder WithProperty(string key, object value)
        {
            _properties ??= new List<LogProperty>();
            _properties.Add(new LogProperty(key, value));
            return this;
        }

        public IDebugXBuilder WithContext(UnityEngine.Object unityObject)
        {
            _unityContext = unityObject;
            return this;
        }

        public void Verbose(string messageTemplate, params object[] propertyValues)
        {
            Write(LogLevel.Verbose, messageTemplate, propertyValues, null);
        }

        public void Debug(string messageTemplate, params object[] propertyValues)
        {
            Write(LogLevel.Debug, messageTemplate, propertyValues, null);
        }

        public void Info(string messageTemplate, params object[] propertyValues)
        {
            Write(LogLevel.Information, messageTemplate, propertyValues, null);
        }

        public void Warning(string messageTemplate, params object[] propertyValues)
        {
            Write(LogLevel.Warning, messageTemplate, propertyValues, null);
        }

        public void Error(string messageTemplate, params object[] propertyValues)
        {
            Write(LogLevel.Error, messageTemplate, propertyValues, null);
        }

        public void Error(System.Exception exception, string messageTemplate, params object[] propertyValues)
        {
            Write(LogLevel.Error, messageTemplate, propertyValues, exception);
        }

        // Global config for stack traces
        // Configure this via DebugXInitializer.Initialize()
        public static bool EnableFullStackTraces = false;

        /// <summary>
        /// When true, console sinks run synchronously on main thread for correct stack traces.
        /// Toggle via Tools/FoundationPlatform/DebugX/Sync Console (Correct Stack Traces). Persisted in EditorPrefs.
        /// </summary>
        public static bool UseSyncConsole = false;

        private void Write(LogLevel level, string messageTemplate, object[] propertyValues,
            System.Exception exception)
        {
            if (!LogPipeline.ShouldEmit(level, _channel))
                return;

            if (level == LogLevel.Error && exception != null && ExplicitErrorDedupe.ShouldSkipErrorLog(exception))
            {
                return;
            }

            var callerInfo = CallerInfoHelper.GetCallerInfo();
            var (renderedMessage, templateProperties) = MessageTemplateParser.Parse(
                messageTemplate, propertyValues);

            // Merge builder properties with template properties
            var allProperties = MergeProperties(templateProperties, _properties);

            // Capture stack trace if error/fatal or explicitly enabled
            string stackTrace = null;
            if (level == LogLevel.Error || level == LogLevel.Fatal || EnableFullStackTraces)
            {
                // Skip frames: 0=GetStackTrace, 1=Write, 2=Debug/Info/etc, 3=Caller
                stackTrace = new StackTrace(3, true).ToString();
            }

            var logEvent = new LogEvent(
                level,
                messageTemplate,
                renderedMessage,
                allProperties,
                _channel,
                _sourceContext,
                callerInfo,
                exception,
                _unityContext,
                stackTrace
            );

            LogPipeline.Emit(logEvent);

            if (level == LogLevel.Error && exception == null)
            {
                ExplicitErrorDedupe.RegisterExplicitFailure(allProperties);
            }
        }

        private LogProperty[] MergeProperties(LogProperty[] template, List<LogProperty> builder)
        {
            if (builder == null || builder.Count == 0) return template;
            if (template == null || template.Length == 0) return builder.ToArray();

            var merged = new LogProperty[template.Length + builder.Count];
            System.Array.Copy(template, merged, template.Length);
            builder.CopyTo(merged, template.Length);
            return merged;
        }
    }
}

