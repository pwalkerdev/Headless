// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Scripting;

[SupportedTargets("CSharp", versions: "latest|3|4|5|6|7|7.1|7.2|7.3|8|9|10|11|12", runtimes: "any|net80")]
public class CSharpScriptInterpreter(CommandLineOptions commandLineOptions, CSharpScriptInterpreterOptions interpreterOptions) : IScriptCompiler, IScriptInvoker
{
    internal static string[] ImplicitImports { get; } = [ "Headless.Targeting.CSharp.Framework", "System", "System.Collections", "System.Collections.Generic", "System.Linq" ];
    internal static MetadataReference[] AssemblyReferences { get; } = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.ExportedTypes.Any(t => ImplicitImports.Contains(t.Namespace))).Select(RoslynScriptingExtensions.GetMetadataReference).OfType<MetadataReference>().ToArray();

    public async Task<ICompileResult> Compile(string script)
    {
        if (interpreterOptions.ImplementationScheme != CSharpScriptImplementationScheme.Automatic)
            throw new NotSupportedException("The specified execution scheme is currently unsupported. It's probably not too long away though");

        // TODO: This method is in need of a re-work... Although it does work, I have some ideas to make it faster, more flexible and most importantly, easier to read. It was originally copied from a different project of mine and well yeah you will probably be able to tell from all the stuff it doesn't need to do.
        if (!commandLineOptions.LanguageVersion.ResolveLanguageVersion(out var languageVersion))
            return CompileResult.Create(false, $"Unrecognised value: \"{commandLineOptions.LanguageVersion}\" specified for parameter: \"LanguageVersion\"", null);

        //var roslynScriptOptions = ScriptOptions.Default.WithLanguageVersion(languageVersion).WithReferences(AssemblyReferences).WithImports(ImplicitImports).WithEmitDebugInformation(commandLineOptions.RunMode == RunMode.Debug);
        //var roslynScript = CSharpScript.Create(script, roslynScriptOptions);
        //var filePath = @$"Headless+{Guid.NewGuid()}.cs";
        var filePath = "prototypervfs:///Untitled-1.cs";
        var roslynScript = CSharpScript.Create($"#load \"{filePath}\"", ScriptOptions.Default
                                                                        .WithLanguageVersion(languageVersion)
                                                                        .WithReferences(AssemblyReferences)
                                                                        .WithImports(ImplicitImports)
                                                                        .WithEmitDebugInformation(commandLineOptions.RunMode == RunMode.Debug)
                                                                        .WithFilePath(filePath)
                                                                        .WithFileEncoding(Encoding.UTF8)
                                                                        .WithSourceResolver(new HeadlessCSharpScriptSourceResolver(new() { { filePath, script } })));

        try
        {
            var compilation = roslynScript.GetCompilation();
            var syntaxTree = compilation.SyntaxTrees.First();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            ScriptImplementationDescriptor implementationDescriptor;

            if ((await syntaxTree.GetRootAsync()).Get<GlobalStatementSyntax>().SelectMany(gss => gss.Get<ExpressionStatementSyntax>()).SingleOrDefault()?.Expression is { } entryExpression)
            {
                // ReSharper disable once RedundantAssignment
                implementationDescriptor = new(MethodBodyImplementation.Expression, entryExpression.Get<ParameterSyntax>().Concat(entryExpression.Get<ParameterListSyntax>().SelectMany(pl => pl.Parameters)).Select((p, i) => new MethodParameter(i, p.Identifier.Text, semanticModel.ResolveParameterConcreteType(p))).ToArray());

                // If the script is formatted as a lambda, wrap it in `new Func<object>(() => myScript)` - this improves compatibility with older language versions.
                var wrapperType = entryExpression.ChildNodes().OfType<ExpressionSyntax>().FirstOrDefault() is { } expr && semanticModel.GetTypeInfo(expr).Type is { Name: not "Void" } type ? $"Func<{type.Name}>" : "Action";
                var wrapper = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(wrapperType), SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new [] { SyntaxFactory.Argument(entryExpression) })), null);

                roslynScript = CSharpScript.Create(wrapper.NormalizeWhitespace().ToString(), roslynScript.Options);
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

                //roslynScript = CSharpScript.Create(entryMethod.Parent!.InsertNodesAfter(entryMethod, [globalStatement.NormalizeWhitespace()]).ToString(), roslynScript.Options);
                roslynScript = CSharpScript.Create($"#load \"{filePath}\"{Environment.NewLine}{globalStatement.NormalizeWhitespace()}", roslynScript.Options);
            }
            else
                throw new InvalidOperationException("Unable to determine script entry point -- eventually this won't be a problem... But for now scripts need to be written as a single method or expression body. Local methods are supported");

            var roslynAnalysis = roslynScript.Compile();
            return CompileResult.Create(roslynAnalysis.All(msg => msg.Severity < DiagnosticSeverity.Error), string.Join(Environment.NewLine, roslynAnalysis), roslynScript);
        }
        catch (Exception e)
        {
            return CompileResult.Create(false, $"ERROR: {e.Message}", roslynScript);
        }
    }

    public async Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult { IsSuccess: true, RoslynScript: { } rs })
            return InvocationResult<TResult?>.Create(false, "Unable to invoke script due to compilation errors!", default);

        try
        {
            var delegateType = (await rs.RunAsync()).ReturnValue;
            var @delegate = delegateType.GetType().GetMethod("Invoke");
            var result = (TResult?)@delegate?.Invoke(delegateType, null);

            return InvocationResult<TResult?>.Create(true, string.Empty, result);
        }
        catch (Exception e) when (e is { InnerException: {} ie })
        {
            return InvocationResult<TResult?>.Create(false, new StringBuilder($"An exception was thrown by the target of invocation. Message: {ie.Message}{Environment.NewLine}").AppendLine(ie.StackTrace), default);
        }
        catch (Exception e)
        {
            return InvocationResult<TResult?>.Create(false, new StringBuilder($"An exception was thrown by the target of invocation. Message: {e.Message}{Environment.NewLine}").AppendLine(e.StackTrace), default);
        }
    }
}

internal class HeadlessCSharpScriptSourceResolver(Dictionary<string, string>? scriptsByName = null) : SourceReferenceResolver
{
    private readonly SourceFileResolver _baseResolver = new([], AppContext.BaseDirectory);
    private readonly Dictionary<string, string> _inMemorySourceFiles = scriptsByName ?? [];

    public override string? NormalizePath(string path, string? baseFilePath) => path.StartsWith("Headless+") ? path : _baseResolver.NormalizePath(path, baseFilePath);
    public override string? ResolveReference(string path, string? baseFilePath) => _inMemorySourceFiles.ContainsKey(path) ? path : _baseResolver.ResolveReference(path, baseFilePath);
    public override Stream OpenRead(string resolvedPath) => _inMemorySourceFiles.TryGetValue(resolvedPath, out var content) ? new MemoryStream(Encoding.UTF8.GetBytes(content)) : _baseResolver.OpenRead(resolvedPath);

    // public override string? NormalizePath(string path, string? baseFilePath) => _baseResolver.NormalizePath(path, baseFilePath);
    // public override string? ResolveReference(string path, string? baseFilePath) => _baseResolver.ResolveReference(path, baseFilePath);
    // public override Stream OpenRead(string resolvedPath) => _baseResolver.OpenRead(resolvedPath);

    protected bool Equals(HeadlessCSharpScriptSourceResolver other) => Equals(_inMemorySourceFiles, other._inMemorySourceFiles) && Equals(_baseResolver, other._baseResolver);
    public override bool Equals(object? obj) => obj is HeadlessCSharpScriptSourceResolver sr && (ReferenceEquals(this, obj) || Equals(sr));
    public override int GetHashCode() => unchecked (((37 * 397) ^ (_inMemorySourceFiles?.GetHashCode() ?? 0)) * 397) ^ (_baseResolver?.GetHashCode() ?? 0);
}