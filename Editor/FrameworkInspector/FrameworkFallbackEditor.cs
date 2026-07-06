#if UNITY_EDITOR
using UnityEditor;

namespace Framework.Inspector.Editor
{
    /// <summary>
    /// Global fallback inspector that renders EVERY object through the in-house
    /// <see cref="FrameworkEditor"/> engine, so every object with <see cref="Framework.Inspector"/>
    /// attributes draws correctly without a per-type editor.
    /// A concrete <c>[CustomEditor(typeof(T))]</c> always beats this <c>isFallback</c> editor, so
    /// hand-written inspectors keep priority.
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class FrameworkFallbackEditor : FrameworkEditor { }
}
#endif
