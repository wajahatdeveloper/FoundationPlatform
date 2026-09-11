#if UNITY_EDITOR
namespace AetherNexus.FoundationPlatform.ComponentMenus.Editor
{
    /// <summary>One reviewed line of <c>Temp/ComponentMenus/proposal.tsv</c>.</summary>
    internal readonly struct ComponentMenuProposalRow
    {
        public ComponentMenuProposalRow(ComponentMenuVerdict verdict, string menu, string typeName, string packageId, string scriptPath)
        {
            Verdict = verdict;
            Menu = menu;
            TypeName = typeName;
            PackageId = packageId;
            ScriptPath = scriptPath;
        }

        public ComponentMenuVerdict Verdict { get; }
        public string Menu { get; }
        public string TypeName { get; }
        public string PackageId { get; }
        public string ScriptPath { get; }
    }
}
#endif
