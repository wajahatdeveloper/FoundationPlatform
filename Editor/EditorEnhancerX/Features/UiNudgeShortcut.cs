#if AETHERNEXUS_UIWIDGETS
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Pixel-nudge selected <see cref="RectTransform"/>s via Alt+Arrow (1px) /
    /// Alt+Shift+Arrow (coarse). Bare arrows stay Unity Hierarchy navigation.
    /// Gated by <c>AETHERNEXUS_UIWIDGETS</c> (set by UIWidgets package).
    /// </summary>
    internal static class UiNudgeShortcut {

        [Shortcut("EditorEnhancerX/UI Nudge Left", KeyCode.LeftArrow, ShortcutModifiers.Alt)]
        private static void NudgeLeft() => Nudge(-1f, 0f, false);
        [Shortcut("EditorEnhancerX/UI Nudge Right", KeyCode.RightArrow, ShortcutModifiers.Alt)]
        private static void NudgeRight() => Nudge(1f, 0f, false);
        [Shortcut("EditorEnhancerX/UI Nudge Up", KeyCode.UpArrow, ShortcutModifiers.Alt)]
        private static void NudgeUp() => Nudge(0f, 1f, false);
        [Shortcut("EditorEnhancerX/UI Nudge Down", KeyCode.DownArrow, ShortcutModifiers.Alt)]
        private static void NudgeDown() => Nudge(0f, -1f, false);
        [Shortcut("EditorEnhancerX/UI Nudge Left (Coarse)", KeyCode.LeftArrow, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void NudgeLeftCoarse() => Nudge(-1f, 0f, true);
        [Shortcut("EditorEnhancerX/UI Nudge Right (Coarse)", KeyCode.RightArrow, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void NudgeRightCoarse() => Nudge(1f, 0f, true);
        [Shortcut("EditorEnhancerX/UI Nudge Up (Coarse)", KeyCode.UpArrow, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void NudgeUpCoarse() => Nudge(0f, 1f, true);
        [Shortcut("EditorEnhancerX/UI Nudge Down (Coarse)", KeyCode.DownArrow, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        private static void NudgeDownCoarse() => Nudge(0f, -1f, true);

        private static bool Nudge(float dirX, float dirY, bool coarse) {
            if (!EditorEnhancerXSettings.Active)
                return false;
            var settings = EditorEnhancerXSettings.instance;
            float step = coarse ? settings.nudgeStepCoarse : settings.nudgeStep;
            var delta = new Vector2(dirX * step, dirY * step);

            var rects = CollectSelectedRectTransforms();
            if (rects.Count == 0)
                return false;

            var targets = new Object[rects.Count];
            for (var i = 0; i < rects.Count; i++)
                targets[i] = rects[i];
            Undo.RecordObjects(targets, "Nudge UI");

            for (var i = 0; i < rects.Count; i++)
                rects[i].anchoredPosition += delta;

            return true;
        }

        private static List<RectTransform> CollectSelectedRectTransforms() {
            var result = new List<RectTransform>();
            var transforms = Selection.transforms;
            for (var i = 0; i < transforms.Length; i++) {
                if (transforms[i] is RectTransform rt)
                    result.Add(rt);
            }
            return result;
        }
    }
}
#endif
