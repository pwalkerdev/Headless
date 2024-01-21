// ReSharper disable once CheckNamespace
namespace Headless.CSharp.Extensions;

public static class RoslynScriptingExtensions
{
    public static IEnumerable<TChildTypeFilter> Get<TChildTypeFilter>(this SyntaxNode node) where TChildTypeFilter : SyntaxNode => node.ChildNodes().OfType<TChildTypeFilter>();

    public static string? ResolveParameterConcreteTypeName(this SemanticModel semanticModel, BaseParameterSyntax paramSyntax)
    {
        if (paramSyntax.Type == null || semanticModel.GetSymbolInfo(paramSyntax.Type).Symbol is not INamedTypeSymbol symbol)
            return null;

        return symbol.SpecialType == SpecialType.None ? symbol.ToString() : symbol.SpecialType.ToString().Replace('_', '.');
    }
    public static Type? ResolveParameterConcreteType(this SemanticModel semanticModel, BaseParameterSyntax paramSyntax) => semanticModel.ResolveParameterConcreteTypeName(paramSyntax) is { Length: > 0 } tn ? Type.GetType(tn) : null;
}