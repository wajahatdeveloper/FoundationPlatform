#if UNITY_EDITOR
using System.Collections.Generic;

namespace AetherNexus.FoundationPlatform.FeatureFinder.Editor
{
    /// <summary>
    /// Contributes Feature Finder rows that are not <c>[MenuItem]</c> entries. Implementors are
    /// auto-discovered by <see cref="FeatureCatalog"/> through <c>TypeCache</c> and need a public
    /// parameterless constructor - there is no registration call. This is how a package whose
    /// assembly FoundationPlatform cannot reference (GameEngineCore, for one) gets its features
    /// into the index.
    /// </summary>
    public interface IFeatureCatalogSource
    {
        void Contribute(List<FeatureEntry> into);
    }
}
#endif
