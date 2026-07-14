#if UNITY_EDITOR
namespace FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// One stale MonoBehaviour found in an asset: its script serializes fewer fields than the
    /// asset's YAML still carries. <see cref="OrphanFields"/> are the top-level YAML keys the
    /// current script no longer declares (dropped/renamed without <c>[FormerlySerializedAs]</c>).
    /// </summary>
    public readonly struct StaleFinding
    {
        /// <summary>Project-relative path of the scene/prefab/asset the component lives in.</summary>
        public readonly string AssetPath;

        /// <summary>The MonoBehaviour block's own local file id (<c>--- !u!114 &amp;<b>id</b></c>).</summary>
        public readonly long ComponentFileId;

        /// <summary>The owning GameObject's local file id (<c>m_GameObject: {fileID: <b>id</b>}</c>); 0 for assets.</summary>
        public readonly long GameObjectFileId;

        /// <summary>Full name of the resolved script type, for display.</summary>
        public readonly string TypeName;

        /// <summary>Top-level YAML keys present in the asset but not declared by the current script.</summary>
        public readonly string[] OrphanFields;

        public StaleFinding(string assetPath, long componentFileId, long gameObjectFileId, string typeName, string[] orphanFields)
        {
            AssetPath = assetPath;
            ComponentFileId = componentFileId;
            GameObjectFileId = gameObjectFileId;
            TypeName = typeName;
            OrphanFields = orphanFields;
        }

        public string OrphanList => OrphanFields != null ? string.Join(", ", OrphanFields) : string.Empty;
    }
}
#endif
