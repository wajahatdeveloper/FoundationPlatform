#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.CreateMenus.Editor
{
    /// <summary>Why a first-party <c>ScriptableObject</c> does or does not belong in <c>Assets ▸ Create</c>.</summary>
    internal enum CreateMenuVerdict
    {
        /// <summary>The type already declares <c>[CreateAssetMenu]</c>.</summary>
        AlreadySet,

        /// <summary>A designer authors this asset by hand, so a missing create entry is a finding.</summary>
        Authoring,

        /// <summary>A section of a larger config asset, never a standalone asset on disk.</summary>
        ConfigSection,

        /// <summary>Only ever instantiated by an editor generator, which owns how many exist.</summary>
        GeneratorOwned,

        /// <summary>No signal either way. Reported so the gap stays visible instead of silently excluded.</summary>
        Unclassified,
    }
}
#endif
