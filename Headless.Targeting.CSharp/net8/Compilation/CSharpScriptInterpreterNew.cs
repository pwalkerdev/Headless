// ReSharper disable once CheckNamespace
using Microsoft.CodeAnalysis.Emit;

namespace Headless.Targeting.CSharp.Compilation;

[SupportedTargets("CSharp-new", versions: "latest|3|4|5|6|7|7.1|7.2|7.3|8|9|10|11|12", runtimes: "any|net80")]
public class CSharpScriptInterpreterNew(CommandLineOptions commandLineOptions, CSharpScriptInterpreterOptions interpreterOptions) : IScriptCompiler, IScriptInvoker
{
    public async Task<ICompileResult> Compile(string script)
    {
        try
        {
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion).WithKind(SourceCodeKind.Regular);
            var syntaxTree = (interpreterOptions.ImplementationScheme switch
            {
                CSharpScriptImplementationScheme.Expression => SyntaxTreeBuilder.FromExpression(script, parseOptions),
                CSharpScriptImplementationScheme.SingleStatement => SyntaxTreeBuilder.FromStatement(script, parseOptions),
                CSharpScriptImplementationScheme.MultiStatement => SyntaxTreeBuilder.FromStatement(script, parseOptions),
                CSharpScriptImplementationScheme.Method => SyntaxTreeBuilder.FromMethod(script, parseOptions),
                CSharpScriptImplementationScheme.MethodExpressionBody => SyntaxTreeBuilder.FromMethod(script, parseOptions),
                CSharpScriptImplementationScheme.Class => SyntaxTreeBuilder.FromClass(script, parseOptions),
                CSharpScriptImplementationScheme.Namespace => SyntaxTreeBuilder.FromNamespace(script, parseOptions),
                _ => throw new NotImplementedException()
            }).WithFilePath(SourceFilePath);

            var compilation = CreateCompilation([SyntaxTreeBuilder.FromNamespace(string.Join(Environment.NewLine, Usings.Select(u => $"global using {u};")), parseOptions).WithFilePath(".usings.cs"), syntaxTree]);

            var (success, diagnostics, bytes) = EmitCompilation(compilation);
            if (!success || commandLineOptions.RunMode == RunMode.CompileOnly)
                return await Task.FromResult<ICompileResult>(CompileResult.Create(success, string.Join(Environment.NewLine, diagnostics), default(Assembly)));

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
            var entryInfo = ass.GetTypes().Where(t => t.IsClass && (!t.IsAbstract || t.IsSealed))   // Static classes are abstract and sealed
                .Select(t => new 
                {
                    Type = t,                                                                       // Fetch each type
                    MethodInfo = t.GetMethods().Select(m => new
                    {
                        Method = m,                                                                 // Fetch each type's methods
                        EntryPointAttribute = m.GetCustomAttribute<Framework.EntryPointAttribute>() // Fetch each member's EntryPointAttribute (if specified)
                    })
                    .OrderBy(tpl => tpl.EntryPointAttribute == null)                                // Order methods by the specification of EntryPointAttribute
                    .FirstOrDefault()                                                               // Take first
                })
                .Where(info => info.MethodInfo != null)
                .OrderBy(info => info.MethodInfo?.EntryPointAttribute == null)                      // Order types by their first method's entry point attribute instance
                .First();                                                                           // First will be the explicit entry point or the first exported type's first method

            var instance = !entryInfo.Type.IsAbstract ? Activator.CreateInstance(entryInfo.Type) : null;
            var result = entryInfo.MethodInfo!.Method.Invoke(instance, entryInfo.MethodInfo.EntryPointAttribute?.Arguments);

            return await Task.FromResult(InvocationResult<TResult?>.Create(true, string.Empty, (TResult?)result));
        }
        catch (Exception e) when (e is { InnerException: { } ie })
        {
            return await Task.FromResult(InvocationResult<TResult?>.Create(false, new StringBuilder().AppendLine(ie.Message).AppendLine(ie.StackTrace), default));
        }
        catch (Exception e)
        {
            return await Task.FromResult(InvocationResult<TResult?>.Create(false, new StringBuilder().AppendLine(e.Message).AppendLine(e.StackTrace), default));
        }
    }

    private LanguageVersion LanguageVersion { get; } = commandLineOptions.LanguageVersion.ResolveLanguageVersion();

    private string SourceFilePath { get; } = interpreterOptions.FileName ?? commandLineOptions.InputMode switch
    {
        ScriptInputMode.File => commandLineOptions.Script,
        ScriptInputMode.Stream => $"{commandLineOptions.Postamble}.cs",
        _ => $"Headless+{Guid.NewGuid()}.cs"
    };
    
    private static string[] Usings => CSharpScriptInterpreter.ImplicitImports; // TODO: Default to a list of all implicit namespaces with commandline argumnt to override
    private static MetadataReference[] References => CSharpScriptInterpreter.AssemblyReferences; // TODO: Update static references to include all assemblies in target platform

    private CSharpCompilationOptions CompilationOptions { get; } = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        //.WithAllowUnsafe() // TODO: Add command line option to allow unsafe
        //.WithPlatform() // TODO: Add command line option for target platform
        .WithUsings(Usings) // NOTE: The .WithUsings() method only affects compilations with kind SourceCodeKind.Script - this is important information that is not documented :)
        .WithOptimizationLevel(commandLineOptions.RunMode == RunMode.Debug ? OptimizationLevel.Debug : OptimizationLevel.Release)
        .WithConcurrentBuild(true);

    private CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTree) => CSharpCompilation.Create(SourceFilePath, syntaxTree, References, CompilationOptions);

    private static (bool success, ImmutableArray<Diagnostic> diagnostics, byte[] bytes) EmitCompilation(CSharpCompilation compilation)
    {
        using var assemblyStream = new MemoryStream();
        var emitResult = compilation.Emit(assemblyStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
        
        return (emitResult.Success, emitResult.Diagnostics, assemblyStream.ToArray()); // TODO: might be worth revisiting this depending on performance
    }
}