using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace DebugXLogging
{
    internal class DebugLogger : IDebugLogger
    {
        private readonly string _context;
        private string _overrideFormat;
        private string _defaultChannel;
        
        private string _tempChannel;
        private List<LogProperty> _tempProperties;
        private UnityEngine.Object _tempUnityContext;

        public string Context => _context;

        internal DebugLogger(string context)
        {
            _context = context;
        }

        public IDebugLogger SetFormat(string format)
        {
            _overrideFormat = format;
            return this;
        }

        private string GetCurrentFormat() => _overrideFormat ?? LoggerFactory.GlobalClassLoggerFormat;

        public IDebugLogger SetDefaultChannel(string channel)
        {
            _defaultChannel = channel;
            return this;
        }

        public IDebugLogger WithChannel(LogChannel channel)
        {
            _tempChannel = channel.Name;
            return this;
        }

        public IDebugLogger WithProperty(string key, object value)
        {
            _tempProperties ??= new List<LogProperty>();
            _tempProperties.Add(new LogProperty(key, value));
            return this;
        }

        public IDebugLogger WithContext(UnityEngine.Object unityObject)
        {
            _tempUnityContext = unityObject;
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

        private void Write(LogLevel level, string messageTemplate, object[] propertyValues, System.Exception exception)
        {
            // Early bail-out before expensive operations
            if (!LogConfig.IsEnabled(level)) return;
            var channel = _tempChannel ?? _defaultChannel;
            if (channel != null && !LogConfig.IsChannelEnabled(channel)) return;

            if (level == LogLevel.Error && exception != null && ExplicitErrorDedupe.ShouldSkipErrorLog(exception))
            {
                return;
            }

            var format = GetCurrentFormat();
            var formattedTemplate = string.Format(format, _context, messageTemplate);
            
            var (renderedMessage, templateProperties) = MessageTemplateParser.Parse(formattedTemplate, propertyValues);

            var allProperties = MergeProperties(templateProperties, _tempProperties);

            string stackTrace = null;
            if (level == LogLevel.Error || level == LogLevel.Fatal || DebugXBuilder.EnableFullStackTraces)
            {
                stackTrace = new StackTrace(3, true).ToString();
            }

            var callerInfo = CallerInfoHelper.GetCallerInfo();
            var logEvent = new LogEvent(
                level,
                formattedTemplate,
                renderedMessage,
                allProperties,
                channel,
                _context,
                callerInfo,
                exception,
                _tempUnityContext,
                stackTrace
            );

            LogPipeline.Emit(logEvent);

            if (level == LogLevel.Error && exception == null)
            {
                ExplicitErrorDedupe.RegisterExplicitFailure(allProperties);
            }

            ClearTemporaryState();
        }

        private void ClearTemporaryState()
        {
            _tempChannel = null;
            _tempProperties?.Clear();
            _tempUnityContext = null;
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
