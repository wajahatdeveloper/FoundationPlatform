using UnityEngine;

// ============================================================================
// Core Game Events
// ============================================================================
// These are the actual events used by the game systems.
// To create EventChannel ScriptableObjects for these, use:
// Tools → EventBus → Generate Event Channel
// ============================================================================

/// <summary>
/// Published when the game is paused.
/// Used by GameManager.
/// System/UI event - published by services or view roots (see DOMAIN_PACKAGE_SHAPE.md; product types use *Screen).
/// </summary>
public class GamePaused : BaseGameEvent
{
    [EventData]
    public float PauseTime { get; set; }
    [EventData]
    public string PauseReason { get; set; }
    [EventData]
    public bool IsManualPause { get; set; }
    
    public GamePaused(float pauseTime, string pauseReason = "Manual", bool isManualPause = true)
    {
        PauseTime = pauseTime;
        PauseReason = pauseReason;
        IsManualPause = isManualPause;
    }
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
    
    public GameResumed(float resumeTime, float pauseDuration, string resumeReason = "Manual")
    {
        ResumeTime = resumeTime;
        PauseDuration = pauseDuration;
        ResumeReason = resumeReason;
    }
}