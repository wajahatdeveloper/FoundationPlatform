using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.DebugX.ConsoleView;

namespace AetherNexus.FoundationPlatform.DebugX.ConsoleView.Editor
{
    /// <summary>One display row: an entry plus how many identical entries it represents when collapsed.</summary>
    internal struct RowRef
    {
        public ConsoleEntry Entry;
        public int Count;
    }

    /// <summary>
    /// Applies the console's filter state to the store to produce the display row list. Persisted state
    /// (level toggles, collapse, excluded channels, ignore list, saved tabs) lives in the per-project
    /// <see cref="DebugXConsoleSettings"/>; search text/regex are transient session state.
    ///
    /// The search string is parsed once when it changes (never per entry). Plain terms must all match,
    /// "-term" must not match, "prop:Key" / "prop:Key=Value" match structured properties. Building the
    /// row list has an incremental fast path: when only new entries were appended since the last pass
    /// (the common case under log traffic) only those are filtered; any filter change, clear, sort,
    /// compiler-mirror change or collapse+eviction combination falls back to a full rebuild.
    /// </summary>
    internal sealed class ConsoleFilterModel
    {
        private static DebugXConsoleSettings S => DebugXConsoleSettings.Instance;

        public bool ShowLog { get => S.showLog; set { S.showLog = value; Invalidate(); } }
        public bool ShowWarning { get => S.showWarning; set { S.showWarning = value; Invalidate(); } }
        public bool ShowError { get => S.showError; set { S.showError = value; Invalidate(); } }
        public bool ShowVerbose { get => S.showVerbose; set { S.showVerbose = value; Invalidate(); } }
        public bool ShowSourceDebugX { get => S.showSourceDebugX; set { S.showSourceDebugX = value; Invalidate(); } }
        public bool ShowSourceUnity { get => S.showSourceUnity; set { S.showSourceUnity = value; Invalidate(); } }
        public bool ShowSourceCompiler { get => S.showSourceCompiler; set { S.showSourceCompiler = value; Invalidate(); } }
        public bool SearchInStack { get => S.searchInStack; set { S.searchInStack = value; Invalidate(); } }
        public bool Collapse { get => S.collapse; set { S.collapse = value; Invalidate(); } }

        public List<string> ExcludedChannels => S.excludedChannels;
        public List<string> IgnoreList => S.ignore;
        public List<FilterTab> Tabs => S.tabs;

        // Transient (not persisted here; the window mirrors them into settings).
        private string _search = "";
        public string Search
        {
            get => _search;
            set
            {
                value ??= "";
                if (_search == value) return;
                _search = value;
                _searchParsed = false;
                Invalidate();
            }
        }

        private bool _useRegex;
        public bool UseRegex
        {
            get => _useRegex;
            set
            {
                if (_useRegex == value) return;
                _useRegex = value;
                _searchParsed = false;
                Invalidate();
            }
        }

        /// <summary>False when regex mode is on and the pattern does not parse (rows are then not filtered).</summary>
        public bool SearchRegexValid { get; private set; } = true;

        /// <summary>Plain include terms of the parsed search — used by the window for match highlighting.</summary>
        public IReadOnlyList<string> IncludeTerms
        {
            get { EnsureSearchParsed(); return _includeTerms; }
        }

        /// <summary>Compiled regex when regex mode is on and valid, otherwise null.</summary>
        public Regex ActiveRegex
        {
            get { EnsureSearchParsed(); return _regex; }
        }

        /// <summary>All channels ever seen or seeded, for the channel dropdown.</summary>
        public readonly SortedSet<string> Channels = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        // --- Parsed search state (rebuilt only when Search/UseRegex change) ---
        private readonly List<string> _includeTerms = new List<string>();
        private readonly List<string> _excludeTerms = new List<string>();
        private readonly List<KeyValuePair<string, string>> _propTerms = new List<KeyValuePair<string, string>>(); // Value null = key must exist
        private bool _searchParsed;
        private bool _hasSearch;
        private Regex _regex;

        // Hash mirror of ExcludedChannels for O(1) lookups during Build.
        private readonly HashSet<string> _excludedSet = new HashSet<string>();

        // --- Incremental-build state ---
        private bool _structureDirty = true;
        private long _consumedSeq;          // ConsoleLogStore.AppendedTotal already folded into the rows
        private long _lastEvictedTotal;
        private int _lastClearCount = -1;
        private int _lastCompilerVersion = -1;
        private int _compilerRowCount;      // rows at the tail of the list that mirror compiler diagnostics
        private readonly Dictionary<string, int> _collapseIndex = new Dictionary<string, int>(); // ring rows only
        private readonly List<RowRef> _newRows = new List<RowRef>(); // scratch for incremental appends

        public void Save()
        {
            S.Save();
            // Callers mutate the exposed lists (ExcludedChannels/IgnoreList/Tabs) directly and then
            // Save() — treat every save as a potential filter change.
            Invalidate();
        }

        /// <summary>Forces the next Build to run the full (non-incremental) path.</summary>
        public void Invalidate() => _structureDirty = true;

        public void SeedChannels(IEnumerable<string> known)
        {
            foreach (var c in known)
                if (!string.IsNullOrEmpty(c)) Channels.Add(c);
        }

        public bool CategoryEnabled(ConsoleCategory c)
        {
            switch (c)
            {
                case ConsoleCategory.Warning: return ShowWarning;
                case ConsoleCategory.Error: return ShowError;
                default: return ShowLog;
            }
        }

        /// <summary>
        /// Rebuilds (or incrementally extends) the filtered row list from the store.
        /// Pass allowIncremental=false whenever the caller re-orders rows afterwards (active sort).
        /// </summary>
        public void Build(List<RowRef> outRows, bool allowIncremental)
        {
            EnsureSearchParsed();
            SyncExcludedSet();

            if (allowIncremental && !_structureDirty && TryBuildIncremental(outRows))
                return;

            BuildFull(outRows);
        }

        /// <summary>Rebuilds without incremental extension.</summary>
        public void Build(List<RowRef> outRows) => Build(outRows, false);

        private void SyncExcludedSet()
        {
            _excludedSet.Clear();
            var list = ExcludedChannels;
            for (int i = 0; i < list.Count; i++)
                if (!string.IsNullOrEmpty(list[i])) _excludedSet.Add(list[i]);
        }

        private void BuildFull(List<RowRef> outRows)
        {
            outRows.Clear();
            _collapseIndex.Clear();

            int ringCount = ConsoleLogStore.Count;
            for (int i = 0; i < ringCount; i++)
                ConsiderRing(ConsoleLogStore.Get(i), outRows, outRows, 0);

            // Compiler diagnostics sit at the tail and collapse only among themselves; their mirror is
            // rebuilt wholesale on change (CompilerVersion), so their collapse dict is not retained.
            int compilerStart = outRows.Count;
            Dictionary<string, int> compilerIndex = Collapse ? new Dictionary<string, int>() : null;
            var compiler = ConsoleLogStore.CompilerEntries;
            for (int i = 0; i < compiler.Count; i++)
            {
                var e = compiler[i];
                if (e == null) continue;
                if (!string.IsNullOrEmpty(e.Channel)) Channels.Add(e.Channel);
                if (!Passes(e)) continue;

                if (compilerIndex != null)
                {
                    string key = e.CollapseKey ?? e.Message ?? "";
                    if (compilerIndex.TryGetValue(key, out int idx))
                    {
                        var row = outRows[idx];
                        row.Count++;
                        row.Entry = e; // show the latest occurrence
                        outRows[idx] = row;
                        continue;
                    }
                    compilerIndex[key] = outRows.Count;
                }
                outRows.Add(new RowRef { Entry = e, Count = 1 });
            }
            _compilerRowCount = outRows.Count - compilerStart;

            _lastClearCount = ConsoleLogStore.ClearCount;
            _lastCompilerVersion = ConsoleLogStore.CompilerVersion;
            _lastEvictedTotal = ConsoleLogStore.EvictedTotal;
            _consumedSeq = ConsoleLogStore.AppendedTotal;
            _structureDirty = false;
        }

        /// <summary>Appends only the ring entries added since the last pass. Returns false when ineligible.</summary>
        private bool TryBuildIncremental(List<RowRef> outRows)
        {
            if (ConsoleLogStore.ClearCount != _lastClearCount) return false;
            if (ConsoleLogStore.CompilerVersion != _lastCompilerVersion) return false;

            long evictedTotal = ConsoleLogStore.EvictedTotal;
            bool hadEviction = evictedTotal != _lastEvictedTotal;
            if (hadEviction && Collapse) return false; // front-drop would shift collapse-dict indices

            int ringCount = ConsoleLogStore.Count;
            long baseSeq = ConsoleLogStore.AppendedTotal - ringCount; // seq of ring index 0
            int startIndex = (int)Math.Max(0, Math.Min(ringCount, _consumedSeq - baseSeq));

            if (hadEviction)
            {
                // Rows are in ascending-Id order in the unsorted view; drop the ones the ring evicted.
                long firstId = ConsoleLogStore.FirstId;
                int ringRows = outRows.Count - _compilerRowCount;
                int drop = 0;
                while (drop < ringRows && outRows[drop].Entry.Id < firstId) drop++;
                if (drop > 0) outRows.RemoveRange(0, drop);
                _lastEvictedTotal = evictedTotal;
            }

            if (startIndex < ringCount)
            {
                _newRows.Clear();
                int baseIndex = outRows.Count - _compilerRowCount; // insertion point (before compiler tail)
                for (int i = startIndex; i < ringCount; i++)
                    ConsiderRing(ConsoleLogStore.Get(i), outRows, _newRows, baseIndex);
                if (_newRows.Count > 0)
                {
                    outRows.InsertRange(baseIndex, _newRows);
                    _newRows.Clear();
                }
            }

            _consumedSeq = ConsoleLogStore.AppendedTotal;
            return true;
        }

        /// <summary>
        /// Filters one ring entry into the row list. Collapse-dict indices &lt; baseIndex refer to
        /// <paramref name="existing"/>, indices ≥ baseIndex refer to <paramref name="added"/> (full
        /// rebuild passes the same list for both with baseIndex 0).
        /// </summary>
        private void ConsiderRing(ConsoleEntry e, List<RowRef> existing, List<RowRef> added, int baseIndex)
        {
            if (e == null) return;

            if (!string.IsNullOrEmpty(e.Channel))
                Channels.Add(e.Channel);

            if (!Passes(e)) return;

            if (Collapse && e.Source != ConsoleSource.Marker)
            {
                string key = e.CollapseKey ?? e.Message ?? "";
                if (_collapseIndex.TryGetValue(key, out int idx))
                {
                    if (idx >= baseIndex)
                    {
                        var row = added[idx - baseIndex];
                        row.Count++;
                        row.Entry = e; // show the latest occurrence (timestamp/stack of the newest hit)
                        added[idx - baseIndex] = row;
                    }
                    else
                    {
                        var row = existing[idx];
                        row.Count++;
                        row.Entry = e;
                        existing[idx] = row;
                    }
                    return;
                }
                _collapseIndex[key] = baseIndex + added.Count;
            }

            added.Add(new RowRef { Entry = e, Count = 1 });
        }

        private bool Passes(ConsoleEntry e)
        {
            if (e.Source == ConsoleSource.Marker) return true; // dividers ignore all filters

            if (!SourceEnabled(e.Source)) return false;
            if (!CategoryEnabled(e.Category)) return false;
            if (!ShowVerbose && e.Level <= LogLevel.Debug) return false;
            if (!string.IsNullOrEmpty(e.Channel) && _excludedSet.Contains(e.Channel)) return false;
            if (IsIgnored(e.Message)) return false;
            if (!MatchesSearch(e)) return false;
            return true;
        }

        private bool SourceEnabled(ConsoleSource source)
        {
            switch (source)
            {
                case ConsoleSource.Unity: return ShowSourceUnity;
                case ConsoleSource.Compiler: return ShowSourceCompiler;
                default: return ShowSourceDebugX;
            }
        }

        private bool IsIgnored(string message)
        {
            if (string.IsNullOrEmpty(message) || IgnoreList.Count == 0) return false;
            for (int i = 0; i < IgnoreList.Count; i++)
            {
                var term = IgnoreList[i];
                if (!string.IsNullOrEmpty(term) &&
                    message.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        // --- Search ---

        private void EnsureSearchParsed()
        {
            if (_searchParsed) return;
            _searchParsed = true;

            _includeTerms.Clear();
            _excludeTerms.Clear();
            _propTerms.Clear();
            _regex = null;
            SearchRegexValid = true;
            _hasSearch = !string.IsNullOrEmpty(_search);
            if (!_hasSearch) return;

            if (_useRegex)
            {
                try { _regex = new Regex(_search, RegexOptions.IgnoreCase); }
                catch { SearchRegexValid = false; }
                return;
            }

            foreach (var raw in _search.Split(' '))
            {
                if (string.IsNullOrEmpty(raw)) continue;

                if (raw.StartsWith("prop:", StringComparison.OrdinalIgnoreCase) && raw.Length > 5)
                {
                    string body = raw.Substring(5);
                    int eq = body.IndexOf('=');
                    if (eq > 0)
                        _propTerms.Add(new KeyValuePair<string, string>(body.Substring(0, eq), body.Substring(eq + 1)));
                    else
                        _propTerms.Add(new KeyValuePair<string, string>(body, null));
                }
                else if (raw[0] == '-' && raw.Length > 1)
                {
                    _excludeTerms.Add(raw.Substring(1));
                }
                else
                {
                    _includeTerms.Add(raw);
                }
            }

            _hasSearch = _includeTerms.Count > 0 || _excludeTerms.Count > 0 || _propTerms.Count > 0;
        }

        private bool MatchesSearch(ConsoleEntry e)
        {
            if (!_hasSearch) return true;

            if (_useRegex)
            {
                if (_regex == null) return true; // invalid pattern: don't hide everything
                if (!string.IsNullOrEmpty(e.Message) && _regex.IsMatch(e.Message)) return true;
                if (!string.IsNullOrEmpty(e.Channel) && _regex.IsMatch(e.Channel)) return true;
                if (SearchInStack)
                {
                    if (!string.IsNullOrEmpty(e.RawStackTrace) && _regex.IsMatch(e.RawStackTrace)) return true;
                    if (!string.IsNullOrEmpty(e.ExceptionText) && _regex.IsMatch(e.ExceptionText)) return true;
                }
                return false;
            }

            for (int i = 0; i < _includeTerms.Count; i++)
                if (!ContainsTerm(e, _includeTerms[i])) return false;

            for (int i = 0; i < _excludeTerms.Count; i++)
                if (ContainsTerm(e, _excludeTerms[i])) return false;

            for (int i = 0; i < _propTerms.Count; i++)
                if (!MatchesProperty(e, _propTerms[i].Key, _propTerms[i].Value)) return false;

            return true;
        }

        private bool ContainsTerm(ConsoleEntry e, string term)
        {
            if (!string.IsNullOrEmpty(e.Message) &&
                e.Message.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(e.Channel) &&
                e.Channel.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (SearchInStack)
            {
                if (!string.IsNullOrEmpty(e.RawStackTrace) &&
                    e.RawStackTrace.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(e.ExceptionText) &&
                    e.ExceptionText.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static bool MatchesProperty(ConsoleEntry e, string key, string value)
        {
            var props = e.Properties;
            if (props == null || props.Length == 0) return false;

            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (string.IsNullOrEmpty(p.Key) ||
                    !string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                if (value == null) return true; // key-exists filter
                string v = p.Value != null ? p.Value.ToString() : "null";
                if (v.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
