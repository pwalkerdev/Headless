namespace Headless.Targeting.JavaScript;

[SupportedTargets("JavaScript", versions: "latest|es5|es6|es7|es8|es9|es10|es11|es12|es13|es14|es2015|es2016|es2017|es2018|es2019|es2020|es2021|es2022|es2023", runtimes: "any")]
public class JavaScriptInterpreter(JavaScriptInterpreterOptions interpreterOptions) : IScriptCompiler, IScriptInvoker
{
    public Task<ICompileResult> Compile(string script)
    {
        try
        {
            var jintScript = new JavaScriptParser(new ParserOptions { Tolerant = !interpreterOptions.StrictMode }).ParseScript(script);
            return Task.FromResult<ICompileResult>(new CompileResult(true, string.Empty, jintScript));
        }
        catch (Exception e)
        {
            return Task.FromResult<ICompileResult>(new CompileResult(false, $"ERROR: {e.Message}", null));
        }
    }

    public Task<IInvocationResult<TResult?>> Run<TResult>(ICompileResult compileResult)
    {
        if (compileResult is not CompileResult jintCompileResult || jintCompileResult.JintScript?.ToJavaScriptString() is not { Length: > 0 } script)
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(false, "Unable to invoke - invalid or unrecognised compiler result", default));

        try
        {
            var result = new Engine(options => { options.Strict = interpreterOptions.StrictMode; }).Evaluate(script);
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(true, string.Empty, (TResult?)result.ToObject()));
        }
        catch (Exception e)
        {
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(false, $"ERROR: {e.Message}", default));
        }
    }
}