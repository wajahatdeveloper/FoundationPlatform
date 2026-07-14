using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.Editor.Tools
{
using FoundationPlatform.DebugX;
    
    /// <summary>
    /// Handles lightmap data preservation for prefabs, allowing lightmaps to be baked once and reused across scenes.
    /// This component automatically applies stored lightmap data when the prefab is instantiated.
    /// </summary>
    [ExecuteInEditMode]
    public class PrefabLightmapData : MonoBehaviour
    {
        #region Constants
        private const int INVALID_LIGHTMAP_INDEX = 0xFFFE;
        private const int NO_LIGHTMAP_INDEX = -1;
        #endregion

        #region Serialized Fields
        [Header("Lightmap Settings")]
        [Tooltip("Reassigns shaders when applying the baked lightmaps. Might conflict with some shaders like transparent HDRP.")]
        [SerializeField] private bool releaseShaders = true;

        [Tooltip("Enable debug logging for troubleshooting lightmap issues.")]
        [SerializeField] private bool enableDebugLogging = false;

        [Header("Lightmap Data")]
        [SerializeField] private RendererInfo[] rendererInfos = new RendererInfo[0];
        [SerializeField] private Texture2D[] lightmaps = new Texture2D[0];
        [SerializeField] private Texture2D[] lightmapsDir = new Texture2D[0];
        [SerializeField] private Texture2D[] shadowMasks = new Texture2D[0];
        [SerializeField] private LightInfo[] lightInfos = new LightInfo[0];
        #endregion

        #region Private Fields
        private bool isInitialized = false;
        private static readonly Dictionary<string, Shader> shaderCache = new Dictionary<string, Shader>();
        #endregion

        #region Data Structures
        [System.Serializable]
        public struct RendererInfo
        {
            public Renderer renderer;
            public int lightmapIndex;
            public Vector4 lightmapOffsetScale;

            public bool IsValid => renderer != null && lightmapIndex >= 0;
        }

        [System.Serializable]
        public struct LightInfo
        {
            public Light light;
            public int lightmapBakeType;
            public int mixedLightingMode;

            public bool IsValid => light != null;
        }
        #endregion


        #region Unity Lifecycle
        private void Awake()
        {
            InitializeLightmapData();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            // Reset initialization flag when data changes in editor
            isInitialized = false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually initialize lightmap data. Useful for runtime prefab instantiation.
        /// </summary>
        public void InitializeLightmapData()
        {
            if (isInitialized) return;

            if (!ValidateLightmapData())
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"[PrefabLightmapData] Invalid lightmap data on {gameObject.name}", this);
                return;
            }

            try
            {
                ApplyLightmapData();
                isInitialized = true;

                if (enableDebugLogging)
                    DebugX.Debug($"[PrefabLightmapData] Successfully initialized lightmap data for {gameObject.name}", this);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PrefabLightmapData] Failed to initialize lightmap data: {e.Message}", this);
            }
        }

        /// <summary>
        /// Resets the initialization state, forcing re-initialization on next call.
        /// </summary>
        public void ResetInitialization()
        {
            isInitialized = false;
        }
#if UNITY_EDITOR
		[ContextMenu("Reset Initialization")]
		private void ContextMenuResetInitialization()
		{
			ResetInitialization();
		}
#endif
        #endregion

        #region Private Methods
        /// <summary>
        /// Validates that all lightmap data arrays are properly configured.
        /// </summary>
        private bool ValidateLightmapData()
        {
            if (rendererInfos == null || rendererInfos.Length == 0)
            {
                if (enableDebugLogging)
                    Debug.LogWarning("[PrefabLightmapData] No renderer information available");
                return false;
            }

            if (lightmaps == null || lightmaps.Length == 0)
            {
                if (enableDebugLogging)
                    Debug.LogWarning("[PrefabLightmapData] No lightmap textures available");
                return false;
            }

            // Validate array length consistency
            if (lightmapsDir != null && lightmapsDir.Length != lightmaps.Length)
            {
                Debug.LogWarning($"[PrefabLightmapData] LightmapsDir length ({lightmapsDir.Length}) doesn't match Lightmaps length ({lightmaps.Length})", this);
                return false;
            }

            if (shadowMasks != null && shadowMasks.Length != lightmaps.Length)
            {
                Debug.LogWarning($"[PrefabLightmapData] ShadowMasks length ({shadowMasks.Length}) doesn't match Lightmaps length ({lightmaps.Length})", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the stored lightmap data to the current scene's lightmap settings.
        /// </summary>
        private void ApplyLightmapData()
        {
            var currentLightmaps = LightmapSettings.lightmaps;
            var lightmapLookup = CreateLightmapLookup(currentLightmaps);
            var offsetIndexes = new int[lightmaps.Length];
            var newLightmaps = new List<LightmapData>();

            // Process each stored lightmap
            for (int i = 0; i < lightmaps.Length; i++)
            {
                if (lightmapLookup.TryGetValue(lightmaps[i], out int existingIndex))
                {
                    offsetIndexes[i] = existingIndex;
                }
                else
                {
                    offsetIndexes[i] = currentLightmaps.Length + newLightmaps.Count;
                    newLightmaps.Add(CreateLightmapData(i));
                }
            }

            // Combine existing and new lightmaps
            var combinedLightmaps = new LightmapData[currentLightmaps.Length + newLightmaps.Count];
            currentLightmaps.CopyTo(combinedLightmaps, 0);
            newLightmaps.CopyTo(combinedLightmaps, currentLightmaps.Length);

            // Set lightmap mode
            SetLightmapMode();

            // Apply renderer and light information
            ApplyRendererInfo(rendererInfos, offsetIndexes, lightInfos);

            // Update lightmap settings
            LightmapSettings.lightmaps = combinedLightmaps;
        }

        /// <summary>
        /// Creates a lookup dictionary for existing lightmaps for O(1) access.
        /// </summary>
        private Dictionary<Texture2D, int> CreateLightmapLookup(LightmapData[] currentLightmaps)
        {
            var lookup = new Dictionary<Texture2D, int>();
            for (int i = 0; i < currentLightmaps.Length; i++)
            {
                if (currentLightmaps[i].lightmapColor != null)
                {
                    lookup[currentLightmaps[i].lightmapColor] = i;
                }
            }
            return lookup;
        }

        /// <summary>
        /// Creates a LightmapData entry for the specified index.
        /// </summary>
        private LightmapData CreateLightmapData(int index)
        {
            return new LightmapData
            {
                lightmapColor = lightmaps[index],
                lightmapDir = (lightmapsDir != null && index < lightmapsDir.Length) ? lightmapsDir[index] : null,
                shadowMask = (shadowMasks != null && index < shadowMasks.Length) ? shadowMasks[index] : null
            };
        }

        /// <summary>
        /// Sets the appropriate lightmap mode based on available directional data.
        /// </summary>
        private void SetLightmapMode()
        {
            bool hasDirectionalData = lightmapsDir != null && lightmapsDir.Length == lightmaps.Length;
            bool allDirectionalTexturesValid = true;

            if (hasDirectionalData)
            {
                foreach (var dirTexture in lightmapsDir)
                {
                    if (dirTexture == null)
                    {
                        allDirectionalTexturesValid = false;
                        break;
                    }
                }
            }

            LightmapSettings.lightmapsMode = (hasDirectionalData && allDirectionalTexturesValid)
                ? LightmapsMode.CombinedDirectional
                : LightmapsMode.NonDirectional;
        }

        /// <summary>
        /// Handles scene loading events to reinitialize lightmap data.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset initialization state when scene loads
            isInitialized = false;
            InitializeLightmapData();
        }

        /// <summary>
        /// Applies lightmap data to renderers and lights with proper validation and error handling.
        /// </summary>
        private void ApplyRendererInfo(RendererInfo[] rendererInfos, int[] lightmapOffsetIndex, LightInfo[] lightInfos)
        {
            if (rendererInfos == null || lightmapOffsetIndex == null)
            {
                Debug.LogError("[PrefabLightmapData] Invalid parameters for ApplyRendererInfo", this);
                return;
            }

            // Apply renderer information
            for (int i = 0; i < rendererInfos.Length; i++)
            {
                var info = rendererInfos[i];
                if (!info.IsValid)
                {
                    if (enableDebugLogging)
                        Debug.LogWarning($"[PrefabLightmapData] Invalid renderer info at index {i}", this);
                    continue;
                }

                // Validate lightmap index bounds
                if (info.lightmapIndex < 0 || info.lightmapIndex >= lightmapOffsetIndex.Length)
                {
                    Debug.LogError($"[PrefabLightmapData] Invalid lightmap index {info.lightmapIndex} for renderer {info.renderer.name}", this);
                    continue;
                }

                try
                {
                    info.renderer.lightmapIndex = lightmapOffsetIndex[info.lightmapIndex];
                    info.renderer.lightmapScaleOffset = info.lightmapOffsetScale;

                    if (releaseShaders)
                    {
                        ReassignShaders(info.renderer.sharedMaterials);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PrefabLightmapData] Failed to apply renderer info for {info.renderer.name}: {e.Message}", this);
                }
            }

            // Apply light information
            if (lightInfos != null)
            {
                for (int i = 0; i < lightInfos.Length; i++)
                {
                    var lightInfo = lightInfos[i];
                    if (!lightInfo.IsValid)
                    {
                        if (enableDebugLogging)
                            Debug.LogWarning($"[PrefabLightmapData] Invalid light info at index {i}", this);
                        continue;
                    }

                    try
                    {
                        var bakingOutput = new LightBakingOutput
                        {
                            isBaked = true,
                            lightmapBakeType = (LightmapBakeType)lightInfo.lightmapBakeType,
                            mixedLightingMode = (MixedLightingMode)lightInfo.mixedLightingMode
                        };

                        lightInfo.light.bakingOutput = bakingOutput;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[PrefabLightmapData] Failed to apply light info for {lightInfo.light.name}: {e.Message}", this);
                    }
                }
            }
        }

        /// <summary>
        /// Optimized shader reassignment with caching to avoid repeated Shader.Find calls.
        /// </summary>
        private void ReassignShaders(Material[] materials)
        {
            if (materials == null) return;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                var sh = materials[i].shader;
                if (sh == null) continue;
                string shaderName = sh.name;
                if (string.IsNullOrEmpty(shaderName)) continue;

                // Use cached shader lookup
                if (!shaderCache.TryGetValue(shaderName, out Shader shader))
                {
                    shader = Shader.Find(shaderName);
                    if (shader != null)
                    {
                        shaderCache[shaderName] = shader;
                    }
                }

                if (shader != null)
                {
                    materials[i].shader = shader;
                }
                else if (enableDebugLogging)
                {
                    Debug.LogWarning($"[PrefabLightmapData] Shader not found: {shaderName}", this);
                }
            }
        }
        #endregion

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// Menu item to bake lightmap data for all PrefabLightmapData components in the scene.
        /// </summary>
        [UnityEditor.MenuItem(MenuPaths.Utilities.BakePrefabLightmaps, false, MenuPriorities.Utilities + 3)]
        public static void GenerateLightmapInfo()
        {
            if (!ValidateLightmappingSettings())
            {
                return;
            }

            try
            {
                UnityEditor.Lightmapping.Bake();
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
            var prefabInstances = FindObjectsByType<PrefabLightmapData>(FindObjectsSortMode.None);
            
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
            var rendererInfos = new List<RendererInfo>();
            var lightmaps = new List<Texture2D>();
            var lightmapsDir = new List<Texture2D>();
            var shadowMasks = new List<Texture2D>();
            var lightInfos = new List<LightInfo>();

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
        private static void GenerateLightmapInfo(GameObject root, List<RendererInfo> rendererInfos, List<Texture2D> lightmaps,
            List<Texture2D> lightmapsDir, List<Texture2D> shadowMasks, List<LightInfo> lightInfos)
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

        /// <summary>
        /// Processes all MeshRenderers in the hierarchy to extract lightmap data.
        /// </summary>
        private static void ProcessRenderers(GameObject root, List<RendererInfo> rendererInfos, List<Texture2D> lightmaps,
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

                    var info = new RendererInfo
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
        private static void ProcessLights(GameObject root, List<LightInfo> lightInfos)
        {
            var lights = root.GetComponentsInChildren<Light>(true);
            int processedCount = 0;

            foreach (Light light in lights)
            {
                if (light == null) continue;

                try
                {
                    var lightInfo = new LightInfo
                    {
                        light = light,
                        lightmapBakeType = (int)light.lightmapBakeType
                    };

                    // Get mixed lighting mode based on Unity version
#if UNITY_2020_1_OR_NEWER
                    lightInfo.mixedLightingMode = (int)UnityEditor.Lightmapping.lightingSettings.mixedBakeMode;
#elif UNITY_2018_1_OR_NEWER
                    lightInfo.mixedLightingMode = (int)UnityEditor.LightmapEditorSettings.mixedBakeMode;
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
#endif
        #endregion
    }
}