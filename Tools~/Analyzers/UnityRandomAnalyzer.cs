using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AetherNexus.Analyzers
{
    /// <summary>
    /// Gameplay randomness must come from the deterministic streams (<c>RandomX</c>). <c>UnityEngine.Random</c> is
    /// allowed only where the enclosing member, type or assembly declares <c>[PresentationRandom]</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnityRandomAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "AN0002";
        private const string OptOutAttribute = "AetherNexus.FoundationPlatform.Attributes.PresentationRandomAttribute";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            "UnityEngine.Random outside presentation code",
            "'UnityEngine.Random.{0}' is not deterministic; use RandomX.Stream(\"<system>\") for gameplay, or mark presentation-only code [PresentationRandom]",
            "Determinism",
            RuleSeverity.Enforced,
            isEnabledByDefault: true,
            helpLinkUri: "docs/11-Determinism.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                INamedTypeSymbol random = start.Compilation.GetTypeByMetadataName("UnityEngine.Random");
                if (random == null)
                    return;
                if (HasOptOut(start.Compilation.Assembly))
                    return;

                start.RegisterOperationAction(c =>
                {
                    ISymbol member = c.Operation switch
                    {
                        IInvocationOperation invocation => invocation.TargetMethod,
                        IPropertyReferenceOperation property => property.Property,
                        IFieldReferenceOperation field => field.Field,
                        _ => null,
                    };
                    if (member == null || !SymbolEqualityComparer.Default.Equals(member.ContainingType, random))
                        return;

                    SyntaxTree tree = c.Operation.Syntax.SyntaxTree;
                    if (!FirstPartyScope.Contains(tree) || FirstPartyScope.IsEditorCode(tree, c.Compilation))
                        return;

                    for (ISymbol s = c.ContainingSymbol; s != null && s.Kind != SymbolKind.Namespace; s = s.ContainingSymbol)
                    {
                        if (HasOptOut(s))
                            return;
                        if (s is IMethodSymbol accessor && accessor.AssociatedSymbol != null && HasOptOut(accessor.AssociatedSymbol))
                            return;
                    }

                    c.ReportDiagnostic(Diagnostic.Create(Rule, c.Operation.Syntax.GetLocation(), member.Name));
                }, OperationKind.Invocation, OperationKind.PropertyReference, OperationKind.FieldReference);
            });
        }

        private static bool HasOptOut(ISymbol symbol)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == OptOutAttribute)
                    return true;
            }
            return false;
        }
    }
}
