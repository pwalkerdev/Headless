﻿// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Extensions;

internal static class RoslynScriptingExtensions
{
    public static IEnumerable<TChildTypeFilter> Get<TChildTypeFilter>(this SyntaxNode node) where TChildTypeFilter : SyntaxNode => node.ChildNodes().OfType<TChildTypeFilter>();

    public static string? ResolveParameterConcreteTypeName(this SemanticModel semanticModel, BaseParameterSyntax paramSyntax)
    {
        if (paramSyntax.Type == null || semanticModel.GetSymbolInfo(paramSyntax.Type).Symbol is not INamedTypeSymbol symbol)
            return null;

        return symbol.SpecialType == SpecialType.None ? symbol.ToString() : symbol.SpecialType.ToString().Replace('_', '.');
    }

    public static Type? ResolveParameterConcreteType(this SemanticModel semanticModel, BaseParameterSyntax paramSyntax) => semanticModel.ResolveParameterConcreteTypeName(paramSyntax) is { Length: > 0 } tn ? Type.GetType(tn) : null;

    public static unsafe MetadataReference? GetMetadataReference(this Assembly assembly) => assembly.TryGetRawMetadata(out var blob, out var length) ? AssemblyMetadata.Create(ModuleMetadata.CreateFromMetadata((IntPtr)blob, length)).GetReference() : null;
}