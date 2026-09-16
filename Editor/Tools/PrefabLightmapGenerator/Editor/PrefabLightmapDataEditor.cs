#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using AetherNexus.FoundationPlatform.Tools;
using AetherNexus.FoundationPlatform.Editor.Tools;

namespace AetherNexus.FoundationPlatform.Editor.Tools.Editor
{
    /// <summary>
    /// Custom editor for PrefabLightmapData component with improved Inspector interface.
    /// </summary>
    [CustomEditor(typeof(PrefabLightmapData))]
    public class PrefabLightmapDataEditor : AetherInspectorEditor
    {
        #region Serialized Properties
        private SerializedProperty releaseShadersProp;
        private SerializedProperty enableDebugLoggingProp;
        private SerializedProperty rendererInfosProp;
        private SerializedProperty lightmapsProp;
        private SerializedProperty lightmapsDirProp;
        private SerializedProperty shadowMasksProp;
        private SerializedProperty lightInfosProp;
        #endregion

        #region Unity Lifecycle
        protected override void OnEnable()
        {
            base.OnEnable();
            // Cache serialized properties
            releaseShadersProp = serializedObject.FindProperty("releaseShaders");
            enableDebugLoggingProp = serializedObject.FindProperty("enableDebugLogging");
            rendererInfosProp = serializedObject.FindProperty("rendererInfos");
            lightmapsProp = serializedObject.FindProperty("lightmaps");
            lightmapsDirProp = serializedObject.FindProperty("lightmapsDir");
            shadowMasksProp = serializedObject.FindProperty("shadowMasks");
            lightInfosProp = serializedObject.FindProperty("lightInfos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Removed static heading
            DrawSettings();
            DrawDataSections();
            DrawDebugInfo();
            DrawButtons();

            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region GUI Drawing Methods
        // Removed DrawHeader method usage and label drawing

        private void DrawSettings()
        {
            // Removed static "Settings" heading; keep fields only
            EditorGUILayout.PropertyField(releaseShadersProp, new GUIContent("Release Shaders", 
                "Reassigns shaders when applying baked lightmaps. May conflict with some shaders like transparent HDRP."));
            EditorGUILayout.PropertyField(enableDebugLoggingProp, new GUIContent("Enable Debug Logging", 
                "Shows detailed logging information for troubleshooting."));
        }

        private void DrawDataSections()
        {
            using (var renderers = GuiKit.CollapsedSection("PrefabLightmap.Renderers",
                       $"Renderer Information ({rendererInfosProp.arraySize})"))
            {
                if (renderers.Expanded)
                    DrawRendererInfo();
            }

            using (var lightmaps = GuiKit.CollapsedSection("PrefabLightmap.Lightmaps",
                       $"Lightmap Information ({lightmapsProp.arraySize})"))
            {
                if (lightmaps.Expanded)
                    DrawLightmapInfo();
            }

            using (var lights = GuiKit.CollapsedSection("PrefabLightmap.Lights",
                       $"Light Information ({lightInfosProp.arraySize})"))
            {
                if (lights.Expanded)
                    DrawLightInfo();
            }
        }

        private void DrawRendererInfo()
        {
            if (rendererInfosProp.arraySize == 0)
            {
                GuiKit.InfoBox("No renderer information available. Bake lightmaps to generate data.", InfoMessageType.Info);
                return;
            }

            for (int i = 0; i < rendererInfosProp.arraySize; i++)
            {
                var element = rendererInfosProp.GetArrayElementAtIndex(i);
                var rendererProp = element.FindPropertyRelative("renderer");
                var lightmapIndexProp = element.FindPropertyRelative("lightmapIndex");
                var offsetScaleProp = element.FindPropertyRelative("lightmapOffsetScale");

                GuiKit.BeginBox($"Renderer {i}");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(rendererProp, new GUIContent("Renderer"));
                    EditorGUILayout.PropertyField(lightmapIndexProp, new GUIContent("Lightmap Index"));
                    EditorGUILayout.PropertyField(offsetScaleProp, new GUIContent("Offset & Scale"));
                }
                GuiKit.EndBox();
            }
        }

        private void DrawLightmapInfo()
        {
            if (lightmapsProp.arraySize == 0)
            {
                GuiKit.InfoBox("No lightmap textures available. Bake lightmaps to generate data.", InfoMessageType.Info);
                return;
            }

            GuiKit.Title("Lightmap Textures");
            for (int i = 0; i < lightmapsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Lightmap {i}", GUILayout.Width(80));
                EditorGUILayout.PropertyField(lightmapsProp.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }

            if (lightmapsDirProp.arraySize > 0)
            {
                GuiKit.Title("Directional Lightmap Textures");
                for (int i = 0; i < lightmapsDirProp.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Dir {i}", GUILayout.Width(80));
                    EditorGUILayout.PropertyField(lightmapsDirProp.GetArrayElementAtIndex(i), GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (shadowMasksProp.arraySize > 0)
            {
                GuiKit.Title("Shadow Mask Textures");
                for (int i = 0; i < shadowMasksProp.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Shadow {i}", GUILayout.Width(80));
                    EditorGUILayout.PropertyField(shadowMasksProp.GetArrayElementAtIndex(i), GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DrawLightInfo()
        {
            if (lightInfosProp.arraySize == 0)
            {
                GuiKit.InfoBox("No light information available. Bake lightmaps to generate data.", InfoMessageType.Info);
                return;
            }

            for (int i = 0; i < lightInfosProp.arraySize; i++)
            {
                var element = lightInfosProp.GetArrayElementAtIndex(i);
                var lightProp = element.FindPropertyRelative("light");
                var bakeTypeProp = element.FindPropertyRelative("lightmapBakeType");
                var mixedModeProp = element.FindPropertyRelative("mixedLightingMode");

                GuiKit.BeginBox($"Light {i}");
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(lightProp, new GUIContent("Light"));
                    EditorGUILayout.PropertyField(bakeTypeProp, new GUIContent("Bake Type"));
                    EditorGUILayout.PropertyField(mixedModeProp, new GUIContent("Mixed Lighting Mode"));
                }
                GuiKit.EndBox();
            }
        }

        private void DrawDebugInfo()
        {
            using var section = GuiKit.CollapsedSection("PrefabLightmap.Debug", "Debug Information");
            if (!section.Expanded)
                return;

            GuiKit.Title("Data Validation");
            EditorGUILayout.LabelField($"Renderer Infos: {rendererInfosProp.arraySize}");
            EditorGUILayout.LabelField($"Lightmaps: {lightmapsProp.arraySize}");
            EditorGUILayout.LabelField($"Directional Maps: {lightmapsDirProp.arraySize}");
            EditorGUILayout.LabelField($"Shadow Masks: {shadowMasksProp.arraySize}");
            EditorGUILayout.LabelField($"Lights: {lightInfosProp.arraySize}");

            GuiKit.Title("Validation Status");
            bool isValid = ValidateData();
            EditorGUILayout.LabelField($"Valid: {(isValid ? "Yes" : "No")}");

            if (!isValid)
                GuiKit.ValidationBox("Data validation failed. Check the console for details.", InfoMessageType.Warning);
        }

        private void DrawButtons()
        {
            using (GuiKit.ActionRow())
            {
                if (GuiKit.ActionButton("Initialize Now"))
                {
                    var target = (PrefabLightmapData)serializedObject.targetObject;
                    target.InitializeLightmapData();
                }

                if (GuiKit.ActionButton("Bake All Prefab Lightmaps"))
                    PrefabLightmapBaker.GenerateLightmapInfo();
            }
        }
        #endregion

        #region Helper Methods
        private bool ValidateData()
        {
            var target = (PrefabLightmapData)serializedObject.targetObject;
            
            // Use reflection to access private validation method
            var method = typeof(PrefabLightmapData).GetMethod("ValidateLightmapData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                return (bool)method.Invoke(target, null);
            }
            
            return false;
        }
        #endregion
    }
}
#endif
