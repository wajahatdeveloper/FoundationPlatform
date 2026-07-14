#if UNITY_EDITOR
using System.Collections.Generic;
using FoundationPlatform.Editor.Utilities.Debugging;
using FoundationPlatform.TweenX;
using FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace FoundationPlatform.TweenX.EditorTools
{
    /// <summary>
    /// Global tween debugger: a live list of every tween the <see cref="TweenManager"/> is running,
    /// with per-row progress, clock, loop state and pause/kill controls, plus a global pause and
    /// kill-all. The in-scene <see cref="TweenDebugSection"/> is the per-object glance; this is the
    /// whole-scene view.
    /// </summary>
    public sealed class TweenDebuggerWindow : EditorWindow
    {
        private static readonly List<TweenManager.TweenInfo> _buffer = new(256);
        private Vector2 _scroll;

        [MenuItem(MenuPaths.WindowTweenX.TweenDebugger, priority = MenuPriorities.WindowTweenX)]
        public static void Open()
        {
            var win = GetWindow<TweenDebuggerWindow>("Tween Debugger");
            win.minSize = new Vector2(360f, 240f);
            win.Show();
        }

        private void OnEnable() => EditorApplication.update += Repaint;
        private void OnDisable() => EditorApplication.update -= Repaint;

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to inspect live tweens.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"Active: {TweenManager.ActiveCount}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Global x{TweenManager.GlobalTimeScale:0.00}", EditorStyles.miniLabel);
                if (GUILayout.Button(TweenManager.IsPaused ? "Resume All" : "Pause All", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    if (TweenManager.IsPaused) TweenManager.ResumeAll(); else TweenManager.PauseAll();
                }
                if (GUILayout.Button("Kill All", EditorStyles.toolbarButton, GUILayout.Width(60f))) TweenManager.KillAll();
            }

            TweenManager.GetActive(_buffer);
            if (_buffer.Count == 0)
            {
                EditorGUILayout.LabelField("(no live tweens)", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _buffer.Count; i++)
            {
                var info = _buffer[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(info.Target, typeof(Object), true);
                    EditorGUILayout.LabelField($"{info.ValueType} · {info.Clock}", EditorStyles.miniLabel, GUILayout.Width(140f));
                }

                if (info.DelayRemaining > 0f)
                    DebugDrawKit.Bar("progress", 0f, $"delay {info.DelayRemaining:F1}s", DebugDrawKit.Warn);
                else
                    DebugDrawKit.Bar("progress", info.Progress, $"{info.Progress * 100f:F0}%",
                        info.Paused ? DebugDrawKit.Neutral : DebugDrawKit.Fill);

                using (new EditorGUILayout.HorizontalScope())
                {
                    string loops = info.LoopCount < 0 ? $"∞ ({info.LoopsDone})"
                        : info.LoopCount > 1 ? $"{info.LoopsDone + 1}/{info.LoopCount}" : "once";
                    EditorGUILayout.LabelField($"loops {loops} · {info.Elapsed:F2}/{info.Duration:F2}s", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    var handle = TweenManager.HandleOf(info);
                    if (GUILayout.Button(info.Paused ? "Resume" : "Pause", EditorStyles.miniButton, GUILayout.Width(64f)))
                    {
                        if (info.Paused) handle.Play(); else handle.Pause();
                    }
                    if (GUILayout.Button("Kill", EditorStyles.miniButton, GUILayout.Width(44f))) handle.Kill();
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
