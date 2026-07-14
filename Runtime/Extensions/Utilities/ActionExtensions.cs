using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public class OnceAction
{
    private Action action;
    private bool invoked;

    public OnceAction(Action action)
    {
        this.action = action;
        this.invoked = false;
    }

    public void Invoke()
    {
        if (!invoked)
        {
            invoked = true;
            action?.Invoke();
            RemoveListener();
        }
    }

    private void RemoveListener()
    {
        action = null;
    }
}

public static class ActionExtensions
{
    public static OnceAction Once(this Action action)
    {
        return new OnceAction(action);
    }
}
}

