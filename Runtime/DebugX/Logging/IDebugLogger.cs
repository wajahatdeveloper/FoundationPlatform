namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Instance-based logger with automatic class context injection
    /// </summary>
    public interface IDebugLogger
    {
        /// <summary>
        /// Context identifier for this logger (class/context name)
        /// </summary>
        string Context { get; }

        /// <summary>
        /// Set custom format for this logger instance. Use {0} for context, {1} for message
        /// </summary>
        IDebugLogger SetFormat(string format);

        /// <summary>
        /// Set persistent default channel for this logger instance
        /// </summary>
        IDebugLogger SetDefaultChannel(string channel);

        /// <summary>
        /// Configure channel for next log operation (fluent, overrides default channel)
        /// </summary>
        IDebugLogger WithChannel(LogChannel channel);

        /// <summary>
        /// Add custom property for next log operation (fluent)
        /// </summary>
        IDebugLogger WithProperty(string key, object value);

        /// <summary>
        /// Add Unity context for next log operation (fluent)
        /// </summary>
        IDebugLogger WithContext(UnityEngine.Object unityObject);

        void Verbose(string messageTemplate, params object[] propertyValues);
        void Debug(string messageTemplate, params object[] propertyValues);
        void Info(string messageTemplate, params object[] propertyValues);
        void Warning(string messageTemplate, params object[] propertyValues);
        void Error(string messageTemplate, params object[] propertyValues);
        void Error(System.Exception exception, string messageTemplate, params object[] propertyValues);
    }
}
