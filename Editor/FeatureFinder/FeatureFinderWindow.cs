#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
    /// <summary>
    /// Search-first index of every project-owned menu entry. Exists because a designer who does not
    /// already know a tool's name cannot find it in a menu tree: here they type what they want to do
    /// ("sword hand grip") and get the tool that does it.
    /// </summary>
    internal sealed class FeatureFinderWindow : EditorWindow
    {
        private const string SearchControlName = "FeatureFinderSearch";

        // Rows are variable height (wrapped blurb), so there is no cheap way to cull off-screen ones.
        // Nobody scrolls past the fortieth hit anyway - narrowing the query is the faster path.
        private const int MaxVisibleRows = 40;

        private static readonly string[] KindTabs = { "All", "Windows", "Assets", "Actions", "Generators", "Validation", "Debug" };
        private static readonly Color RecentAccent = new Color(0.36f, 0.62f, 0.47f, 1f);
        private static readonly GUIContent ChipContent = new GUIContent();
        private static readonly Comparison<(int score, FeatureEntry entry)> ByScoreDescending =
            (a, b) => b.score.CompareTo(a.score);

        private readonly List<FeatureEntry> _results = new();
        private readonly List<(int score, FeatureEntry entry)> _scored = new();

        private string _query = "";
        private int _kindTab;
        private int _selected;
        private Vector2 _scroll;
        private bool _focusQueued;

        private string _builtQuery;
        private int _builtTab = -1;
        private int _builtVersion = -1;

        private Rect _selectedRect;
        private float _viewportHeight;
        private bool _scrollToSelection;

        [MenuItem(MenuPaths.DomainWindow.FindFeature, false, MenuPriorities.WindowDomainFinder)]
        [DesignerFeature(
            "Find Feature",
            "Search every tool, generator, and authored asset type by what you want to do, instead of hunting through menus for a name you do not know yet.",
            "find search feature tool window palette command lookup discover help what can i do",
            DesignerFeatureKind.Window,
            "docs/09-EditorHub.md")]
        internal static void Open()
        {
            var window = GetWindow<FeatureFinderWindow>(true, "Find Feature");
            window.minSize = new Vector2(520f, 320f);
            window._focusQueued = true;
            window.Focus();
        }

        // Space cannot be expressed in [MenuItem] shortcut syntax, and Shortcut Manager entries are
        // rebindable by the designer under Edit > Shortcuts.
        [Shortcut("Domain/Find Feature", KeyCode.Space, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void OpenViaShortcut() => Open();

        private void OnEnable()
        {
            _focusQueued = true;
        }

        private void OnGUI()
        {
            // Rebuilding only on Layout keeps the row set identical between the layout and repaint
            // passes of a frame, and cuts scoring from once per event to once per frame.
            if (Event.current.type == EventType.Layout) SyncResults();

            HandleKeyboard();
            DrawSearchBar();
            DrawResults();
        }

        private void SyncResults()
        {
            int version = FeatureCatalog.Version;
            if (_builtQuery == _query && _builtTab == _kindTab && _builtVersion == version) return;

            BuildResults();
            _builtQuery = _query;
            _builtTab = _kindTab;
            _builtVersion = FeatureCatalog.Version;
        }

        private void BuildResults()
        {
            _results.Clear();
            _scored.Clear();

            var entries = FeatureCatalog.Entries;
            if (string.IsNullOrWhiteSpace(_query))
            {
                BuildBrowseOrder(entries);
            }
            else
            {
                string[] words = _query.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (!PassesKindFilter(entry)) continue;
                    if (!FeatureFuzzyMatch.TryScore(entry, words, out int score)) continue;

                    _scored.Add((score + entry.SortBias + Recents.Bonus(entry.Key), entry));
                }

                _scored.Sort(ByScoreDescending);
                foreach (var hit in _scored) _results.Add(hit.entry);
            }

            if (_selected >= _results.Count) _selected = Mathf.Max(0, _results.Count - 1);
        }

        // With no query there is nothing to rank by, so the last few things the designer actually ran
        // are the most useful thing to put under the cursor.
        private void BuildBrowseOrder(IReadOnlyList<FeatureEntry> entries)
        {
            var recent = Recents.Keys;
            for (int r = 0; r < recent.Count; r++)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry.Key != recent[r] || !PassesKindFilter(entry)) continue;
                    _results.Add(entry);
                    break;
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!PassesKindFilter(entry)) continue;
                if (Recents.Contains(entry.Key)) continue;
                _results.Add(entry);
            }
        }

        private bool PassesKindFilter(in FeatureEntry entry)
        {
            switch (_kindTab)
            {
                case 0: return true;
                case 1: return entry.Kind == DesignerFeatureKind.Window;
                case 2: return entry.Kind == DesignerFeatureKind.Asset;
                case 3: return entry.Kind == DesignerFeatureKind.Action;
                case 4: return entry.Kind == DesignerFeatureKind.Generator;
                case 5: return entry.Kind == DesignerFeatureKind.Validation;
                default: return entry.Kind == DesignerFeatureKind.Debug;
            }
        }

        private void HandleKeyboard()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.DownArrow:
                    MoveSelection(1);
                    e.Use();
                    break;
                case KeyCode.UpArrow:
                    MoveSelection(-1);
                    e.Use();
                    break;
                case KeyCode.PageDown:
                    MoveSelection(5);
                    e.Use();
                    break;
                case KeyCode.PageUp:
                    MoveSelection(-5);
                    e.Use();
                    break;
                case KeyCode.Home:
                    MoveSelection(-_results.Count);
                    e.Use();
                    break;
                case KeyCode.End:
                    MoveSelection(_results.Count);
                    e.Use();
                    break;
                case KeyCode.Tab:
                    _kindTab = (_kindTab + (e.shift ? KindTabs.Length - 1 : 1)) % KindTabs.Length;
                    _selected = 0;
                    e.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_results.Count > 0) Execute(_results[_selected]);
                    e.Use();
                    break;
                case KeyCode.Escape:
                    // A typo should cost one keypress to undo, not the whole window.
                    if (_query.Length > 0) ClearQuery();
                    else Close();
                    e.Use();
                    break;
            }
        }

        private void MoveSelection(int delta)
        {
            _selected = Mathf.Clamp(_selected + delta, 0, Mathf.Max(0, _results.Count - 1));
            _scrollToSelection = true;
            Repaint();
        }

        private void ClearQuery()
        {
            _query = "";
            _selected = 0;
            _scroll = Vector2.zero;
            _focusQueued = true;
        }

        private void DrawSearchBar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.SetNextControlName(SearchControlName);
                    string typed = EditorGUILayout.TextField(_query, EditorStyles.toolbarSearchField);
                    if (typed != _query)
                    {
                        _query = typed;
                        _selected = 0;
                        _scroll = Vector2.zero;
                    }

                    if (_query.Length > 0 && GUILayout.Button("\u2715", EditorStyles.miniButton, GUILayout.Width(22f)))
                        ClearQuery();

                    // Only needed after adding a menu entry or asset without a domain reload.
                    if (GUILayout.Button("\u21bb", EditorStyles.miniButton, GUILayout.Width(22f)))
                    {
                        FeatureCatalog.Invalidate();
                        _builtVersion = -1;
                    }
                }

                if (_focusQueued)
                {
                    EditorGUI.FocusTextInControl(SearchControlName);
                    _focusQueued = false;
                }

                _kindTab = GuiKit.Toolbar(_kindTab, KindTabs);

                int shown = Mathf.Min(_results.Count, MaxVisibleRows);
                string counts = shown < _results.Count
                    ? $"{shown} of {_results.Count} matches"
                    : $"{_results.Count} of {FeatureCatalog.Entries.Count} features";
                EditorGUILayout.LabelField(
                    $"{counts}   \u2191\u2193 move, Enter open, Tab filter, Esc clear",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawResults()
        {
            if (_results.Count == 0)
            {
                GuiKit.InfoBox($"Nothing matches \"{_query}\". Try a plainer word: \"weapon\", \"hand\", \"spawn\", \"validate\".");
                return;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                int count = Mathf.Min(_results.Count, MaxVisibleRows);
                for (int i = 0; i < count; i++) DrawRow(_results[i], i);

                if (count < _results.Count)
                    EditorGUILayout.LabelField($"{_results.Count - count} more - add a word to narrow it down.",
                        EditorStyles.centeredGreyMiniLabel);
            }

            if (Event.current.type == EventType.Repaint)
            {
                _viewportHeight = GUILayoutUtility.GetLastRect().height;
                ApplyScrollToSelection();
            }
        }

        private void ApplyScrollToSelection()
        {
            if (!_scrollToSelection || _viewportHeight <= 0f) return;
            _scrollToSelection = false;

            float target = _scroll.y;
            if (_selectedRect.yMax > _scroll.y + _viewportHeight) target = _selectedRect.yMax - _viewportHeight;
            else if (_selectedRect.y < _scroll.y) target = _selectedRect.y;

            if (Mathf.Approximately(target, _scroll.y)) return;
            _scroll.y = Mathf.Max(0f, target);
            Repaint();
        }

        private void DrawRow(in FeatureEntry entry, int index)
        {
            bool isSelected = index == _selected;
            var rowStyle = isSelected ? SelectedRow : EditorStyles.helpBox;

            using (new EditorGUILayout.VerticalScope(rowStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(entry.Title, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (Recents.Contains(entry.Key)) DrawChip("recent", RecentAccent);
                    DrawChip(entry.Kind.ToString(), GuiKit.TagAccentFallback);
                    if (IsUnmapped(entry)) DrawChip("unmapped", GuiKit.TagWarningAccent);
                    if (!entry.IsTagged) DrawChip("untagged", GuiKit.TagWarningAccent);
                }

                if (entry.IsTagged) EditorGUILayout.LabelField(entry.Blurb, WrappedMini);
                EditorGUILayout.LabelField(entry.MenuPath, EditorStyles.miniLabel);
                if (entry.Detail.Length > 0) EditorGUILayout.LabelField(entry.Detail, EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    string verb = entry.Kind == DesignerFeatureKind.Asset ? "Create" : "Open";
                    if (GUILayout.Button(verb, GUILayout.Width(70f))) Execute(entry);
                    if (entry.Doc.Length > 0 && GUILayout.Button("Doc", GUILayout.Width(50f))) OpenDoc(entry.Doc);
                    GUILayout.FlexibleSpace();
                }
            }

            var rect = GUILayoutUtility.GetLastRect();
            if (isSelected) _selectedRect = rect;

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selected = index;
                if (Event.current.clickCount == 2) Execute(entry);
                Event.current.Use();
                Repaint();
            }
        }

        private static void DrawChip(string label, Color accent)
        {
            ChipContent.text = label;
            float width = EditorStyles.miniLabel.CalcSize(ChipContent).x + 14f;
            var rect = GUILayoutUtility.GetRect(width, 16f, GUILayout.Width(width));
            GuiKit.TagPill(rect, ChipContent, accent);
        }

        private void Execute(in FeatureEntry entry)
        {
            Recents.Record(entry.Key);
            _builtVersion = -1;

            // Asset rows run inline: the destination picker is a context menu that needs the current
            // GUI event, and staying open lets a designer create several assets in a row.
            if (entry.Kind == DesignerFeatureKind.Asset)
            {
                entry.Invoke();
                return;
            }

            // Everything else opens a window or runs a dialog, so get out of its way first.
            Action invoke = entry.Invoke;
            Close();
            EditorApplication.delayCall += () => invoke();
        }

        private static bool IsUnmapped(in FeatureEntry entry) =>
            entry.Kind == DesignerFeatureKind.Asset &&
            entry.Detail.StartsWith("unmapped", StringComparison.Ordinal);

        private static void OpenDoc(string repoRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string full = Path.Combine(projectRoot, repoRelativePath);
            if (!File.Exists(full))
                throw new FileNotFoundException($"DesignerFeature doc path does not exist: {repoRelativePath}", full);

            Application.OpenURL("file:///" + full.Replace('\\', '/'));
        }

        /// <summary>
        /// Most-recent-first list of executed feature keys, kept in EditorPrefs so it survives domain
        /// reloads and project restarts.
        /// </summary>
        private static class Recents
        {
            private const string PrefKey = "AetherNexus.FeatureFinder.Recent";
            private const int Capacity = 6;
            private const int BonusPerRank = 5;

            private static List<string> _keys;

            internal static IReadOnlyList<string> Keys => Loaded;

            internal static bool Contains(string key) => Loaded.IndexOf(key) >= 0;

            internal static int Bonus(string key)
            {
                int rank = Loaded.IndexOf(key);
                return rank < 0 ? 0 : (Capacity - rank) * BonusPerRank;
            }

            internal static void Record(string key)
            {
                var keys = Loaded;
                keys.Remove(key);
                keys.Insert(0, key);
                if (keys.Count > Capacity) keys.RemoveRange(Capacity, keys.Count - Capacity);

                EditorPrefs.SetString(PrefKey, string.Join("\n", keys));
            }

            private static List<string> Loaded
            {
                get
                {
                    if (_keys == null)
                    {
                        _keys = new List<string>(EditorPrefs.GetString(PrefKey, "")
                            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    return _keys;
                }
            }
        }

        private static GUIStyle _selectedRow;
        private static GUIStyle SelectedRow
        {
            get
            {
                if (_selectedRow == null)
                {
                    _selectedRow = new GUIStyle(EditorStyles.helpBox);
                    var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                    tex.SetPixel(0, 0, new Color(0.24f, 0.42f, 0.68f, 0.35f));
                    tex.Apply();
                    _selectedRow.normal.background = tex;
                }
                return _selectedRow;
            }
        }

        private static GUIStyle _wrappedMini;
        private static GUIStyle WrappedMini
        {
            get
            {
                if (_wrappedMini == null) _wrappedMini = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
                return _wrappedMini;
            }
        }
    }
}
#endif
