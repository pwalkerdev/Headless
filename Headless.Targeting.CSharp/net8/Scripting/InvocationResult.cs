// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Scripting;

internal class InvocationResult<TResult> : IInvocationResult<TResult>
{
    public bool IsSuccess { get; init; }
    public string Messages { get; init; } = string.Empty;
    public TResult? Result { get; init; }

    internal static InvocationResult<TResult> Create(bool isSuccess, StringBuilder messagesBuilder, TResult? result) => Create(isSuccess, messagesBuilder.ToString(), result);
    internal static InvocationResult<TResult> Create(bool isSuccess, string messages, TResult? result) => new()
    {
        IsSuccess = isSuccess,
        Messages = messages,
        Result = result
    };
}