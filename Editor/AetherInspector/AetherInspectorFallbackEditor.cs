#if UNITY_EDITOR
using UnityEditor;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Global fallback inspector that renders EVERY object through the in-house
    /// <see cref="AetherInspectorEditor"/> engine, so every object with <see cref="AetherNexus.FoundationPlatform.AetherInspector"/>
    /// attributes draws correctly without a per-type editor.
    /// A concrete <c>[CustomEditor(typeof(T))]</c> always beats this <c>isFallback</c> editor, so
    /// hand-written inspectors keep priority.
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class AetherInspectorFallbackEditor : AetherInspectorEditor { }
}
#endif
