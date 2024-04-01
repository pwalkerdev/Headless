namespace Headless.Core;

public interface IResult
{
    bool IsSuccess { get; }
    string Messages { get; }
}

public interface ICompileResult : IResult;

public interface IInvocationResult<out TResult> : IResult
{
    TResult? Result { get; }
}