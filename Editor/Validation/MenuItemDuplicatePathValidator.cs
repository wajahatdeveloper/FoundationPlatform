#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// Regression guard for editor menus: two <c>[MenuItem]</c> registrations resolving to the
    /// same path (and the same execute/validate role) silently shadow one another in Unity — no
    /// compile error, no runtime warning, just a menu entry that quietly calls the wrong method.
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
            var groups = new Dictionary<(string path, bool isValidate), List<(MethodInfo method, string path)>>();

            foreach (var method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            {
                foreach (var attribute in method.GetCustomAttributes<MenuItem>())
                {
                    var key = (attribute.menuItem, attribute.validate);
                    if (!groups.TryGetValue(key, out var items))
                        groups[key] = items = new List<(MethodInfo, string)>();
                    items.Add((method, attribute.menuItem));
                }
            }

            var duplicates = new List<string>();
            foreach (var entry in groups)
            {
                if (entry.Value.Count <= 1)
                    continue;

                var (path, isValidate) = entry.Key;
                var role = isValidate ? "validate" : "execute";
                var owners = string.Join(", ", entry.Value.Select(m => $"{m.method.DeclaringType?.FullName}.{m.method.Name}"));
                var message = $"[MenuItemDuplicatePathValidator] Duplicate MenuItem \"{path}\" ({role}) registered by: {owners}";
                Debug.LogError(message);
                duplicates.Add(message);
            }

            return duplicates;
        }
    }
}
#endif
