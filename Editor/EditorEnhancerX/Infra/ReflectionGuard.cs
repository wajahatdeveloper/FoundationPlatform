using System;
using System.Reflection;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.EditorEnhancerX {
    /// <summary>
    /// Guarded access to internal editor APIs. Resolve members once; any miss flips
    /// <see cref="Available"/> to false so the owning feature can self-disable
    /// (and its settings UI can grey out) instead of throwing.
    /// </summary>
    public sealed class ReflectionGuard {
        public const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public bool Available { get; private set; } = true;

        /// <summary>Resolves an internal UnityEditor type by full name (e.g. "UnityEditor.AddComponent.AddComponentWindow").</summary>
        public Type EditorType(string fullName) {
            var type = typeof(UnityEditor.Editor).Assembly.GetType(fullName, false);
            if (type == null) Available = false;
            return type;
        }

        public MethodInfo Method(Type type, string name, BindingFlags flags, Type[] parameters) {
            if (type == null) { Available = false; return null; }
            var method = parameters == null
                ? type.GetMethod(name, flags)
                : type.GetMethod(name, flags, null, parameters, null);
            if (method == null) Available = false;
            return method;
        }

        /// <summary>Resolves a method by name only (no parameter-type overload matching).</summary>
        public MethodInfo Method(Type type, string name, BindingFlags flags) => Method(type, name, flags, null);

        public FieldInfo Field(Type type, string name, BindingFlags flags) {
            if (type == null) { Available = false; return null; }
            var field = type.GetField(name, flags);
            if (field == null) Available = false;
            return field;
        }

        public PropertyInfo Property(Type type, string name, BindingFlags flags) {
            if (type == null) { Available = false; return null; }
            var property = type.GetProperty(name, flags);
            if (property == null) Available = false;
            return property;
        }
    }
}
