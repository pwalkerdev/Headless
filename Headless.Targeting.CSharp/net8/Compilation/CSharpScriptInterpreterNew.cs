// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Compilation;

[SupportedTargets("CSharp-new", versions: "latest|3|4|5|6|7|7.1|7.2|7.3|8|9|10|11|12", runtimes: "any|net80")]
public class CSharpScriptInterpreterNew(CommandLineOptions commandLineOptions, CSharpScriptInterpreterOptions interpreterOptions) : IScriptCompiler, IScriptInvoker
{
    public async Task<ICompileResult> Compile(string script)
    {
        try
        {
            var roslynScript = interpreterOptions.ImplementationScheme switch
            {
                CSharpScriptImplementationScheme.Method => CreateScriptWithMethodBody(script),
                _ => throw new NotImplementedException()
            };

            var roslynAnalysis = roslynScript.Compile();
            return await Task.FromResult<ICompileResult>(CompileResult.Create(roslynAnalysis.All(msg => msg.Severity < DiagnosticSeverity.Error), string.Join(Environment.NewLine, roslynAnalysis), roslynScript));
        }
        catch (Exception e)
        {
            return await Task.FromResult<ICompileResult>(CompileResult.Create(false, $"ERROR: {e.Message}", default));
        }
    }

    public async Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult { IsSuccess: true, RoslynScript: { } rs })
            return InvocationResult<TResult?>.Create(false, "Unable to invoke script due to compilation errors!", default);

        try
        {
            //var delegateType = (await rs.RunAsync()).ReturnValue;
            //var @delegate = delegateType.GetType().GetMethod("Invoke");
            //var result = (TResult?)@delegate?.Invoke(delegateType, null);
            var result = (TResult)(await rs.RunAsync()).ReturnValue;

            return InvocationResult<TResult?>.Create(true, string.Empty, result);
        }
        catch (Exception e)
        {
            return InvocationResult<TResult?>.Create(false, new StringBuilder($"An exception was thrown by the target of invocation. Message: {e.Message}").AppendLine(e.StackTrace), default);
        }
    }

    private LanguageVersion LanguageVersion { get; } = commandLineOptions.LanguageVersion.ResolveLanguageVersion();

    private string SourceFilePath { get; } = interpreterOptions.FileName ?? commandLineOptions.InputMode switch
    {
        ScriptInputMode.File => commandLineOptions.Script,
        ScriptInputMode.Stream => $"{commandLineOptions.Postamble}.cs",
        _ => $"Headless+{Guid.NewGuid()}.cs"
    };

    private Script<object> CreateScriptWithMethodBody(string script) =>
        CSharpScript.Create(script, ScriptOptions.Default
            .WithLanguageVersion(LanguageVersion)
            .WithReferences(CSharpScriptInterpreter.AssemblyReferences)
            .WithImports(CSharpScriptInterpreter.ImplicitImports)
            .WithEmitDebugInformation(commandLineOptions.RunMode == RunMode.Debug)
            .WithFilePath(SourceFilePath)
            .WithFileEncoding(Encoding.UTF8)
            .WithSourceResolver(new HeadlessCSharpScriptSourceResolver(new() { { SourceFilePath, script } })));

    //private CSharpCompilation CreateCompilationFromMethod(string source) =>
    //    CSharpCompilation.Create(Guid.NewGuid().ToString().Replace("-", "")) // TODO - allow for explicit naming of scripts through command line. Named scripts can be cached and re-run
    //        .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
    //        .WithReferences(CSharpScriptInterpreter.AssemblyReferences)
    //        .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(source));
}