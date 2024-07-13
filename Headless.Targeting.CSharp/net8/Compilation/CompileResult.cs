// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Compilation;

internal class CompileResult : ICompileResult
{
    public bool IsSuccess { get; init; }
    public string Messages { get; init; } = string.Empty;
    [Obsolete]
    internal Script<object>? RoslynScript { get; init; }
    internal Assembly? OutputAssebly { get; init; }

    [Obsolete]
    internal static CompileResult Create(bool isSuccess, StringBuilder outputBuilder, Script<object>? roslynScript) => Create(isSuccess, outputBuilder.ToString(), roslynScript);
    [Obsolete]
    internal static CompileResult Create(bool isSuccess, string output, Script<object>? roslynScript) => new()
    {
        IsSuccess = isSuccess,
        Messages = output,
        RoslynScript = roslynScript
    };

    internal static CompileResult Create(bool isSuccess, StringBuilder outputBuilder, Assembly? outputAssembly) => Create(isSuccess, outputBuilder.ToString(), outputAssembly);
    internal static CompileResult Create(bool isSuccess, string output, Assembly? outputAssembly) => new()
    {
        IsSuccess = isSuccess,
        Messages = output,
        OutputAssebly = outputAssembly
    };
}