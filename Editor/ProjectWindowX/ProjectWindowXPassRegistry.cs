using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWindowX {
    /// <summary>
    /// Registry of <see cref="IProjectWindowXPass"/>es for Project window row drawing.
    /// Concrete passes with a public parameterless constructor are auto-discovered via
    /// <see cref="TypeCache"/>; instances can also be <see cref="Register"/>ed manually.
    /// Mirrors HierarchyXPanelRegistry's discovery pattern.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowXPassRegistry {

        private static readonly List<IProjectWindowXPass> passes = new List<IProjectWindowXPass>();
        private static bool discovered;

        /// <summary>Raised when the pass set changes, so hosts can repaint.</summary>
        public static event Action Changed;

        static ProjectWindowXPassRegistry() { }

        /// <summary>Add a pass instance (idempotent). Sorted by <see cref="IProjectWindowXPass.Order"/>.</summary>
        public static void Register(IProjectWindowXPass pass) {
            if (pass == null || passes.Contains(pass))
                return;
            EnsureDiscovered();
            passes.Add(pass);
            passes.Sort((a, b) => a.Order.CompareTo(b.Order));
            Changed?.Invoke();
        }

        public static void Unregister(IProjectWindowXPass pass) {
            if (pass != null && passes.Remove(pass))
                Changed?.Invoke();
        }

        /// <summary>Ordered, read-only view of the registered passes.</summary>
        public static IReadOnlyList<IProjectWindowXPass> Passes {
            get {
                EnsureDiscovered();
                return passes;
            }
        }

        private static void EnsureDiscovered() {
            if (discovered)
                return;
            discovered = true;

            foreach (var type in TypeCache.GetTypesDerivedFrom<IProjectWindowXPass>()) {
                if (type.IsAbstract || type.IsInterface)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                if (passes.Exists(p => p.GetType() == type))
                    continue;
                try {
                    passes.Add((IProjectWindowXPass)Activator.CreateInstance(type));
                } catch (Exception e) {
                    Debug.LogWarning("ProjectWindowX: could not instantiate pass " + type.Name + "\n" + e);
                }
            }
            passes.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }
}
