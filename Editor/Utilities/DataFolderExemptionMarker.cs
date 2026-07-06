#if UNITY_EDITOR
using UnityEngine;

namespace FoundationPlatform.Editor.Utilities
{
    [CreateAssetMenu(
        fileName = "DataFolderExemptionMarker",
        menuName = "Foundation Platform/Exemption Marker")]
    /// <summary>
    /// Place in a folder to exempt that folder and all descendants from Hub manifest ownership,
    /// drift, and unclaimed-type validation. Types whose defining script lives under the marker
    /// folder (including local assembly roots) are also excluded from manifest population.
    /// </summary>
    public sealed class DataFolderExemptionMarker : ScriptableObject
    {
    }
}
#endif
