#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System;
using System.IO;
using FoundationPlatform.Utilities.Menus;

namespace FoundationPlatform.Editor.Utilities
{
    /// <summary>
    /// Scene switcher window, an editor window for switching between scenes.
    /// </summary>
    public class SceneSwitcherWindow : EditorWindow
    {
        private const string PrefKeyScenesSource = "SceneSwitcher.scenesSource";
        private const string PrefKeyOpenSceneMode = "SceneSwitcher.openSceneMode";
        private const string PrefKeySelectedSceneIndex = "SceneSwitcher.selectedSceneToPlay";
        private const string PrefKeySearchText = "SceneSwitcher.searchText";
        private const string PrefKeySortAlphabetically = "SceneSwitcher.sortAlphabetically";
        private const string PrefKeyShowSceneCreateButton = "SceneSwitcher.showCreateSceneButton";
        
        public enum ScenesSource
        {
            Assets,
            BuildSettings
        }

        public class SceneListData
        {
            public string sceneName;
            public string scenePath;

            public SceneListData(string sceneName, string scenePath)
            {
                this.sceneName = sceneName;
                this.scenePath = scenePath;
            }
        }

        protected int selectedSceneToPlay = -1;
        protected List<SceneListData> sceneListDatas = new List<SceneListData>();
        private bool sceneListDirty = true;
        protected GUIStyle commandButtonStyle;
        protected GUIContent playButtonContent;
        protected GUIContent addIconContent;
        protected GUIContent pingIconContent;
        protected GUIContent setPlayOnIconContent;
        protected GUIContent setPlayOffIconContent;
        protected GUIContent closeIconContent;
        private bool focusSearchPending;
        protected Vector2 scrollPosBuild;

        protected Vector2 scrollPosition;
        protected string searchText = string.Empty;
        protected ScenesSource scenesSource = ScenesSource.Assets;
        protected OpenSceneMode openSceneMode = OpenSceneMode.Single;
        protected bool sortAlphabetically;
        protected bool showCreateSceneButton;
        protected int selectedTab = 0;

        protected string[] tabs = new string[]
        {
            "Scenes",
            "Settings"
        };

        [MenuItem(MenuPaths.WindowUtilities.SceneSwitcher, priority = 1101)]
        public static void Init()
        {
            var window = EditorWindow.GetWindow<SceneSwitcherWindow>("Scene Switcher");
            window.minSize = new Vector2(250f, 200f);
            window.Show();
        }

        protected virtual void OnEnable()
        {
            this.scenesSource =
                (ScenesSource)EditorPrefs.GetInt(PrefKeyScenesSource, (int)ScenesSource.Assets);
            this.openSceneMode = (OpenSceneMode)EditorPrefs.GetInt(
                PrefKeyOpenSceneMode,
                (int)OpenSceneMode.Single);
            this.selectedSceneToPlay = EditorPrefs.GetInt(PrefKeySelectedSceneIndex, -1);
            this.searchText = EditorPrefs.GetString(PrefKeySearchText, string.Empty);
            this.sortAlphabetically = EditorPrefs.GetBool(PrefKeySortAlphabetically, false);
            this.showCreateSceneButton = EditorPrefs.GetBool(PrefKeyShowSceneCreateButton, false);

            // Refresh the cached scene list when the project's assets change, instead of every repaint.
            EditorApplication.projectChanged += MarkSceneListDirty;
            // Redraw when the set of open scenes changes so open/close state stays live.
            EditorSceneManager.sceneOpened += OnSceneOpenedOrClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            this.focusSearchPending = true;

            // Restore the play mode start scene when the window is enabled
            RefreshSceneList();
            if (this.sceneListDatas.Count > 0 && this.selectedSceneToPlay >= 0 && this.selectedSceneToPlay < this.sceneListDatas.Count)
            {
                string scenePath = this.sceneListDatas[this.selectedSceneToPlay].scenePath;
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    EditorSceneManager.playModeStartScene = sceneAsset;
                }
            }
            else if (this.selectedSceneToPlay == -1)
            {
                // Clear play mode start scene when no scene is selected
                EditorSceneManager.playModeStartScene = null;
            }
        }

        private void OnSceneOpenedOrClosed(Scene scene, OpenSceneMode mode)
        {
            Repaint();
        }

        private void OnSceneClosed(Scene scene)
        {
            Repaint();
        }

        protected virtual void OnDisable()
        {
            EditorApplication.projectChanged -= MarkSceneListDirty;
            EditorSceneManager.sceneOpened -= OnSceneOpenedOrClosed;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorPrefs.SetInt(PrefKeyScenesSource, (int)this.scenesSource);
            EditorPrefs.SetInt(PrefKeyOpenSceneMode, (int)this.openSceneMode);
            EditorPrefs.SetInt(PrefKeySelectedSceneIndex, this.selectedSceneToPlay);
            EditorPrefs.SetString(PrefKeySearchText, this.searchText ?? string.Empty);
            EditorPrefs.SetBool(PrefKeySortAlphabetically, this.sortAlphabetically);
            EditorPrefs.SetBool(PrefKeyShowSceneCreateButton, this.showCreateSceneButton);
        }

        protected virtual void OnGUI()
        {
            if (this.sceneListDirty)
            {
                RefreshSceneList();
            }
            // Validate selectedSceneToPlay: allow -1 (no selection) or valid index
            if (this.sceneListDatas.Count == 0)
            {
                this.selectedSceneToPlay = -1;
            }
            else if (this.selectedSceneToPlay >= this.sceneListDatas.Count)
            {
                // If selected index is out of bounds, reset to -1
                this.selectedSceneToPlay = -1;
            }
            // Allow -1 to remain -1 (no selection)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            this.selectedTab = GUILayout.Toolbar(this.selectedTab, this.tabs, EditorStyles.toolbarButton);
            if (commandButtonStyle == null)
            {
                commandButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedWidth = 64,
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageOnly,
                    fontStyle = FontStyle.Bold
                };
            }
            // Cache reliable small icon contents with fallbacks
            if (addIconContent == null)
            {
                addIconContent = GetFirstAvailableIcon(
                    "Add scene additively",
                    "d_Toolbar Plus",
                    "Toolbar Plus",
                    "d_CreateAddNew",
                    "CreateAddNew");
                if (addIconContent.image == null) addIconContent = new GUIContent("+", "Add scene additively");
            }
            if (pingIconContent == null)
            {
                pingIconContent = GetFirstAvailableIcon(
                    "Ping scene asset",
                    "d_Search Icon",
                    "Search Icon",
                    "d_ViewToolOrbit",
                    "ViewToolOrbit");
                if (pingIconContent.image == null) pingIconContent = new GUIContent("P", "Ping scene asset");
            }
            if (setPlayOnIconContent == null)
            {
                setPlayOnIconContent = GetFirstAvailableIcon(
                    "Startup scene: Play starts here. Click to clear.",
                    "d_Animation.FirstKey",
                    "Animation.FirstKey",
                    "d_Profiler.FirstFrame",
                    "Profiler.FirstFrame");
                if (setPlayOnIconContent.image == null) setPlayOnIconContent = new GUIContent("⏮", "Startup scene: Play starts here. Click to clear.");
            }
            if (setPlayOffIconContent == null)
            {
                setPlayOffIconContent = GetFirstAvailableIcon(
                    "Set as startup scene (Play starts here)",
                    "Animation.FirstKey",
                    "d_Animation.FirstKey",
                    "Profiler.FirstFrame",
                    "d_Profiler.FirstFrame");
                if (setPlayOffIconContent.image == null) setPlayOffIconContent = new GUIContent("⏮", "Set as startup scene (Play starts here)");
            }
            if (closeIconContent == null)
            {
                // No reliable cross-platform close icon; use a glyph to avoid IconContent warnings.
                closeIconContent = new GUIContent("×", "Close this open scene");
            }

            string playButtonTooltip = this.sceneListDatas.Count == 0 
                ? "No scenes available to play" 
                : (EditorApplication.isPlaying 
                    ? "Playing" 
                    : (this.selectedSceneToPlay >= 0 && this.selectedSceneToPlay < this.sceneListDatas.Count
                        ? "Play the scene set in settings."
                        : "Play normally (no scene override)"));
            playButtonContent = EditorGUIUtility.IconContent(EditorApplication.isPlaying ? "d_PlayButton On" : "d_PlayButton", playButtonTooltip);
            EditorGUI.BeginDisabledGroup(this.sceneListDatas.Count == 0);
            bool requestPlay = GUILayout.Toggle(EditorApplication.isPlaying, playButtonContent, commandButtonStyle);
            
            // Handle play mode toggle
            if (requestPlay && !EditorApplication.isPlaying)
            {
                // Start play mode
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // Only set play mode start scene if a scene is selected
                    if (this.selectedSceneToPlay >= 0 && this.selectedSceneToPlay < this.sceneListDatas.Count)
                    {
                        string scenePath = this.sceneListDatas[this.selectedSceneToPlay].scenePath;
                        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    }
                    else
                    {
                        // Clear play mode start scene to allow normal Unity behavior
                        EditorSceneManager.playModeStartScene = null;
                    }
                    EditorApplication.isPlaying = true;
                }
            }
            else if (!requestPlay && EditorApplication.isPlaying)
            {
                // Stop play mode
                EditorApplication.isPlaying = false;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            // Scene count lives in the toolbar so it costs no vertical space in the list.
            if (this.selectedTab == 0)
            {
                int total = this.sceneListDatas.Count;
                int shown = string.IsNullOrEmpty(this.searchText)
                    ? total
                    : this.sceneListDatas.Count(x =>
                        x.sceneName.IndexOf(this.searchText, StringComparison.OrdinalIgnoreCase) >= 0
                        || (x.scenePath != null && x.scenePath.IndexOf(this.searchText, StringComparison.OrdinalIgnoreCase) >= 0));
                string countLabel = string.IsNullOrEmpty(this.searchText)
                    ? $"{total} scene{(total == 1 ? "" : "s")}"
                    : $"{shown}/{total}";
                GUILayout.Label(countLabel, EditorStyles.miniLabel);
            }
            GUI.SetNextControlName("SceneSwitcherSearchField");
            string newSearch = EditorGUILayout.TextField(this.searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(140));
            if (!string.Equals(newSearch, this.searchText, StringComparison.Ordinal))
            {
                this.searchText = newSearch ?? string.Empty;
                Repaint();
            }

            // Clear-search button; disabled when nothing to clear.
            GUIStyle cancelStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton")
                ?? GUI.skin.FindStyle("ToolbarSeachCancelButton"); // Unity's historical typo
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(this.searchText)))
            {
                if (cancelStyle != null)
                {
                    if (GUILayout.Button(GUIContent.none, cancelStyle))
                    {
                        this.searchText = string.Empty;
                        GUI.FocusControl(null);
                        Repaint();
                    }
                }
                else if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    this.searchText = string.Empty;
                    GUI.FocusControl(null);
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Focus the search field the first time the window is drawn after enabling.
            if (this.focusSearchPending && Event.current.type == EventType.Repaint)
            {
                this.focusSearchPending = false;
                EditorGUI.FocusTextInControl("SceneSwitcherSearchField");
            }
            this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
            EditorGUILayout.BeginVertical();
            switch (this.selectedTab)
            {
                case 0:
                    ScenesTabGUI();
                    break;
                case 1:
                    SettingsTabGUI();
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        protected virtual void SettingsTabGUI()
        {
            this.scenesSource = (ScenesSource)EditorGUILayout.EnumPopup("Scenes Source", this.scenesSource);
            this.openSceneMode = (OpenSceneMode)EditorGUILayout.EnumPopup("Open Scene Mode", this.openSceneMode);

            bool previousSortAlphabetically = this.sortAlphabetically;
            string selectedScenePath = GetSelectedScenePath();
            this.sortAlphabetically = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Sort alphabetically",
                    "When off, scenes follow Build Settings order. With Assets source, build-listed scenes appear first, then the rest alphabetically."),
                this.sortAlphabetically);
            if (previousSortAlphabetically != this.sortAlphabetically)
            {
                RefreshSceneList();
                RemapSelectedSceneToPlayByPath(selectedScenePath);
                Repaint();
            }
            
            this.showCreateSceneButton = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Show Create Scene Button",
                    "When off, Create New Scene Button will not show."),
                this.showCreateSceneButton);

            // Create options array with "None" as first option
            string[] sceneNames = sceneListDatas.Select(x => x.sceneName).ToArray();
            string[] options = new string[sceneNames.Length + 1];
            options[0] = "None";
            Array.Copy(sceneNames, 0, options, 1, sceneNames.Length);
            
            // Convert internal -1 to popup index 0, and adjust other indices
            int popupIndex = selectedSceneToPlay >= 0 && selectedSceneToPlay < sceneListDatas.Count 
                ? selectedSceneToPlay + 1 
                : 0;
            
            popupIndex = EditorGUILayout.Popup("Select Scene to Play", popupIndex, options);
            
            // Convert popup index back to internal index (-1 for "None")
            selectedSceneToPlay = popupIndex == 0 ? -1 : popupIndex - 1;
            
            // Update play mode start scene based on selection
            if (selectedSceneToPlay >= 0 && selectedSceneToPlay < sceneListDatas.Count)
            {
                string scenePath = sceneListDatas[selectedSceneToPlay].scenePath;
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    EditorSceneManager.playModeStartScene = sceneAsset;
                }
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
            
            EditorPrefs.SetInt(PrefKeySelectedSceneIndex, selectedSceneToPlay);
        }

        protected virtual void ScenesTabGUI()
        {
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var filtered = string.IsNullOrEmpty(searchText)
                ? sceneListDatas
                : sceneListDatas.Where(x =>
                    x.sceneName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                    || (x.scenePath != null && x.scenePath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

            if (filtered.Count == 0)
            {
                if (sceneListDatas.Count == 0)
                {
                    GUILayout.Label("No Scenes Found", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.Label("Create New Scenes", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.Label("And Switch Between them here", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    GUILayout.Label("No scenes match your search", EditorStyles.centeredGreyMiniLabel);
                }
            }

            for (int i = 0; i < filtered.Count; i++)
            {
                string path = filtered[i].scenePath;
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (sceneAsset == null)
                {
                    // Scene asset was deleted/unimportable since the list was built; skip the row.
                    continue;
                }
                EditorBuildSettingsScene buildScene = buildScenes.Find((editorBuildScene) =>
                {
                    return editorBuildScene.path == path;
                });
                Scene scene = SceneManager.GetSceneByPath(path);
                bool isOpen = scene.IsValid() && scene.isLoaded;
                bool canClose = isOpen && SceneManager.loadedSceneCount > 1;
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(isOpen))
                    {
                        // Mark it with a dot while the scene is open; keep the disabled button style.
                        string nameLabel = isOpen ? "● " + sceneAsset.name : sceneAsset.name;
                        if (GUILayout.Button(new GUIContent(nameLabel, isOpen ? "Scene is open" : path), GUILayout.ExpandWidth(true)))
                        {
                            Open(path);
                        }
                    }

                    if (isOpen)
                    {
                        // Open scenes get a close button (disabled when it's the only loaded scene).
                        using (new EditorGUI.DisabledScope(!canClose))
                        {
                            if (GUILayout.Button(closeIconContent, GUILayout.Width(24), GUILayout.Height(18)))
                            {
                                CloseScene(scene);
                            }
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(addIconContent, GUILayout.Width(24), GUILayout.Height(18)))
                        {
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                            }
                        }
                    }

                    if (GUILayout.Button(pingIconContent, GUILayout.Width(24), GUILayout.Height(18)))
                    {
                        EditorGUIUtility.PingObject(sceneAsset);
                        Selection.activeObject = sceneAsset;
                    }

                    bool isSelectedPlay = this.sceneListDatas.Count > 0 && this.selectedSceneToPlay >= 0 && this.selectedSceneToPlay < this.sceneListDatas.Count && this.sceneListDatas[this.selectedSceneToPlay].scenePath == path;
                    var setPlayContent = isSelectedPlay ? setPlayOnIconContent : setPlayOffIconContent;
                    bool toggled = GUILayout.Toggle(isSelectedPlay, setPlayContent, EditorStyles.miniButton, GUILayout.Width(24), GUILayout.Height(18));
                    if (toggled && !isSelectedPlay)
                    {
                        // Select this scene
                        int idx = this.sceneListDatas.FindIndex(s => s.scenePath == path);
                        if (idx >= 0)
                        {
                            this.selectedSceneToPlay = idx;
                            EditorPrefs.SetInt(PrefKeySelectedSceneIndex, this.selectedSceneToPlay);
                            // Set the play mode start scene so it works with normal play button
                            EditorSceneManager.playModeStartScene = sceneAsset;
                        }
                    }
                    else if (!toggled && isSelectedPlay)
                    {
                        // Deselect the scene
                        this.selectedSceneToPlay = -1;
                        EditorPrefs.SetInt(PrefKeySelectedSceneIndex, this.selectedSceneToPlay);
                        // Clear the play mode start scene to allow normal Unity behavior
                        EditorSceneManager.playModeStartScene = null;
                    }
                }

                // Right-click anywhere on the row opens the context menu.
                Rect rowRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
                {
                    ShowRowContextMenu(path, sceneAsset, buildScene != null);
                    Event.current.Use();
                }
            }
            
            if (this.showCreateSceneButton)
            {
                if (GUILayout.Button("Create New Scene"))
                {
                    Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(newScene);
                }
            }
        }

        private void CloseScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }
            if (SceneManager.loadedSceneCount <= 1)
            {
                return;
            }
            // Offer to save if the scene has unsaved changes before removing it.
            if (scene.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            EditorSceneManager.CloseScene(scene, true);
            Repaint();
        }

        private void ShowRowContextMenu(string path, SceneAsset sceneAsset, bool inBuild)
        {
            var menu = new GenericMenu();
            Scene scene = SceneManager.GetSceneByPath(path);
            bool isOpen = scene.IsValid() && scene.isLoaded;

            if (isOpen)
            {
                menu.AddDisabledItem(new GUIContent("Open (Single)"));
                if (SceneManager.loadedSceneCount > 1)
                {
                    menu.AddItem(new GUIContent("Close"), false, () => CloseScene(scene));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Close"));
                }
            }
            else
            {
                menu.AddItem(new GUIContent("Open (Single)"), false, () => Open(path));
                menu.AddItem(new GUIContent("Open Additive"), false, () =>
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }
                });
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Ping / Select Asset"), false, () =>
            {
                EditorGUIUtility.PingObject(sceneAsset);
                Selection.activeObject = sceneAsset;
            });
            menu.AddItem(new GUIContent("Show in Explorer"), false, () => EditorUtility.RevealInFinder(path));
            menu.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = path);

            menu.AddSeparator(string.Empty);
            bool isStartup = this.selectedSceneToPlay >= 0
                && this.selectedSceneToPlay < this.sceneListDatas.Count
                && this.sceneListDatas[this.selectedSceneToPlay].scenePath == path;
            if (isStartup)
            {
                menu.AddItem(new GUIContent("Clear Startup Scene"), false, () =>
                {
                    this.selectedSceneToPlay = -1;
                    EditorPrefs.SetInt(PrefKeySelectedSceneIndex, this.selectedSceneToPlay);
                    EditorSceneManager.playModeStartScene = null;
                    Repaint();
                });
            }
            else
            {
                menu.AddItem(new GUIContent("Set as Startup Scene"), false, () =>
                {
                    int idx = this.sceneListDatas.FindIndex(s => s.scenePath == path);
                    if (idx >= 0)
                    {
                        this.selectedSceneToPlay = idx;
                        EditorPrefs.SetInt(PrefKeySelectedSceneIndex, this.selectedSceneToPlay);
                        EditorSceneManager.playModeStartScene = sceneAsset;
                        Repaint();
                    }
                });
            }

            menu.AddSeparator(string.Empty);
            if (inBuild)
            {
                menu.AddItem(new GUIContent("Remove from Build Settings"), false, () => SetSceneInBuild(path, false));
            }
            else
            {
                menu.AddItem(new GUIContent("Add to Build Settings"), false, () => SetSceneInBuild(path, true));
            }

            menu.ShowAsContext();
        }

        private void SetSceneInBuild(string path, bool add)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int existing = scenes.FindIndex(s => s.path == path);
            if (add)
            {
                if (existing >= 0)
                {
                    scenes[existing].enabled = true;
                }
                else
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }
            else if (existing >= 0)
            {
                scenes.RemoveAt(existing);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            MarkSceneListDirty();
        }

        public virtual void Open(string path)
        {
            if (EditorSceneManager.EnsureUntitledSceneHasBeenSaved(
                    "You don't have saved the Untitled Scene, Do you want to leave?"))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(path, this.openSceneMode);
            }
        }

        private void MarkSceneListDirty()
        {
            this.sceneListDirty = true;
            Repaint();
        }

        private void RefreshSceneList()
        {
            this.sceneListDirty = false;
            sceneListDatas.Clear();
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var items = new List<SceneListData>();

            if (this.sortAlphabetically)
            {
                if (scenesSource == ScenesSource.BuildSettings)
                {
                    string[] guids = buildScenes.Select(x => x.guid.ToString()).ToArray();
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                        if (sceneAsset == null)
                        {
                            continue;
                        }
                        items.Add(new SceneListData(sceneAsset.name, path));
                    }
                }
                else
                {
                    var scenes = EditorAssetScanCache.GetAssets<SceneAsset>();
                    for (int i = 0; i < scenes.Count; i++)
                    {
                        var sceneAsset = scenes[i];
                        if (sceneAsset == null) continue;
                        string path = AssetDatabase.GetAssetPath(sceneAsset);
                        items.Add(new SceneListData(sceneAsset.name, path));
                    }
                }

                items = items.OrderBy(x => x.sceneName).ToList();
            }
            else if (scenesSource == ScenesSource.BuildSettings)
            {
                for (int i = 0; i < buildScenes.Count; i++)
                {
                    string path = buildScenes[i].path;
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (sceneAsset == null)
                    {
                        continue;
                    }
                    items.Add(new SceneListData(sceneAsset.name, path));
                }
            }
            else
            {
                var buildOrderByPath = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < buildScenes.Count; i++)
                {
                    string buildPath = buildScenes[i].path;
                    if (string.IsNullOrEmpty(buildPath))
                    {
                        continue;
                    }
                    buildOrderByPath[buildPath] = i;
                }

                var scenes = EditorAssetScanCache.GetAssets<SceneAsset>();
                var inBuild = new List<SceneListData>();
                var notInBuild = new List<SceneListData>();
                for (int i = 0; i < scenes.Count; i++)
                {
                    var sceneAsset = scenes[i];
                    if (sceneAsset == null) continue;
                    string path = AssetDatabase.GetAssetPath(sceneAsset);

                    var sceneData = new SceneListData(sceneAsset.name, path);
                    if (buildOrderByPath.ContainsKey(path))
                    {
                        inBuild.Add(sceneData);
                    }
                    else
                    {
                        notInBuild.Add(sceneData);
                    }
                }

                inBuild.Sort((a, b) => buildOrderByPath[a.scenePath].CompareTo(buildOrderByPath[b.scenePath]));
                notInBuild.Sort((a, b) => string.Compare(a.sceneName, b.sceneName, StringComparison.OrdinalIgnoreCase));
                items.AddRange(inBuild);
                items.AddRange(notInBuild);
            }

            sceneListDatas.AddRange(items);
        }

        private string GetSelectedScenePath()
        {
            if (this.selectedSceneToPlay >= 0 && this.selectedSceneToPlay < this.sceneListDatas.Count)
            {
                return this.sceneListDatas[this.selectedSceneToPlay].scenePath;
            }

            return null;
        }

        private void RemapSelectedSceneToPlayByPath(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                return;
            }

            int idx = this.sceneListDatas.FindIndex(s => s.scenePath == scenePath);
            this.selectedSceneToPlay = idx >= 0 ? idx : -1;
        }

        private static GUIContent GetFirstAvailableIcon(string tooltip, params string[] iconNames)
        {
            foreach (var name in iconNames)
            {
                // FindTexture returns null silently for missing icons; IconContent logs a warning per miss.
                var tex = EditorGUIUtility.FindTexture(name);
                if (tex != null)
                {
                    return new GUIContent(tex, tooltip);
                }
            }
            return new GUIContent(string.Empty, tooltip);
        }
    }
}
#endif