#if UNITY_EDITOR
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Editor.Utilities.Debugging;
using AetherNexus.FoundationPlatform.TweenX;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.TweenX.EditorTools
{
    /// <summary>
    /// In-context tween glance for the <c>EntityDebuggerOverlay</c>: when a GameObject with live tweens
    /// is selected in the Scene View, this stacks a "Tweens" block showing each tween's progress,
    /// clock, loop state and a Kill button — no window hunting. Auto-discovered via
    /// <see cref="EntityDebugSectionRegistry"/>; no manual registration.
    /// </summary>
    public sealed class TweenDebugSection : IEntityDebugSection
    {
        private static readonly List<TweenManager.TweenInfo> _buffer = new(64);

        public string Title => "Tweens";
        public int Order => 65;

        public bool AppliesTo(GameObject go)
        {
            if (go == null || !Application.isPlaying) return false;
            TweenManager.GetActive(_buffer);
            for (int i = 0; i < _buffer.Count; i++)
                if (ResolveGameObject(_buffer[i].Target) == go) return true;
            return false;
        }

        public void DrawDetail(GameObject go)
        {
            TweenManager.GetActive(_buffer);
            int shown = 0;
            for (int i = 0; i < _buffer.Count; i++)
            {
                var info = _buffer[i];
                if (ResolveGameObject(info.Target) != go) continue;
                shown++;

                string label = $"{info.ValueType}  ({info.Clock})";
                if (info.DelayRemaining > 0f)
                    DebugDrawKit.Bar(label, 0f, $"delay {info.DelayRemaining:F1}s", DebugDrawKit.Warn);
                else
                    DebugDrawKit.Bar(label, info.Progress, $"{info.Progress * 100f:F0}%",
                        info.Paused ? DebugDrawKit.Neutral : DebugDrawKit.Fill);

                EditorGUILayout.BeginHorizontal();
                string loops = info.LoopCount < 0 ? $"loop ∞ ({info.LoopsDone})"
                    : info.LoopCount > 1 ? $"loop {info.LoopsDone + 1}/{info.LoopCount}" : "once";
                EditorGUILayout.LabelField(loops, EditorStyles.miniLabel);
                var handle = TweenManager.HandleOf(info);
                if (GUILayout.Button(info.Paused ? "Resume" : "Pause", EditorStyles.miniButton, GUILayout.Width(64f)))
                {
                    if (info.Paused) handle.Play(); else handle.Pause();
                }
                if (GUILayout.Button("Kill", EditorStyles.miniButton, GUILayout.Width(44f))) handle.Kill();
                EditorGUILayout.EndHorizontal();
            }

            if (shown == 0) EditorGUILayout.LabelField("(no live tweens)", EditorStyles.miniLabel);
        }

        public void OpenFullWindow() => TweenDebuggerWindow.Open();

        internal static GameObject ResolveGameObject(Object o)
        {
            switch (o)
            {
                case GameObject g: return g;
                case Component c: return c != null ? c.gameObject : null;
                default: return null;
            }
        }
    }
}
#endif
