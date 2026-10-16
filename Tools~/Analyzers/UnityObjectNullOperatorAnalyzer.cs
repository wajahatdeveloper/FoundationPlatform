using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AetherNexus.Analyzers
{
    /// <summary>
    /// <c>??</c>, <c>??=</c> and <c>?.</c> test CLR reference identity, so a destroyed Unity object that is still a
    /// live CLR reference passes as non-null. Unity objects need Unity's overloaded <c>==</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnityObjectNullOperatorAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "AN0001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            "Null operator on a UnityEngine.Object",
            "'{0}' on '{1}' bypasses Unity's destroyed-object check; use an explicit '== null' test, TryGetComponent, or GetComponentInSelfOrParents / GetComponentInSelfOrChildren",
            "Unity",
            RuleSeverity.Enforced,
            isEnabledByDefault: true,
            helpLinkUri: ".cursor/rules/unity-null-coalescing.mdc");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                INamedTypeSymbol unityObject = start.Compilation.GetTypeByMetadataName("UnityEngine.Object");
                if (unityObject == null)
                    return;

                start.RegisterSyntaxNodeAction(c => AnalyzeCoalesce(c, unityObject), SyntaxKind.CoalesceExpression);
                start.RegisterSyntaxNodeAction(c => AnalyzeCoalesceAssignment(c, unityObject), SyntaxKind.CoalesceAssignmentExpression);
                start.RegisterSyntaxNodeAction(c => AnalyzeConditionalAccess(c, unityObject), SyntaxKind.ConditionalAccessExpression);
            });
        }

        private static void AnalyzeCoalesce(SyntaxNodeAnalysisContext c, INamedTypeSymbol unityObject)
        {
            if (!FirstPartyScope.Contains(c.Node.SyntaxTree))
                return;
            var node = (BinaryExpressionSyntax)c.Node;
            Report(c, unityObject, node.Left, node.OperatorToken);
        }

        private static void AnalyzeCoalesceAssignment(SyntaxNodeAnalysisContext c, INamedTypeSymbol unityObject)
        {
            if (!FirstPartyScope.Contains(c.Node.SyntaxTree))
                return;
            var node = (AssignmentExpressionSyntax)c.Node;
            Report(c, unityObject, node.Left, node.OperatorToken);
        }

        private static void AnalyzeConditionalAccess(SyntaxNodeAnalysisContext c, INamedTypeSymbol unityObject)
        {
            if (!FirstPartyScope.Contains(c.Node.SyntaxTree))
                return;
            var node = (ConditionalAccessExpressionSyntax)c.Node;
            Report(c, unityObject, node.Expression, node.OperatorToken);
        }

        private static void Report(SyntaxNodeAnalysisContext c, INamedTypeSymbol unityObject, ExpressionSyntax operand, SyntaxToken op)
        {
            ITypeSymbol type = c.SemanticModel.GetTypeInfo(operand, c.CancellationToken).Type;
            if (!DerivesFrom(type, unityObject))
                return;
            c.ReportDiagnostic(Diagnostic.Create(Rule, op.GetLocation(), op.Text, type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        internal static bool DerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            if (type is ITypeParameterSymbol typeParameter)
            {
                foreach (ITypeSymbol constraint in typeParameter.ConstraintTypes)
                {
                    if (DerivesFrom(constraint, baseType))
                        return true;
                }
                return false;
            }
            for (ITypeSymbol t = type; t != null; t = t.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(t, baseType))
                    return true;
            }
            return false;
        }
    }
}
