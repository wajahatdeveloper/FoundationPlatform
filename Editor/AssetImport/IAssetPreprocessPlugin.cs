#if UNITY_EDITOR
using UnityEditor;

namespace FoundationPlatform.Editor.AssetImport
{
    public interface IAssetPreprocessPlugin
    {
        bool CanPreprocess(string assetPath);

        void OnPreprocess(string assetPath, AssetPostprocessor host);
    }
}
#endif
