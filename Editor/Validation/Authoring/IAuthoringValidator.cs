#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// What a validator needs in front of it before it can say anything. This is the cost contract, not a
    /// category: an <see cref="Asset"/> validator is cheap enough to run during a Project row repaint or an
    /// Inspector draw, a <see cref="Project"/> validator is not and runs only when a designer asks.
    /// </summary>
    public enum ValidationScope
    {
        /// <summary>Reads one asset (and assets it directly references). Runs per row and per Inspector draw.</summary>
        Asset = 0,

        /// <summary>Reads the open scene. Runs on scene checks and project checks, never on a row repaint.</summary>
        Scene = 1,

        /// <summary>Walks the project — source lint, coverage reports, mapping sweeps. Explicit invocation only.</summary>
        Project = 2
    }

    /// <summary>What is being validated. <see cref="Target"/> is set for <see cref="ValidationScope.Asset"/> only.</summary>
    public readonly struct ValidationRequest
    {
        public readonly ValidationScope Scope;

        public readonly UnityEngine.Object Target;

        public readonly string AssetPath;

        public ValidationRequest(ValidationScope scope, UnityEngine.Object target, string assetPath)
        {
            Scope = scope;
            Target = target;
            AssetPath = assetPath;
        }
    }

    /// <summary>
    /// One package's authoring checks, in the shared vocabulary. Implementations must be concrete with a
    /// public parameterless constructor; <see cref="AuthoringValidatorRegistry"/> discovers them via
    /// <c>TypeCache</c>, so contributing checks is drop-in with no registration call — the same pattern as
    /// <c>IWorldDebugSection</c> and <c>IFeatureCatalogSource</c>.
    /// </summary>
    public interface IAuthoringValidator
    {
        ValidationScope Scope { get; }

        /// <summary>Grouping key shown to designers, e.g. "Combat authoring", "Content mapping".</summary>
        string Source { get; }

        /// <summary>
        /// For <see cref="ValidationScope.Asset"/>: the type this validator reads, so the registry can skip
        /// it without constructing a request. Null for scene and project scope.
        /// </summary>
        Type TargetType { get; }

        void Collect(in ValidationRequest request, List<AuthoringIssue> issues);
    }

    /// <summary>
    /// Opt-in for validators whose findings belong to one package, so a per-package readout (the Central
    /// window's package task list) can ask for just that package's issues. Most validators are project
    /// concerns with no owning package and do not implement this.
    /// </summary>
    public interface IPackageScopedValidator
    {
        string PackageId { get; }
    }
}
#endif
