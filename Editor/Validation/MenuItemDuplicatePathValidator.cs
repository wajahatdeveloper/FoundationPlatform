#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// Regression guard for the Tools/Window/GameObject/CONTEXT menu restructure: two
    /// <c>[MenuItem]</c> registrations resolving to the same path (and the same execute/validate
    /// role) silently shadow one another in Unity — no compile error, no runtime warning, just a
    /// menu entry that quietly calls the wrong method. As more extension frameworks are added
    /// under the shared <c>Tools/GameEngineCore/&lt;Module&gt;</c> umbrella, this keeps catching
    /// that class of mistake without anyone having to remember to check by hand.
    /// </summary>
    [InitializeOnLoad]
    internal static class MenuItemDuplicatePathValidator
    {
        static MenuItemDuplicatePathValidator()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            EditorApplication.delayCall -= RunOnce;
            FindDuplicates();
        }

        /// <summary>Callable on demand (e.g. after a menu-restructure pass) in addition to the automatic load-time check.</summary>
        internal static List<string> FindDuplicates()
        {
            var groups = new Dictionary<(string path, bool isValidate), List<MethodInfo>>();

            foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            {
                foreach (var attribute in method.GetCustomAttributes<MenuItem>())
                {
                    var key = (attribute.menuItem, attribute.validate);
                    if (!groups.TryGetValue(key, out var methods))
                        groups[key] = methods = new List<MethodInfo>();
                    methods.Add(method);
                }
            }

            var duplicates = new List<string>();
            foreach (var entry in groups)
            {
                if (entry.Value.Count <= 1)
                    continue;

                var (path, isValidate) = entry.Key;
                var role = isValidate ? "validate" : "execute";
                var owners = string.Join(", ", entry.Value.Select(m => $"{m.DeclaringType?.FullName}.{m.Name}"));
                var message = $"[MenuItemDuplicatePathValidator] Duplicate MenuItem \"{path}\" ({role}) registered by: {owners}";
                Debug.LogError(message);
                duplicates.Add(message);
            }

            return duplicates;
        }
    }
}
#endif
