#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using AetherNexus.FoundationPlatform.AetherInspector;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// Makes <c>[Required]</c> and <c>[NotEmpty]</c> mean the same thing to the project that they already
    /// mean to the Inspector. Declaring a dependency on the field is the cheapest way to state it, but the
    /// attribute only ever spoke while its asset was selected — so a missing reference stayed invisible
    /// until someone happened to click the asset. Reading the attributes here is what lets a package delete
    /// a hand-written null check instead of keeping one copy per surface.
    /// <para>
    /// Emptiness follows the Inspector's own rule exactly (object/exposed reference null, string empty,
    /// everything else never empty). A validator that disagreed with the box drawn above the field would be
    /// worse than no validator, so this reads <see cref="RequiredAttribute.MessageType"/> and the custom
    /// message rather than imposing its own.
    /// </para>
    /// <para>
    /// Covers serialized fields declared on the type and its bases. An attribute on a field nested inside a
    /// serialized sub-object is drawn by the Inspector but not reported here.
    /// </para>
    /// </summary>
    public sealed class RequiredFieldAssetValidator : IAuthoringValidator
    {
        public const string SourceName = "Required fields";

        public ValidationScope Scope => ValidationScope.Asset;

        public string Source => SourceName;

        // Every authored asset is a ScriptableObject, and the registry indexes by one type — so this is the
        // widest net a single asset-scope validator can cast. Types with no marked field cost a cache hit.
        public Type TargetType => typeof(ScriptableObject);

        public void Collect(in ValidationRequest request, List<AuthoringIssue> issues)
        {
            RequiredFieldScan.Collect(request.Target, request.AssetPath, issues);
        }
    }

    /// <summary>
    /// The scene half: components carry most of this project's <c>[Required]</c> marks, and a missing
    /// reference on a scene component breaks the same way a missing one on an asset does.
    /// </summary>
    public sealed class RequiredFieldSceneValidator : IAuthoringValidator
    {
        public ValidationScope Scope => ValidationScope.Scene;

        public string Source => RequiredFieldAssetValidator.SourceName;

        public Type TargetType => null;

        public void Collect(in ValidationRequest request, List<AuthoringIssue> issues)
        {
            var components = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < components.Length; i++)
                RequiredFieldScan.Collect(components[i], null, issues);
        }
    }

    /// <summary>Shared scan, so the asset and scene entries cannot drift apart in what they report.</summary>
    internal static class RequiredFieldScan
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly struct MarkedField
        {
            public readonly string Name;
            public readonly string CustomMessage;
            public readonly InfoMessageType MessageType;

            /// <summary>True for <c>[NotEmpty]</c>, which is string-only where Required is not.</summary>
            public readonly bool StringOnly;

            public MarkedField(string name, string customMessage, InfoMessageType messageType, bool stringOnly)
            {
                Name = name;
                CustomMessage = customMessage;
                MessageType = messageType;
                StringOnly = stringOnly;
            }
        }

        // Reflection runs once per type, not once per object: a Project row repaint walks this for every
        // asset in view, and the scene pass walks it for every component in the scene.
        private static readonly Dictionary<Type, MarkedField[]> MarkedByType = new();
        private static readonly MarkedField[] None = Array.Empty<MarkedField>();

        internal static void Collect(UnityEngine.Object target, string assetPath, List<AuthoringIssue> issues)
        {
            if (target == null)
                return;

            MarkedField[] marked = MarkedFor(target.GetType());
            if (marked.Length == 0)
                return;

            using var serialized = new SerializedObject(target);
            for (int i = 0; i < marked.Length; i++)
            {
                MarkedField field = marked[i];
                SerializedProperty property = serialized.FindProperty(field.Name);
                if (property == null)
                    continue;
                if (!IsEmpty(property, field.StringOnly))
                    continue;

                issues.Add(new AuthoringIssue
                {
                    Severity = ToSeverity(field.MessageType),
                    Source = RequiredFieldAssetValidator.SourceName,
                    Message = Describe(target, property, field),
                    RelatedObject = target,
                    AssetPath = assetPath
                });
            }
        }

        private static string Describe(UnityEngine.Object target, SerializedProperty property, MarkedField field)
        {
            string detail = field.CustomMessage != null
                ? InspectorMemberResolver.ResolveString(target, field.CustomMessage)
                : property.displayName + (field.StringOnly ? " must not be empty." : " is required.");

            // The owner's name has to be in the message: in a project-wide list, "Icon is required" alone
            // does not say which of two hundred assets is missing one.
            return target.name + ": " + detail;
        }

        private static bool IsEmpty(SerializedProperty property, bool stringOnly)
        {
            if (stringOnly)
            {
                return property.propertyType == SerializedPropertyType.String
                       && string.IsNullOrEmpty(property.stringValue);
            }

            return property.propertyType switch
            {
                SerializedPropertyType.ObjectReference => property.objectReferenceValue == null,
                SerializedPropertyType.String => string.IsNullOrEmpty(property.stringValue),
                SerializedPropertyType.ExposedReference => property.exposedReferenceValue == null,
                _ => false
            };
        }

        private static AuthoringIssueSeverity ToSeverity(InfoMessageType messageType) => messageType switch
        {
            InfoMessageType.Error => AuthoringIssueSeverity.Error,
            InfoMessageType.Warning => AuthoringIssueSeverity.Warning,
            // A required field drawn with no severity is still a missing dependency, not a note.
            InfoMessageType.Info => AuthoringIssueSeverity.Info,
            InfoMessageType.None => AuthoringIssueSeverity.Warning,
            _ => throw new ArgumentOutOfRangeException(
                nameof(messageType), messageType, "Unknown AetherInspector message type.")
        };

        private static MarkedField[] MarkedFor(Type type)
        {
            if (MarkedByType.TryGetValue(type, out MarkedField[] cached))
                return cached;

            List<MarkedField> found = null;
            for (Type t = type; t != null && t != typeof(ScriptableObject) && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                FieldInfo[] fields = t.GetFields(Flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];

                    var required = field.GetCustomAttribute<RequiredAttribute>();
                    if (required != null)
                    {
                        found ??= new List<MarkedField>();
                        found.Add(new MarkedField(field.Name, required.ErrorMessage, required.MessageType, false));
                    }

                    var notEmpty = field.GetCustomAttribute<NotEmptyAttribute>();
                    if (notEmpty != null)
                    {
                        found ??= new List<MarkedField>();
                        found.Add(new MarkedField(field.Name, notEmpty.ErrorMessage, notEmpty.MessageType, true));
                    }
                }
            }

            MarkedField[] result = found == null ? None : found.ToArray();
            MarkedByType[type] = result;
            return result;
        }
    }
}
#endif
