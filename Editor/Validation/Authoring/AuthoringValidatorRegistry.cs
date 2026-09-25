#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace AetherNexus.FoundationPlatform.Editor.Utilities.Validation
{
    /// <summary>
    /// Auto-discovers every concrete <see cref="IAuthoringValidator"/> across loaded editor assemblies and
    /// serves their findings to the three surfaces that render them: the Project row badge, the Inspector
    /// strip, and Central Validation. A package contributes checks by declaring a type — there is no
    /// registration call to forget, which is what the hand-registered predecessor kept losing.
    /// <para>
    /// Asset-scope validators are indexed by <see cref="IAuthoringValidator.TargetType"/> so a row repaint
    /// runs only the handful that can say anything about that asset. Row-level results are cached and
    /// dropped on <c>projectChanged</c> / <c>hierarchyChanged</c> — the one invalidation scheme in this
    /// system, shared with the content-area badge pass.
    /// </para>
    /// </summary>
    public static class AuthoringValidatorRegistry
    {
        private static IAuthoringValidator[] _validators;
        private static Dictionary<Type, List<IAuthoringValidator>> _assetValidatorsByType;
        private static List<IAuthoringValidator> _sceneValidators;
        private static List<IAuthoringValidator> _projectValidators;

        private static readonly Dictionary<string, AuthoringIssueSeverity> WorstByAssetPath = new();
        private static readonly List<AuthoringIssue> ScratchIssues = new();

        static AuthoringValidatorRegistry()
        {
            EditorApplication.projectChanged += InvalidateResults;
            EditorApplication.hierarchyChanged += InvalidateResults;
        }

        public static IReadOnlyList<IAuthoringValidator> Validators
        {
            get
            {
                EnsureDiscovered();
                return _validators;
            }
        }

        /// <summary>Drop cached findings but keep the discovered validators (content changed, not code).</summary>
        public static void InvalidateResults()
        {
            WorstByAssetPath.Clear();
        }

        /// <summary>Drop discovered validators too (the type set changed).</summary>
        public static void Invalidate()
        {
            _validators = null;
            _assetValidatorsByType = null;
            _sceneValidators = null;
            _projectValidators = null;
            WorstByAssetPath.Clear();
        }

        public static void CollectForAsset(UnityEngine.Object target, List<AuthoringIssue> issues)
        {
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));
            if (target == null)
                return;

            EnsureDiscovered();

            string assetPath = AssetDatabase.GetAssetPath(target);
            var request = new ValidationRequest(ValidationScope.Asset, target, assetPath);

            foreach (var pair in _assetValidatorsByType)
            {
                if (!pair.Key.IsInstanceOfType(target))
                    continue;

                List<IAuthoringValidator> validators = pair.Value;
                for (int i = 0; i < validators.Count; i++)
                    validators[i].Collect(in request, issues);
            }
        }

        public static void CollectForScene(List<AuthoringIssue> issues)
        {
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));

            EnsureDiscovered();

            var request = new ValidationRequest(ValidationScope.Scene, null, null);
            for (int i = 0; i < _sceneValidators.Count; i++)
                _sceneValidators[i].Collect(in request, issues);
        }

        /// <summary>
        /// Project-wide sweep: every project-scope validator, every asset-scope validator run across all
        /// assets of its target type, plus the scene ones — "validate the project" with an open scene that
        /// fails readiness is not a passing project.
        /// <para>
        /// The asset sweep is why this is explicit-invocation only. It loads every asset of every validated
        /// type, which is exactly the cost the scope split exists to keep off a row repaint.
        /// </para>
        /// </summary>
        public static void CollectProject(List<AuthoringIssue> issues)
        {
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));

            EnsureDiscovered();

            var request = new ValidationRequest(ValidationScope.Project, null, null);
            for (int i = 0; i < _projectValidators.Count; i++)
                _projectValidators[i].Collect(in request, issues);

            foreach (var pair in _assetValidatorsByType)
                SweepAssetsOfType(pair.Key, pair.Value, issues);

            CollectForScene(issues);
        }

        private static readonly List<IAuthoringValidator> SingleValidatorScratch = new(1);

        private static List<IAuthoringValidator> SingleValidator(IAuthoringValidator validator)
        {
            SingleValidatorScratch.Clear();
            SingleValidatorScratch.Add(validator);
            return SingleValidatorScratch;
        }

        private static void SweepAssetsOfType(Type targetType, List<IAuthoringValidator> validators, List<AuthoringIssue> issues)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + targetType.Name);
            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                var asset = AssetDatabase.LoadAssetAtPath(path, targetType);
                if (asset == null)
                    continue;

                var request = new ValidationRequest(ValidationScope.Asset, asset, path);
                for (int i = 0; i < validators.Count; i++)
                    validators[i].Collect(in request, issues);
            }
        }

        /// <summary>Every package id declared by an <see cref="IPackageScopedValidator"/>, sorted.</summary>
        public static List<string> PackageIds()
        {
            EnsureDiscovered();
            var ids = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _validators.Length; i++)
            {
                if (_validators[i] is IPackageScopedValidator scoped && !string.IsNullOrWhiteSpace(scoped.PackageId))
                    ids.Add(scoped.PackageId);
            }
            return new List<string>(ids);
        }

        /// <summary>
        /// Findings from the validators that declared themselves owned by <paramref name="packageId"/>.
        /// False when no validator claims that package, which is how a caller distinguishes "this package
        /// reported nothing" from "this package contributes no checks at all".
        /// </summary>
        public static bool CollectForPackage(string packageId, List<AuthoringIssue> issues)
        {
            if (issues == null)
                throw new ArgumentNullException(nameof(issues));
            if (string.IsNullOrWhiteSpace(packageId))
                return false;

            EnsureDiscovered();

            bool found = false;
            for (int i = 0; i < _validators.Length; i++)
            {
                IAuthoringValidator validator = _validators[i];
                if (validator is not IPackageScopedValidator scoped)
                    continue;
                if (!string.Equals(scoped.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
                    continue;

                found = true;
                if (validator.Scope == ValidationScope.Asset)
                {
                    SweepAssetsOfType(validator.TargetType, SingleValidator(validator), issues);
                    continue;
                }

                var request = new ValidationRequest(validator.Scope, null, null);
                validator.Collect(in request, issues);
            }

            return found;
        }

        /// <summary>
        /// Worst severity for one asset, cached for row repaints. False when the asset is clean, which is
        /// the common case and must stay allocation-free after the first call.
        /// </summary>
        public static bool TryGetWorstSeverity(string assetPath, out AuthoringIssueSeverity worst)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                worst = default;
                return false;
            }

            if (WorstByAssetPath.TryGetValue(assetPath, out worst))
                return true;

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                worst = default;
                return false;
            }

            ScratchIssues.Clear();
            CollectForAsset(asset, ScratchIssues);
            if (ScratchIssues.Count == 0)
            {
                worst = default;
                return false;
            }

            worst = AuthoringIssueSeverity.Info;
            for (int i = 0; i < ScratchIssues.Count; i++)
            {
                if (ScratchIssues[i].Severity > worst)
                    worst = ScratchIssues[i].Severity;
            }

            WorstByAssetPath[assetPath] = worst;
            return true;
        }

        private static void EnsureDiscovered()
        {
            if (_validators != null)
                return;

            var all = new List<IAuthoringValidator>();
            _assetValidatorsByType = new Dictionary<Type, List<IAuthoringValidator>>();
            _sceneValidators = new List<IAuthoringValidator>();
            _projectValidators = new List<IAuthoringValidator>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<IAuthoringValidator>())
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidOperationException(
                        $"Authoring validator '{type.FullName}' has no public parameterless constructor; " +
                        "discovery cannot construct it.");

                var validator = (IAuthoringValidator)Activator.CreateInstance(type);
                if (string.IsNullOrWhiteSpace(validator.Source))
                    throw new InvalidOperationException(
                        $"Authoring validator '{type.FullName}' returned an empty Source; it is the grouping key designers read.");

                all.Add(validator);

                switch (validator.Scope)
                {
                    case ValidationScope.Asset:
                        Type targetType = validator.TargetType;
                        if (targetType == null)
                            throw new InvalidOperationException(
                                $"Asset-scope validator '{type.FullName}' returned a null TargetType; " +
                                "the registry needs it to skip the validator without building a request.");
                        if (!_assetValidatorsByType.TryGetValue(targetType, out var bucket))
                            _assetValidatorsByType[targetType] = bucket = new List<IAuthoringValidator>();
                        bucket.Add(validator);
                        break;

                    case ValidationScope.Scene:
                        _sceneValidators.Add(validator);
                        break;

                    case ValidationScope.Project:
                        _projectValidators.Add(validator);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Authoring validator '{type.FullName}' declared unknown scope '{validator.Scope}'.");
                }
            }

            all.Sort(static (a, b) => string.CompareOrdinal(a.Source, b.Source));
            _validators = all.ToArray();
        }
    }
}
#endif
