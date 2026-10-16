using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AetherNexus.Analyzers
{
    internal static class GeneratorUtil
    {
        public static readonly SymbolDisplayFormat GlobalName = SymbolDisplayFormat.FullyQualifiedFormat;

        public static string CodeName(INamedTypeSymbol type) => type.ToDisplayString(GlobalName);

        /// <summary>Concrete, non-generic class with a public parameterless constructor (what <c>new T()</c> needs).</summary>
        public static bool IsConstructible(INamedTypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic || type.IsGenericType)
                return false;
            return type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
        }

        /// <summary>False when a private or protected nesting hides the type from generated code in its own assembly.</summary>
        public static bool IsReachableFromOwnAssembly(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol t = type; t != null; t = t.ContainingType)
            {
                if (t.ContainingType == null)
                    continue;
                switch (t.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        public static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            for (INamedTypeSymbol t = type.BaseType; t != null; t = t.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, baseType))
                    return true;
            }
            return false;
        }

        public static bool Implements(INamedTypeSymbol type, INamedTypeSymbol iface)
        {
            return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iface));
        }

        public static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol ns)
        {
            foreach (INamespaceOrTypeSymbol member in ns.GetMembers())
            {
                if (member is INamespaceSymbol child)
                {
                    foreach (INamedTypeSymbol t in AllTypes(child))
                        yield return t;
                }
                else if (member is INamedTypeSymbol type)
                {
                    foreach (INamedTypeSymbol t in WithNested(type))
                        yield return t;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type)
        {
            yield return type;
            foreach (INamedTypeSymbol nested in type.GetTypeMembers())
            {
                foreach (INamedTypeSymbol t in WithNested(nested))
                    yield return t;
            }
        }

        public static AttributeData FindAttribute(INamedTypeSymbol type, INamedTypeSymbol attribute, bool inherit)
        {
            for (INamedTypeSymbol t = type; t != null; t = inherit ? t.BaseType : null)
            {
                foreach (AttributeData a in t.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute))
                        return a;
                }
            }
            return null;
        }
    }
}
