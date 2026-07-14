#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.StaleComponentGuard.Editor
{
    /// <summary>
    /// Core detection. A component is "stale" when the asset's YAML carries a top-level serialized key
    /// the component's current script no longer declares (a field dropped or renamed without
    /// <c>[FormerlySerializedAs]</c>). Such orphan data is invisible to <see cref="SerializedObject"/>
    /// once a valid script is present — Unity drops unknown keys on load — so the only reliable detector
    /// is a raw-YAML read compared against the type's reflected serialized-field set.
    ///
    /// The field set comes from reflection over the already-loaded assemblies (zero file IO, authoritative
    /// for inheritance / <c>[SerializeField]</c> / <c>[NonSerialized]</c>). Only the YAML half touches disk,
    /// and it streams line-by-line (constant memory) rather than reading whole files.
    /// </summary>
    public static class StaleComponentScanner
    {
        /// <summary>Frozen per-script info: declared field names + display type name. Read lock-free by workers.</summary>
        private sealed class ScriptInfo
        {
            public HashSet<string> Fields;
            public string TypeName;
        }

        // guid(of a MonoScript) -> its ScriptInfo. Frozen after build (main thread) so the parallel scan
        // never touches AssetDatabase (main-thread-only) or takes a lock.
        private static Dictionary<string, ScriptInfo> _infoByGuid;

        // Top-level YAML keys that are never user fields.
        private static readonly HashSet<string> IgnoredKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "references",        // SerializeReference RefId plumbing, not a field
            "serializedVersion", // Unity format-version marker, not a field
        };

        static StaleComponentScanner()
        {
            AssemblyReloadEvents.afterAssemblyReload += InvalidateFieldMap;
            EditorApplication.projectChanged += InvalidateFieldMap;
        }

        private static void InvalidateFieldMap() => _infoByGuid = null;

        // ---- Public entry points (main thread) -------------------------------------------------

        /// <summary>Scan a single scene/prefab/asset file. Safe to call from the main thread.</summary>
        public static List<StaleFinding> ScanAsset(string assetPath)
        {
            EnsureFieldMap();
            var results = new List<StaleFinding>();
            if (string.IsNullOrEmpty(assetPath) || !LooksLikeYamlAsset(assetPath) || !File.Exists(assetPath))
                return results;
            try
            {
                ParseFile(assetPath, _infoByGuid, results);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StaleComponentGuard] Failed to scan {assetPath}: {ex.Message}");
            }
            return results;
        }

        /// <summary>
        /// Sweep every project scene/prefab/asset under <c>Assets/</c>. Parallel over a frozen field map,
        /// with a cancelable progress bar. Main-thread entry; the file reads run on worker threads.
        /// </summary>
        public static List<StaleFinding> ScanProject()
        {
            EnsureFieldMap();
            var map = _infoByGuid;
            var paths = CollectAssetPaths();
            var results = new List<StaleFinding>();
            var resultsLock = new object();

            try
            {
                const int batchSize = 200;
                for (int start = 0; start < paths.Count; start += batchSize)
                {
                    int count = Math.Min(batchSize, paths.Count - start);
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Stale Component Guard",
                            $"Scanning assets ({start}/{paths.Count})…",
                            paths.Count == 0 ? 1f : (float)start / paths.Count))
                        break;

                    Parallel.For(start, start + count, idx =>
                    {
                        var local = new List<StaleFinding>();
                        try
                        {
                            ParseFile(paths[idx], map, local);
                        }
                        catch
                        {
                            // Per-file failure is non-fatal; skip it.
                        }
                        if (local.Count > 0)
                            lock (resultsLock)
                                results.AddRange(local);
                    });
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return results;
        }

        // ---- Field-set reflection --------------------------------------------------------------

        /// <summary>
        /// Serialized field names the given type declares, per Unity's rules: public unless
        /// <c>[NonSerialized]</c>, private only with <c>[SerializeField]</c>; walking base types up to
        /// (but not including) MonoBehaviour/ScriptableObject; plus every <c>[FormerlySerializedAs]</c>
        /// alias (a renamed-with-migration field is NOT stale).
        /// </summary>
        public static HashSet<string> SerializedFieldNames(Type type)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var t = type;
                 t != null && t != typeof(object) && t != typeof(MonoBehaviour) && t != typeof(ScriptableObject);
                 t = t.BaseType)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    bool serialized = field.IsPublic
                        ? field.GetCustomAttribute<NonSerializedAttribute>() == null
                        : field.GetCustomAttribute<SerializeField>() != null;
                    if (!serialized)
                        continue;

                    names.Add(field.Name);
                    foreach (var former in field.GetCustomAttributes<UnityEngine.Serialization.FormerlySerializedAsAttribute>())
                        if (!string.IsNullOrEmpty(former.oldName))
                            names.Add(former.oldName);
                }
            }
            return names;
        }

        private static void EnsureFieldMap()
        {
            if (_infoByGuid != null)
                return;

            var map = new Dictionary<string, ScriptInfo>(StringComparer.Ordinal);
            foreach (var script in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (script == null)
                    continue;
                var type = script.GetClass();
                if (type == null || type.IsAbstract)
                    continue;
                if (!typeof(MonoBehaviour).IsAssignableFrom(type) && !typeof(ScriptableObject).IsAssignableFrom(type))
                    continue;

                var path = AssetDatabase.GetAssetPath(script);
                if (string.IsNullOrEmpty(path))
                    continue;
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                    continue;

                map[guid] = new ScriptInfo { Fields = SerializedFieldNames(type), TypeName = type.FullName };
            }
            _infoByGuid = map;
        }

        // ---- Streaming YAML parse --------------------------------------------------------------

        // A MonoBehaviour block header, e.g. "--- !u!114 &1562349767" (optionally "… stripped").
        private static bool TryParseObjectHeader(string line, out int classId, out long fileId, out bool stripped)
        {
            classId = 0; fileId = 0; stripped = false;
            if (line.Length < 8 || line[0] != '-' || !line.StartsWith("--- !u!", StringComparison.Ordinal))
                return false;
            stripped = line.EndsWith(" stripped", StringComparison.Ordinal);

            int i = 7;
            int cStart = i;
            while (i < line.Length && char.IsDigit(line[i])) i++;
            if (i == cStart || !int.TryParse(line.Substring(cStart, i - cStart), out classId))
                return false;

            int amp = line.IndexOf('&', i);
            if (amp < 0)
                return false;
            i = amp + 1;
            int fStart = i;
            while (i < line.Length && (char.IsDigit(line[i]) || (i == fStart && line[i] == '-'))) i++;
            return i > fStart && long.TryParse(line.Substring(fStart, i - fStart), out fileId);
        }

        // Reads a top-level (exactly two-space-indented) mapping key from a block body line.
        // Returns null for deeper-indented lines, list items, comments, or blanks.
        private static string TryReadTopLevelKey(string line)
        {
            if (line.Length < 3 || line[0] != ' ' || line[1] != ' ' || line[2] == ' ' || line[2] == '-' || line[2] == '#')
                return null;
            int colon = line.IndexOf(':', 2);
            if (colon < 0)
                return null;
            return line.Substring(2, colon - 2);
        }

        private static readonly System.Text.RegularExpressions.Regex GuidRegex =
            new System.Text.RegularExpressions.Regex("guid:\\s*([0-9a-fA-F]{32})",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        private static void ParseFile(string path, Dictionary<string, ScriptInfo> map, List<StaleFinding> results)
        {
            using var reader = new StreamReader(path);

            // Per-block state.
            bool inMono = false;
            long blockFileId = 0;
            long goFileId = 0;
            string scriptGuid = null;
            List<string> topKeys = null;

            void Flush()
            {
                if (inMono && scriptGuid != null && topKeys != null && map.TryGetValue(scriptGuid, out var info))
                {
                    List<string> orphans = null;
                    foreach (var key in topKeys)
                    {
                        if (key.StartsWith("m_", StringComparison.Ordinal) || IgnoredKeys.Contains(key))
                            continue;
                        if (!info.Fields.Contains(key))
                            (orphans ??= new List<string>()).Add(key);
                    }
                    if (orphans != null)
                        results.Add(new StaleFinding(path, blockFileId, goFileId, info.TypeName, orphans.ToArray()));
                }
                inMono = false; blockFileId = 0; goFileId = 0; scriptGuid = null; topKeys = null;
            }

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length > 0 && line[0] == '-' && line.StartsWith("--- ", StringComparison.Ordinal))
                {
                    Flush();
                    if (TryParseObjectHeader(line, out int classId, out long fileId, out bool stripped) && classId == 114 && !stripped)
                    {
                        inMono = true;
                        blockFileId = fileId;
                        topKeys = new List<string>();
                    }
                    continue;
                }

                if (!inMono)
                    continue;

                var key = TryReadTopLevelKey(line);
                if (key == null)
                    continue;

                switch (key)
                {
                    case "m_Script":
                        var gm = GuidRegex.Match(line);
                        if (gm.Success)
                            scriptGuid = gm.Groups[1].Value;
                        break;
                    case "m_GameObject":
                        int fi = line.IndexOf("fileID:", StringComparison.Ordinal);
                        if (fi >= 0)
                        {
                            int s = fi + 7;
                            while (s < line.Length && line[s] == ' ') s++;
                            int e = s;
                            while (e < line.Length && (char.IsDigit(line[e]) || (e == s && line[e] == '-'))) e++;
                            if (e > s) long.TryParse(line.Substring(s, e - s), out goFileId);
                        }
                        break;
                }

                topKeys.Add(key);
            }

            Flush();
        }

        // ---- Asset enumeration -----------------------------------------------------------------

        private static List<string> CollectAssetPaths()
        {
            return new[] { "t:Scene", "t:Prefab", "t:ScriptableObject" }
                .SelectMany(AssetDatabase.FindAssets)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(LooksLikeYamlAsset)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool LooksLikeYamlAsset(string path)
        {
            if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                return false;
            var ext = Path.GetExtension(path);
            if (!ext.Equals(".unity", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".prefab", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".asset", StringComparison.OrdinalIgnoreCase))
                return false;
            // Skip Unity/TMP-owned .asset (their YAML has no user MonoBehaviour fields to go stale).
            if (ext.Equals(".asset", StringComparison.OrdinalIgnoreCase))
            {
                var ns = AssetDatabase.GetMainAssetTypeAtPath(path)?.Namespace;
                if (!string.IsNullOrEmpty(ns) &&
                    (ns == "TMPro" || ns.StartsWith("TMPro.", StringComparison.Ordinal)
                     || ns == "UnityEngine" || ns.StartsWith("UnityEngine.", StringComparison.Ordinal)
                     || ns == "UnityEditor" || ns.StartsWith("UnityEditor.", StringComparison.Ordinal)
                     || ns.StartsWith("Unity.", StringComparison.Ordinal)))
                    return false;
            }
            return true;
        }
    }
}
#endif
