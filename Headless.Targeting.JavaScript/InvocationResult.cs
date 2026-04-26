namespace Headless.Targeting.JavaScript;

internal class InvocationResult<TResult>(bool isSuccess, string messages, Type? resultType, TResult? result) : IInvocationResult<TResult>
{
    public bool IsSuccess { get; } = isSuccess;
    public string Messages { get; } = messages;
    public Type? ResultType { get; } = resultType;
    public TResult? Result { get; } = result;
}
