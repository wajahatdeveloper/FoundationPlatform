#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Tools;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Editor.Tools
{
    /// <summary>
    /// Editor-only baking pipeline for <see cref="PrefabLightmapData"/>. Split out of the runtime
    /// component (which must compile into player builds) so this Lightmapping/PrefabUtility-dependent
    /// code stays confined to the Editor assembly.
    /// </summary>
    public static class PrefabLightmapBaker
    {
        /// <summary>
        /// Menu item to bake lightmap data for all PrefabLightmapData components in the scene.
        /// </summary>
        [MenuItem(MenuPaths.Utilities.BakePrefabLightmaps, false, MenuPriorities.Utilities + 3)]
        public static void GenerateLightmapInfo()
        {
            if (!ValidateLightmappingSettings())
            {
                return;
            }

            try
            {
                Lightmapping.Bake();
                ProcessAllPrefabLightmapData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PrefabLightmapData] Failed to bake lightmaps: {e.Message}");
            }
        }

        /// <summary>
        /// Validates that lightmapping settings are correct for baking.
        /// </summary>
        private static bool ValidateLightmappingSettings()
        {
            // Check if lightmapping is available and properly configured
            try
            {
                // Try to access lightmap settings to ensure they're available
                var currentLightmaps = LightmapSettings.lightmaps;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PrefabLightmapData] Could not access lightmap settings: {e.Message}. Please ensure lighting is properly configured.");
                return false;
            }
        }

        /// <summary>
        /// Processes all PrefabLightmapData components in the scene.
        /// </summary>
        private static void ProcessAllPrefabLightmapData()
        {
            var prefabInstances = UnityEngine.Object.FindObjectsByType<PrefabLightmapData>(FindObjectsSortMode.None);

            if (prefabInstances.Length == 0)
            {
                Debug.LogWarning("[PrefabLightmapData] No PrefabLightmapData components found in the scene.");
                return;
            }

            Debug.Log($"[PrefabLightmapData] Processing {prefabInstances.Length} PrefabLightmapData components...");

            foreach (var instance in prefabInstances)
            {
                try
                {
                    ProcessPrefabLightmapData(instance);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PrefabLightmapData] Failed to process {instance.gameObject.name}: {e.Message}", instance);
                }
            }

            Debug.Log("[PrefabLightmapData] Lightmap baking completed successfully.");
        }

        /// <summary>
        /// Processes a single PrefabLightmapData instance.
        /// </summary>
        private static void ProcessPrefabLightmapData(PrefabLightmapData instance)
        {
            var gameObject = instance.gameObject;
            var rendererInfos = new List<PrefabLightmapData.RendererInfo>();
            var lightmaps = new List<Texture2D>();
            var lightmapsDir = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();
            var lightInfos = new List<PrefabLightmapData.LightInfo>();

            GenerateLightmapInfo(gameObject, rendererInfos, lightmaps, lightmapsDir, shadowMasks, lightInfos);

            // Update instance data
            instance.rendererInfos = rendererInfos.ToArray();
            instance.lightmaps = lightmaps.ToArray();
            instance.lightmapsDir = lightmapsDir.ToArray();
            instance.lightInfos = lightInfos.ToArray();
            instance.shadowMasks = shadowMasks.ToArray();

            // Apply changes to prefab
            ApplyChangesToPrefab(instance);
        }

        /// <summary>
        /// Applies changes to the prefab with proper error handling.
        /// </summary>
        private static void ApplyChangesToPrefab(PrefabLightmapData instance)
        {
            var targetPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance.gameObject) as GameObject;
            if (targetPrefab == null)
            {
                Debug.LogWarning($"[PrefabLightmapData] No prefab found for {instance.gameObject.name}", instance);
                return;
            }

#if UNITY_2018_3_OR_NEWER
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(instance.gameObject);
            if (root != null)
            {
                // Handle nested prefab instances
                ApplyNestedPrefabChanges(instance, root);
            }
            else
            {
                // Handle regular prefab instances
                PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
            }
#else
            // Legacy prefab handling
            PrefabUtility.ReplacePrefab(instance.gameObject, targetPrefab);
#endif
        }

#if UNITY_2018_3_OR_NEWER
        /// <summary>
        /// Handles changes for nested prefab instances.
        /// </summary>
        private static void ApplyNestedPrefabChanges(PrefabLightmapData instance, GameObject root)
        {
            var rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject);
            if (rootPrefab == null)
            {
                Debug.LogError($"[PrefabLightmapData] Could not find root prefab for {instance.gameObject.name}", instance);
                return;
            }

            string rootPath = AssetDatabase.GetAssetPath(rootPrefab);
            if (string.IsNullOrEmpty(rootPath))
            {
                Debug.LogError($"[PrefabLightmapData] Could not get asset path for root prefab", instance);
                return;
            }

            // Unpack the outermost root
            var unpackedRoots = PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots(root, PrefabUnpackMode.OutermostRoot);

            try
            {
                // Apply the changes to the instance
                PrefabUtility.ApplyPrefabInstance(instance.gameObject, InteractionMode.AutomatedAction);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PrefabLightmapData] Failed to apply prefab instance changes: {e.Message}", instance);
            }
            finally
            {
                // Save the root prefab
                try
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(root, rootPath, InteractionMode.AutomatedAction);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PrefabLightmapData] Failed to save root prefab: {e.Message}", instance);
                }
            }
        }
#endif

        /// <summary>
        /// Generates lightmap information for all renderers and lights in the hierarchy.
        /// </summary>
        private static void GenerateLightmapInfo(GameObject root, List<PrefabLightmapData.RendererInfo> rendererInfos, List<Texture2D> lightmaps,
            List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, List<PrefabLightmapData.LightInfo> lightInfos)
        {
            if (root == null)
            {
                Debug.LogError("[PrefabLightmapData] Root GameObject is null");
                return;
            }

            var currentLightmaps = LightmapSettings.lightmaps;
            if (currentLightmaps == null || currentLightmaps.Length == 0)
            {
                Debug.LogWarning("[PrefabLightmapData] No lightmaps found in current scene");
                return;
            }

            // Process renderers
            ProcessRenderers(root, rendererInfos, lightmaps, lightmapsDir, shadowMasks, currentLightmaps);

            // Process lights
            ProcessLights(root, lightInfos);
        }

        private const int INVALID_LIGHTMAP_INDEX = 0xFFFE;
        private const int NO_LIGHTMAP_INDEX = -1;

        /// <summary>
        /// Processes all MeshRenderers in the hierarchy to extract lightmap data.
        /// </summary>
        private static void ProcessRenderers(GameObject root, List<PrefabLightmapData.RendererInfo> rendererInfos, List<Texture2D> lightmaps,
            List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, LightmapData[] currentLightmaps)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            int processedCount = 0;

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null) continue;

                if (renderer.lightmapIndex == NO_LIGHTMAP_INDEX) continue;

                // Validate lightmap index
                if (renderer.lightmapIndex < 0 || renderer.lightmapIndex >= currentLightmaps.Length)
                {
                    Debug.LogWarning($"[PrefabLightmapData] Invalid lightmap index {renderer.lightmapIndex} for renderer {renderer.name}", renderer);
                    continue;
                }

                // Skip invalid lightmap indices
                if (renderer.lightmapIndex == INVALID_LIGHTMAP_INDEX) continue;

                // Check if renderer has valid lightmap data
                if (renderer.lightmapScaleOffset == Vector4.zero) continue;

                try
                {
                    var lightmapData = currentLightmaps[renderer.lightmapIndex];
                    if (lightmapData.lightmapColor == null)
                    {
                        Debug.LogWarning($"[PrefabLightmapData] No lightmap color texture for renderer {renderer.name}", renderer);
                        continue;
                    }

                    var info = new PrefabLightmapData.RendererInfo
                    {
                        renderer = renderer,
                        lightmapOffsetScale = renderer.lightmapScaleOffset
                    };

                    // Find or add lightmap textures
                    int lightmapIndex = lightmaps.IndexOf(lightmapData.lightmapColor);
                    if (lightmapIndex == -1)
                    {
                        lightmapIndex = lightmaps.Count;
                        lightmaps.Add(lightmapData.lightmapColor);
                        lightmapsDir.Add(lightmapData.lightmapDir);
                        shadowMasks.Add(lightmapData.shadowMask);
                    }

                    info.lightmapIndex = lightmapIndex;
                    rendererInfos.Add(info);
                    processedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PrefabLightmapData] Failed to process renderer {renderer.name}: {e.Message}", renderer);
                }
            }

            Debug.Log($"[PrefabLightmapData] Processed {processedCount} renderers with lightmap data");
        }

        /// <summary>
        /// Processes all Lights in the hierarchy to extract baking information.
        /// </summary>
        private static void ProcessLights(GameObject root, List<PrefabLightmapData.LightInfo> lightInfos)
        {
            var lights = root.GetComponentsInChildren<Light>(true);
            int processedCount = 0;

            foreach (Light light in lights)
            {
                if (light == null) continue;

                try
                {
                    var lightInfo = new PrefabLightmapData.LightInfo
                    {
                        light = light,
                        lightmapBakeType = (int)light.lightmapBakeType
                    };

                    // Get mixed lighting mode based on Unity version
#if UNITY_2020_1_OR_NEWER
                    lightInfo.mixedLightingMode = (int)Lightmapping.lightingSettings.mixedBakeMode;
#elif UNITY_2018_1_OR_NEWER
                    lightInfo.mixedLightingMode = (int)LightmapEditorSettings.mixedBakeMode;
#else
                    lightInfo.mixedLightingMode = (int)light.bakingOutput.lightmapBakeType;
#endif

                    lightInfos.Add(lightInfo);
                    processedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PrefabLightmapData] Failed to process light {light.name}: {e.Message}", light);
                }
            }

            Debug.Log($"[PrefabLightmapData] Processed {processedCount} lights");
        }
    }
}
#endif
