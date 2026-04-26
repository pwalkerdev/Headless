// ReSharper disable once CheckNamespace
namespace Headless.Targeting.CSharp.Compilation;

internal class InvocationResult<TResult> : IInvocationResult<TResult>
{
    public bool IsSuccess { get; init; }
    public string Messages { get; init; } = string.Empty;
    public Type? ResultType { get; init; }
    public TResult? Result { get; init; }

    internal static InvocationResult<TResult> Create(bool isSuccess, StringBuilder messagesBuilder, Type? resultType, TResult? result) => Create(isSuccess, messagesBuilder.ToString(), resultType, result);
    internal static InvocationResult<TResult> Create(bool isSuccess, string messages, Type? resultType, TResult? result) => new()
    {
        IsSuccess = isSuccess,
        Messages = messages,
        ResultType = resultType,
        Result = result
    };
}
