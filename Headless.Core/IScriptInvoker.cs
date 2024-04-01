using System.Threading.Tasks;

namespace Headless.Core;

public interface IScriptInvoker
{
    Task<IInvocationResult> Run(ICompileResult compileResult);
    Task<IInvocationResult<TResult>> Run<TResult>(ICompileResult compileResult);
}