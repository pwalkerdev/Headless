using System.Threading.Tasks;

namespace Headless.Core
{
    public interface IRunScripts
    {
        Task<IInvocationResult> Run(ICompileResult compileResult);
        Task<IInvocationResult<TResult>> Run<TResult>(ICompileResult compileResult);
    }
}
