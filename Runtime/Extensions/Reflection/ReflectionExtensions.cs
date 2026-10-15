// Editor-only: no runtime callers found in this project, and the project's "no runtime reflection
// outside editor tools" rule bans compiling GetMethod/GetField/GetProperty-based reflection into
// player builds. Guarded rather than moved to an Editor asmdef so any existing `using
// AetherNexus.FoundationPlatform.Extensions;` call sites in editor-only code keep resolving.
#if UNITY_EDITOR
using System;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class ReflectionExtensions
{

    /// <summary>
    /// Checks if the target object has a property with the specified name
    /// </summary>
    public static bool HasProperty(this object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName) != null;
    }
}
}
#endif

