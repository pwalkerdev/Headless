// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Compilation;

[SupportedTargets("CSharp-new", versions: "latest|3|4|5|6|7|7.1|7.2|7.3|8|9|10|11|12", runtimes: "any|net80")]
public class CSharpScriptInterpreterNew(CommandLineOptions commandLineOptions, CSharpScriptInterpreterOptions interpreterOptions) : IScriptCompiler, IScriptInvoker
{
    public async Task<ICompileResult> Compile(string script)
    {
        try
        {
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion);
            var syntaxTree = interpreterOptions.ImplementationScheme switch
            {
                CSharpScriptImplementationScheme.Expression => SyntaxTreeBuilder.FromExpression(script, parseOptions),
                CSharpScriptImplementationScheme.SingleStatement => SyntaxTreeBuilder.FromStatement(script, parseOptions),
                CSharpScriptImplementationScheme.MultiStatement => SyntaxTreeBuilder.FromStatement(script, parseOptions),
                CSharpScriptImplementationScheme.Method => SyntaxTreeBuilder.FromMethod(script, parseOptions),
                CSharpScriptImplementationScheme.MethodExpressionBody => SyntaxTreeBuilder.FromMethod(script, parseOptions),
                CSharpScriptImplementationScheme.Class => SyntaxTreeBuilder.FromClass(script, parseOptions),
                CSharpScriptImplementationScheme.Namespace => SyntaxTreeBuilder.FromNamespace(script, parseOptions),
                _ => throw new NotImplementedException()
            };

            var compilation = CreateCompilation(syntaxTree);

            var (diagnostics, bytes) = EmitCompilation(compilation);
            if (commandLineOptions.RunMode == RunMode.CompileOnly)
                return await Task.FromResult<ICompileResult>(CompileResult.Create(true, string.Join(Environment.NewLine, diagnostics), default(Assembly)));

            var outputAssembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(bytes));
            return await Task.FromResult<ICompileResult>(CompileResult.Create(true, string.Join(Environment.NewLine, diagnostics), outputAssembly));
        }
        catch (CompilationErrorException e)
        {
            return await Task.FromResult<ICompileResult>(CompileResult.Create(false, string.Join(Environment.NewLine, [$"{e.Message}:{Environment.NewLine}", ..e.Diagnostics]), default(Assembly)));
        }
        catch (Exception e)
        {
            return await Task.FromResult<ICompileResult>(CompileResult.Create(false, $"ERROR: {e.Message}", default(Assembly)));
        }
    }

    public async Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult { IsSuccess: true, OutputAssebly: { } ass })
            return InvocationResult<TResult?>.Create(false, "Unable to invoke script due to compilation errors!", default);

        try
        {
            var type = ass.GetTypes().First(t => t.IsClass && (!t.IsAbstract || t.IsSealed)); // Static classes are abstract and sealed
            var instance = !type.IsAbstract ? Activator.CreateInstance(type) : null;
            var result = type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault()?.Invoke(instance, null);

            return await Task.FromResult(InvocationResult<TResult?>.Create(true, string.Empty, (TResult?)result));
        }
        catch (Exception e)
        {
            return await Task.FromResult(InvocationResult<TResult?>.Create(false, new StringBuilder($"An exception was thrown by the target of invocation. Message: {e.Message}").AppendLine(e.StackTrace), default));
        }
    }

    private LanguageVersion LanguageVersion { get; } = commandLineOptions.LanguageVersion.ResolveLanguageVersion();

    private string SourceFilePath { get; } = interpreterOptions.FileName ?? commandLineOptions.InputMode switch
    {
        ScriptInputMode.File => commandLineOptions.Script,
        ScriptInputMode.Stream => $"{commandLineOptions.Postamble}.cs",
        _ => $"Headless+{Guid.NewGuid()}.cs"
    };
    
    private static MetadataReference[] References => CSharpScriptInterpreter.AssemblyReferences; // TODO - Update static references to include all assemblies in target platform

    private CSharpCompilationOptions CompilationOptions { get; } = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        //.WithAllowUnsafe() // TODO - Add command line option to allow unsafe
        //.WithPlatform() // TODO - Add command line option for target platform
        .WithOptimizationLevel(commandLineOptions.RunMode == RunMode.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release)
        .WithConcurrentBuild(true);

    private CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTree) => CSharpCompilation.Create(SourceFilePath, syntaxTree, References, CompilationOptions);

    private static (ImmutableArray<Diagnostic> diagnostics, byte[] bytes) EmitCompilation(CSharpCompilation compilation)
    {
        using var assemblyStream = new MemoryStream();

        // TODO - this doesn't cover all bases. Code can compile with hints/warnings but they will not be shown to the user
        var emitResult = compilation.Emit(assemblyStream);
        if (!emitResult.Success)
            throw new CompilationErrorException("If you see this error then I haven't finished the compiler rework yet! (not even close)", emitResult.Diagnostics);
        
        return (emitResult.Diagnostics, assemblyStream.ToArray()); // TODO - might be worth revisiting this depending on performance
    }
}