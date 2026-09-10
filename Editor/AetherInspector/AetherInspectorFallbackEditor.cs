#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace AetherNexus.FoundationPlatform.AetherInspector.Editor
{
    /// <summary>
    /// Shared body of the fallback arms below. Each arm carries its own <c>[CustomEditor]</c>
    /// registration, so none of them may inherit another's — hence the attribute-free base.
    /// </summary>
    public abstract class AetherInspectorFallbackEditorBase : AetherInspectorEditor
    {
        protected override bool UseEngineDrawing => AetherInspectorFallbackScope.ShouldDraw(target);
    }

    /// <summary>
    /// Global fallback inspector for components: every MonoBehaviour without a concrete
    /// <c>[CustomEditor]</c> renders through the in-house <see cref="AetherInspectorEditor"/> engine,
    /// so attributed types draw correctly without a per-type editor.
    /// A concrete <c>[CustomEditor(typeof(T))]</c> always beats an <c>isFallback</c> editor, so
    /// hand-written inspectors keep priority.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class AetherInspectorFallbackEditor : AetherInspectorFallbackEditorBase { }

    /// <summary>ScriptableObject arm of <see cref="AetherInspectorFallbackEditor"/>.</summary>
    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class AetherInspectorScriptableObjectFallbackEditor : AetherInspectorFallbackEditorBase { }

    /// <summary>
    /// Widest arm: native components and assets that Unity left on its generic inspector. Only draws
    /// through the engine in <see cref="InspectorFallbackScope.Everything"/>; the two script arms above
    /// are closer to their targets by inheritance, so they win for MonoBehaviour / ScriptableObject.
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class AetherInspectorObjectFallbackEditor : AetherInspectorFallbackEditorBase { }

    /// <summary>Decides which targets the fallback arms hand to the engine, per the project setting.</summary>
    public static class AetherInspectorFallbackScope
    {
        private static readonly Dictionary<Type, bool> s_firstParty = new Dictionary<Type, bool>();

        internal static void ClearCache() => s_firstParty.Clear();

        public static bool ShouldDraw(UnityEngine.Object target)
        {
            // A null target is a missing script; the engine's MissingScriptFixer owns that case.
            if (target == null) return true;

            var type = target.GetType();
            switch (InspectorXSettings.instance.fallbackScope)
            {
                case InspectorFallbackScope.Everything:
                    return true;
                case InspectorFallbackScope.AllScripts:
                    return target is MonoBehaviour || target is ScriptableObject;
                case InspectorFallbackScope.FirstParty:
                    return IsFirstParty(type);
                case InspectorFallbackScope.Attributed:
                    return AetherInspectorRenderer.TypeHasEngineAttributes(type);
                default:
                    throw new InvalidOperationException(
                        $"[FoundationPlatform.AetherInspector] unhandled fallback scope '{InspectorXSettings.instance.fallbackScope}'.");
            }
        }

        // Assembly names in this repo carry no shared prefix (FoundationPlatform.Runtime, ItemSystem.Editor,
        // Game.Features, ...), so ownership is resolved from where the defining asmdef lives. Types compiled
        // into the predefined assemblies have no asmdef and belong to this project by definition.
        private static bool IsFirstParty(Type type)
        {
            if (s_firstParty.TryGetValue(type, out bool cached)) return cached;

            string assemblyName = type.Assembly.GetName().Name;
            string asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            bool firstParty = string.IsNullOrEmpty(asmdefPath)
                ? assemblyName.StartsWith("Assembly-CSharp", StringComparison.Ordinal)
                : asmdefPath.StartsWith("Assets/", StringComparison.Ordinal)
                  || asmdefPath.StartsWith("Packages/com.aethernexus.", StringComparison.Ordinal);

            s_firstParty[type] = firstParty;
            return firstParty;
        }
    }
}
#endif
