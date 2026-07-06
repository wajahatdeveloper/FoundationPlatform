using System;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed class EventProvenance
{
    public ulong EventId;
    public ulong ParentEventId;

    public string PublisherType;
    public string PublisherMethod;

    public string File;
    public int Line;

    public int Frame;
}
#endif

/// <summary>
/// Base class for all game events with timestamp
/// </summary>
public abstract class BaseGameEvent : IIdentity
{
    public DateTime Timestamp { get; }
    
    public virtual Identity Identity { get; set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal EventProvenance Provenance;
#endif
    
    protected BaseGameEvent()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Wall-clock timestamp is for EDITOR/diagnostics only (event history windows).
        // It is intentionally NOT populated in player/simulation builds: DateTime.Now is
        // non-deterministic and must never feed deterministic simulation logic.
        Timestamp = DateTime.Now;
#endif
    }
}
