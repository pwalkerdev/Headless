// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Syntax;

public static class SyntaxTreeBuilder
{
    public static SyntaxTree FromExpression(string expression, CSharpParseOptions options)
    {
        const string template =
@"namespace {{NAMESPACE_NAME}}
{
    public static class {{CLASS_NAME}}
    {
        [EntryPointAttribute]
        public static object {{METHOD_NAME}}()
        {
#line 1
            return {{EXPRESSION_IMPLEMENTATION}};
        }
    }
}";

        var script = template.Replace("{{NAMESPACE_NAME}}", "Headless.Targeting.CSharp.Framework")
            .Replace("{{CLASS_NAME}}", "EmittedExpressionContainerClass")
            .Replace("{{METHOD_NAME}}", "EmittedExpressionContainerMethod")
            .Replace("{{EXPRESSION_IMPLEMENTATION}}", expression);
            
        return BuildSyntaxTree(script, options);
    }

    public static SyntaxTree FromStatement(string statement, CSharpParseOptions options)
    {
        const string template =
@"namespace {{NAMESPACE_NAME}}
{
    public static class {{CLASS_NAME}}
    {
        [EntryPointAttribute]
        public static void {{METHOD_NAME}}()
        {
#line 1
            {{STATEMENT_IMPLEMENTATION}};
        }
    }
}";

        var script = template.Replace("{{NAMESPACE_NAME}}", "Headless.Targeting.CSharp.Framework")
            .Replace("{{CLASS_NAME}}", "EmittedStatementContainerClass")
            .Replace("{{METHOD_NAME}}", "EmittedStatementContainerMethod")
            .Replace("{{STATEMENT_IMPLEMENTATION}}", statement);
            
        return BuildSyntaxTree(script, options);
    }
    
    public static SyntaxTree FromMethod(string method, CSharpParseOptions options)
    {
        const string template =
@"namespace {{NAMESPACE_NAME}}
{
    public class {{CLASS_NAME}}
    {
        [EntryPointAttribute]
#line 1
        {{METHOD_IMPLEMENTATION}}
    }
}";

        var script = template.Replace("{{NAMESPACE_NAME}}", "Headless.Targeting.CSharp.Framework")
            .Replace("{{CLASS_NAME}}", "EmittedMethodContainerClass")
            .Replace("{{METHOD_IMPLEMENTATION}}", method);

        var result = BuildSyntaxTree(script, options);
        var entryPoint = result.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m is { AttributeLists.Count: 1 } && m.AttributeLists.ToString() == "[EntryPointAttribute]");
        if (entryPoint is null || entryPoint.Modifiers.All(m => m.ToString() != "public") == true)
        {
            throw new Exception("Entry point method must be public (at the moment but I might remove this limitation later...)"); // TODO: Create an exception class for compilation / semantic errors
        }

        return result;
    }

    public static SyntaxTree FromClass(string @class, CSharpParseOptions options)
    {
        const string template =
@"namespace {{NAMESPACE_NAME}}
{
#line 1
    {{CLASS_IMPLEMENTATION}}
}";

        var script = template.Replace("{{NAMESPACE_NAME}}", "Headless.Targeting.CSharp.Framework")
            .Replace("{{CLASS_IMPLEMENTATION}}", @class);

        return BuildSyntaxTree(script, options);
    }

    // public static SyntaxTree FromClass(string @class, CSharpParseOptions options)
    // {
    //     var script = "namespace Headless.Targeting.CSharp.Framework;\r\n\r\n" + @class;
    //     return BuildSyntaxTree(script, options);
    // }

    public static SyntaxTree FromNamespace(string @namespace, CSharpParseOptions options)
    {
        return BuildSyntaxTree(@namespace, options);
    }

    private static SyntaxTree BuildSyntaxTree(string script, CSharpParseOptions options) => SyntaxFactory.ParseSyntaxTree(script, options, encoding: Encoding.UTF8);
}