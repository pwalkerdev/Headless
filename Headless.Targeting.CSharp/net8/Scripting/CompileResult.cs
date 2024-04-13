// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Scripting;

internal class CompileResult : ICompileResult
{
    public bool IsSuccess { get; init; }
    public string Messages { get; init; } = string.Empty;
    internal Script<object>? RoslynScript { get; init; }

    internal static CompileResult Create(bool isSuccess, StringBuilder outputBuilder, Script<object>? roslynScript) => Create(isSuccess, outputBuilder.ToString(), roslynScript);
    internal static CompileResult Create(bool isSuccess, string output, Script<object>? roslynScript) => new()
    {
        IsSuccess = isSuccess,
        Messages = output,
        RoslynScript = roslynScript
    };
}