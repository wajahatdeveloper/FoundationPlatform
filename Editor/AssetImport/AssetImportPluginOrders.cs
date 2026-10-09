#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Editor.AssetImport
{
    public static class AssetImportPluginOrders
    {
        public const int Authoring = 0;
        public const int ScriptsHierarchyValidator = 50;
        public const int PackageIntegration = 100;
        public const int DomainEvents = 150;
        public const int RegistryImportBatch = 300;
        public const int AnimationSet = 500;
    }
}
#endif
