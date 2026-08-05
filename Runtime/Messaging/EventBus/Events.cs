using UnityEngine;

// ============================================================================
// Core Game Events
// ============================================================================
// These are the actual events used by the game systems.
// To generate a new BaseGameEvent C# class (not a ScriptableObject), use:
// Window → Utilities → Create Event Channel...
// ============================================================================

/// <summary>
/// Published when the game is paused.
/// Used by GameManager.
/// System/UI event - published by services or view roots (see DOMAIN_PACKAGE_SHAPE.md; product types use *Screen).
/// </summary>
namespace AetherNexus.FoundationPlatform.Messaging
{
public class GamePaused : BaseGameEvent
{
    [EventData]
    public float PauseTime { get; set; }
    [EventData]
    public string PauseReason { get; set; }
    [EventData]
    public bool IsManualPause { get; set; }
    
    public GamePaused(float pauseTime, string pauseReason, bool isManualPause)
    {
        PauseTime = pauseTime;
        PauseReason = pauseReason;
        IsManualPause = isManualPause;
    }

    /// <summary>Creates a manual pause event.</summary>
    public GamePaused(float pauseTime, string pauseReason) : this(pauseTime, pauseReason, true) { }

    /// <summary>Creates a manual pause event.</summary>
    public GamePaused(float pauseTime) : this(pauseTime, "Manual", true) { }
}

/// <summary>
/// Published when the game is resumed from pause.
/// Used by GameManager.
/// System/UI event - published by services or view roots (see DOMAIN_PACKAGE_SHAPE.md; product types use *Screen).
/// </summary>
public class GameResumed : BaseGameEvent
{
    [EventData]
    public float ResumeTime { get; set; }
    [EventData]
    public float PauseDuration { get; set; }
    [EventData]
    public string ResumeReason { get; set; }
    
    public GameResumed(float resumeTime, float pauseDuration, string resumeReason)
    {
        ResumeTime = resumeTime;
        PauseDuration = pauseDuration;
        ResumeReason = resumeReason;
    }

    /// <summary>Creates a manual resume event.</summary>
    public GameResumed(float resumeTime, float pauseDuration) : this(resumeTime, pauseDuration, "Manual") { }
}}
