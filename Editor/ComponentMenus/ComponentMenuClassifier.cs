#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using AetherNexus.FoundationPlatform.DesignerSurfaces.Editor;
using AetherNexus.FoundationPlatform.Utilities.Menus;

namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    /// <summary>
    /// Guesses whether a designer places a component by hand. Three signals put a type out of the
    /// menu: another type demands it through <c>[RequireComponent]</c>, first-party code adds it at
    /// runtime, or it has nothing to author and no feature tag. Every verdict carries its reason
    /// because the proposal file is reviewed before anything is stamped.
    /// </summary>
    internal sealed class ComponentMenuClassifier
    {
        private static readonly Regex AddComponentCall = new Regex(
            @"AddComponent\s*(?:<\s*(?<generic>[A-Za-z_][A-Za-z0-9_]*)\s*>|\(\s*typeof\s*\(\s*(?<typeOf>[A-Za-z_][A-Za-z0-9_]*)\s*\))",
            RegexOptions.Compiled);

        private readonly HashSet<Type> requiredByOthers = new HashSet<Type>();
        private readonly HashSet<string> addedByCode = new HashSet<string>(StringComparer.Ordinal);

        internal ComponentMenuClassifier()
        {
            IndexRequireComponentTargets();
            IndexRuntimeAddComponentCalls();
        }

        internal ComponentMenuVerdict Classify(Type type, out string reason)
        {
            if (requiredByOthers.Contains(type))
            {
                reason = "sidecar: another type declares [RequireComponent] for it";
                return ComponentMenuVerdict.Internal;
            }

            if (addedByCode.Contains(type.Name))
            {
                reason = "code-added: first-party runtime code constructs it";
                return ComponentMenuVerdict.Internal;
            }

            if (type.Name.EndsWith("Base", StringComparison.Ordinal))
            {
                reason = "base class: a 'Base' type is inherited, not placed";
                return ComponentMenuVerdict.Internal;
            }

            int serializedFields = CountSerializedFields(type);
            bool tagged = type.GetCustomAttribute<DesignerFeatureAttribute>(false) != null;

            if (serializedFields == 0 && !tagged)
            {
                reason = "nothing to author: no serialized fields and no [DesignerFeature]";
                return ComponentMenuVerdict.Internal;
            }

            reason = tagged
                ? $"designer-facing: [DesignerFeature], {serializedFields} serialized field(s)"
                : $"designer-facing: {serializedFields} serialized field(s)";
            return ComponentMenuVerdict.DesignerPlaced;
        }

        /// <summary><c>[RequireComponent]</c> targets declared anywhere in the project.</summary>
        internal IReadOnlyList<Type> RequiredComponentsOf(Type type)
        {
            var targets = new List<Type>(3);
            foreach (RequireComponent attribute in type.GetCustomAttributes<RequireComponent>(true))
            {
                AddTarget(targets, attribute.m_Type0);
                AddTarget(targets, attribute.m_Type1);
                AddTarget(targets, attribute.m_Type2);
            }

            return targets;
        }

        private static void AddTarget(List<Type> targets, Type target)
        {
            if (target != null)
                targets.Add(target);
        }

        private void IndexRequireComponentTargets()
        {
            foreach (Type owner in TypeCache.GetTypesWithAttribute<RequireComponent>())
            {
                foreach (Type target in RequiredComponentsOf(owner))
                {
                    if (target != owner)
                        requiredByOthers.Add(target);
                }
            }
        }

        private void IndexRuntimeAddComponentCalls()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!FirstPartyScriptPaths.IsFirstParty(path))
                    continue;

                // An editor helper adding a component is a designer doing it through a tool, which
                // argues for keeping the component in the menu rather than hiding it.
                if (path.Contains("/Editor/"))
                    continue;

                foreach (Match match in AddComponentCall.Matches(File.ReadAllText(Path.GetFullPath(path))))
                {
                    string generic = match.Groups["generic"].Value;
                    addedByCode.Add(string.IsNullOrEmpty(generic) ? match.Groups["typeOf"].Value : generic);
                }
            }
        }

        private static int CountSerializedFields(Type type)
        {
            int count = 0;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsNotSerialized || field.IsStatic)
                    continue;

                if (field.IsPublic || field.GetCustomAttribute<SerializeField>(false) != null)
                    count++;
            }

            return count;
        }
    }
}
#endif
