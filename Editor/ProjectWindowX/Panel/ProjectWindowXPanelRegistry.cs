using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {

    [InitializeOnLoad]
    public static class ProjectWindowXPanelRegistry {

        private static readonly List<IProjectPanelSection> sections = new List<IProjectPanelSection>();
        private static bool discovered;

        public static event Action Changed;

        static ProjectWindowXPanelRegistry() { }

        public static void Register(IProjectPanelSection section) {
            if (section == null || sections.Contains(section))
                return;
            EnsureDiscovered();
            sections.Add(section);
            sections.Sort((a, b) => a.Order.CompareTo(b.Order));
            Changed?.Invoke();
        }

        public static void Unregister(IProjectPanelSection section) {
            if (section != null && sections.Remove(section))
                Changed?.Invoke();
        }

        public static IReadOnlyList<IProjectPanelSection> Sections {
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

            foreach (var type in TypeCache.GetTypesDerivedFrom<IProjectPanelSection>()) {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                if (sections.Exists(s => s.GetType() == type))
                    continue;
                try {
                    sections.Add((IProjectPanelSection)Activator.CreateInstance(type));
                } catch (Exception e) {
                    Debug.LogWarning("ProjectWindowX: could not instantiate panel section " + type.Name + "\n" + e);
                }
            }
            sections.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}
