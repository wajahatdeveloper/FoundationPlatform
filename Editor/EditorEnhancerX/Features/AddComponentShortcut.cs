using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Opens an Add Component search at the mouse by shortcut. Prefers Unity's internal
    /// AddComponentWindow (reflection-guarded); falls back to an in-house searchable
    /// popup over TypeCache when the internal API is unavailable.
    /// </summary>
    [InitializeOnLoad]
    internal static class AddComponentShortcut {

        private static readonly MethodInfo showMethod;

        static AddComponentShortcut() {
            try {
                var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AddComponent.AddComponentWindow", false);
                showMethod = type?.GetMethod("Show",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(Rect), typeof(GameObject[]) }, null);
            } catch {
                showMethod = null;
            }

            KeyRouter.Register("addComponent",
                () => EditorEnhancerXSettings.instance.addComponentKey,
                KeyScope.SceneView | KeyScope.Hierarchy,
                Execute);
        }

        private static bool Execute() {
            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length == 0)
                return false;

            var mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            var screen = GUIUtility.GUIToScreenPoint(mouse);
            var rect = new Rect(screen.x - 115f, screen.y, 230f, 0f);

            if (showMethod != null) {
                try {
                    showMethod.Invoke(null, new object[] { rect, gameObjects });
                    return true;
                } catch {
                    // fall through to the in-house popup
                }
            }

            AddComponentPopupX.Open(rect, gameObjects);
            return true;
        }
    }

    /// <summary>Fallback searchable component list (public APIs only).</summary>
    internal sealed class AddComponentPopupX : EditorWindow {

        private GameObject[] targets;
        private string search = string.Empty;
        private Vector2 scroll;
        private List<Type> componentTypes;

        internal static void Open(Rect screenRect, GameObject[] targets) {
            var window = CreateInstance<AddComponentPopupX>();
            window.targets = targets;
            window.CollectTypes();
            window.ShowAsDropDown(screenRect, new Vector2(260f, 340f));
        }

        private void CollectTypes() {
            componentTypes = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<Component>()) {
                if (type.IsAbstract || type.IsGenericType || !type.IsPublic)
                    continue;
                componentTypes.Add(type);
            }
            componentTypes.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        private void OnGUI() {
            GUI.SetNextControlName("AddComponentPopupXSearch");
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (Event.current.type == EventType.Layout)
                EditorGUI.FocusTextInControl("AddComponentPopupXSearch");

            scroll = EditorGUILayout.BeginScrollView(scroll);
            var shown = 0;
            var hasSearch = !string.IsNullOrEmpty(search);
            foreach (var type in componentTypes) {
                if (hasSearch && type.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (shown++ >= 120)
                    break;
                if (GUILayout.Button(type.Name, EditorStyles.label, GUILayout.Height(18f))) {
                    foreach (var go in targets)
                        Undo.AddComponent(go, type);
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Close();
        }
    }
}
