#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Replacement inspector body for missing-script components: extracts the surviving
    /// serialized field names, scores every project MonoScript by field-name overlap,
    /// and offers one-click reassignment via the m_Script property (data preserved).
    /// </summary>
    internal static class MissingScriptFixer
    {
        private sealed class Candidate
        {
            public MonoScript script;
            public float score;   // 0..1 matched-field ratio
        }

        // Cached per missing component's field signature.
        private static string cachedSignature;
        private static List<Candidate> cachedCandidates;

        internal static void OnGUI(SerializedObject serializedObject)
        {
            EditorGUILayout.HelpBox("The script referenced by this component is missing. Pick a replacement below — serialized data is preserved for matching field names.", MessageType.Warning);

            var fieldNames = CollectFieldNames(serializedObject);
            if (fieldNames.Count == 0)
            {
                EditorGUILayout.LabelField("No serialized data survived — candidates cannot be ranked.", EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"Serialized fields ({fieldNames.Count}): {string.Join(", ", fieldNames.Take(8))}{(fieldNames.Count > 8 ? "…" : "")}",
                    EditorStyles.wordWrappedMiniLabel);

                var candidates = RankCandidates(fieldNames);
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Best matches", EditorStyles.boldLabel);
                var shown = 0;
                foreach (var candidate in candidates)
                {
                    if (shown++ >= 5 || candidate.score <= 0f)
                        break;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{candidate.script.GetClass().FullName}  ({candidate.score:P0})");
                        if (GUILayout.Button("Use", GUILayout.Width(50f)))
                        {
                            AssignScript(serializedObject, candidate.script);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                if (shown == 0)
                    EditorGUILayout.LabelField("No script matches the serialized fields.", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4f);
            EditorGUI.BeginChangeCheck();
            var manual = (MonoScript)EditorGUILayout.ObjectField("Assign Manually", null, typeof(MonoScript), false);
            if (EditorGUI.EndChangeCheck() && manual != null && manual.GetClass() != null)
            {
                AssignScript(serializedObject, manual);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space(4f);
            var goProp = serializedObject.FindProperty("m_GameObject");
            var go = goProp != null ? goProp.objectReferenceValue as GameObject : null;
            using (new EditorGUI.DisabledScope(go == null))
            {
                if (GUILayout.Button("Remove All Missing Scripts On This GameObject"))
                {
                    Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void AssignScript(SerializedObject serializedObject, MonoScript script)
        {
            var scriptProp = serializedObject.FindProperty("m_Script");
            if (scriptProp == null)
                return;
            scriptProp.objectReferenceValue = script;
            serializedObject.ApplyModifiedProperties();
        }

        private static List<string> CollectFieldNames(SerializedObject serializedObject)
        {
            var names = new List<string>();
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!iterator.name.StartsWith("m_"))
                    names.Add(iterator.name);
            }
            return names;
        }

        private static List<Candidate> RankCandidates(List<string> fieldNames)
        {
            var signature = string.Join("|", fieldNames);
            if (signature == cachedSignature && cachedCandidates != null)
                return cachedCandidates;

            var fieldSet = new HashSet<string>(fieldNames);
            var results = new List<Candidate>();

            foreach (var script in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (script == null)
                    continue;
                var type = script.GetClass();
                if (type == null || type.IsAbstract || !typeof(MonoBehaviour).IsAssignableFrom(type))
                    continue;

                var matched = 0;
                var total = 0;
                for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                {
                    foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        var serialized = field.IsPublic
                            ? field.GetCustomAttribute<System.NonSerializedAttribute>() == null
                            : field.GetCustomAttribute<SerializeField>() != null;
                        if (!serialized)
                            continue;
                        total++;
                        if (fieldSet.Contains(field.Name))
                            matched++;
                    }
                }

                if (matched == 0)
                    continue;
                var denominator = Mathf.Max(total, fieldNames.Count);
                results.Add(new Candidate { script = script, score = denominator > 0 ? (float)matched / denominator : 0f });
            }

            results.Sort((a, b) => b.score.CompareTo(a.score));
            cachedSignature = signature;
            cachedCandidates = results;
            return results;
        }
    }
}
#endif
