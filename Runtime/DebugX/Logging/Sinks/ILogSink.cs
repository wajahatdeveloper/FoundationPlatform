namespace AetherNexus.FoundationPlatform.Logging
{
    /// <summary>
    /// Base interface for all log sinks
    /// </summary>
    public interface ILogSink
    {
        bool RequiresMainThread { get; }
        bool ShouldEmit(LogEvent logEvent);
        void Emit(LogEvent logEvent);
    }
}

