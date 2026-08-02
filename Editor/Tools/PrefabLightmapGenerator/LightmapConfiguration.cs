using UnityEngine;
using System;

namespace AetherNexus.FoundationPlatform.Editor.Tools
{
    /// <summary>
    /// Configuration settings for PrefabLightmapData behavior.
    /// This ScriptableObject allows for centralized configuration of lightmap settings.
    /// </summary>
    [CreateAssetMenu(fileName = "LightmapConfiguration", menuName = "Lightmap Generator/Lightmap Configuration", order = 30)]
    public class LightmapConfiguration : ScriptableObject
    {
        #region Serialized Fields
        [Header("Default Settings")]
        [Tooltip("Default value for releaseShaders on new PrefabLightmapData components")]
        public bool defaultReleaseShaders = true;

        [Tooltip("Default value for enableDebugLogging on new PrefabLightmapData components")]
        public bool defaultEnableDebugLogging = false;

        [Header("Performance Settings")]
        [Tooltip("Maximum number of lightmaps to process in a single batch")]
        [Range(10, 1000)]
        public int maxLightmapsPerBatch = 100;

        [Tooltip("Enable shader caching to improve performance")]
        public bool enableShaderCaching = true;

        [Tooltip("Maximum number of shaders to cache")]
        [Range(10, 500)]
        public int maxShaderCacheSize = 100;

        [Header("Validation Settings")]
        [Tooltip("Enable strict validation of lightmap data")]
        public bool enableStrictValidation = true;

        [Tooltip("Warn about potential performance issues")]
        public bool warnAboutPerformanceIssues = true;

        [Tooltip("Maximum number of renderers to process before showing a warning")]
        [Range(50, 1000)]
        public int maxRenderersWarningThreshold = 200;

        [Header("Editor Settings")]
        [Tooltip("Show detailed progress during lightmap baking")]
        public bool showDetailedProgress = true;

        [Tooltip("Auto-save prefabs after lightmap baking")]
        public bool autoSavePrefabs = true;

        [Tooltip("Create backup before modifying prefabs")]
        public bool createBackupBeforeModification = true;
        #endregion

        #region Static Instance
        private static LightmapConfiguration _instance;
        
        /// <summary>
        /// Gets the current settings instance. Creates a default one if none exists.
        /// </summary>
        public static LightmapConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LightmapConfiguration>("LightmapConfiguration");
                    if (_instance == null)
                    {
                        _instance = CreateDefaultSettings();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Validates the current settings and returns any issues found.
        /// </summary>
        public string[] ValidateSettings()
        {
            var issues = new System.Collections.Generic.List<string>();

            if (maxLightmapsPerBatch <= 0)
                issues.Add("Max lightmaps per batch must be greater than 0");

            if (maxShaderCacheSize <= 0)
                issues.Add("Max shader cache size must be greater than 0");

            if (maxRenderersWarningThreshold <= 0)
                issues.Add("Max renderers warning threshold must be greater than 0");

            return issues.ToArray();
        }

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        public void ResetToDefaults()
        {
            defaultReleaseShaders = true;
            defaultEnableDebugLogging = false;
            maxLightmapsPerBatch = 100;
            enableShaderCaching = true;
            maxShaderCacheSize = 100;
            enableStrictValidation = true;
            warnAboutPerformanceIssues = true;
            maxRenderersWarningThreshold = 200;
            showDetailedProgress = true;
            autoSavePrefabs = true;
            createBackupBeforeModification = true;
        }
        #endregion

        #region Private Methods
        private static LightmapConfiguration CreateDefaultSettings()
        {
            var settings = CreateInstance<LightmapConfiguration>();
            settings.ResetToDefaults();
            return settings;
        }
        #endregion

        #region Unity Lifecycle
        private void OnValidate()
        {
            // Ensure values are within valid ranges. Guarded so a no-op OnValidate (values already
            // in range) doesn't dirty the asset on every domain reload/scene open.
            if (maxLightmapsPerBatch < 1) maxLightmapsPerBatch = 1;
            if (maxShaderCacheSize < 1) maxShaderCacheSize = 1;
            if (maxRenderersWarningThreshold < 1) maxRenderersWarningThreshold = 1;
        }
        #endregion
    }
}
