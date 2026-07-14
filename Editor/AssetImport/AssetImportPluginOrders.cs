#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.Editor.AssetImport
{
    public static class AssetImportPluginOrders
    {
        public const int CentralAuthoring = 0;
        public const int ScriptsHierarchyValidator = 50;
        public const int PackageIntegration = 100;
        public const int DomainEvents = 150;
        public const int GameplayTagReference = 200;
        public const int RegistryImportBatch = 300;
        public const int UIValidation = 400;
        public const int AnimationSet = 500;
    }
}
#endif
