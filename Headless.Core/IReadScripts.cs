using System.Threading.Tasks;

namespace Headless.Core
{
    public interface IReadScripts
    {
        Task<ICompileResult> Compile(string script);
    }
}
