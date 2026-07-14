#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace AetherNexus.FoundationPlatform.Editor.Tools.Editor
{
    /// <summary>
    /// Custom editor for PrefabLightmapData component with improved Inspector interface.
    /// </summary>
    [CustomEditor(typeof(PrefabLightmapData))]
    public class PrefabLightmapDataEditor : UnityEditor.Editor
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

        #region Private Fields
        private bool showRendererInfo = true;
        private bool showLightmapInfo = true;
        private bool showLightInfo = true;
        private bool showDebugInfo = false;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
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
            EditorGUILayout.Space();
        }

        private void DrawDataSections()
        {
            // Renderer Information
            showRendererInfo = EditorGUILayout.Foldout(showRendererInfo, 
                $"Renderer Information ({rendererInfosProp.arraySize})", true);
            if (showRendererInfo)
            {
                EditorGUI.indentLevel++;
                DrawRendererInfo();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Lightmap Information
            showLightmapInfo = EditorGUILayout.Foldout(showLightmapInfo, 
                $"Lightmap Information ({lightmapsProp.arraySize})", true);
            if (showLightmapInfo)
            {
                EditorGUI.indentLevel++;
                DrawLightmapInfo();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Light Information
            showLightInfo = EditorGUILayout.Foldout(showLightInfo, 
                $"Light Information ({lightInfosProp.arraySize})", true);
            if (showLightInfo)
            {
                EditorGUI.indentLevel++;
                DrawLightInfo();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRendererInfo()
        {
            if (rendererInfosProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No renderer information available. Bake lightmaps to generate data.", MessageType.Info);
                return;
            }

            for (int i = 0; i < rendererInfosProp.arraySize; i++)
            {
                var element = rendererInfosProp.GetArrayElementAtIndex(i);
                var rendererProp = element.FindPropertyRelative("renderer");
                var lightmapIndexProp = element.FindPropertyRelative("lightmapIndex");
                var offsetScaleProp = element.FindPropertyRelative("lightmapOffsetScale");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Renderer {i}", EditorStyles.miniBoldLabel);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(rendererProp, new GUIContent("Renderer"));
                EditorGUILayout.PropertyField(lightmapIndexProp, new GUIContent("Lightmap Index"));
                EditorGUILayout.PropertyField(offsetScaleProp, new GUIContent("Offset & Scale"));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawLightmapInfo()
        {
            if (lightmapsProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No lightmap textures available. Bake lightmaps to generate data.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Lightmap Textures", EditorStyles.miniBoldLabel);
            for (int i = 0; i < lightmapsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Lightmap {i}", GUILayout.Width(80));
                EditorGUILayout.PropertyField(lightmapsProp.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }

            if (lightmapsDirProp.arraySize > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Directional Lightmap Textures", EditorStyles.miniBoldLabel);
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
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Shadow Mask Textures", EditorStyles.miniBoldLabel);
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
                EditorGUILayout.HelpBox("No light information available. Bake lightmaps to generate data.", MessageType.Info);
                return;
            }

            for (int i = 0; i < lightInfosProp.arraySize; i++)
            {
                var element = lightInfosProp.GetArrayElementAtIndex(i);
                var lightProp = element.FindPropertyRelative("light");
                var bakeTypeProp = element.FindPropertyRelative("lightmapBakeType");
                var mixedModeProp = element.FindPropertyRelative("mixedLightingMode");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Light {i}", EditorStyles.miniBoldLabel);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(lightProp, new GUIContent("Light"));
                EditorGUILayout.PropertyField(bakeTypeProp, new GUIContent("Bake Type"));
                EditorGUILayout.PropertyField(mixedModeProp, new GUIContent("Mixed Lighting Mode"));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawDebugInfo()
        {
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Debug Information", true);
            if (showDebugInfo)
            {
                EditorGUI.indentLevel++;
                
                var target = (PrefabLightmapData)serializedObject.targetObject;
                
                EditorGUILayout.LabelField("Data Validation", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Renderer Infos: {rendererInfosProp.arraySize}");
                EditorGUILayout.LabelField($"Lightmaps: {lightmapsProp.arraySize}");
                EditorGUILayout.LabelField($"Directional Maps: {lightmapsDirProp.arraySize}");
                EditorGUILayout.LabelField($"Shadow Masks: {shadowMasksProp.arraySize}");
                EditorGUILayout.LabelField($"Lights: {lightInfosProp.arraySize}");
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Validation Status", EditorStyles.miniBoldLabel);
                bool isValid = ValidateData();
                EditorGUILayout.LabelField($"Valid: {(isValid ? "Yes" : "No")}", 
                    isValid ? EditorStyles.label : EditorStyles.boldLabel);
                
                if (!isValid)
                {
                    EditorGUILayout.HelpBox("Data validation failed. Check the console for details.", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Initialize Now"))
            {
                var target = (PrefabLightmapData)serializedObject.targetObject;
                target.InitializeLightmapData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Bake All Prefab Lightmaps", GUILayout.Height(30)))
            {
                // Call the public static method directly
                PrefabLightmapData.GenerateLightmapInfo();
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
