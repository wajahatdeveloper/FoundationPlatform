#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Coarse bucket a <see cref="DesignerFeatureAttribute"/> entry falls into. Drives the filter
    /// tabs in the Feature Finder, not any behaviour.
    /// </summary>
    public enum DesignerFeatureKind
    {
        Window,
        Action,
        Generator,
        Validation,
        Debug
    }
}
#endif
