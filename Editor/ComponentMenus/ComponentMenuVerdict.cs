#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    /// <summary>Where a component belongs in <c>Add Component</c>, as proposed by the classifier and confirmed in review.</summary>
    internal enum ComponentMenuVerdict
    {
        /// <summary>The type already declares <c>[AddComponentMenu]</c>; the pass leaves it alone.</summary>
        AlreadySet,

        /// <summary>A designer adds this by hand, so it gets a menu path.</summary>
        DesignerPlaced,

        /// <summary>A sidecar or code-added component, so it gets <c>[AddComponentMenu("")]</c> and disappears from the menu.</summary>
        Internal,
    }
}
#endif
