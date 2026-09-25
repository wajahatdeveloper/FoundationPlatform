using System;

namespace AetherNexus.FoundationPlatform.Attributes
{
    /// <summary>
    /// Where assets of an authored type belong. Patterns are folder paths relative to <c>Assets/Content</c>:
    /// <c>*</c> matches one segment (directly after <c>Domains</c> it must be a registered content area),
    /// <c>**</c> any depth, and a leading <c>***</c> anywhere in the project. A subclass inherits its base's
    /// homes unless it declares its own. Read by editor tooling only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ContentHomeAttribute : Attribute
    {
        public const string AnywhereToken = "***";

        public string[] Patterns { get; }

        public ContentHomeAttribute(params string[] patterns)
        {
            if (patterns == null || patterns.Length == 0)
                throw new ArgumentException("[ContentHome] requires at least one folder pattern.", nameof(patterns));

            for (int i = 0; i < patterns.Length; i++)
                Validate(patterns[i], i);

            Patterns = patterns;
        }

        private static void Validate(string pattern, int index)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("[ContentHome] pattern " + index + " is empty.", nameof(pattern));
            if (pattern.IndexOf('\\') >= 0 || pattern.StartsWith("/") || pattern.EndsWith("/"))
                throw new ArgumentException("[ContentHome] pattern '" + pattern + "' must use '/' separators with no leading or trailing slash.", nameof(pattern));
            if (pattern.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || string.Equals(pattern, "Assets", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("[ContentHome] pattern '" + pattern + "' must be relative to Assets/Content.", nameof(pattern));

            string[] segments = pattern.Split('/');
            for (int s = 0; s < segments.Length; s++)
            {
                string segment = segments[s];
                if (segment.Length == 0)
                    throw new ArgumentException("[ContentHome] pattern '" + pattern + "' has an empty segment.", nameof(pattern));
                if (segment == AnywhereToken)
                {
                    if (s != 0)
                        throw new ArgumentException("[ContentHome] pattern '" + pattern + "': '***' is only valid as the first segment.", nameof(pattern));
                    continue;
                }
                if (segment == "*" || segment == "**")
                    continue;
                if (segment.IndexOf('*') >= 0)
                    throw new ArgumentException("[ContentHome] pattern '" + pattern + "': segment '" + segment + "' mixes a wildcard with text.", nameof(pattern));
            }
        }
    }
}
