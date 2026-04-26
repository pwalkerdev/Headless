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
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(false, "Unable to invoke - invalid or unrecognised compiler result", null, default));

        try
        {
            var evaluateResult = new Engine(options => { options.Strict = interpreterOptions.StrictMode; }).Evaluate(script);
            var resultObject = (TResult?)evaluateResult.ToObject();

            // NOTE: because JavaScript is so "chill like that" with data types, i have to resort to using GetType() on the result value which ofcourse will be null if the result is null.
            //  this does make the result type inaccurate in some circumstances but i am not aware of a better way or if implicit typing can be done with this JS compiler.
            //  as usual, it's fine. might come back to this later, might not. don't care to be frank because JS sucks anyway
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(true, string.Empty, resultObject?.GetType(), resultObject));
        }
        catch (Exception e)
        {
            return Task.FromResult<IInvocationResult<TResult?>>(new InvocationResult<TResult?>(false, $"ERROR: {e.Message}", null, default));
        }
    }
}
