#if UNITY_EDITOR
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
    /// <summary>
    /// One row in the Feature Finder. Built either from a <see cref="DesignerFeatureAttribute"/>
    /// (<see cref="IsTagged"/> true, full metadata) or from a bare <c>[MenuItem]</c> path, so gaps in
    /// tagging stay visible instead of silently hiding a tool.
    /// </summary>
    internal readonly struct FeatureEntry
    {
        internal FeatureEntry(string menuPath, string title, string blurb, string keywords,
            DesignerFeatureKind kind, string doc, bool isTagged)
        {
            MenuPath = menuPath;
            Title = title;
            Blurb = blurb;
            Keywords = keywords;
            Kind = kind;
            Doc = doc;
            IsTagged = isTagged;
            SearchHaystack = (title + " " + keywords + " " + menuPath).ToLowerInvariant();
        }

        internal string MenuPath { get; }
        internal string Title { get; }
        internal string Blurb { get; }
        internal string Keywords { get; }
        internal DesignerFeatureKind Kind { get; }
        internal string Doc { get; }
        internal bool IsTagged { get; }
        internal string SearchHaystack { get; }
    }
}
#endif
