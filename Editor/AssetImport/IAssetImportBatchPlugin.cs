#if UNITY_EDITOR
namespace FoundationPlatform.Editor.AssetImport
{
    public interface IAssetImportBatchPlugin
    {
        int Order { get; }

        bool ShouldRun(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths);

        void Run(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths);
    }
}
#endif
