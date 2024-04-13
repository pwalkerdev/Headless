namespace Headless.Targeting.JavaScript;

internal class InvocationResult<TResult>(bool isSuccess, string messages, TResult? result) : IInvocationResult<TResult>
{
    public bool IsSuccess { get; } = isSuccess;
    public string Messages { get; } = messages;
    public TResult? Result { get; } = result;
}