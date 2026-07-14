#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.FrameworkInspector.Editor
{
    /// <summary>
    /// "Save Values When Exiting Play Mode": components flagged via the context menu are
    /// serialized to JSON just before play mode ends and re-applied (with Undo) after the
    /// edit-mode domain reload. Objects are tracked by GlobalObjectId so the watch list
    /// survives instance-id churn; the list itself lives in SessionState.
    /// Play-mode-instantiated objects can't be tracked (no persistent id) — only
    /// scene objects that existed before entering play mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeValuesSaver
    {
        private const string WatchKey = "FrameworkInspectorX.PlayModeSaver.Watch";
        private const string DataKey = "FrameworkInspectorX.PlayModeSaver.Data";

        [Serializable]
        private sealed class StringListPayload
        {
            public List<string> items = new List<string>();
        }

        [Serializable]
        private sealed class CapturePayload
        {
            public List<string> ids = new List<string>();
            public List<string> jsons = new List<string>();
        }

        static PlayModeValuesSaver()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        internal static bool IsWatched(Component component)
        {
            var id = IdOf(component);
            return id != null && LoadWatchList().items.Contains(id);
        }

        internal static void Toggle(Component component)
        {
            var id = IdOf(component);
            if (id == null)
            {
                Debug.LogWarning("[FrameworkInspector] This object has no persistent id (created in play mode?) — values can't be saved across play mode.");
                return;
            }

            var list = LoadWatchList();
            if (!list.items.Remove(id))
                list.items.Add(id);
            SessionState.SetString(WatchKey, JsonUtility.ToJson(list));
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!InspectorXSettings.instance.saveComponentValuesInPlayMode)
                return;

            if (change == PlayModeStateChange.ExitingPlayMode)
                Capture();
            else if (change == PlayModeStateChange.EnteredEditMode)
                Apply();
        }

        private static void Capture()
        {
            var watch = LoadWatchList();
            if (watch.items.Count == 0)
                return;

            var capture = new CapturePayload();
            foreach (var id in watch.items)
            {
                var obj = Resolve(id);
                if (obj == null)
                    continue;
                capture.ids.Add(id);
                capture.jsons.Add(EditorJsonUtility.ToJson(obj));
            }

            if (capture.ids.Count > 0)
                SessionState.SetString(DataKey, JsonUtility.ToJson(capture));
        }

        private static void Apply()
        {
            var json = SessionState.GetString(DataKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;
            SessionState.EraseString(DataKey);

            var capture = JsonUtility.FromJson<CapturePayload>(json);
            if (capture == null)
                return;

            var applied = 0;
            for (var i = 0; i < capture.ids.Count; i++)
            {
                var obj = Resolve(capture.ids[i]);
                if (obj == null)
                    continue;
                Undo.RecordObject(obj, "Apply Play Mode Values");
                EditorJsonUtility.FromJsonOverwrite(capture.jsons[i], obj);
                EditorUtility.SetDirty(obj);
                applied++;
            }

            if (applied > 0)
                Debug.Log($"[FrameworkInspector] Applied play-mode values to {applied} component(s).");
        }

        private static StringListPayload LoadWatchList()
        {
            var json = SessionState.GetString(WatchKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new StringListPayload();
            return JsonUtility.FromJson<StringListPayload>(json) ?? new StringListPayload();
        }

        private static string IdOf(UnityEngine.Object obj)
        {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            // identifierType 0 = null/unresolvable (e.g. objects created during play mode).
            return id.identifierType == 0 ? null : id.ToString();
        }

        private static UnityEngine.Object Resolve(string idString)
        {
            if (!GlobalObjectId.TryParse(idString, out var id))
                return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
        }
    }
}
#endif
