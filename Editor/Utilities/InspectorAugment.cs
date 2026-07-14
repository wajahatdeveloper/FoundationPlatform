#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Linq;
using FoundationPlatform.Utilities.Menus;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
static class InspectorAugment
{
    static InspectorAugment()
    {
        Editor.finishedDefaultHeaderGUI -= DrawInspectorSearchTool;
        Editor.finishedDefaultHeaderGUI += DrawInspectorSearchTool;
    }

    private static string Search;
    private static GameObject LastSelection;
    private static Dictionary<Component, HideFlags> OriginalHideFlags;
    private static bool _repaintSubscribed;

    [MenuItem(MenuPaths.ContextComponent.FoldAllComponents, priority = 50)]
    public static void FoldAllComponents()
    {
        if (!Selection.activeGameObject)
        {
            return;
        }

        var components = Selection.activeGameObject.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(comp, false);
        }

        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }

    [MenuItem(MenuPaths.ContextComponent.FoldAllComponents, validate = true)]
    private static bool FoldAllComponents_Validate()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem(MenuPaths.ContextComponent.ExpandAllComponents, priority = 51)]
    public static void ExpandAllComponents()
    {
        if (!Selection.activeGameObject)
        {
            return;
        }

        var components = Selection.activeGameObject.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(comp, true);
        }

        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }

    [MenuItem(MenuPaths.ContextComponent.ExpandAllComponents, validate = true)]
    private static bool ExpandAllComponents_Validate()
    {
        return Selection.activeGameObject != null;
    }

    static void DrawInspectorSearchTool(Editor editor)
    {
        if (editor == null || editor.target == null)
        {
            return;
        }

        // Check if the editor target is a GameObject and if there's an active selection
        if (editor.target.GetType() != typeof(GameObject) || !Selection.activeGameObject)
        {
            return;
        }

        // Update search state if the selection has changed
        if (LastSelection != Selection.activeGameObject)
        {
            LastSelection = Selection.activeGameObject;
            Search = "";
            OriginalHideFlags = new Dictionary<Component, HideFlags>();
            var initialComponents = Selection.activeGameObject.GetComponents<Component>();
            foreach (var c in initialComponents)
            {
                if (c == null) continue;
                OriginalHideFlags[c] = c.hideFlags;
            }
        }

        // Get all components of the active GameObject
        var components = Selection.activeGameObject.GetComponents<Component>();

        try
        {
            EditorGUILayout.BeginHorizontal();
            Search = EditorGUILayout.TextField(Search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                Search = string.Empty;
                EditorApplication.update -= RepaintAllEditors;
                _repaintSubscribed = false;
            }
            EditorGUILayout.LabelField(
                $"Hidden Components: {components.Count(c => c != null && c.hideFlags == HideFlags.HideInInspector):00} | {components.Count(c => c != null):00}",
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter }, GUILayout.Width(145));
            EditorGUILayout.EndHorizontal();

            // Update component visibility based on the search text
            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (OriginalHideFlags == null)
                {
                    OriginalHideFlags = new Dictionary<Component, HideFlags>();
                }
                if (!OriginalHideFlags.ContainsKey(comp))
                {
                    // Capture the true baseline without the bit this tool manages, so a
                    // component first seen mid-search never gets stuck hidden.
                    OriginalHideFlags[comp] = string.IsNullOrEmpty(Search)
                        ? comp.hideFlags
                        : (comp.hideFlags & ~HideFlags.HideInInspector);
                }

                var original = OriginalHideFlags[comp];
                if (string.IsNullOrEmpty(Search))
                {
                    if (comp.hideFlags != original)
                    {
                        comp.hideFlags = original;
                    }
                }
                else
                {
                    bool match = comp.GetType().Name.IndexOf(Search, StringComparison.OrdinalIgnoreCase) >= 0;
                    var desired = match ? original : (original | HideFlags.HideInInspector);
                    if (comp.hideFlags != desired)
                    {
                        comp.hideFlags = desired;
                    }
                }
            }

            if (!string.IsNullOrEmpty(Search) && !_repaintSubscribed)
            {
                EditorApplication.update += RepaintAllEditors;
                _repaintSubscribed = true;
            }
        }
        catch (Exception e)
        {
            // Log the exception for debugging purposes
            Debug.LogError($"Error in DrawInspectorSearchTool: {e.Message}");
        }
    }

    private static void RepaintAllEditors()
    {
        // Unsubscribe to prevent multiple calls
        EditorApplication.update -= RepaintAllEditors;
        _repaintSubscribed = false;
        ActiveEditorTracker.sharedTracker.RebuildIfNecessary();
    }
}
#endif
