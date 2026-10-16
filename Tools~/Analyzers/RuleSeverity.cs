using Microsoft.CodeAnalysis;

namespace AetherNexus.Analyzers
{
    internal static class RuleSeverity
    {
        // A SURVEY build reports rule hits as warnings, so one offline build lists every hit instead of stopping at
        // the first assembly that fails: dotnet build -c Release -p:DefineConstants=SURVEY -o <temp>.
#if SURVEY
        public const DiagnosticSeverity Enforced = DiagnosticSeverity.Warning;
#else
        public const DiagnosticSeverity Enforced = DiagnosticSeverity.Error;
#endif
    }
}
