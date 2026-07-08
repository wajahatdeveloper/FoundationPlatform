using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyX {

    /// <summary>
    /// Registry of <see cref="IHierarchyPanelSection"/>s for the Hierarchy docked setup panel.
    /// Concrete sections with a public parameterless constructor are auto-discovered via
    /// <see cref="TypeCache"/>; instances can also be <see cref="Register"/>ed manually.
    /// Mirrors <see cref="HierarchyXRegistry"/>'s discovery pattern.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyXPanelRegistry {

        private static readonly List<IHierarchyPanelSection> sections = new List<IHierarchyPanelSection>();
        private static bool discovered;

        /// <summary>Raised when the section set changes, so hosts can repaint.</summary>
        public static event Action Changed;

        static HierarchyXPanelRegistry() { }

        /// <summary>Add a section instance (idempotent). Sorted by <see cref="IHierarchyPanelSection.Order"/>.</summary>
        public static void Register(IHierarchyPanelSection section) {
            if (section == null || sections.Contains(section))
                return;
            EnsureDiscovered();
            sections.Add(section);
            sections.Sort((a, b) => a.Order.CompareTo(b.Order));
            Changed?.Invoke();
        }

        public static void Unregister(IHierarchyPanelSection section) {
            if (section != null && sections.Remove(section))
                Changed?.Invoke();
        }

        /// <summary>Ordered, read-only view of the registered sections.</summary>
        public static IReadOnlyList<IHierarchyPanelSection> Sections {
            get {
                EnsureDiscovered();
                return sections;
            }
        }

        public static bool HasAny {
            get {
                EnsureDiscovered();
                return sections.Count > 0;
            }
        }

        private static void EnsureDiscovered() {
            if (discovered)
                return;
            discovered = true;

            foreach (var type in TypeCache.GetTypesDerivedFrom<IHierarchyPanelSection>()) {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                if (sections.Exists(s => s.GetType() == type))
                    continue;
                try {
                    sections.Add((IHierarchyPanelSection)Activator.CreateInstance(type));
                } catch (Exception e) {
                    Debug.LogWarning("HierarchyX: could not instantiate panel section " + type.Name + "\n" + e);
                }
            }
            sections.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}
