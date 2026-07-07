using System;

/// <summary>
/// Base class for events that require validation
/// </summary>
public abstract class BaseValidatableEvent : BaseGameEvent
{
    public bool IsValidated { get; set; }
    public string ValidationMessage { get; set; }
    
    protected BaseValidatableEvent()
    {
        IsValidated = false;
        ValidationMessage = string.Empty;
    }
}
