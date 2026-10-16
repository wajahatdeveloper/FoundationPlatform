using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AetherNexus.Analyzers
{
    /// <summary>
    /// Pooled actions are reused, so every payload member must return to its default between uses. For a
    /// <c>partial</c> <c>GameAction</c> subclass that does not write <c>Reset()</c> itself, generates an override that
    /// calls <c>base.Reset()</c>, then defaults each settable auto-property and field it declares, and calls
    /// <c>Clear()</c> on initialized collection members. A member whose initializer is not a clearable collection
    /// needs a hand-written <c>Reset()</c>; the generator reports it instead of guessing.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class GameActionResetGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor NeedsManualReset = new DiagnosticDescriptor(
            "AN0101",
            "GameAction Reset cannot be generated",
            "'{0}.{1}' has an initializer that is not a clearable collection; write Reset() by hand for '{0}'",
            "Codegen",
            RuleSeverity.Enforced,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                    static (node, _) => node is ClassDeclarationSyntax c && c.BaseList != null && c.Modifiers.Any(SyntaxKind.PartialKeyword),
                    static (ctx, ct) => ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct) as INamedTypeSymbol)
                .Where(static t => t != null)
                .Collect();

            context.RegisterSourceOutput(candidates.Combine(context.CompilationProvider), static (spc, pair) => Execute(spc, pair.Left, pair.Right));
        }

        private static void Execute(SourceProductionContext context, System.Collections.Immutable.ImmutableArray<INamedTypeSymbol> candidates, Compilation compilation)
        {
            INamedTypeSymbol gameAction = compilation.GetTypeByMetadataName("AetherNexus.GameEngineCore.GameAction");
            if (gameAction == null)
                return;

            foreach (INamedTypeSymbol type in candidates.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
            {
                if (!GeneratorUtil.DerivesFrom(type, gameAction))
                    continue;
                if (type.GetMembers("Reset").OfType<IMethodSymbol>().Any(m => m.Parameters.Length == 0 && !m.IsImplicitlyDeclared))
                    continue;
                if (!AllContainersPartial(type))
                    continue;

                var statements = new List<string>();
                bool ok = true;
                foreach (ISymbol member in type.GetMembers())
                {
                    if (member.IsStatic || member.IsImplicitlyDeclared)
                        continue;
                    if (!TryResetStatement(member, out string statement, out bool needsManual))
                    {
                        if (needsManual)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(NeedsManualReset, member.Locations.FirstOrDefault(), type.Name, member.Name));
                            ok = false;
                        }
                        continue;
                    }
                    statements.Add(statement);
                }
                if (!ok)
                    continue;

                context.AddSource(HintName(type), Render(type, statements));
            }
        }

        private static bool TryResetStatement(ISymbol member, out string statement, out bool needsManual)
        {
            statement = null;
            needsManual = false;

            ITypeSymbol type;
            ExpressionSyntax initializer;
            bool assignable;
            switch (member)
            {
                case IFieldSymbol field when !field.IsConst:
                    if (!(field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is VariableDeclaratorSyntax declarator))
                        return false;
                    type = field.Type;
                    initializer = declarator.Initializer?.Value;
                    assignable = !field.IsReadOnly;
                    break;
                case IPropertySymbol property when !property.IsIndexer:
                    if (!(property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax syntax) || !IsAutoProperty(syntax))
                        return false;
                    type = property.Type;
                    initializer = syntax.Initializer?.Value;
                    assignable = property.SetMethod != null && !property.SetMethod.IsInitOnly;
                    if (property.SetMethod == null)
                        assignable = false;
                    break;
                default:
                    return false;
            }

            string name = "this." + EscapeIdentifier(member.Name);
            if (initializer == null)
            {
                if (!assignable)
                    return false;
                statement = name + " = default;";
                return true;
            }

            if (HasClear(type))
            {
                statement = name + ".Clear();";
                return true;
            }

            needsManual = assignable;
            return false;
        }

        private static bool IsAutoProperty(PropertyDeclarationSyntax syntax)
        {
            return syntax.ExpressionBody == null
                   && syntax.AccessorList != null
                   && syntax.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null);
        }

        private static bool HasClear(ITypeSymbol type)
        {
            for (ITypeSymbol t = type; t != null; t = t.BaseType)
            {
                if (t.GetMembers("Clear").OfType<IMethodSymbol>().Any(m => !m.IsStatic && m.Parameters.Length == 0 && m.DeclaredAccessibility == Accessibility.Public))
                    return true;
            }
            return false;
        }

        private static bool AllContainersPartial(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol t = type.ContainingType; t != null; t = t.ContainingType)
            {
                bool partial = t.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax d && d.Modifiers.Any(SyntaxKind.PartialKeyword));
                if (!partial)
                    return false;
            }
            return true;
        }

        private static string Render(INamedTypeSymbol type, List<string> statements)
        {
            var chain = new List<INamedTypeSymbol>();
            for (INamedTypeSymbol t = type; t != null; t = t.ContainingType)
                chain.Insert(0, t);

            var sb = new StringBuilder();
            sb.Append("// <auto-generated/> AetherNexus.Analyzers.GameActionResetGenerator\n");
            bool hasNamespace = !type.ContainingNamespace.IsGlobalNamespace;
            if (hasNamespace)
                sb.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).Append("\n{\n");

            string indent = "\t";
            foreach (INamedTypeSymbol t in chain)
            {
                sb.Append(indent).Append("partial ").Append(t.IsRecord ? "record " : "class ").Append(t.Name);
                if (t.TypeParameters.Length > 0)
                    sb.Append('<').Append(string.Join(", ", t.TypeParameters.Select(p => p.Name))).Append('>');
                sb.Append('\n').Append(indent).Append("{\n");
                indent += "\t";
            }

            sb.Append(indent).Append("public override void Reset()\n").Append(indent).Append("{\n");
            sb.Append(indent).Append("\tbase.Reset();\n");
            foreach (string statement in statements)
                sb.Append(indent).Append('\t').Append(statement).Append('\n');
            sb.Append(indent).Append("}\n");

            for (int i = chain.Count - 1; i >= 0; i--)
            {
                indent = indent.Substring(1);
                sb.Append(indent).Append("}\n");
            }
            if (hasNamespace)
                sb.Append("}\n");
            return sb.ToString();
        }

        private static string HintName(INamedTypeSymbol type)
        {
            string name = type.ToDisplayString().Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');
            return name + ".Reset.g.cs";
        }

        private static string EscapeIdentifier(string name)
        {
            return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
        }
    }
}
