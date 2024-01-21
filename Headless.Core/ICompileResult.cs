namespace Headless.Core
{
    public interface ICompileResult
    {
        bool IsSuccess { get; }
        string Messages { get; }
    }
}