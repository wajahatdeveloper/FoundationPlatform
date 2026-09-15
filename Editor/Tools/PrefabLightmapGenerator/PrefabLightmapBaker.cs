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
        [DesignerFeature(
            "Bake Prefab Lightmaps",
            "Stores baked lighting on the prefab itself so it keeps its lightmaps when spawned into a scene at runtime.",
            "lightmap bake prefab lighting gi baked light spawn instantiate keep lighting dark",
            DesignerFeatureKind.Action,
            "")]
        public static void GenerateLightmapInfo()
        {
            Lightmapping.Bake();

            if (LightmapSettings.lightmaps.Length == 0)
            {
                Debug.LogError("[PrefabLightmapData] Bake produced no lightmaps. Check the scene's lighting settings and that the renderers are marked Contribute GI.");
                return;
            }

            ProcessAllPrefabLightmapData();
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
            // Applying on the outermost root covers nested instances too. Unpacking first (as this
            // tool used to) breaks the instance link and writes a flattened hierarchy over the asset.
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(instance.gameObject);
            if (root == null)
            {
                Debug.LogWarning(
                    $"[PrefabLightmapData] '{instance.gameObject.name}' is not a prefab instance; its baked data stays scene-only.",
                    instance);
                return;
            }

            PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);
        }

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

        private const int NO_LIGHTMAP_INDEX = -1;

        /// <summary>
        /// Processes all MeshRenderers in the hierarchy to extract lightmap data.
        /// </summary>
        private static void ProcessRenderers(GameObject root, List<PrefabLightmapData.RendererInfo> rendererInfos, List<Texture2D> lightmaps,
            List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, LightmapData[] currentLightmaps)
        {
            // Inactive included: a renderer disabled at author time still carries baked data, and the
            // lights below are already collected with includeInactive.
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
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

            // Throws when the scene has no Lighting Settings asset assigned. Read it once so that is one
            // clear failure instead of the same exception logged per light.
            var mixedLightingMode = (int)Lightmapping.lightingSettings.mixedBakeMode;

            foreach (Light light in lights)
            {
                if (light == null) continue;

                lightInfos.Add(new PrefabLightmapData.LightInfo
                {
                    light = light,
                    lightmapBakeType = (int)light.lightmapBakeType,
                    mixedLightingMode = mixedLightingMode
                });
                processedCount++;
            }

            Debug.Log($"[PrefabLightmapData] Processed {processedCount} lights");
        }
    }
}
#endif
