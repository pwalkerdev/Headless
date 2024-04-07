// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Scripting;

[SupportedTargets("CSharp", versions: "latest|3|4|5|6|7|7.1|7.2|7.3|8|9|10|11|12", runtimes: "any|net80")]
public class CSharpScriptInterpreter(IOptions<CommandLineOptions> commandLineOptions) : IScriptCompiler, IScriptInvoker
{
    private static readonly string[] _implicitImports = ["Headless.Targeting.CSharp.Framework", "System", "System.Collections", "System.Collections.Generic", "System.Linq"];
    private static readonly MetadataReference[] _assemblyReferences = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.ExportedTypes.Any(t => _implicitImports.Contains(t.Namespace))).Select(RoslynScriptingExtensions.GetMetadataReference).OfType<MetadataReference>().ToArray();

    public async Task<ICompileResult> Compile(string script)
    {
        // TODO: This method is in need of a re-work... Although it does work, I have some ideas to make it faster, more flexible and most importantly, easier to read. It was originally copied from a different project of mine and well yeah you will probably be able to tell from all the stuff it doesn't need to do.
        var options = commandLineOptions.Value;
        if (!options.LanguageVersion.ResolveLanguageVersion(out var languageVersion))
            return CompileResult.Create(false, $"Unrecognised value: \"{options.LanguageVersion}\" specified for parameter: \"LanguageVersion\"", null);

        var roslynScriptOptions = ScriptOptions.Default.WithLanguageVersion(languageVersion).WithReferences(_assemblyReferences).WithImports(_implicitImports);
        var roslynScript = CSharpScript.Create(script, roslynScriptOptions);
        try
        {
            var compilation = roslynScript.GetCompilation();
            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            ScriptImplementationDescriptor implementationDescriptor;

            if ((await syntaxTree.GetRootAsync()).Get<GlobalStatementSyntax>().SelectMany(gss => gss.Get<ExpressionStatementSyntax>()).SingleOrDefault()?.Expression is { } entryExpression)
            {
                // ReSharper disable once RedundantAssignment
                implementationDescriptor = new(MethodBodyImplementation.Expression, entryExpression.Get<ParameterSyntax>().Concat(entryExpression.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters)).Select((p, i) => new MethodParameter(i, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());

                // If the script is formatted as a lambda, wrap it in `new Func<object>(() => myScript)` - this improves compatibility with older language versions.
                var wrapperType = entryExpression.ChildNodes().OfType<ExpressionSyntax>().FirstOrDefault() is { } expr && semanticModel.GetTypeInfo(expr).Type is { Name: not "Void" } type ? $"Func<{type.Name}>" : "Action";
                var wrapper = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(wrapperType), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new [] { SyntaxFactory.Argument(entryExpression) })), null);

                roslynScript = CSharpScript.Create(wrapper.NormalizeWhitespace().ToString(), roslynScriptOptions);
            }
            else if ((await syntaxTree.GetRootAsync()).Get<MethodDeclarationSyntax>().SingleOrDefault() is { } entryMethod)
            {
                implementationDescriptor = new(MethodBodyImplementation.Block, entryMethod.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters).Select((p, i) => new MethodParameter(i, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());

                // If the script is a method, we need to inject a statement at the end to invoke the method
                var methodInvocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(entryMethod.Identifier.ToFullString()))
                    .AddArgumentListArguments(implementationDescriptor.ParameterDescriptors.Select((_, i) => SyntaxFactory.Argument(SyntaxFactory.IdentifierName($"p{i}"))).ToArray());
                var lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(methodInvocation)
                    .AddParameterListParameters(implementationDescriptor.ParameterDescriptors.Select((param, i) => SyntaxFactory.Parameter(SyntaxFactory.Identifier($"p{i}")).WithType(SyntaxFactory.ParseTypeName(param.Type!.Name))).ToArray());
                var wrapperType = entryMethod.ReturnType.ToString() == "void"
                    ? $"Action{(implementationDescriptor.ParameterDescriptors.Length == 0 ? "" : $"<{string.Join(',', implementationDescriptor.ParameterDescriptors.Select(pd => pd.Type!.Name))}>")}"
                    : $"Func<{(implementationDescriptor.ParameterDescriptors.Length == 0 ? "" : $"{string.Join(',', implementationDescriptor.ParameterDescriptors.Select(pd => pd.Type!.Name))},")}{entryMethod.ReturnType}>";
                var wrapper = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(wrapperType), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(lambdaExpression) })), null);
                var globalStatement = SyntaxFactory.GlobalStatement(SyntaxFactory.ExpressionStatement(wrapper)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.SemicolonToken, string.Empty, string.Empty, SyntaxTriviaList.Empty)));

                roslynScript = CSharpScript.Create(entryMethod.Parent!.InsertNodesAfter(entryMethod, new[] { globalStatement.NormalizeWhitespace() }).ToString(), roslynScriptOptions);
            }
            else
                throw new InvalidOperationException("Unable to determine script entry point -- eventually this won't be a problem... But for now scripts need to be written as a single method or expression body. Local methods are supported");

            var roslynAnalysis = roslynScript.Compile();
            return CompileResult.Create(roslynAnalysis.All(msg => msg.Severity < DiagnosticSeverity.Error), string.Join(Environment.NewLine, roslynAnalysis), roslynScript);
        }
        catch (Exception e)
        {
            return CompileResult.Create(false, $"Exception: {e.Message}", roslynScript);
        }
    }

    public async Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult { IsSuccess: true, RoslynScript: { } rs })
            return InvocationResult<TResult?>.Create(false, "Unable to invoke script due to compilation errors!", default);

        var messages = new StringBuilder();
        try
        {
            var sw = Stopwatch.StartNew();
            var delegateType = (await rs.RunAsync()).ReturnValue;
            var @delegate = delegateType.GetType().GetMethod("Invoke");
            var result = (TResult?)@delegate?.Invoke(delegateType, null);
            sw.Stop();

            messages.AppendLine().AppendLine($"{string.Join("", Enumerable.Repeat('-', 13))}OUTPUT{string.Join("", Enumerable.Repeat('-', 13))}").AppendLine();
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