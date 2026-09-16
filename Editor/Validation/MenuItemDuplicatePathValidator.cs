#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// Regression guard for editor menus: two <c>[MenuItem]</c> registrations resolving to the
    /// same path (and the same execute/validate role) silently shadow one another in Unity — no
    /// compile error, no runtime warning, just a menu entry that quietly calls the wrong method.
    /// </summary>
    internal sealed class MenuItemDuplicateValidator : IAuthoringValidator
    {
        private const string SourceName = "Editor Menus";

        public ValidationScope Scope => ValidationScope.Project;

        public string Source => SourceName;

        public System.Type TargetType => null;

        public void Collect(in ValidationRequest request, List<AuthoringIssue> issues)
        {
            var duplicates = MenuItemDuplicatePathValidator.FindDuplicates();
            for (int i = 0; i < duplicates.Count; i++)
            {
                issues.Add(new AuthoringIssue
                {
                    Severity = AuthoringIssueSeverity.Error,
                    Source = SourceName,
                    Message = duplicates[i],
                });
            }
        }
    }

    internal static class MenuItemDuplicatePathValidator
    {
        /// <summary>Reports rather than logs; Central Validation owns the surfacing.</summary>
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
                duplicates.Add($"Duplicate MenuItem \"{path}\" ({role}) registered by: {owners}");
            }

            return duplicates;
        }
    }
}
#endif
