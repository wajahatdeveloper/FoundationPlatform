#if UNITY_EDITOR
using System;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
    /// <summary>
    /// One row in the Feature Finder. Built either from a <see cref="DesignerFeatureAttribute"/>
    /// (<see cref="IsTagged"/> true, full metadata) or from a bare <c>[MenuItem]</c> path, so gaps in
    /// tagging stay visible instead of silently hiding a tool. Contributors from other assemblies
    /// build entries through <see cref="IFeatureCatalogSource"/>.
    /// </summary>
    public readonly struct FeatureEntry
    {
        /// <param name="menuPath">Menu path for a <c>[MenuItem]</c> entry, or "" for a contributed one.</param>
        /// <param name="detail">Secondary line under the path (mapped folder, mapping state, ...), or "".</param>
        /// <param name="sortBias">Added to the relevance score so a source can rank its own entries.</param>
        /// <param name="invoke">What the row's primary button does.</param>
        public FeatureEntry(string menuPath, string title, string blurb, string keywords,
            DesignerFeatureKind kind, string doc, bool isTagged, string detail, int sortBias, Action invoke)
        {
            if (invoke == null)
                throw new ArgumentNullException(nameof(invoke), $"Feature '{title}' has no invoke action.");

            MenuPath = menuPath;
            Title = title;
            Blurb = blurb;
            Keywords = keywords;
            Kind = kind;
            Doc = doc;
            IsTagged = isTagged;
            Detail = detail;
            SortBias = sortBias;
            Invoke = invoke;
            // Lowercased once at build time: scoring runs over every entry on every keystroke, so
            // folding case per comparison would allocate a string per entry per query word.
            TitleLower = title.ToLowerInvariant();
            SearchHaystack = (title + " " + keywords + " " + menuPath + " " + detail).ToLowerInvariant();
            Key = menuPath.Length > 0 ? menuPath : title;
        }

        public string MenuPath { get; }
        public string Title { get; }
        public string Blurb { get; }
        public string Keywords { get; }
        public DesignerFeatureKind Kind { get; }
        public string Doc { get; }
        public bool IsTagged { get; }
        public string Detail { get; }
        public int SortBias { get; }
        public Action Invoke { get; }

        internal string TitleLower { get; }
        internal string SearchHaystack { get; }

        /// <summary>Stable identity across catalog rebuilds, for remembering recently used rows.</summary>
        internal string Key { get; }
    }
}
#endif
