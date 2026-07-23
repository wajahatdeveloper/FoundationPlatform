#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Right-click object-field selector: a searchable dropdown listing compatible
    /// scene objects and assets. Assigns via the property path so it stays valid
    /// even if the inspector rebuilds while the popup is open.
    /// </summary>
    internal sealed class ObjectSelectorPopupX : EditorWindow
    {
        private const int MaxAssetResults = 200;

        private Object targetObject;
        private string propertyPath;
        private string search = string.Empty;
        private Vector2 scroll;
        private readonly List<Object> sceneCandidates = new List<Object>();
        private readonly List<Object> assetCandidates = new List<Object>();

        internal static void Open(Rect activatorRect, Type type, bool allowScene, SerializedProperty prop)
        {
            var window = CreateInstance<ObjectSelectorPopupX>();
            window.targetObject = prop.serializedObject.targetObject;
            window.propertyPath = prop.propertyPath;
            window.Collect(type, allowScene);

            var screenRect = new Rect(GUIUtility.GUIToScreenPoint(activatorRect.position), Vector2.zero);
            window.ShowAsDropDown(screenRect, new Vector2(280f, 380f));
        }

        private void Collect(Type type, bool allowScene)
        {
            if (type == null || !typeof(Object).IsAssignableFrom(type))
                type = typeof(Object);

            if (allowScene)
            {
                var found = FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                foreach (var obj in found)
                {
                    if (!EditorUtility.IsPersistent(obj))
                        sceneCandidates.Add(obj);
                }
            }

            var guids = AssetDatabase.FindAssets("t:" + type.Name);
            var count = Mathf.Min(guids.Length, MaxAssetResults);
            for (var i = 0; i < count; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset != null)
                    assetCandidates.Add(asset);
            }
        }

        private void OnGUI()
        {
            GUI.SetNextControlName("ObjectSelectorPopupXSearch");
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (Event.current.type == EventType.Layout)
                EditorGUI.FocusTextInControl("ObjectSelectorPopupXSearch");

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (GUILayout.Button("(None)", EditorStyles.label))
                Assign(null);

            DrawSection("Scene", sceneCandidates);
            DrawSection("Assets", assetCandidates);

            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Close();
        }

        private void DrawSection(string title, List<Object> candidates)
        {
            if (candidates.Count == 0)
                return;

            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            var hasSearch = !string.IsNullOrEmpty(search);
            foreach (var candidate in candidates)
            {
                if (candidate == null)
                    continue;
                if (hasSearch && candidate.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var content = new GUIContent(candidate.name, AssetPreview.GetMiniThumbnail(candidate));
                if (GUILayout.Button(content, EditorStyles.label, GUILayout.Height(18f)))
                    Assign(candidate);
            }
        }

        private void Assign(Object value)
        {
            if (targetObject != null)
            {
                var so = new SerializedObject(targetObject);
                var prop = so.FindProperty(propertyPath);
                if (prop != null)
                {
                    prop.objectReferenceValue = value;
                    so.ApplyModifiedProperties();
                }
            }
            Close();
        }
    }
}
#endif
