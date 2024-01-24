// ReSharper disable once CheckNamespace
namespace Headless.CSharp.Scripting;

[SupportedLanguages("CSharp")]
public class CSharpScriptInterpreter : IReadScripts, IRunScripts
{
    private static readonly string[] _implicitImports = ["Headless.CSharp.Framework", "System", "System.Collections", "System.Collections.Generic", "System.Linq"];
    private static readonly Assembly[] _referenceAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetExportedTypes().Any(t => _implicitImports.Contains(t.Namespace))).ToArray();

    public Task<ICompileResult> Compile(string script)
    {
        var roslynScriptOptions = ScriptOptions.Default.AddReferences(_referenceAssemblies).AddImports(_implicitImports);
        var roslynScript = CSharpScript.Create(script, roslynScriptOptions);
        var output = new StringBuilder();
        try
        {
            var compilation = roslynScript.GetCompilation();
            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            ScriptImplementationDescriptor implementationDescriptor;

            if (syntaxTree.GetRoot().Get<GlobalStatementSyntax>().SelectMany(gss => gss.Get<ExpressionStatementSyntax>()).SingleOrDefault()?.Expression is { } entryExpression)
            {
                var paramOrder = 0;
                // ReSharper disable once RedundantAssignment
                implementationDescriptor = new(MethodBodyImplementation.Expression, entryExpression.Get<ParameterSyntax>().Concat(entryExpression.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters)).Select(p => new MethodParameter(++paramOrder, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());
            }
            else if (syntaxTree.GetRoot().Get<MethodDeclarationSyntax>().SingleOrDefault() is { } entryMethod)
            {
                var paramOrder = 0;
                implementationDescriptor = new(MethodBodyImplementation.Block, entryMethod.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters).Select(p => new MethodParameter(++paramOrder, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());

                // If the script is a method, we need to inject a statement at the end to invoke the method. This does not apply to expression body scripts
                var methodInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(entryMethod.Identifier.ToFullString()))
                    .AddArgumentListArguments(Enumerable.Range(1, implementationDescriptor.ParameterDescriptors.Length).Select(i => SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"p{i}"))).ToArray())
                    .NormalizeWhitespace();
                var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(methodInvocation)
                    .AddParameterListParameters(Enumerable.Range(1, implementationDescriptor.ParameterDescriptors.Length).Select(i => SyntaxFactory.Parameter(SyntaxFactory.Identifier($"p{i}")).WithType(SyntaxFactory.ParseTypeName(implementationDescriptor.ParameterDescriptors.ElementAt(i - 1).Type!.Name))).ToArray())
                    .NormalizeWhitespace();
                var globalStatement = SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(lambdaExpression)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.SemicolonToken, string.Empty, string.Empty, SyntaxTriviaList.Empty)))
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(Environment.NewLine));

                syntaxTree = syntaxTree.WithRootAndOptions(syntaxTree.GetRoot().InsertNodesAfter(entryMethod, new [] { globalStatement }), syntaxTree.Options);
                roslynScript = CSharpScript.Create(syntaxTree.GetRoot().ToFullString(), roslynScriptOptions);
            }
            else
            {
                return Task.FromResult<ICompileResult>(CompileResult.Create(false, output.AppendLine("Unable to determine script entry point -- eventually this won't be a problem... But for now scripts need to be written as a single method or expression body. Local methods are supported"), roslynScript));
            }

            output.AppendJoin(Environment.NewLine, roslynScript.Compile());
        }
        catch (Exception e)
        {
            return Task.FromResult<ICompileResult>(CompileResult.Create(false, output.AppendLine($"Exception: {e.Message}"), roslynScript));
        }

        return Task.FromResult<ICompileResult>(CompileResult.Create(true, output, roslynScript));
    }

    public async Task<IInvocationResult> Run(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult cr || cr is not { RoslynScript: { } rs })
            return InvocationResult.Create(false, "Unable to invoke script due to compilation errors!");

        var messages = new StringBuilder(6);
        messages.AppendLine($"{string.Join("", Enumerable.Repeat('-', 13))}OUTPUT{string.Join("", Enumerable.Repeat('-', 13))}");
        messages.AppendLine();

        var sw = Stopwatch.StartNew();
        var delegateType = (await rs.RunAsync()).ReturnValue;
        var @delegate = delegateType.GetType().GetMethod("Invoke");
        @delegate?.Invoke(delegateType, null);
        sw.Stop();

        messages.AppendLine();
        messages.AppendLine($"TIME ELAPSED: {TimeSpan.FromTicks(sw.ElapsedTicks).TotalSeconds:N4}s");
        messages.AppendLine();

        return InvocationResult.Create(true, messages);
    }

    public async Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        // TODO: This code is duplicated from the method above. It can be consolidated, but it is a bit complicated due to the generic/non-generic result. I'm going to come back to it later.
        if (compileResult is not CompileResult cr || cr is not { RoslynScript: { } rs })
            return InvocationResult<TResult?>.Create(false, "Unable to invoke script due to compilation errors!", default);

        var messages = new StringBuilder(6);
        messages.AppendLine($"{string.Join("", Enumerable.Repeat('-', 13))}OUTPUT{string.Join("", Enumerable.Repeat('-', 13))}");
        messages.AppendLine();

        var sw = Stopwatch.StartNew();
        var delegateType = (await rs.RunAsync()).ReturnValue;
        var @delegate = delegateType.GetType().GetMethod("Invoke");
        var result = (TResult?)@delegate?.Invoke(delegateType, null);
        sw.Stop();

        messages.AppendLine();
        messages.AppendLine($"RESULT VALUE: {result}");
        messages.AppendLine($"TIME ELAPSED: {TimeSpan.FromTicks(sw.ElapsedTicks).TotalSeconds:N4}s");
        messages.AppendLine();

        return InvocationResult<TResult?>.Create(true, messages, result);
    }

    //public static void CompileAndRun(string script)
    //{
    //    // TODO: Implement proper logging with verbosity
    //    //Console.WriteLine($"Compiling script & invoking it in the .NET 8 runtime...{Environment.NewLine}");
    //    //Console.WriteLine($"Script: {script}");

    //    var roslynScriptOptions = ScriptOptions.Default.AddReferences(_referenceAssemblies).AddImports(_implicitImports);
    //    var roslynScript = CSharpScript.Create(script, roslynScriptOptions);
    //    var compilerResult = ImmutableArray<Diagnostic>.Empty;
    //    try
    //    {
    //        var compilation = roslynScript.GetCompilation();
    //        var syntaxTree = compilation.SyntaxTrees.Single();
    //        var semanticModel = compilation.GetSemanticModel(syntaxTree);
    //        ScriptImplementationDescriptor implementationDescriptor;

    //        if (syntaxTree.GetRoot().Get<GlobalStatementSyntax>().SelectMany(gss => gss.Get<ExpressionStatementSyntax>()).SingleOrDefault()?.Expression is { } entryExpression)
    //        {
    //            var paramOrder = 0;
    //            // ReSharper disable once RedundantAssignment
    //            implementationDescriptor = new(MethodBodyImplementation.Expression, entryExpression.Get<ParameterSyntax>().Concat(entryExpression.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters)).Select(p => new MethodParameter(++paramOrder, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());
    //        }
    //        else if (syntaxTree.GetRoot().Get<MethodDeclarationSyntax>().SingleOrDefault() is { } entryMethod)
    //        {
    //            var paramOrder = 0;
    //            implementationDescriptor = new(MethodBodyImplementation.Block, entryMethod.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters).Select(p => new MethodParameter(++paramOrder, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());

    //            // If the script is a method, we need to inject a statement at the end to invoke the method. This does not apply to expression body scripts
    //            var methodInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(entryMethod.Identifier.ToFullString()))
    //                .AddArgumentListArguments(Enumerable.Range(1, implementationDescriptor.ParameterDescriptors.Length).Select(i => SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"p{i}"))).ToArray())
    //                .NormalizeWhitespace();
    //            var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(methodInvocation)
    //                .AddParameterListParameters(Enumerable.Range(1, implementationDescriptor.ParameterDescriptors.Length).Select(i => SyntaxFactory.Parameter(SyntaxFactory.Identifier($"p{i}")).WithType(SyntaxFactory.ParseTypeName(implementationDescriptor.ParameterDescriptors.ElementAt(i - 1).Type!.Name))).ToArray())
    //                .NormalizeWhitespace();
    //            var globalStatement = SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(lambdaExpression)
    //                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.SemicolonToken, string.Empty, string.Empty, SyntaxTriviaList.Empty)))
    //                .WithLeadingTrivia(SyntaxFactory.Whitespace(Environment.NewLine));

    //            syntaxTree = syntaxTree.WithRootAndOptions(syntaxTree.GetRoot().InsertNodesAfter(entryMethod, new [] { globalStatement }), syntaxTree.Options);
    //            roslynScript = CSharpScript.Create(syntaxTree.GetRoot().ToFullString(), roslynScriptOptions);
    //        }
    //        else
    //        {
    //            Console.WriteLine("Unable to determine script entry point -- eventually this won't be a problem... But for now scripts need to be written as a single method or expression body. Local methods are supported");
    //            return;
    //        }

    //        compilerResult = roslynScript.Compile();
    //        // TODO: Implement proper logging with verbosity
    //        //if (compilerResult.Any())
    //        //    Console.WriteLine($"Compiler Warnings: {compilerResult.Summarise()}");

    //        Console.WriteLine($"{string.Join("", Enumerable.Repeat('-', 13))}OUTPUT{string.Join("", Enumerable.Repeat('-', 13))}");
    //        Console.WriteLine();

    //        var sw = Stopwatch.StartNew();
    //        var delegateType = roslynScript.RunAsync().Result.ReturnValue;
    //        var @delegate = delegateType.GetType().GetMethod("Invoke");
    //        var result = @delegate?.Invoke(delegateType, null);
    //        sw.Stop();

    //        Console.WriteLine();
    //        Console.WriteLine($"RESULT VALUE: {result}");
    //        Console.WriteLine($"TIME ELAPSED: {TimeSpan.FromTicks(sw.ElapsedTicks).TotalSeconds:N4}s");
    //        Console.WriteLine();
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine($"Exception -- {e.Message}{Environment.NewLine + Environment.NewLine + new StringBuilder(compilerResult.Length).AppendJoin(Environment.NewLine, compilerResult)}");
    //    }
    //}
}