#if UNITY_EDITOR
using FoundationPlatform.FrameworkInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.TweenX.EditorTools
{
    /// <summary>
    /// Inspector + scene preview for <see cref="TweenAnimator"/>. Inherits the parity-engine editor so
    /// the component's attribute-decorated inspector (foldouts, conditionals, Play/Stop buttons) renders,
    /// and adds Scene-View handles: each positional step draws its destination as a movable handle with a
    /// dashed line from the current transform, so designers can author motion targets visually.
    /// </summary>
    [CustomEditor(typeof(TweenAnimator))]
    public sealed class TweenAnimatorEditor : FrameworkEditor
    {
        private void OnSceneGUI()
        {
            var anim = (TweenAnimator)target;
            if (anim == null || anim.Steps == null) return;

            var tr = anim.transform;
            for (int i = 0; i < anim.Steps.Count; i++)
            {
                var step = anim.Steps[i];
                if (!TryGetWorldDestination(tr, step, out Vector3 world)) continue;

                Handles.color = new Color(0.35f, 0.6f, 0.9f, 1f);
                Handles.DrawDottedLine(tr.position, world, 4f);

                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(world, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(anim, "Move Tween Destination");
                    WriteWorldDestination(tr, step, moved);
                    EditorUtility.SetDirty(anim);
                }

                Handles.Label(world, $"Step {i}: {step.Target}");
            }
        }

        private static bool TryGetWorldDestination(Transform tr, TweenAnimator.TweenStep step, out Vector3 world)
        {
            switch (step.Target)
            {
                case TweenAnimator.TweenTarget.Move:
                    world = step.ToVector; return true;
                case TweenAnimator.TweenTarget.LocalMove:
                    world = tr.parent != null ? tr.parent.TransformPoint(step.ToVector) : step.ToVector; return true;
                default:
                    world = default; return false;
            }
        }

        private static void WriteWorldDestination(Transform tr, TweenAnimator.TweenStep step, Vector3 world)
        {
            switch (step.Target)
            {
                case TweenAnimator.TweenTarget.Move:
                    step.ToVector = world; break;
                case TweenAnimator.TweenTarget.LocalMove:
                    step.ToVector = tr.parent != null ? tr.parent.InverseTransformPoint(world) : world; break;
            }
        }
    }
}
#endif
