#if UNITY_EDITOR
using FoundationPlatform.FrameworkInspector.Editor;
using UnityEditor;

namespace FoundationPlatform.TweenX.EditorTools
{
    /// <summary>
    /// Routes <see cref="TweenAnimator.TweenStep"/> through the parity-engine reflected drawer so its
    /// <c>[ShowIf]</c>/<c>[HideIf]</c> conditionals, <c>[EnumToggleButtons]</c>, and per-field labels
    /// render correctly (Unity's default drawer ignores those). Three lines, no custom GUI.
    /// </summary>
    [CustomPropertyDrawer(typeof(TweenAnimator.TweenStep))]
    internal sealed class TweenStepDrawer : FrameworkReflectedDrawer { }
}
#endif
