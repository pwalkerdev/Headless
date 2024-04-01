// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Scripting;

[SupportedTargets("CSharp", versions: "latest|3|4|5|6|7|7.1|7.2|7.3|8|9|10|11|12", runtimes: "any|net80")]
public class CSharpScriptInterpreter(IOptions<CommandLineOptions> commandLineOptions) : IScriptCompiler, IScriptInvoker
{
    private static readonly string[] _implicitImports = ["Headless.Targeting.CSharp.Framework", "System", "System.Collections", "System.Collections.Generic", "System.Linq"];
    private static readonly Assembly[] _referenceAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetExportedTypes().Any(t => _implicitImports.Contains(t.Namespace))).ToArray();

    public async Task<ICompileResult> Compile(string script)
    {
        var output = new StringBuilder();

        var options = commandLineOptions.Value;
        if (!options.LanguageVersion.ResolveLanguageVersion(out var languageVersion))
            return CompileResult.Create(false, output.AppendLine($"Unrecognised value: \"{options.LanguageVersion}\" specified for parameter: \"LanguageVersion\""), null);

        var roslynScriptOptions = ScriptOptions.Default.WithLanguageVersion(languageVersion).AddReferences(_referenceAssemblies).AddImports(_implicitImports);
        var roslynScript = CSharpScript.Create(script, roslynScriptOptions);
        try
        {
            var compilation = roslynScript.GetCompilation();
            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            ScriptImplementationDescriptor implementationDescriptor;

            if ((await syntaxTree.GetRootAsync()).Get<GlobalStatementSyntax>().SelectMany(gss => gss.Get<ExpressionStatementSyntax>()).SingleOrDefault()?.Expression is { } entryExpression)
            {
                var paramOrder = 0;
                // ReSharper disable once RedundantAssignment
                implementationDescriptor = new(MethodBodyImplementation.Expression, entryExpression.Get<ParameterSyntax>().Concat(entryExpression.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters)).Select(p => new MethodParameter(++paramOrder, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());

                // If the script is formatted as a lambda, wrap it in `new Func<object>(() => myScript)` - this improves compatibility with older language versions.
                var wrapperFunc = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("Func<object>"), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new [] { SyntaxFactory.Argument(entryExpression) })), null)
                    .NormalizeWhitespace();

                roslynScript = CSharpScript.Create(wrapperFunc.ToString(), roslynScriptOptions);
            }
            else if ((await syntaxTree.GetRootAsync()).Get<MethodDeclarationSyntax>().SingleOrDefault() is { } entryMethod)
            {
                var paramOrder = 0;
                implementationDescriptor = new(MethodBodyImplementation.Block, entryMethod.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters).Select(p => new MethodParameter(++paramOrder, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());

                // If the script is a method, we need to inject a statement at the end to invoke the method
                var methodInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(entryMethod.Identifier.ToFullString()))
                    .AddArgumentListArguments(implementationDescriptor.ParameterDescriptors.Select((_, i) => SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"p{i}"))).ToArray());
                var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(methodInvocation)
                    .AddParameterListParameters(implementationDescriptor.ParameterDescriptors.Select((param, i) => SyntaxFactory.Parameter(SyntaxFactory.Identifier($"p{i}")).WithType(SyntaxFactory.ParseTypeName(param.Type!.Name))).ToArray());
                var wrapperFunc = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName($"Func<{entryMethod.ReturnType}>"), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new [] { SyntaxFactory.Argument(lambdaExpression) })), null);
                var globalStatement = SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(wrapperFunc)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.SemicolonToken, string.Empty, string.Empty, SyntaxTriviaList.Empty)))
                    .NormalizeWhitespace()
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(Environment.NewLine));

                roslynScript = CSharpScript.Create(entryMethod.Parent!.InsertNodesAfter(entryMethod, new[] { globalStatement }).ToString(), roslynScriptOptions);
            }
            else
                throw new InvalidOperationException("Unable to determine script entry point -- eventually this won't be a problem... But for now scripts need to be written as a single method or expression body. Local methods are supported");

            var roslynAnalysis = roslynScript.Compile();
            return CompileResult.Create(roslynAnalysis.All(msg => msg.Severity < DiagnosticSeverity.Error), output.AppendJoin(Environment.NewLine, roslynAnalysis), roslynScript);
        }
        catch (Exception e)
        {
            return CompileResult.Create(false, output.AppendLine($"Exception: {e.Message}"), roslynScript);
        }
    }

    public async Task<IInvocationResult> Run(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult cr || cr is not { RoslynScript: { } rs })
            return InvocationResult.Create(false, "Unable to invoke script due to compilation errors!");

        var messages = new StringBuilder();
        try
        {
            var sw = Stopwatch.StartNew();
            var delegateType = (await rs.RunAsync()).ReturnValue;
            var @delegate = delegateType.GetType().GetMethod("Invoke");
            @delegate?.Invoke(delegateType, null);
            sw.Stop();

            messages.AppendLine($"{string.Join("", Enumerable.Repeat('-', 13))}OUTPUT{string.Join("", Enumerable.Repeat('-', 13))}").AppendLine();
            messages.AppendLine($"TIME ELAPSED: {TimeSpan.FromTicks(sw.ElapsedTicks).TotalSeconds:N4}s").AppendLine();

            return InvocationResult.Create(true, messages);
        }
        catch (Exception e)
        {
            return InvocationResult.Create(false, messages.AppendLine($"Exception was thrown by the target of invocation: {e.Message}").AppendLine(e.StackTrace));
        }
    }

    public async Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        // TODO: This code is duplicated from the method above. It can be consolidated, but it is a bit complicated due to the generic/non-generic result. I'm going to come back to it later.
        if (compileResult is not CompileResult cr || cr is not { RoslynScript: { } rs })
            return InvocationResult<TResult?>.Create(false, "Unable to invoke script due to compilation errors!", default);

        var messages = new StringBuilder();
        try
        {
            var sw = Stopwatch.StartNew();
            var delegateType = (await rs.RunAsync()).ReturnValue;
            var @delegate = delegateType.GetType().GetMethod("Invoke");
            var result = (TResult?)@delegate?.Invoke(delegateType, null);
            sw.Stop();

            messages.AppendLine($"{string.Join("", Enumerable.Repeat('-', 13))}OUTPUT{string.Join("", Enumerable.Repeat('-', 13))}").AppendLine();
            messages.AppendLine($"RESULT VALUE: {result}");
            messages.AppendLine($"TIME ELAPSED: {TimeSpan.FromTicks(sw.ElapsedTicks).TotalSeconds:N4}s").AppendLine();

            return InvocationResult<TResult?>.Create(true, messages, result);
        }
        catch (Exception e)
        {
            return InvocationResult<TResult?>.Create(false, messages.AppendLine($"An exception was thrown by the target of invocation. Message: {e.Message}").AppendLine(e.StackTrace), default);
        }
    }
}