#if UNITY_EDITOR
using UnityEditor;

namespace FoundationPlatform.Editor.AssetImport
{
    internal sealed class HomamAssetImportHub : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            AssetImportPluginRegistry.InvokeBatch(
                importedAssets,
                deletedAssets,
                movedAssets,
                movedFromAssetPaths);
        }

        private void OnPreprocessAsset()
        {
            AssetImportPluginRegistry.InvokePreprocess(assetPath, this);
        }
    }
}
#endif
