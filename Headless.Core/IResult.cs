namespace Headless.Core;

public interface IResult
{
    bool IsSuccess { get; }
    string Messages { get; }
}

public interface ICompileResult : IResult;

public interface IInvocationResult : IResult;

public interface IInvocationResult<out TResult> : IInvocationResult
{
    TResult Result { get; }
}