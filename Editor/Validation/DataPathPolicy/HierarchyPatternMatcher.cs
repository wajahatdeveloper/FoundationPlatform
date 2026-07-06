#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace FoundationPlatform.Editor.Utilities
{
    public static class HierarchyPatternMatcher
    {
        public static bool Match(
            string assetPath,
            string pattern,
            IReadOnlyList<string> knownDomains,
            out string capturedDomain,
            out string reason)
        {
            capturedDomain = string.Empty;
            reason = string.Empty;

            string normalizedAsset = DataFolderMappingPathUtility.NormalizeAssetPath(assetPath);
            string normalizedPattern = DataFolderMappingPathUtility.NormalizeAssetPath(pattern);
            if (string.IsNullOrWhiteSpace(normalizedAsset) || string.IsNullOrWhiteSpace(normalizedPattern))
            {
                reason = "Asset path or pattern is empty.";
                return false;
            }

            string[] assetSegments = normalizedAsset.Split('/');
            string[] patternSegments = normalizedPattern.Split('/');
            return MatchSegments(assetSegments, 0, patternSegments, 0, knownDomains, ref capturedDomain, out reason);
        }

        public static IEnumerable<string> ExpandConcreteFolders(string pattern, IReadOnlyList<string> knownDomains)
        {
            var result = new List<string>();
            string normalizedPattern = DataFolderMappingPathUtility.NormalizeAssetPath(pattern);
            if (string.IsNullOrWhiteSpace(normalizedPattern))
                return result;

            if (normalizedPattern.IndexOf('*') < 0)
            {
                if (AssetDatabase.IsValidFolder(normalizedPattern))
                    result.Add(normalizedPattern);
                return result;
            }

            string searchRoot = BuildSearchRootBeforeWildcard(normalizedPattern);
            if (string.IsNullOrEmpty(searchRoot) || !AssetDatabase.IsValidFolder(searchRoot))
                return result;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Match(searchRoot, normalizedPattern, knownDomains, out _, out _) && seen.Add(searchRoot))
                result.Add(searchRoot);

            string[] guids = AssetDatabase.FindAssets("t:DefaultAsset", new[] { searchRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string candidate = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!AssetDatabase.IsValidFolder(candidate))
                    continue;
                if (!Match(candidate, normalizedPattern, knownDomains, out _, out _))
                    continue;
                if (seen.Add(candidate))
                    result.Add(candidate);
            }

            return result;
        }

        private static bool MatchSegments(
            string[] assetSegments,
            int assetIndex,
            string[] patternSegments,
            int patternIndex,
            IReadOnlyList<string> knownDomains,
            ref string capturedDomain,
            out string reason)
        {
            reason = string.Empty;

            if (patternIndex == patternSegments.Length)
                return assetIndex == assetSegments.Length;

            string token = patternSegments[patternIndex];
            if (token == "**")
            {
                if (patternIndex == patternSegments.Length - 1)
                    return true;

                for (int i = assetIndex; i <= assetSegments.Length; i++)
                {
                    string capturedClone = capturedDomain;
                    if (MatchSegments(assetSegments, i, patternSegments, patternIndex + 1, knownDomains, ref capturedClone, out reason))
                    {
                        capturedDomain = capturedClone;
                        return true;
                    }
                }

                reason = string.Empty;
                return false;
            }

            if (assetIndex >= assetSegments.Length)
                return false;

            if (token == "*")
            {
                if (IsDomainsWildcardPosition(patternSegments, patternIndex))
                {
                    string domainCandidate = assetSegments[assetIndex];
                    if (!IsKnownDomain(domainCandidate, knownDomains))
                    {
                        reason = "UnknownDomain: '" + domainCandidate + "'";
                        return false;
                    }

                    capturedDomain = domainCandidate;
                }

                return MatchSegments(assetSegments, assetIndex + 1, patternSegments, patternIndex + 1, knownDomains, ref capturedDomain, out reason);
            }

            if (!string.Equals(token, assetSegments[assetIndex], StringComparison.OrdinalIgnoreCase))
                return false;

            return MatchSegments(assetSegments, assetIndex + 1, patternSegments, patternIndex + 1, knownDomains, ref capturedDomain, out reason);
        }

        private static bool IsDomainsWildcardPosition(string[] patternSegments, int patternIndex)
        {
            return patternIndex > 0 &&
                   patternIndex < patternSegments.Length &&
                   string.Equals(patternSegments[patternIndex - 1], "Domains", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsKnownDomain(string domain, IReadOnlyList<string> knownDomains)
        {
            if (knownDomains == null || knownDomains.Count == 0)
                return true;
            for (int i = 0; i < knownDomains.Count; i++)
            {
                string known = knownDomains[i];
                if (string.IsNullOrWhiteSpace(known))
                    continue;
                if (string.Equals(known.Trim(), domain, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string BuildSearchRootBeforeWildcard(string pattern)
        {
            string[] segments = pattern.Split('/');
            var rootSegments = new List<string>();
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                if (segment.IndexOf('*') >= 0)
                    break;
                rootSegments.Add(segment);
            }

            if (rootSegments.Count == 0)
                return string.Empty;
            return string.Join("/", rootSegments);
        }
    }
}
#endif
