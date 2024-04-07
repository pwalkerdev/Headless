using System.Threading.Tasks;

namespace Headless.Core;

public interface IScriptCompiler
{
    Task<ICompileResult> Compile(string script);
}