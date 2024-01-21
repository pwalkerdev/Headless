namespace Headless.Core
{
    public interface IInvocationResult
    {
        bool IsSuccess { get; }
        string Messages { get; }
    }

    public interface IInvocationResult<out TResult> : IInvocationResult
    {
        TResult Result { get; }
    }
}
