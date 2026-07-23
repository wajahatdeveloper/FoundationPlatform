using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Registry of <see cref="IProjectWindowXContextMenu"/> contributors for the hover "+" menu.
    /// Concrete types with a public parameterless constructor are auto-discovered via
    /// <see cref="TypeCache"/>; instances can also be <see cref="Register"/>ed manually.
    /// Mirrors HierarchyXPanelRegistry's discovery pattern.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowXContextMenuRegistry {

        private static readonly List<IProjectWindowXContextMenu> menus = new List<IProjectWindowXContextMenu>();
        private static bool discovered;

        /// <summary>Raised when the contributor set changes.</summary>
        public static event Action Changed;

        static ProjectWindowXContextMenuRegistry() { }

        /// <summary>Add a contributor (idempotent). Sorted by <see cref="IProjectWindowXContextMenu.Order"/>.</summary>
        public static void Register(IProjectWindowXContextMenu menu) {
            if (menu == null || menus.Contains(menu))
                return;
            EnsureDiscovered();
            menus.Add(menu);
            menus.Sort((a, b) => a.Order.CompareTo(b.Order));
            Changed?.Invoke();
        }

        public static void Unregister(IProjectWindowXContextMenu menu) {
            if (menu != null && menus.Remove(menu))
                Changed?.Invoke();
        }

        /// <summary>Ordered, read-only view of the registered contributors.</summary>
        public static IReadOnlyList<IProjectWindowXContextMenu> Menus {
            get {
                EnsureDiscovered();
                return menus;
            }
        }

        private static void EnsureDiscovered() {
            if (discovered)
                return;
            discovered = true;

            foreach (var type in TypeCache.GetTypesDerivedFrom<IProjectWindowXContextMenu>()) {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                if (menus.Exists(m => m.GetType() == type))
                    continue;
                try {
                    menus.Add((IProjectWindowXContextMenu)Activator.CreateInstance(type));
                } catch (Exception e) {
                    Debug.LogWarning("ProjectWindowX: could not instantiate context menu " + type.Name + "\n" + e);
                }
            }
            menus.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}
