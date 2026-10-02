using System;
using AetherNexus.FoundationPlatform.AetherInspector;
using UnityEngine;

namespace AetherNexus.FoundationPlatform
{
    public enum FragmentSource
    {
        InlineCustom,
        Shared
    }

    public interface IFragmentConfig<out TPayload>
    {
        TPayload Payload { get; }
    }

    [Serializable]
    [HeaderMember("PromoteToShared", nameof(source))]
    public class FragmentData<TConfig, TPayload>
        where TConfig : ScriptableObject, IFragmentConfig<TPayload>
        where TPayload : class, new()
    {
        [Tooltip("Shared: use a reusable asset. Inline Custom: author the values on this field only.")]
        [SerializeField] private FragmentSource source = FragmentSource.Shared;

        [LabelText("Asset")]
        [ShowIf(nameof(source), FragmentSource.Shared)]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [SerializeField] private TConfig shared;

        [HideLabel]
        [ShowIf(nameof(source), FragmentSource.InlineCustom)]
        [InlineProperty]
        [SerializeField] private TPayload custom = new();

        public FragmentSource Source => source;
        public TConfig Shared => shared;
        public TPayload Custom => custom;

        public TPayload Value => source == FragmentSource.Shared
            ? ((UnityEngine.Object)shared != null ? shared.Payload : null)
            : custom;

        public void SetShared(TConfig config)
        {
            shared = config;
            source = FragmentSource.Shared;
        }

#if UNITY_EDITOR
        [ShowIf(nameof(source), FragmentSource.InlineCustom)]
        [Button("Promote to Shared")]
        private void PromoteToShared()
        {
            string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Save Shared " + typeof(TConfig).Name,
                "NewShared" + typeof(TConfig).Name,
                "asset",
                "Enter a name for the new asset.");
            if (string.IsNullOrEmpty(path))
                return;

            var asset = ScriptableObject.CreateInstance<TConfig>();
            InitConfigFromCustom(asset, custom);

            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();

            shared = asset;
            source = FragmentSource.Shared;
        }

        protected virtual void InitConfigFromCustom(TConfig config, TPayload customPayload) { }
#endif
    }
}
