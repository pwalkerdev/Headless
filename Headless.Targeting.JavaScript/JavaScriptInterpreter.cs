using Esprima;
using Esprima.Ast;
using Esprima.Utils;
using Headless.Core;
using Headless.Core.Attributes;
using Jint;

namespace Headless.Targeting.JavaScript;

[SupportedTargets("JavaScript", versions: "latest|es5|es6|es7|es8|es9|es10|es11|es12|es13|es14|es2015|es2016|es2017|es2018|es2019|es2020|es2021|es2022|es2023", runtimes: "any")]
public class JavaScriptInterpreter : IScriptCompiler, IScriptInvoker
{
    public Task<ICompileResult> Compile(string script)
    {
        try
        {
            var jintScript = new JavaScriptParser().ParseScript(script);
            return Task.FromResult<ICompileResult>(new CompileResult(true, string.Empty, jintScript));
        }
        catch (Exception e)
        {
            return Task.FromResult<ICompileResult>(new CompileResult(false, e.Message, null));
        }
    }

    public Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult jintCompileResult || jintCompileResult.JintScript?.ToJavaScriptString() is not { Length: > 0 } script)
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(false, "Unable to invoke - invalid or unrecognised compiler result", default));

        try
        {
            var result = new Engine().Evaluate(script);
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(true, string.Empty, (TResult?)result.ToObject()));
        }
        catch (Exception e)
        {
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(false, e.Message, default));
        }
    }
}

internal class CompileResult(bool isSuccess, string messages, Script? jintScript) : ICompileResult
{
    public bool IsSuccess { get; } = isSuccess;
    public string Messages { get; } = messages;
    public Script? JintScript { get; } = jintScript;
}

public class InvocationResult<TResult>(bool isSuccess, string messages, TResult? result) : IInvocationResult<TResult>
{
    public bool IsSuccess { get; } = isSuccess;
    public string Messages { get; } = messages;
    public TResult? Result { get; } = result;
}