using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

namespace AetherNexus.FoundationPlatform.Tools
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

        // Public (not private) so the editor-only baking pipeline in
        // Editor/Tools/PrefabLightmapGenerator/PrefabLightmapBaker.cs — a separate assembly — can
        // assign baked data directly, without needing an InternalsVisibleTo/reflection workaround.
        [Header("Lightmap Data")]
        [SerializeField] public RendererInfo[] rendererInfos = new RendererInfo[0];
        [SerializeField] public Texture2D[] lightmaps = new Texture2D[0];
        [SerializeField] public Texture2D[] lightmapsDir = new Texture2D[0];
        [SerializeField] public Texture2D[] shadowMasks = new Texture2D[0];
        [SerializeField] public LightInfo[] lightInfos = new LightInfo[0];
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
    }
}
