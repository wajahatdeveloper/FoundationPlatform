using System;

namespace AetherNexus.FoundationPlatform.Utilities.Menus
{
    /// <summary>
    /// Marks a <c>[MenuItem]</c> method or a <c>[CreateAssetMenu]</c> ScriptableObject as a
    /// designer-discoverable feature. Sits beside the <c>[MenuItem]</c> on the same method, or on
    /// the asset type itself; the Feature Finder indexes both so a designer can reach a tool by
    /// typing what they want to do ("sword hand grip") instead of knowing the menu label. Untagged
    /// entries still appear in the Finder, but name-only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DesignerFeatureAttribute : Attribute
    {
        /// <param name="title">Intent-first name shown as the result row headline.</param>
        /// <param name="blurb">One sentence: what a designer accomplishes with this.</param>
        /// <param name="keywords">Space-separated designer vocabulary, including the words a
        /// designer would guess rather than the ones the code uses.</param>
        /// <param name="doc">Repo-relative doc path, or <c>""</c> when there is none.</param>
        public DesignerFeatureAttribute(string title, string blurb, string keywords, DesignerFeatureKind kind, string doc)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("DesignerFeature requires a title.", nameof(title));
            if (string.IsNullOrWhiteSpace(blurb))
                throw new ArgumentException($"DesignerFeature '{title}' requires a blurb.", nameof(blurb));
            if (string.IsNullOrWhiteSpace(keywords))
                throw new ArgumentException($"DesignerFeature '{title}' requires keywords.", nameof(keywords));
            if (doc == null)
                throw new ArgumentException($"DesignerFeature '{title}' must pass \"\" for doc when there is none.", nameof(doc));

            Title = title;
            Blurb = blurb;
            Keywords = keywords;
            Kind = kind;
            Doc = doc;
        }

        public string Title { get; }
        public string Blurb { get; }
        public string Keywords { get; }
        public DesignerFeatureKind Kind { get; }
        public string Doc { get; }
    }
}
