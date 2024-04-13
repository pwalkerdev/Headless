namespace Headless.Targeting.JavaScript;

internal class CompileResult(bool isSuccess, string messages, Script? jintScript) : ICompileResult
{
    public bool IsSuccess { get; } = isSuccess;
    public string Messages { get; } = messages;
    public Script? JintScript { get; } = jintScript;
}