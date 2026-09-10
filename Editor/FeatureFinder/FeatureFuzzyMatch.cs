#if UNITY_EDITOR
using System;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
    /// <summary>
    /// Subsequence scoring over title + keywords + menu path. Multi-word queries must match every
    /// word, so "sword hand" narrows instead of widening.
    /// </summary>
    internal static class FeatureFuzzyMatch
    {
        private const int NoMatch = int.MinValue;

        /// <summary>Higher is better. Returns false when the entry should be filtered out.</summary>
        internal static bool TryScore(in FeatureEntry entry, string[] queryWords, out int score)
        {
            score = 0;
            foreach (string word in queryWords)
            {
                int wordScore = ScoreWord(entry, word);
                if (wordScore == NoMatch)
                {
                    score = 0;
                    return false;
                }
                score += wordScore;
            }

            // A tagged entry carries a curated blurb and keywords, so it is the better answer when
            // an untagged menu path happens to score the same.
            if (entry.IsTagged) score += 25;
            return true;
        }

        private static int ScoreWord(in FeatureEntry entry, string word)
        {
            string title = entry.TitleLower;

            if (title.StartsWith(word, StringComparison.Ordinal)) return 100;
            if (ContainsWholeWord(title, word)) return 80;
            if (title.Contains(word)) return 60;

            string haystack = entry.SearchHaystack;
            if (ContainsWholeWord(haystack, word)) return 45;
            if (haystack.Contains(word)) return 30;

            return IsSubsequence(haystack, word) ? 10 : NoMatch;
        }

        private static bool ContainsWholeWord(string haystack, string word)
        {
            int from = 0;
            while (true)
            {
                int at = haystack.IndexOf(word, from, StringComparison.Ordinal);
                if (at < 0) return false;

                bool leftOk = at == 0 || !char.IsLetterOrDigit(haystack[at - 1]);
                int end = at + word.Length;
                bool rightOk = end >= haystack.Length || !char.IsLetterOrDigit(haystack[end]);
                if (leftOk && rightOk) return true;

                from = at + 1;
            }
        }

        private static bool IsSubsequence(string haystack, string word)
        {
            int w = 0;
            for (int i = 0; i < haystack.Length && w < word.Length; i++)
            {
                if (haystack[i] == word[w]) w++;
            }
            return w == word.Length;
        }
    }
}
#endif
