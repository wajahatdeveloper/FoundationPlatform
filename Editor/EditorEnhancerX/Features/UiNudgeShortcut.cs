#if AETHERNEXUS_UIWIDGETS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Pixel-nudge selected <see cref="RectTransform"/>s via Alt+Arrow (1px) /
    /// Alt+Shift+Arrow (coarse). Bare arrows stay Unity Hierarchy navigation.
    /// Gated by <c>AETHERNEXUS_UIWIDGETS</c> (set by UIWidgets package).
    /// </summary>
    [InitializeOnLoad]
    internal static class UiNudgeShortcut {

        static UiNudgeShortcut() {
            // Global: when Project Settings ▸ EditorEnhancerX ▸ Global Key Capture is on
            // (so Alt+Arrow works with Inspector focused).
            var scope = KeyScope.SceneView | KeyScope.Hierarchy | KeyScope.Global;
            KeyRouter.Register("nudgeLeft", () => EditorEnhancerXSettings.instance.nudgeLeftKey, scope, () => Nudge(-1f, 0f, false));
            KeyRouter.Register("nudgeRight", () => EditorEnhancerXSettings.instance.nudgeRightKey, scope, () => Nudge(1f, 0f, false));
            KeyRouter.Register("nudgeUp", () => EditorEnhancerXSettings.instance.nudgeUpKey, scope, () => Nudge(0f, 1f, false));
            KeyRouter.Register("nudgeDown", () => EditorEnhancerXSettings.instance.nudgeDownKey, scope, () => Nudge(0f, -1f, false));
            KeyRouter.Register("nudgeLeftCoarse", () => EditorEnhancerXSettings.instance.nudgeLeftCoarseKey, scope, () => Nudge(-1f, 0f, true));
            KeyRouter.Register("nudgeRightCoarse", () => EditorEnhancerXSettings.instance.nudgeRightCoarseKey, scope, () => Nudge(1f, 0f, true));
            KeyRouter.Register("nudgeUpCoarse", () => EditorEnhancerXSettings.instance.nudgeUpCoarseKey, scope, () => Nudge(0f, 1f, true));
            KeyRouter.Register("nudgeDownCoarse", () => EditorEnhancerXSettings.instance.nudgeDownCoarseKey, scope, () => Nudge(0f, -1f, true));
        }

        private static bool Nudge(float dirX, float dirY, bool coarse) {
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
