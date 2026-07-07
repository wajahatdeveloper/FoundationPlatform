using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FoundationPlatform.DebugX;
using DebugXLogging;
using DebugXLogging.ConsoleView;
using FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DebugXLogging.ConsoleView.Editor
{
    /// <summary>
    /// In-house replacement for Editor Console Pro 3, built on UI Toolkit. Reads the always-on
    /// <see cref="ConsoleLogStore"/> (structured DebugX entries + captured Unity logs + mirrored
    /// compiler diagnostics) and renders a recycled <see cref="ListView"/> with a filter toolbar,
    /// saved search tabs, a detail pane with clickable stack frames and an inline source snippet, and
    /// a Watch panel. Refreshes are coalesced to ~15 Hz off the store's version counter.
    /// </summary>
    public sealed class DebugXConsoleWindow : EditorWindow
    {
        private const string PrefAutoscroll = "DebugXConsole.Autoscroll";
        private const string PrefErrorPause = "DebugXConsole.ErrorPause";
        private const string PrefShowWatch = "DebugXConsole.ShowWatch";

        private enum SortColumn { None, Time, Channel, Count, Message }

        private static readonly (SortColumn Col, string Label, int Width)[] HeaderCols =
        {
            (SortColumn.Time, "Time", 78),
            (SortColumn.Channel, "Channel", 96),
            (SortColumn.Count, "N", 34),
            (SortColumn.Message, "Message", 0),
        };

        private readonly ConsoleFilterModel _filter = new ConsoleFilterModel();
        private readonly List<RowRef> _rows = new List<RowRef>();
        private readonly HashSet<int> _selectedSet = new HashSet<int>();
        private readonly Dictionary<SortColumn, Label> _sortLabels = new Dictionary<SortColumn, Label>();

        private ListView _list;
        private ScrollView _detailScroll;
        private VisualElement _detail;
        private VisualElement _tabsRow;
        private VisualElement _header;
        private ScrollView _listScroll;
        private Label _statusLabel;
        private VisualElement _compilingOverlay;
        private Label _compilingLabel;

        // Lean toolbar: live actions only. Everything set-and-forget lives in the Filter ▾ / ⋯ menus.
        private ToolbarToggle _logToggle, _warnToggle, _errToggle;
        private ToolbarToggle _regexToggle, _pauseToggle;
        private ToolbarButton _filterButton;
        private ToolbarSearchField _searchField;

        // Filter chips row: every row-hiding filter gets a dismissible chip so nothing hides silently.
        private VisualElement _chipsRow;

        // Centered hint shown when filters hide every entry.
        private VisualElement _emptyHint;
        private Label _emptyHintLabel;

        // Search debounce: keystrokes update _pendingSearch; the filter applies ~250ms after typing stops.
        private string _pendingSearch;
        private IVisualElementScheduledItem _searchDebounce;

        // Persists the detail-pane split height after the user finishes dragging (debounced disk write).
        private IVisualElementScheduledItem _detailSizeSaveDebounce;

        // Watch pane (left column of the detail area).
        private ScrollView _watchScroll;
        private VisualElement _watchPane;
        private bool _watchFlashPending;

        // Live row elements for column-resize restyling (recreated whenever the ListView rebuilds).
        private readonly List<VisualElement> _rowPool = new List<VisualElement>();
        private const int CallerLineHeight = 14;

        // Header "N" column parts — only shown while Collapse is on (counts are meaningless otherwise).
        private VisualElement _countHandle;

        private bool _paused;

        private int _lastVersion = -1;
        private long _lastErrorSeen;

        private bool _autoscroll = true;
        private bool _errorPause;
        private bool _showWatch;
        private bool _isPipelineCompiling;

        private SortColumn _sortColumn = SortColumn.None;
        private bool _sortAsc = true;

        // Drag-selection state.
        private int _dragButton = -1;
        private int _dragAnchor;
        private bool _dragMoved;

        [MenuItem(MenuPaths.WindowDomain.DebugXConsole)]
        public static void Open()
        {
            var w = GetWindow<DebugXConsoleWindow>();
            w.titleContent = new GUIContent("DebugX Console");
            w.minSize = new Vector2(560f, 300f);
            w.Show();
        }

        private void OnEnable()
        {
            _autoscroll = EditorPrefs.GetBool(PrefAutoscroll, true);
            _errorPause = EditorPrefs.GetBool(PrefErrorPause, false);
            _showWatch = EditorPrefs.GetBool(PrefShowWatch, false);
            _filter.SeedChannels(KnownChannels());

            // Only errors logged from now on should trigger Error Pause — not ones already in the store.
            _lastErrorSeen = ConsoleLogStore.LastErrorId;

            var s = DebugXConsoleSettings.Instance;
            _sortColumn = (SortColumn)Mathf.Clamp(s.sortColumn, 0, 4);
            _sortAsc = s.sortAsc;
            _filter.Search = s.search ?? "";
            _filter.UseRegex = s.searchRegex;

            // Keep the runtime store flags in sync with the per-project settings.
            ConsoleLogStore.ClearOnPlay = s.clearOnPlay;
            ConsoleLogStore.CaptureCompilerErrors = s.captureCompilerErrors;

            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private void OnDisable()
        {
            _filter.Save();
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }

        private void OnCompilationStarted(object context) => _isPipelineCompiling = true;
        private void OnCompilationFinished(object context) => _isPipelineCompiling = false;

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            root.Add(BuildToolbar());

            // Filter chips: every active row-hiding filter appears here, one click to remove.
            _chipsRow = new VisualElement();
            _chipsRow.style.flexDirection = FlexDirection.Row;
            _chipsRow.style.flexWrap = Wrap.Wrap;
            _chipsRow.style.alignItems = Align.Center;
            _chipsRow.style.flexShrink = 0;
            _chipsRow.style.paddingLeft = 4;
            _chipsRow.style.display = DisplayStyle.None;
            root.Add(_chipsRow);

            _tabsRow = new VisualElement();
            _tabsRow.style.flexDirection = FlexDirection.Row;
            _tabsRow.style.flexShrink = 0;
            root.Add(_tabsRow);
            RebuildTabs();

            _header = BuildHeader();
            root.Add(_header);
            UpdateHeaderArrows();

            // Detail pane is the split's fixed pane — drag the divider to resize; height persists.
            float detailHeight = Mathf.Clamp(DebugXConsoleSettings.Instance.detailPaneHeight, 80, 1000);
            var split = new TwoPaneSplitView(1, detailHeight, TwoPaneSplitViewOrientation.Vertical);
            split.style.flexGrow = 1;

            _list = new ListView
            {
                fixedItemHeight = ItemHeight(),
                selectionType = SelectionType.Multiple,
                makeItem = MakeRow,
                bindItem = BindRow,
                itemsSource = _rows
            };
            _list.style.flexGrow = 1;
            _list.selectionChanged += _ => OnSelectionChanged();
            _list.itemsChosen += _ => OpenSelected();
            _list.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.C) { CopySelected(); evt.StopPropagation(); }
            });
            // Autoscroll while drag-selecting near the top/bottom edge of the list.
            _list.RegisterCallback<PointerMoveEvent>(OnListPointerMoveWhileDragging);

            // Wrap the list so a centered "hidden by filters" hint can overlay it when 0 rows match.
            var listWrap = new VisualElement();
            listWrap.style.flexGrow = 1;
            listWrap.Add(_list);

            _emptyHint = new VisualElement();
            _emptyHint.style.position = Position.Absolute;
            _emptyHint.style.left = 0;
            _emptyHint.style.right = 0;
            _emptyHint.style.top = 0;
            _emptyHint.style.bottom = 0;
            _emptyHint.style.alignItems = Align.Center;
            _emptyHint.style.justifyContent = Justify.Center;
            _emptyHint.style.display = DisplayStyle.None;
            _emptyHintLabel = new Label();
            _emptyHintLabel.style.color = new Color(0.65f, 0.65f, 0.68f);
            _emptyHintLabel.style.fontSize = 13;
            _emptyHintLabel.style.marginBottom = 6;
            _emptyHint.Add(_emptyHintLabel);
            var resetBtn = new Button(ResetFilters) { text = "Reset Filters" };
            _emptyHint.Add(resetBtn);
            listWrap.Add(_emptyHint);

            split.Add(listWrap);

            // Releasing anywhere ends a drag-selection (covers releases outside a row).
            root.RegisterCallback<PointerUpEvent>(_ => _dragButton = -1);

            // Detail area: optional Watch column on the left, entry details on the right.
            var detailArea = new VisualElement();
            detailArea.style.flexDirection = FlexDirection.Row;
            detailArea.style.flexGrow = 1;

            _watchScroll = new ScrollView();
            _watchScroll.style.width = 260;
            _watchScroll.style.flexShrink = 0;
            _watchScroll.style.borderRightWidth = 1;
            _watchScroll.style.borderRightColor = new Color(0f, 0f, 0f, 0.3f);
            _watchScroll.style.display = DisplayStyle.None;
            _watchPane = new VisualElement();
            _watchPane.style.paddingLeft = _watchPane.style.paddingRight = 6;
            _watchPane.style.paddingTop = _watchPane.style.paddingBottom = 4;
            _watchScroll.Add(_watchPane);
            detailArea.Add(_watchScroll);

            _detailScroll = new ScrollView();
            _detailScroll.style.flexGrow = 1;
            _detail = new VisualElement();
            _detail.style.paddingLeft = _detail.style.paddingRight = 6;
            _detail.style.paddingTop = _detail.style.paddingBottom = 4;
            _detailScroll.Add(_detail);
            detailArea.Add(_detailScroll);

            split.Add(detailArea);

            // Persist the dragged split height (debounced: geometry events fire per drag frame).
            _detailSizeSaveDebounce = root.schedule.Execute(() => DebugXConsoleSettings.Instance.Save());
            _detailSizeSaveDebounce.Pause();
            detailArea.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                int h = Mathf.RoundToInt(evt.newRect.height);
                var s = DebugXConsoleSettings.Instance;
                if (h > 50 && s.detailPaneHeight != h)
                {
                    s.detailPaneHeight = h;
                    _detailSizeSaveDebounce.ExecuteLater(500);
                }
            });

            root.Add(split);

            _statusLabel = new Label();
            _statusLabel.style.flexShrink = 0;
            _statusLabel.style.fontSize = 11;
            _statusLabel.style.paddingLeft = 6;
            _statusLabel.style.paddingTop = _statusLabel.style.paddingBottom = 2;
            _statusLabel.style.color = new Color(0.6f, 0.6f, 0.62f);
            _statusLabel.style.borderTopWidth = 1;
            _statusLabel.style.borderTopColor = new Color(0f, 0f, 0f, 0.3f);
            root.Add(_statusLabel);

            _compilingOverlay = BuildCompilingOverlay();
            root.Add(_compilingOverlay); // last child → drawn on top

            // Ctrl+F focuses the search field; F8 / Shift+F8 jump between errors.
            root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey && evt.keyCode == KeyCode.F)
                {
                    var input = _searchField?.Q<TextField>() ?? (VisualElement)_searchField;
                    input?.Focus();
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.F8)
                {
                    JumpError(evt.shiftKey ? -1 : 1);
                    evt.StopPropagation();
                }
            });

            RefreshDetail();
            RefreshWatch();
            UpdateCompilingOverlay();
            root.schedule.Execute(Tick).Every(66);
            Rebuild();
            UpdateSearchValidity(); // the restored search may be an invalid regex
        }

        /// <summary>ListView item height: base row plus the caller line in two-line mode.</summary>
        private static float ItemHeight() =>
            ConsoleColorConfig.RowHeight + (ConsoleColorConfig.TwoLineRows ? CallerLineHeight : 0);

        private void OnListPointerMoveWhileDragging(PointerMoveEvent evt)
        {
            if (_dragButton < 0) return;
            if (_listScroll == null) _listScroll = _list?.Q<ScrollView>();
            var vs = _listScroll?.verticalScroller;
            if (vs == null) return;

            const float edge = 24f;
            // localPosition is relative to the event target (a row), not the list — convert explicitly.
            float y = _list.WorldToLocal((Vector2)evt.position).y;
            float h = _list.resolvedStyle.height;
            if (y < edge)
                vs.value = Mathf.Max(vs.lowValue, vs.value - (edge - y) * 0.5f);
            else if (y > h - edge)
                vs.value = Mathf.Min(vs.highValue, vs.value + (y - (h - edge)) * 0.5f);
        }

        private VisualElement BuildCompilingOverlay()
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            overlay.pickingMode = PickingMode.Ignore;
            overlay.style.display = DisplayStyle.None;

            _compilingLabel = new Label("Compiling…");
            _compilingLabel.style.fontSize = 30;
            _compilingLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _compilingLabel.style.color = Color.white;
            overlay.Add(_compilingLabel);

            return overlay;
        }

        private void UpdateCompilingOverlay()
        {
            if (_compilingOverlay == null) return;
            bool compiling = EditorApplication.isCompiling || _isPipelineCompiling;
            bool importing = EditorApplication.isUpdating;
            bool shouldShow = compiling || importing;

            var targetDisplay = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
            string targetText = compiling ? "Compiling…" : "Importing…";

            bool changed = false;
            if (_compilingOverlay.style.display != targetDisplay)
            {
                _compilingOverlay.style.display = targetDisplay;
                changed = true;
            }
            if (shouldShow && _compilingLabel.text != targetText)
            {
                _compilingLabel.text = targetText;
                changed = true;
            }

            if (changed)
            {
                Repaint();
            }
        }

        private void Update()
        {
            UpdateCompilingOverlay();
        }

        // --- Toolbar ---

        private Toolbar BuildToolbar()
        {
            var bar = new Toolbar();

            // Clear + behaviour dropdown (clear-on-play/build config lives where the action is).
            var clear = new ToolbarButton(() => { ConsoleLogStore.Clear(); _list.ClearSelection(); RefreshDetail(); Rebuild(); }) { text = "Clear" };
            clear.style.flexShrink = 0;
            bar.Add(clear);
            var clearMenuBtn = new ToolbarButton(ShowClearMenu) { text = "▾" };
            clearMenuBtn.tooltip = "Clear options";
            clearMenuBtn.style.flexShrink = 0;
            bar.Add(clearMenuBtn);

            // Search takes all free toolbar space — it's the primary power tool.
            _searchField = new ToolbarSearchField { value = _filter.Search };
            _searchField.style.flexGrow = 1;
            _searchField.style.flexShrink = 1;
            _searchField.style.minWidth = 110;
            _searchField.style.width = new StyleLength(StyleKeyword.Auto);
            _searchField.tooltip = "Space-separated terms (all must match). -term excludes. prop:Key or prop:Key=Value matches structured properties.";
            _searchDebounce = _searchField.schedule.Execute(ApplyPendingSearch);
            _searchDebounce.Pause();
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _pendingSearch = evt.newValue ?? "";
                _searchDebounce.ExecuteLater(250); // debounce: don't rebuild + hit disk per keystroke
            });
            bar.Add(_searchField);

            _regexToggle = MakeToggle(".*", _filter.UseRegex, v => { _filter.UseRegex = v; PersistSearch(); Rebuild(); UpdateSearchValidity(); });
            _regexToggle.tooltip = "Interpret search as a regular expression";
            _regexToggle.style.flexShrink = 0;
            bar.Add(_regexToggle);

            _logToggle = MakeToggle("Log", _filter.ShowLog, v => { _filter.ShowLog = v; _filter.Save(); Rebuild(); });
            _warnToggle = MakeToggle("Warn", _filter.ShowWarning, v => { _filter.ShowWarning = v; _filter.Save(); Rebuild(); });
            _errToggle = MakeToggle("Err", _filter.ShowError, v => { _filter.ShowError = v; _filter.Save(); Rebuild(); });
            AddIcon(_logToggle, "console.infoicon.sml");
            AddIcon(_warnToggle, "console.warnicon.sml");
            AddIcon(_errToggle, "console.erroricon.sml");
            string soloTip = "Click: toggle.  Ctrl/Shift-click: solo (show only this; again = show all).";
            _logToggle.tooltip = _warnToggle.tooltip = _errToggle.tooltip = soloTip;
            AddLevelSolo(_logToggle, ConsoleCategory.Log);
            AddLevelSolo(_warnToggle, ConsoleCategory.Warning);
            AddLevelSolo(_errToggle, ConsoleCategory.Error);
            _logToggle.style.flexShrink = 0;
            _warnToggle.style.flexShrink = 0;
            _errToggle.style.flexShrink = 0;
            bar.Add(_logToggle);
            bar.Add(_warnToggle);
            bar.Add(_errToggle);

            // One menu owns every row-hiding filter; badge shows how many are active.
            _filterButton = new ToolbarButton(ShowFilterMenu) { text = "Filter ▾" };
            _filterButton.tooltip = "Row-hiding filters: verbosity, sources, channels, collapse, ignore list";
            _filterButton.style.flexShrink = 0;
            bar.Add(_filterButton);

            _pauseToggle = MakeToggle("", _paused, v =>
            {
                _paused = v;
                if (!v) { _lastVersion = -1; Tick(); } // resume: catch up immediately
            });
            _pauseToggle.tooltip = "Freeze the view while logs keep buffering in the background";
            AddIcon(_pauseToggle, "PauseButton");
            if (_pauseToggle.Q<Image>() == null) _pauseToggle.text = "Pause"; // icon missing on this Unity version
            _pauseToggle.style.flexShrink = 0;
            bar.Add(_pauseToggle);

            var optionsBtn = new ToolbarButton(BuildOptionsMenu) { text = "⋯" };
            optionsBtn.tooltip = "View options, watch panel, export, settings";
            optionsBtn.style.flexShrink = 0;
            bar.Add(optionsBtn);

            return bar;
        }

        /// <summary>Dropdown next to Clear: clearing behaviour + one-off clears.</summary>
        private void ShowClearMenu()
        {
            var menu = new GenericMenu();
            var s = DebugXConsoleSettings.Instance;
            menu.AddItem(new GUIContent("Clear on Play"), ConsoleLogStore.ClearOnPlay, () =>
            {
                ConsoleLogStore.ClearOnPlay = !ConsoleLogStore.ClearOnPlay;
                s.clearOnPlay = ConsoleLogStore.ClearOnPlay;
                s.Save();
            });
            menu.AddItem(new GUIContent("Clear on Build"), s.clearOnBuild, () =>
            {
                s.clearOnBuild = !s.clearOnBuild;
                s.Save();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Clear Watches"), false, () => { ConsoleLogStore.ClearWatches(); RefreshWatch(); });
            menu.AddItem(new GUIContent("Clear Ignore List"), false, () => { _filter.IgnoreList.Clear(); _filter.Save(); Rebuild(); });
            menu.ShowAsContext();
        }

        /// <summary>The Filter ▾ menu: every option that decides which rows are visible.</summary>
        private void ShowFilterMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Collapse Identical"), _filter.Collapse, () => { _filter.Collapse = !_filter.Collapse; _filter.Save(); Rebuild(); });
            menu.AddItem(new GUIContent("Show Verbose∕Debug"), _filter.ShowVerbose, () => { _filter.ShowVerbose = !_filter.ShowVerbose; _filter.Save(); Rebuild(); });
            menu.AddItem(new GUIContent("Search in Stack Traces"), _filter.SearchInStack, () => { _filter.SearchInStack = !_filter.SearchInStack; _filter.Save(); Rebuild(); });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Sources/DebugX"), _filter.ShowSourceDebugX, () => { _filter.ShowSourceDebugX = !_filter.ShowSourceDebugX; _filter.Save(); Rebuild(); });
            menu.AddItem(new GUIContent("Sources/Unity"), _filter.ShowSourceUnity, () => { _filter.ShowSourceUnity = !_filter.ShowSourceUnity; _filter.Save(); Rebuild(); });
            menu.AddItem(new GUIContent("Sources/Compiler"), _filter.ShowSourceCompiler, () => { _filter.ShowSourceCompiler = !_filter.ShowSourceCompiler; _filter.Save(); Rebuild(); });
            menu.AddItem(new GUIContent("Channels/Show All"), false, () => { _filter.ExcludedChannels.Clear(); _filter.Save(); Rebuild(); });
            menu.AddItem(new GUIContent("Channels/Hide All"), false, () =>
            {
                _filter.ExcludedChannels.Clear();
                foreach (var ch in _filter.Channels) _filter.ExcludedChannels.Add(ch);
                _filter.Save();
                Rebuild();
            });
            menu.AddSeparator("Channels/");
            foreach (var ch in _filter.Channels)
            {
                string c = ch;
                bool shown = !_filter.ExcludedChannels.Contains(c);
                menu.AddItem(new GUIContent("Channels/" + c), shown, () =>
                {
                    if (_filter.ExcludedChannels.Contains(c)) _filter.ExcludedChannels.Remove(c);
                    else _filter.ExcludedChannels.Add(c);
                    _filter.Save();
                    Rebuild();
                });
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Save Filter as Tab"), false, AddTabFromSearch);
            menu.AddItem(new GUIContent("Reset Filters"), false, ResetFilters);
            menu.ShowAsContext();
        }

        /// <summary>Filter-button badge: count of active filters that silently hide rows.</summary>
        private void UpdateFilterBadge()
        {
            if (_filterButton == null) return;
            int n = 0;
            if (!_filter.ShowVerbose) n++;
            if (!_filter.ShowSourceDebugX) n++;
            if (!_filter.ShowSourceUnity) n++;
            if (!_filter.ShowSourceCompiler) n++;
            if (_filter.ExcludedChannels.Count > 0) n++;
            if (_filter.IgnoreList.Count > 0) n++;
            _filterButton.text = n > 0 ? $"Filter ({n}) ▾" : "Filter ▾";
        }

        /// <summary>Resets every tab-owned/row-hiding filter except the curated ignore list.</summary>
        private void ResetFilters()
        {
            DebugXConsoleSettings.Instance.activeTab = -1;
            _filter.Search = "";
            _filter.UseRegex = false;
            _filter.ShowLog = _filter.ShowWarning = _filter.ShowError = true;
            _filter.ShowVerbose = true;
            _filter.ShowSourceDebugX = _filter.ShowSourceUnity = _filter.ShowSourceCompiler = true;
            _filter.ExcludedChannels.Clear();
            _searchField?.SetValueWithoutNotify("");
            _pendingSearch = null;
            PersistSearch();
            SyncToggleValues();
            _filter.Save();
            RebuildTabs();
            Rebuild();
            UpdateSearchValidity();
        }

        private void SyncToggleValues()
        {
            if (_pauseToggle != null) _pauseToggle.SetValueWithoutNotify(_paused);
            if (_regexToggle != null) _regexToggle.SetValueWithoutNotify(_filter.UseRegex);
            if (_logToggle != null) _logToggle.SetValueWithoutNotify(_filter.ShowLog);
            if (_warnToggle != null) _warnToggle.SetValueWithoutNotify(_filter.ShowWarning);
            if (_errToggle != null) _errToggle.SetValueWithoutNotify(_filter.ShowError);
        }

        /// <summary>Applies the debounced search text to the filter and persists it.</summary>
        private void ApplyPendingSearch()
        {
            if (_pendingSearch == null) return;
            _filter.Search = _pendingSearch;
            _pendingSearch = null;
            PersistSearch();
            Rebuild();
            UpdateSearchValidity();
        }

        /// <summary>Tints the search field red while an invalid regex pattern is active.</summary>
        private void UpdateSearchValidity()
        {
            if (_searchField == null) return;
            bool invalid = _filter.UseRegex && !_filter.SearchRegexValid;
            _searchField.style.backgroundColor = invalid
                ? new StyleColor(new Color(0.55f, 0.15f, 0.15f, 0.55f))
                : new StyleColor(StyleKeyword.Null);
            _searchField.tooltip = invalid
                ? "Invalid regular expression — rows are not being filtered"
                : "Space-separated terms (all must match). -term excludes. prop:Key or prop:Key=Value matches structured properties.";
        }

        /// <summary>Ctrl/Shift-click on a level toggle solos that level (intercepted before the toggle flips).</summary>
        private void AddLevelSolo(ToolbarToggle toggle, ConsoleCategory cat)
        {
            toggle.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (!(evt.ctrlKey || evt.commandKey || evt.shiftKey)) return;
                evt.StopImmediatePropagation();
                evt.StopPropagation();
                SoloLevel(cat);
            }, TrickleDown.TrickleDown);
        }

        private void SoloLevel(ConsoleCategory cat)
        {
            int on = (_filter.ShowLog ? 1 : 0) + (_filter.ShowWarning ? 1 : 0) + (_filter.ShowError ? 1 : 0);
            bool onlyThis = GetLevel(cat) && on == 1;

            if (onlyThis)
            {
                _filter.ShowLog = _filter.ShowWarning = _filter.ShowError = true;
            }
            else
            {
                _filter.ShowLog = cat == ConsoleCategory.Log;
                _filter.ShowWarning = cat == ConsoleCategory.Warning;
                _filter.ShowError = cat == ConsoleCategory.Error;
            }

            _logToggle.SetValueWithoutNotify(_filter.ShowLog);
            _warnToggle.SetValueWithoutNotify(_filter.ShowWarning);
            _errToggle.SetValueWithoutNotify(_filter.ShowError);
            _filter.Save();
            Rebuild();
        }

        private bool GetLevel(ConsoleCategory c)
        {
            switch (c)
            {
                case ConsoleCategory.Warning: return _filter.ShowWarning;
                case ConsoleCategory.Error: return _filter.ShowError;
                default: return _filter.ShowLog;
            }
        }

        private static void AddIcon(ToolbarToggle toggle, string iconName)
        {
            var content = EditorGUIUtility.IconContent(iconName);
            if (content == null || content.image == null) return;
            var img = new Image { image = content.image, scaleMode = ScaleMode.ScaleToFit };
            img.style.width = 14;
            img.style.height = 14;
            img.style.marginRight = 2;
            img.style.flexShrink = 0;
            img.style.alignSelf = Align.Center;
            toggle.Insert(0, img);
        }

        private static ToolbarToggle MakeToggle(string text, bool value, System.Action<bool> onChange)
        {
            var t = new ToolbarToggle { text = text, value = value };
            t.RegisterValueChangedCallback(evt => onChange(evt.newValue));
            return t;
        }

        private void BuildOptionsMenu()
        {
            var menu = new GenericMenu();

            // Behaviour (set-and-forget toggles that used to crowd the toolbar).
            menu.AddItem(new GUIContent("Watch Panel"), _showWatch, () =>
            {
                _showWatch = !_showWatch;
                EditorPrefs.SetBool(PrefShowWatch, _showWatch);
                RefreshWatch();
            });
            menu.AddItem(new GUIContent("Pause on Error"), _errorPause, () =>
            {
                _errorPause = !_errorPause;
                EditorPrefs.SetBool(PrefErrorPause, _errorPause);
            });
            menu.AddItem(new GUIContent("Autoscroll"), _autoscroll, () =>
            {
                _autoscroll = !_autoscroll;
                EditorPrefs.SetBool(PrefAutoscroll, _autoscroll);
            });
            menu.AddItem(new GUIContent("Capture Compiler Errors"), ConsoleLogStore.CaptureCompilerErrors, () =>
            {
                ConsoleLogStore.CaptureCompilerErrors = !ConsoleLogStore.CaptureCompilerErrors;
                DebugXConsoleSettings.Instance.captureCompilerErrors = ConsoleLogStore.CaptureCompilerErrors;
                DebugXConsoleSettings.Instance.Save();
                Rebuild();
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Jump to Next Error (F8)"), false, () => JumpError(1));
            menu.AddItem(new GUIContent("Jump to Previous Error (Shift+F8)"), false, () => JumpError(-1));
            menu.AddSeparator("");
            AddExportItems(menu, "Export/");
            menu.AddSeparator("");

            // Appearance.
            AddTimeFormatItem(menu, "Timestamp/None", ConsoleColorConfig.TimeFormat.None);
            AddTimeFormatItem(menu, "Timestamp/Clock", ConsoleColorConfig.TimeFormat.Clock);
            AddTimeFormatItem(menu, "Timestamp/Clock + ms", ConsoleColorConfig.TimeFormat.ClockMillis);
            AddTimeFormatItem(menu, "Timestamp/Delta (since previous)", ConsoleColorConfig.TimeFormat.Delta);
            AddTimeFormatItem(menu, "Timestamp/Frame Number", ConsoleColorConfig.TimeFormat.Frame);
            menu.AddItem(new GUIContent("Alternating Rows"), ConsoleColorConfig.AlternatingRows, () =>
            {
                ConsoleColorConfig.AlternatingRows = !ConsoleColorConfig.AlternatingRows;
                _list.RefreshItems();
            });
            menu.AddItem(new GUIContent("Column Header"), ConsoleColorConfig.ShowHeader, () =>
            {
                ConsoleColorConfig.ShowHeader = !ConsoleColorConfig.ShowHeader;
                if (_header != null)
                    _header.style.display = ConsoleColorConfig.ShowHeader ? DisplayStyle.Flex : DisplayStyle.None;
            });
            menu.AddItem(new GUIContent("Two-Line Rows (caller under message)"), ConsoleColorConfig.TwoLineRows, () =>
            {
                ConsoleColorConfig.TwoLineRows = !ConsoleColorConfig.TwoLineRows;
                ApplyRowHeight();
            });
            menu.AddItem(new GUIContent("Font/Increase"), false, () => { ConsoleColorConfig.FontSize++; RebuildRows(); });
            menu.AddItem(new GUIContent("Font/Decrease"), false, () => { ConsoleColorConfig.FontSize--; RebuildRows(); });
            menu.AddItem(new GUIContent("Font/Reset"), false, () => { ConsoleColorConfig.FontSize = ConsoleColorConfig.DefaultFontSize; RebuildRows(); });
            menu.AddItem(new GUIContent("Row Height/Increase"), false, () => { ConsoleColorConfig.RowHeight += 2; ApplyRowHeight(); });
            menu.AddItem(new GUIContent("Row Height/Decrease"), false, () => { ConsoleColorConfig.RowHeight -= 2; ApplyRowHeight(); });
            menu.AddItem(new GUIContent("Row Height/Reset"), false, () => { ConsoleColorConfig.RowHeight = ConsoleColorConfig.DefaultRowHeight; ApplyRowHeight(); });
            menu.AddItem(new GUIContent("Reset Column Widths"), false, ResetColumnWidths);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open Editor Log Folder"), false, OpenLogFolder);
            menu.AddItem(new GUIContent("Project Settings…"), false, () => SettingsService.OpenProjectSettings("Project/DebugX Console"));
            menu.ShowAsContext();
        }

        private void AddExportItems(GenericMenu menu, string prefix)
        {
            menu.AddItem(new GUIContent(prefix + "All/Text (.txt)"), false, () => ConsoleExport.Export(_rows, ExportFormat.Text));
            menu.AddItem(new GUIContent(prefix + "All/CSV (.csv)"), false, () => ConsoleExport.Export(_rows, ExportFormat.Csv));
            menu.AddItem(new GUIContent(prefix + "All/NDJSON (.json)"), false, () => ConsoleExport.Export(_rows, ExportFormat.Ndjson));

            if (_selectedSet.Count > 0)
            {
                menu.AddItem(new GUIContent(prefix + "Selected/Text (.txt)"), false, () => ConsoleExport.Export(SelectedRows(), ExportFormat.Text));
                menu.AddItem(new GUIContent(prefix + "Selected/CSV (.csv)"), false, () => ConsoleExport.Export(SelectedRows(), ExportFormat.Csv));
                menu.AddItem(new GUIContent(prefix + "Selected/NDJSON (.json)"), false, () => ConsoleExport.Export(SelectedRows(), ExportFormat.Ndjson));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(prefix + "Selected/(no selection)"));
            }
        }

        private void AddTimeFormatItem(GenericMenu menu, string path, ConsoleColorConfig.TimeFormat fmt)
        {
            menu.AddItem(new GUIContent(path), ConsoleColorConfig.TimeStampFormat == fmt, () =>
            {
                ConsoleColorConfig.TimeStampFormat = fmt;
                _list.RefreshItems();
            });
        }

        private static void OpenLogFolder()
        {
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Logs", "Editor"));
            System.IO.Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        // --- Saved tabs ---

        private static readonly Color ActiveTabColor = new Color(0.25f, 0.42f, 0.60f, 0.65f);

        private void RebuildTabs()
        {
            _tabsRow.Clear();
            if (_filter.Tabs.Count == 0) return;

            int active = DebugXConsoleSettings.Instance.activeTab;

            var allBtn = new Button(ApplyAllTab) { text = "All" };
            allBtn.style.marginLeft = 2;
            allBtn.tooltip = "Clear the tab filter (show everything)";
            if (active < 0) allBtn.style.backgroundColor = ActiveTabColor;
            _tabsRow.Add(allBtn);

            for (int i = 0; i < _filter.Tabs.Count; i++)
            {
                int index = i;
                var tab = _filter.Tabs[i];
                var btn = new Button(() => ApplyTab(index)) { text = tab.Name };
                btn.style.marginLeft = 2;
                if (index == active) btn.style.backgroundColor = ActiveTabColor;
                btn.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Rename", _ => StartTabRename(index));
                    evt.menu.AppendAction("Move Left", _ => MoveTab(index, -1),
                        index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Move Right", _ => MoveTab(index, 1),
                        index < _filter.Tabs.Count - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Remove tab", _ => RemoveTab(index));
                }));
                _tabsRow.Add(btn);
            }

            var plus = new Button(AddTabFromSearch) { text = "+" };
            plus.tooltip = "Save the current filter as a tab";
            plus.style.marginLeft = 2;
            _tabsRow.Add(plus);
        }

        /// <summary>"All" pseudo-tab: resets the tab-owned filter state (search, levels, channel exclusions).</summary>
        private void ApplyAllTab()
        {
            DebugXConsoleSettings.Instance.activeTab = -1;
            _filter.Search = "";
            _filter.UseRegex = false;
            _filter.ShowLog = _filter.ShowWarning = _filter.ShowError = true;
            _filter.ExcludedChannels.Clear();
            AfterTabApplied();
        }

        private void ApplyTab(int index)
        {
            if (index < 0 || index >= _filter.Tabs.Count) return;
            var t = _filter.Tabs[index];
            DebugXConsoleSettings.Instance.activeTab = index;

            _filter.Search = t.Search ?? "";
            _filter.UseRegex = t.UseRegex;
            _filter.ShowLog = t.showLog;
            _filter.ShowWarning = t.showWarning;
            _filter.ShowError = t.showError;
            _filter.ExcludedChannels.Clear();
            if (t.excludedChannels != null) _filter.ExcludedChannels.AddRange(t.excludedChannels);
            AfterTabApplied();
        }

        private void AfterTabApplied()
        {
            _searchField?.SetValueWithoutNotify(_filter.Search);
            _pendingSearch = null; // a queued debounced keystroke must not overwrite the tab's search
            PersistSearch();
            SyncToggleValues();
            _filter.Save();
            RebuildTabs();
            Rebuild();
            UpdateSearchValidity();
        }

        private void StartTabRename(int index)
        {
            if (index < 0 || index >= _filter.Tabs.Count) return;
            int childIndex = index + 1; // +1 for the "All" button
            if (childIndex >= _tabsRow.childCount) return;

            var field = new TextField { value = _filter.Tabs[index].Name };
            field.style.minWidth = 90;
            field.style.marginLeft = 2;
            _tabsRow.RemoveAt(childIndex);
            _tabsRow.Insert(childIndex, field);
            field.Focus();
            field.SelectAll();

            bool done = false;
            void Commit(bool save)
            {
                if (done) return;
                done = true;
                if (save && !string.IsNullOrWhiteSpace(field.value))
                {
                    _filter.Tabs[index].Name = field.value.Trim();
                    _filter.Save();
                }
                RebuildTabs();
            }

            field.RegisterCallback<FocusOutEvent>(_ => Commit(true));
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) Commit(true);
                else if (evt.keyCode == KeyCode.Escape) Commit(false);
            });
        }

        private void MoveTab(int index, int dir)
        {
            int target = index + dir;
            if (index < 0 || index >= _filter.Tabs.Count || target < 0 || target >= _filter.Tabs.Count) return;

            (_filter.Tabs[index], _filter.Tabs[target]) = (_filter.Tabs[target], _filter.Tabs[index]);

            var s = DebugXConsoleSettings.Instance;
            if (s.activeTab == index) s.activeTab = target;
            else if (s.activeTab == target) s.activeTab = index;

            _filter.Save();
            RebuildTabs();
        }

        private void RemoveTab(int index)
        {
            if (index < 0 || index >= _filter.Tabs.Count) return;
            _filter.Tabs.RemoveAt(index);

            var s = DebugXConsoleSettings.Instance;
            if (s.activeTab == index) s.activeTab = -1;
            else if (s.activeTab > index) s.activeTab--;

            _filter.Save();
            RebuildTabs();
        }

        private void AddTabFromSearch()
        {
            // Capture the full current filter state (search + levels + channel exclusions).
            string name = string.IsNullOrEmpty(_filter.Search) ? $"Tab {_filter.Tabs.Count + 1}" : _filter.Search;
            _filter.Tabs.Add(new FilterTab
            {
                Name = name,
                Search = _filter.Search,
                UseRegex = _filter.UseRegex,
                showLog = _filter.ShowLog,
                showWarning = _filter.ShowWarning,
                showError = _filter.ShowError,
                excludedChannels = new List<string>(_filter.ExcludedChannels)
            });
            DebugXConsoleSettings.Instance.activeTab = _filter.Tabs.Count - 1;
            _filter.Save();
            RebuildTabs();
        }

        // --- Row rendering ---

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Column;

            var top = new VisualElement { name = "top" };
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            top.style.height = ConsoleColorConfig.RowHeight;
            top.style.flexShrink = 0;

            var dot = new Label("●") { name = "dot" };
            dot.style.width = 14; dot.style.flexShrink = 0; dot.style.unityTextAlign = TextAnchor.MiddleCenter;

            var src = new Label { name = "src" };
            src.style.width = 16; src.style.flexShrink = 0; src.style.unityTextAlign = TextAnchor.MiddleCenter;
            src.style.unityFontStyleAndWeight = FontStyle.Bold;

            var time = new Label { name = "time" };
            time.style.width = ConsoleColorConfig.TimeWidth; time.style.flexShrink = 0; time.style.color = new Color(0.55f, 0.55f, 0.58f);

            var chan = new Label { name = "chan" };
            chan.style.width = ConsoleColorConfig.ChannelWidth; chan.style.flexShrink = 0;
            chan.style.unityFontStyleAndWeight = FontStyle.Bold; chan.style.overflow = Overflow.Hidden;
            // Click a channel chip to solo that channel.
            chan.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                int idx = RowIndex(row);
                if (idx < 0) return;
                var ch = _rows[idx].Entry.Channel;
                if (string.IsNullOrEmpty(ch)) return;
                evt.StopImmediatePropagation();
                evt.StopPropagation();
                SoloChannel(ch);
            });

            var cnt = new Label { name = "cnt" };
            cnt.style.width = ConsoleColorConfig.CountWidth; cnt.style.flexShrink = 0; cnt.style.unityTextAlign = TextAnchor.MiddleRight;
            cnt.style.color = new Color(0.7f, 0.7f, 0.72f);

            var msg = new Label { name = "msg" };
            msg.style.flexGrow = 1; msg.style.overflow = Overflow.Hidden;
            msg.style.whiteSpace = WhiteSpace.NoWrap; msg.style.textOverflow = TextOverflow.Ellipsis;

            top.Add(dot); top.Add(src); top.Add(time); top.Add(chan); top.Add(cnt); top.Add(msg);
            row.Add(top);

            // Second line (two-line mode): caller file:line, dimmed, aligned under the message columns.
            var caller = new Label { name = "caller" };
            caller.style.height = CallerLineHeight;
            caller.style.marginLeft = 30; // dot + src columns
            caller.style.color = new Color(0.5f, 0.5f, 0.54f);
            caller.style.overflow = Overflow.Hidden;
            caller.style.whiteSpace = WhiteSpace.NoWrap;
            caller.style.textOverflow = TextOverflow.Ellipsis;
            caller.style.display = DisplayStyle.None;
            row.Add(caller);

            _rowPool.Add(row);

            // Drag-selection: press (left or right) sets an anchor; entering rows while held extends the
            // range. A right press with no drag opens the context menu on release.
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                int idx = RowIndex(row);
                if (idx < 0) return;
                _dragButton = evt.button;
                _dragAnchor = idx;
                _dragMoved = false;
                if (evt.button == 1 && !_selectedSet.Contains(idx))
                    _list.SetSelection(idx);
            });
            row.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (_dragButton < 0) return;
                int idx = RowIndex(row);
                if (idx < 0) return;
                _dragMoved = true;
                SelectRange(_dragAnchor, idx);
            });
            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_dragButton == 1 && !_dragMoved)
                    ShowRowMenu(RowIndex(row));
                _dragButton = -1;
            });

            return row;
        }

        private void BindRow(VisualElement el, int i)
        {
            el.userData = i;
            var r = _rows[i];
            var e = r.Entry;
            bool isMarker = e.Source == ConsoleSource.Marker;
            var col = isMarker ? ConsoleColorConfig.MarkerColor : ConsoleColorConfig.LevelColor(e.Level);
            int fs = ConsoleColorConfig.FontSize;

            var dot = el.Q<Label>("dot");
            dot.text = isMarker ? "" : "●";
            dot.style.color = col;

            var src = el.Q<Label>("src");
            src.style.fontSize = fs - 2;
            switch (e.Source)
            {
                case ConsoleSource.Unity: src.text = "U"; src.style.color = new Color(0.4f, 0.72f, 0.9f); break;
                case ConsoleSource.Compiler: src.text = "C"; src.style.color = new Color(1f, 0.5f, 0.4f); break;
                case ConsoleSource.Marker: src.text = ""; break;
                default: src.text = "D"; src.style.color = new Color(0.55f, 0.55f, 0.58f); break;
            }

            el.tooltip = Truncate(e.Message, 500);

            var time = el.Q<Label>("time");
            time.style.fontSize = fs - 1;
            var fmt = ConsoleColorConfig.TimeStampFormat;
            // Delta against the previous DISPLAY row is meaningless when the list is re-sorted; fall
            // back to clock time while a sort is active.
            if (fmt == ConsoleColorConfig.TimeFormat.Delta && _sortColumn != SortColumn.None)
                fmt = ConsoleColorConfig.TimeFormat.Clock;
            switch (fmt)
            {
                case ConsoleColorConfig.TimeFormat.None:
                    time.text = "";
                    break;
                case ConsoleColorConfig.TimeFormat.ClockMillis:
                    time.text = e.Timestamp.ToString("HH:mm:ss.fff");
                    break;
                case ConsoleColorConfig.TimeFormat.Delta:
                    time.text = i > 0
                        ? "+" + (e.Timestamp - _rows[i - 1].Entry.Timestamp).TotalSeconds.ToString("0.000")
                        : "+0.000";
                    break;
                case ConsoleColorConfig.TimeFormat.Frame:
                    time.text = e.FrameCount >= 0 ? e.FrameCount.ToString() : "";
                    break;
                default:
                    time.text = e.Timestamp.ToString("HH:mm:ss");
                    break;
            }

            // Keep the channel column at its fixed width even when empty, so the message column stays
            // aligned across rows (blank the text rather than collapsing the element).
            var chan = el.Q<Label>("chan");
            chan.style.fontSize = fs - 1;
            if (!isMarker && !string.IsNullOrEmpty(e.Channel))
            {
                chan.text = e.Channel;
                chan.style.color = ConsoleColorConfig.ChannelColor(e.Channel);
            }
            else
            {
                chan.text = "";
            }

            var cnt = el.Q<Label>("cnt");
            cnt.style.display = _filter.Collapse ? DisplayStyle.Flex : DisplayStyle.None;
            cnt.text = !isMarker && r.Count > 1 ? r.Count.ToString() : "";
            cnt.style.fontSize = fs - 1;

            var msg = el.Q<Label>("msg");
            msg.text = isMarker ? "<noparse>" + e.Message + "</noparse>" : Highlight(FirstLine(e.Message));
            msg.style.color = col;
            msg.style.fontSize = fs;
            msg.style.unityFontStyleAndWeight = isMarker ? FontStyle.Italic : FontStyle.Normal;

            var caller = el.Q<Label>("caller");
            bool twoLine = ConsoleColorConfig.TwoLineRows && !isMarker;
            caller.style.display = twoLine ? DisplayStyle.Flex : DisplayStyle.None;
            if (twoLine)
            {
                caller.text = ConsoleFormat.EnsureCallerSummary(e);
                caller.style.fontSize = fs - 2;
            }

            el.style.backgroundColor = _selectedSet.Contains(i)
                ? ConsoleColorConfig.SelectionColor
                : ConsoleColorConfig.RowBackground(i);
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }

        // --- Detail pane ---

        private void RefreshDetail()
        {
            if (_detail == null) return;
            _detail.Clear();

            int idx = _list != null ? _list.selectedIndex : -1;
            if (idx < 0 || idx >= _rows.Count)
            {
                _detail.Add(Hint("Select a log entry to see details."));
                return;
            }

            var e = _rows[idx].Entry;
            ConsoleFormat.EnsureDerived(e);

            string frame = e.FrameCount >= 0 ? $"  •  frame {e.FrameCount}" : "";
            _detail.Add(Meta($"{e.Level}  •  {e.Source}  •  {(string.IsNullOrEmpty(e.Channel) ? "-" : e.Channel)}  •  {e.Timestamp:HH:mm:ss.fff}{frame}"));

            var message = new Label(e.Message ?? "");
            message.enableRichText = false; // render message literally (no rich-text tag interpretation)
            message.style.whiteSpace = WhiteSpace.Normal;
            message.style.marginTop = 2;
            message.style.fontSize = ConsoleColorConfig.FontSize;
            message.selection.isSelectable = true;
            _detail.Add(message);

            if (!string.IsNullOrEmpty(e.PropertiesText))
                _detail.Add(Meta(e.PropertiesText));

            // Best real source location (skips DebugX internals; falls back to first stack frame).
            if (ConsoleNavigation.TryBestSource(e, out string srcPath, out int srcLine))
            {
                _detail.Add(Link($"{UnityConsoleStackFormatter.ToUnityProjectPath(srcPath) ?? srcPath}:{srcLine}",
                    () => ConsoleNavigation.OpenPath(srcPath, srcLine)));

                var snippet = ConsoleSnippet.Build(srcPath, srcLine);
                if (snippet != null)
                    _detail.Add(snippet);
            }

            if (!string.IsNullOrEmpty(e.DisplayStack))
            {
                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.alignItems = Align.Center;
                headerRow.style.marginTop = 6;

                var header = Meta("Stack Trace");
                var spacer = new VisualElement();
                spacer.style.flexGrow = 1;
                string stackText = e.DisplayStack;
                var copyBtn = new Button(() => EditorGUIUtility.systemCopyBuffer = stackText) { text = "Copy" };
                copyBtn.style.fontSize = 10;
                copyBtn.style.paddingTop = copyBtn.style.paddingBottom = 0;

                headerRow.Add(header);
                headerRow.Add(spacer);
                headerRow.Add(copyBtn);
                _detail.Add(headerRow);

                foreach (var line in e.DisplayStack.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    if (ConsoleNavigation.TryParseFirstFrame(line, out string path, out int ln))
                        _detail.Add(Link(line.Trim(), () => ConsoleNavigation.OpenPath(path, ln)));
                    else
                        _detail.Add(Mono(line));
                }
            }
        }

        /// <summary>
        /// Rebuilds the Watch column (left of the detail pane). Rows updated within the last ~600ms get
        /// a highlight that decays on the next tick, so changing values are easy to spot.
        /// </summary>
        private void RefreshWatch()
        {
            if (_watchPane == null) return;
            _watchScroll.style.display = _showWatch ? DisplayStyle.Flex : DisplayStyle.None;
            _watchFlashPending = false;
            if (!_showWatch) return;

            _watchPane.Clear();

            var title = Meta($"Watch ({ConsoleLogStore.Watches.Count})");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            _watchPane.Add(title);

            var watches = ConsoleLogStore.Watches;
            if (watches.Count == 0)
            {
                _watchPane.Add(Hint("No watched variables. Call DebugX.Watch(name, value)."));
                return;
            }

            var now = System.DateTime.Now;
            for (int i = 0; i < watches.Count; i++)
            {
                var w = watches[i];
                var l = new Label($"{w.Name} = {w.Value}   (x{w.UpdateCount})");
                l.style.fontSize = ConsoleColorConfig.FontSize;
                if ((now - w.LastUpdate).TotalMilliseconds < 600)
                {
                    l.style.backgroundColor = new Color(0.95f, 0.75f, 0.25f, 0.22f);
                    _watchFlashPending = true; // decay on a later tick
                }
                _watchPane.Add(l);
            }
        }

        // --- Tick / rebuild ---

        private void Tick()
        {
            UpdateCompilingOverlay();

            // Decay the "recently updated" highlight in the watch pane once updates stop.
            if (_watchFlashPending && _showWatch && ConsoleLogStore.Version == _lastVersion)
                RefreshWatch();

            if (_paused) return;
            if (ConsoleLogStore.Version == _lastVersion) return;
            _lastVersion = ConsoleLogStore.Version;
            Rebuild(incremental: true);
        }

        private void Rebuild(bool incremental = false)
        {
            if (_list == null) return;

            // The incremental fast path only applies to the unsorted view: an active sort re-orders
            // the row list, which invalidates the model's positional bookkeeping.
            bool sorted = _sortColumn != SortColumn.None;
            _filter.Build(_rows, incremental && !sorted);
            ApplySort();
            _list.RefreshItems();
            UpdateCounts();

            // Smart autoscroll: only stick to the bottom when the user is already there, so scrolling
            // up to read isn't yanked back down by incoming logs.
            if (_autoscroll && _rows.Count > 0 && IsNearBottom())
                _list.ScrollToItem(_rows.Count - 1);

            HandleErrorPause();
            UpdateStatus();
            UpdateCountColumn();
            UpdateFilterBadge();
            RebuildChips();
            UpdateEmptyHint();
            RefreshWatch();
        }

        /// <summary>Rebuilds the dismissible filter-chip row. Hidden when nothing is filtered.</summary>
        private void RebuildChips()
        {
            if (_chipsRow == null) return;
            _chipsRow.Clear();

            if (!_filter.ShowVerbose)
                _chipsRow.Add(MakeChip("hide Verbose∕Debug", () => { _filter.ShowVerbose = true; _filter.Save(); Rebuild(); }));
            if (!_filter.ShowSourceDebugX)
                _chipsRow.Add(MakeChip("source: DebugX off", () => { _filter.ShowSourceDebugX = true; _filter.Save(); Rebuild(); }));
            if (!_filter.ShowSourceUnity)
                _chipsRow.Add(MakeChip("source: Unity off", () => { _filter.ShowSourceUnity = true; _filter.Save(); Rebuild(); }));
            if (!_filter.ShowSourceCompiler)
                _chipsRow.Add(MakeChip("source: Compiler off", () => { _filter.ShowSourceCompiler = true; _filter.Save(); Rebuild(); }));

            var excluded = _filter.ExcludedChannels;
            if (excluded.Count > 0 && excluded.Count <= 5)
            {
                foreach (var ch in excluded)
                {
                    string c = ch;
                    _chipsRow.Add(MakeChip("channel: " + c, () => { _filter.ExcludedChannels.Remove(c); _filter.Save(); Rebuild(); }));
                }
            }
            else if (excluded.Count > 5)
            {
                _chipsRow.Add(MakeChip($"{excluded.Count} channels hidden", () => { _filter.ExcludedChannels.Clear(); _filter.Save(); Rebuild(); }));
            }

            if (_filter.IgnoreList.Count > 0)
                _chipsRow.Add(MakeChip($"{_filter.IgnoreList.Count} ignored message{(_filter.IgnoreList.Count > 1 ? "s" : "")}",
                    () => { _filter.IgnoreList.Clear(); _filter.Save(); Rebuild(); }));

            bool any = _chipsRow.childCount > 0;
            if (any)
            {
                var reset = new Button(ResetFilters) { text = "Reset all" };
                reset.tooltip = "Restore every filter to its default";
                reset.style.fontSize = 10;
                reset.style.marginLeft = 6;
                _chipsRow.Add(reset);
            }
            _chipsRow.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static Button MakeChip(string label, System.Action onRemove)
        {
            var chip = new Button(onRemove) { text = "✕ " + label };
            chip.tooltip = "Remove this filter";
            chip.style.fontSize = 10;
            chip.style.marginLeft = 2;
            chip.style.marginTop = chip.style.marginBottom = 1;
            chip.style.paddingLeft = chip.style.paddingRight = 6;
            chip.style.backgroundColor = new Color(0.25f, 0.35f, 0.45f, 0.45f);
            chip.style.borderTopLeftRadius = chip.style.borderTopRightRadius = 8;
            chip.style.borderBottomLeftRadius = chip.style.borderBottomRightRadius = 8;
            return chip;
        }

        /// <summary>Shows the centered "hidden by filters" hint when the view is empty but the store is not.</summary>
        private void UpdateEmptyHint()
        {
            if (_emptyHint == null) return;
            int total = ConsoleLogStore.Count + ConsoleLogStore.CompilerEntries.Count;
            bool show = _rows.Count == 0 && total > 0;
            _emptyHint.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
                _emptyHintLabel.text = $"{total} entr{(total == 1 ? "y" : "ies")} hidden by filters";
        }

        /// <summary>The N (collapse count) column only appears while Collapse is on.</summary>
        private void UpdateCountColumn()
        {
            var display = _filter.Collapse ? DisplayStyle.Flex : DisplayStyle.None;
            if (_sortLabels.TryGetValue(SortColumn.Count, out var label))
                label.style.display = display;
            if (_countHandle != null)
                _countHandle.style.display = display;
        }

        private bool IsNearBottom()
        {
            if (_listScroll == null) _listScroll = _list?.Q<ScrollView>();
            var vs = _listScroll?.verticalScroller;
            if (vs == null) return true;
            return vs.highValue <= 0f || vs.value >= vs.highValue - 4f;
        }

        private void UpdateStatus()
        {
            if (_statusLabel == null) return;
            int total = ConsoleLogStore.Count + ConsoleLogStore.CompilerEntries.Count;
            var sb = new StringBuilder();
            sb.Append("Showing ").Append(_rows.Count).Append(" of ").Append(total);
            if (_selectedSet.Count > 0) sb.Append("   •   ").Append(_selectedSet.Count).Append(" selected");
            if (_paused) sb.Append("   •   PAUSED");
            _statusLabel.text = sb.ToString();
        }

        private void RebuildRows()
        {
            _list.RefreshItems();
            RefreshDetail();
        }

        private void ApplyRowHeight()
        {
            _rowPool.Clear(); // ListView.Rebuild discards recycled rows; MakeRow repopulates the pool
            _list.fixedItemHeight = ItemHeight();
            _list.Rebuild();
        }

        private void UpdateCounts()
        {
            if (_logToggle != null) _logToggle.text = $"Log {ConsoleLogStore.LogCount}";
            if (_warnToggle != null) _warnToggle.text = $"Warn {ConsoleLogStore.WarningCount}";
            if (_errToggle != null) _errToggle.text = $"Err {ConsoleLogStore.ErrorCount}";

            int err = ConsoleLogStore.ErrorCount;
            if (titleContent != null)
                titleContent.text = err > 0 ? $"DebugX ● {err}" : "DebugX Console";
        }

        private void HandleErrorPause()
        {
            if (!_errorPause || !EditorApplication.isPlaying) return;
            if (ConsoleLogStore.LastErrorId > _lastErrorSeen)
            {
                _lastErrorSeen = ConsoleLogStore.LastErrorId;
                EditorApplication.isPaused = true;
            }
        }

        private void JumpError(int dir)
        {
            if (_rows.Count == 0) return;
            int start = _list.selectedIndex;
            int i = start;
            for (int step = 0; step < _rows.Count; step++)
            {
                i += dir;
                if (i < 0) i = _rows.Count - 1;
                else if (i >= _rows.Count) i = 0;
                if (_rows[i].Entry.Category == ConsoleCategory.Error)
                {
                    _list.selectedIndex = i;
                    _list.ScrollToItem(i);
                    return;
                }
            }
        }

        private void OpenSelected()
        {
            int idx = _list.selectedIndex;
            if (idx >= 0 && idx < _rows.Count)
                ConsoleNavigation.OpenEntry(_rows[idx].Entry);
        }

        // --- Selection ---

        private int RowIndex(VisualElement row) => row.userData is int i ? i : -1;

        private void OnSelectionChanged()
        {
            _selectedSet.Clear();
            foreach (int i in _list.selectedIndices)
                _selectedSet.Add(i);
            _list.RefreshItems();
            RefreshDetail();
            UpdateStatus();
        }

        private void PersistSearch()
        {
            var s = DebugXConsoleSettings.Instance;
            s.search = _filter.Search;
            s.searchRegex = _filter.UseRegex;
            s.Save();
        }

        private static string BuildEntryText(ConsoleEntry e)
        {
            ConsoleFormat.EnsureDerived(e);
            var sb = new StringBuilder();
            sb.Append('[').Append(e.Timestamp.ToString("HH:mm:ss.fff")).Append("] ").Append(e.Level);
            if (!string.IsNullOrEmpty(e.Channel)) sb.Append(" [").Append(e.Channel).Append(']');
            sb.Append('\n').Append(e.Message);
            if (!string.IsNullOrEmpty(e.PropertiesText)) sb.Append('\n').Append(e.PropertiesText);
            if (!string.IsNullOrEmpty(e.DisplayStack)) sb.Append('\n').Append(e.DisplayStack);
            return sb.ToString();
        }

        private void SelectRange(int a, int b)
        {
            int lo = Mathf.Min(a, b);
            int hi = Mathf.Max(a, b);
            var idxs = new List<int>(hi - lo + 1);
            for (int i = lo; i <= hi; i++) idxs.Add(i);
            _list.SetSelection(idxs);
        }

        private List<RowRef> SelectedRows()
        {
            var idxs = new List<int>(_selectedSet);
            idxs.Sort();
            var res = new List<RowRef>(idxs.Count);
            foreach (int i in idxs)
                if (i >= 0 && i < _rows.Count) res.Add(_rows[i]);
            return res;
        }

        private void SoloChannel(string ch)
        {
            int total = _filter.Channels.Count;
            bool onlyThis = total > 1 && !_filter.ExcludedChannels.Contains(ch) && _filter.ExcludedChannels.Count >= total - 1;

            _filter.ExcludedChannels.Clear();
            if (!onlyThis)
                foreach (var c in _filter.Channels)
                    if (!string.Equals(c, ch)) _filter.ExcludedChannels.Add(c);

            _filter.Save();
            Rebuild();
        }

        // Wraps literal text in <noparse> so any '<'/'>' in a message renders literally (not as rich-text
        // tags), while search matches are wrapped in colour tags that stay OUTSIDE the noparse spans.
        private string Highlight(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            if (_filter.UseRegex)
                return HighlightRegex(text);

            var terms = _filter.IncludeTerms; // parsed once by the filter model, not per row
            if (terms.Count == 0)
                return "<noparse>" + text + "</noparse>";

            var sb = new StringBuilder();
            var lit = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                int matchLen = MatchLenAt(text, i, terms);
                if (matchLen > 0)
                {
                    FlushLiteral(sb, lit);
                    sb.Append("<color=#FFD54A><b><noparse>").Append(text, i, matchLen).Append("</noparse></b></color>");
                    i += matchLen;
                }
                else
                {
                    lit.Append(text[i]);
                    i++;
                }
            }
            FlushLiteral(sb, lit);
            return sb.ToString();
        }

        private string HighlightRegex(string text)
        {
            var rx = _filter.ActiveRegex;
            if (rx == null)
                return "<noparse>" + text + "</noparse>";

            StringBuilder sb = null;
            int last = 0;
            foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
            {
                if (m.Length == 0) continue; // zero-width matches would loop the markup forever
                sb ??= new StringBuilder();
                if (m.Index > last)
                    sb.Append("<noparse>").Append(text, last, m.Index - last).Append("</noparse>");
                sb.Append("<color=#FFD54A><b><noparse>").Append(m.Value).Append("</noparse></b></color>");
                last = m.Index + m.Length;
            }

            if (sb == null)
                return "<noparse>" + text + "</noparse>";
            if (last < text.Length)
                sb.Append("<noparse>").Append(text, last, text.Length - last).Append("</noparse>");
            return sb.ToString();
        }

        private static void FlushLiteral(StringBuilder sb, StringBuilder lit)
        {
            if (lit.Length == 0) return;
            sb.Append("<noparse>").Append(lit).Append("</noparse>");
            lit.Clear();
        }

        private static int MatchLenAt(string text, int i, IReadOnlyList<string> terms)
        {
            for (int t = 0; t < terms.Count; t++)
            {
                var term = terms[t];
                int len = term.Length;
                if (len > 0 && i + len <= text.Length &&
                    string.Compare(text, i, term, 0, len, System.StringComparison.OrdinalIgnoreCase) == 0)
                    return len;
            }
            return 0;
        }

        private void CopySelected()
        {
            if (_selectedSet.Count == 0) return;
            var idxs = new List<int>(_selectedSet);
            idxs.Sort();

            var sb = new StringBuilder();
            foreach (int i in idxs)
            {
                if (i < 0 || i >= _rows.Count) continue;
                var e = _rows[i].Entry;
                sb.Append('[').Append(e.Timestamp.ToString("HH:mm:ss")).Append("] ");
                if (!string.IsNullOrEmpty(e.Channel)) sb.Append('[').Append(e.Channel).Append("] ");
                sb.AppendLine(e.Message);
            }
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }

        private void ShowRowMenu(int idx)
        {
            if (idx < 0 || idx >= _rows.Count) return;
            if (!_selectedSet.Contains(idx)) _list.SetSelection(idx);

            var e = _rows[idx].Entry;
            var menu = new GenericMenu();
            int n = _selectedSet.Count;
            menu.AddItem(new GUIContent(n > 1 ? $"Copy {n} Rows" : "Copy"), false, CopySelected);
            menu.AddItem(new GUIContent("Copy Details"), false, () => EditorGUIUtility.systemCopyBuffer = BuildEntryText(e));
            menu.AddItem(new GUIContent("Open Source"), false, () => ConsoleNavigation.OpenEntry(e));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Ignore this message"), false, () =>
            {
                if (!string.IsNullOrEmpty(e.Message)) { _filter.IgnoreList.Add(e.Message); _filter.Save(); Rebuild(); }
            });
            if (!string.IsNullOrEmpty(e.Channel))
                menu.AddItem(new GUIContent("Hide channel: " + e.Channel), false, () =>
                {
                    _filter.ExcludedChannels.Add(e.Channel); _filter.Save(); Rebuild();
                });
            menu.ShowAsContext();
        }

        // --- Header / sorting ---

        private VisualElement BuildHeader()
        {
            var h = new VisualElement();
            h.style.flexDirection = FlexDirection.Row;
            h.style.flexShrink = 0;
            h.style.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
            h.style.borderBottomWidth = 1;
            h.style.borderBottomColor = new Color(0f, 0f, 0f, 0.4f);

            // Spacer aligning with the row's level-dot (14) + source-tag (16) columns.
            var spacer = new VisualElement();
            spacer.style.width = 30;
            spacer.style.flexShrink = 0;
            h.Add(spacer);

            _sortLabels.Clear();
            foreach (var c in HeaderCols)
            {
                var col = c.Col;
                var l = new Label(c.Label);
                int w = ColumnWidth(col);
                if (w > 0) { l.style.width = w; l.style.flexShrink = 0; }
                else l.style.flexGrow = 1;
                l.style.unityFontStyleAndWeight = FontStyle.Bold;
                l.style.fontSize = 11;
                l.style.paddingLeft = 2;
                l.style.color = new Color(0.7f, 0.7f, 0.72f);
                l.RegisterCallback<ClickEvent>(_ => OnHeaderClick(col));
                _sortLabels[col] = l;
                h.Add(l);

                if (w > 0)
                {
                    var handle = MakeResizeHandle(col);
                    if (col == SortColumn.Count) _countHandle = handle;
                    h.Add(handle);
                }
            }

            h.style.display = ConsoleColorConfig.ShowHeader ? DisplayStyle.Flex : DisplayStyle.None;
            return h;
        }

        // --- Column resizing ---

        private static int ColumnWidth(SortColumn col)
        {
            switch (col)
            {
                case SortColumn.Time: return ConsoleColorConfig.TimeWidth;
                case SortColumn.Channel: return ConsoleColorConfig.ChannelWidth;
                case SortColumn.Count: return ConsoleColorConfig.CountWidth;
                default: return 0;
            }
        }

        /// <summary>Invisible drag strip after a header label (negative margins keep header/row columns aligned).</summary>
        private VisualElement MakeResizeHandle(SortColumn col)
        {
            var handle = new VisualElement();
            handle.style.width = 6;
            handle.style.marginLeft = -3;
            handle.style.marginRight = -3;
            handle.style.flexShrink = 0;
            handle.style.alignSelf = Align.Stretch;
            handle.tooltip = "Drag to resize column";

            int startX = 0, startW = 0;
            bool dragging = false;
            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                dragging = true;
                startX = (int)evt.position.x;
                startW = ColumnWidth(col);
                handle.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging) return;
                SetColumnWidthLive(col, startW + (int)(evt.position.x - startX));
            });
            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dragging) return;
                dragging = false;
                handle.ReleasePointer(evt.pointerId);
                DebugXConsoleSettings.Instance.Save(); // persist once per drag, not per pixel
            });
            return handle;
        }

        private void SetColumnWidthLive(SortColumn col, int width)
        {
            var s = DebugXConsoleSettings.Instance;
            switch (col)
            {
                case SortColumn.Time: s.colTimeWidth = Mathf.Clamp(width, 30, 220); break;
                case SortColumn.Channel: s.colChannelWidth = Mathf.Clamp(width, 40, 300); break;
                case SortColumn.Count: s.colCountWidth = Mathf.Clamp(width, 20, 100); break;
                default: return;
            }
            ApplyColumnWidths();
        }

        /// <summary>Pushes the current column widths to the header labels and all live row elements.</summary>
        private void ApplyColumnWidths()
        {
            foreach (var c in HeaderCols)
            {
                int w = ColumnWidth(c.Col);
                if (w > 0 && _sortLabels.TryGetValue(c.Col, out var l))
                    l.style.width = w;
            }

            foreach (var row in _rowPool)
            {
                var time = row.Q<Label>("time");
                if (time != null) time.style.width = ConsoleColorConfig.TimeWidth;
                var chan = row.Q<Label>("chan");
                if (chan != null) chan.style.width = ConsoleColorConfig.ChannelWidth;
                var cnt = row.Q<Label>("cnt");
                if (cnt != null) cnt.style.width = ConsoleColorConfig.CountWidth;
            }
        }

        private void ResetColumnWidths()
        {
            var s = DebugXConsoleSettings.Instance;
            s.colTimeWidth = 78;
            s.colChannelWidth = 96;
            s.colCountWidth = 34;
            s.Save();
            ApplyColumnWidths();
        }

        private void UpdateHeaderArrows()
        {
            foreach (var c in HeaderCols)
            {
                if (!_sortLabels.TryGetValue(c.Col, out var l)) continue;
                l.text = c.Label + (_sortColumn == c.Col ? (_sortAsc ? " ▲" : " ▼") : "");
            }
        }

        private void OnHeaderClick(SortColumn col)
        {
            if (_sortColumn != col) { _sortColumn = col; _sortAsc = true; }
            else if (_sortAsc) { _sortAsc = false; }
            else { _sortColumn = SortColumn.None; }

            _list.ClearSelection(); // row indices remap after a re-sort
            var s = DebugXConsoleSettings.Instance;
            s.sortColumn = (int)_sortColumn;
            s.sortAsc = _sortAsc;
            s.Save();
            UpdateHeaderArrows();
            Rebuild();
        }

        private void ApplySort()
        {
            if (_sortColumn == SortColumn.None) return;
            _rows.Sort((a, b) =>
            {
                int c = CompareRows(a, b, _sortColumn);
                return _sortAsc ? c : -c;
            });
        }

        private static int CompareRows(RowRef a, RowRef b, SortColumn col)
        {
            switch (col)
            {
                case SortColumn.Time: return a.Entry.Timestamp.CompareTo(b.Entry.Timestamp);
                case SortColumn.Channel: return string.Compare(a.Entry.Channel ?? "", b.Entry.Channel ?? "", System.StringComparison.OrdinalIgnoreCase);
                case SortColumn.Count: return a.Count.CompareTo(b.Count);
                case SortColumn.Message: return string.Compare(a.Entry.Message ?? "", b.Entry.Message ?? "", System.StringComparison.OrdinalIgnoreCase);
                default: return 0;
            }
        }

        // --- Helpers ---

        private static string FirstLine(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int nl = s.IndexOf('\n');
            return nl < 0 ? s : s.Substring(0, nl);
        }

        private static Label Hint(string text)
        {
            var l = new Label(text) { style = { color = new Color(0.6f, 0.6f, 0.62f) } };
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        private static Label Meta(string text)
        {
            var l = new Label(text) { style = { color = new Color(0.68f, 0.68f, 0.7f) } };
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.fontSize = 11;
            return l;
        }

        private static Label Mono(string text)
        {
            var l = new Label(text);
            l.enableRichText = false;
            l.selection.isSelectable = true;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.color = new Color(0.7f, 0.7f, 0.72f);
            l.style.fontSize = 11;
            return l;
        }

        private static Label Link(string text, System.Action onClick)
        {
            var l = new Label(text);
            l.enableRichText = false;
            l.selection.isSelectable = true;
            l.style.color = new Color(0.4f, 0.7f, 1f);
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.fontSize = 11;
            l.RegisterCallback<ClickEvent>(_ => onClick());
            l.RegisterCallback<MouseEnterEvent>(_ => l.style.unityFontStyleAndWeight = FontStyle.Bold);
            l.RegisterCallback<MouseLeaveEvent>(_ => l.style.unityFontStyleAndWeight = FontStyle.Normal);
            return l;
        }

        private static IEnumerable<string> KnownChannels()
        {
            var result = new List<string>();
            try
            {
                var fields = typeof(LogChannels).GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (var f in fields)
                {
                    if (f.FieldType != typeof(LogChannel)) continue;
                    var ch = (LogChannel)f.GetValue(null);
                    if (!string.IsNullOrEmpty(ch.Name)) result.Add(ch.Name);
                }
            }
            catch { /* reflection best-effort */ }
            return result;
        }
    }
}
