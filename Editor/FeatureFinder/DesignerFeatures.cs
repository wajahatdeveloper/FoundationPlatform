#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
    /// <summary>
    /// Title-based lookup into the Feature Finder index, so reports and help text can name a tool without
    /// hardcoding its menu path. A title that no longer exists throws instead of printing stale advice.
    /// </summary>
    public static class DesignerFeatures
    {
        public static string MenuPathFor(string title)
        {
            IReadOnlyList<FeatureEntry> entries = FeatureCatalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].IsTagged && string.Equals(entries[i].Title, title, StringComparison.Ordinal))
                    return entries[i].MenuPath;
            }
            throw new KeyNotFoundException($"No [DesignerFeature] titled '{title}'. Update the caller to a current feature title.");
        }

        /// <summary>"Title (Menu ▸ Path)" for use in user-facing hints.</summary>
        public static string Describe(string title)
        {
            return title + " (" + MenuPathFor(title).Replace("/", " ▸ ") + ")";
        }
    }
}
#endif
