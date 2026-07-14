namespace AetherNexus.FoundationPlatform.DebugX
{
    /// <summary>
    /// Fluent builder interface for DebugX structured logging
    /// </summary>
    public interface IDebugXBuilder
    {
        IDebugXBuilder WithChannel(LogChannel channel);
        IDebugXBuilder WithSourceContext(string context);
        IDebugXBuilder WithProperty(string key, object value);
        IDebugXBuilder WithContext(UnityEngine.Object unityObject);

        void Verbose(string messageTemplate, params object[] propertyValues);
        void Debug(string messageTemplate, params object[] propertyValues);
        void Info(string messageTemplate, params object[] propertyValues);
        void Warning(string messageTemplate, params object[] propertyValues);
        void Error(string messageTemplate, params object[] propertyValues);
        void Error(System.Exception exception, string messageTemplate, params object[] propertyValues);
    }
}

