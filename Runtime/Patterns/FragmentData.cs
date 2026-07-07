using System;
using Framework.Inspector;
using UnityEngine;

namespace FoundationPlatform
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
    public class FragmentData<TConfig, TPayload>
        where TConfig : ScriptableObject, IFragmentConfig<TPayload>
        where TPayload : class, new()
    {
        [BoxGroup("Frag", false)]
        [BoxGroup("Frag/SrcBox", ShowLabel = false)]
        [HorizontalGroup("Frag/SrcBox/Src")]
        [GUIColor(0.55f, 0.55f, 0.6f)]
        [LabelWidth(100)]
        [SerializeField] private FragmentSource source = FragmentSource.Shared;

        [BoxGroup("Frag")]
        [LabelWidth(100)]
        [ShowIf(nameof(source), FragmentSource.Shared)]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [SerializeField] private TConfig shared;

        [BoxGroup("Frag")]
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
        [HorizontalGroup("Frag/SrcBox/Src", Width = 170)]
        [GUIColor(0.55f, 0.55f, 0.6f)]
        [ShowIf(nameof(source), FragmentSource.InlineCustom)]
        [Button("Promote to Shared Asset")]
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
