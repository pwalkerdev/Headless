// ReSharper disable once CheckNamespace
namespace Headless.Targetting.CSharp.Scripting;

public class InvocationResult : IInvocationResult
{
    public bool IsSuccess { get; init; }
    public string Messages { get; init; } = string.Empty;

    internal static InvocationResult Create(bool isSuccess, StringBuilder messagesBuilder) => Create(isSuccess, messagesBuilder.ToString());
    internal static InvocationResult Create(bool isSuccess, string messages) => new()
    {
        IsSuccess = isSuccess,
        Messages = messages
    };
}

public class InvocationResult<TResult> : InvocationResult, IInvocationResult<TResult>
{
    private readonly TResult? _result;
    public TResult Result { get => _result ?? default!; init => _result = value; }

    internal static InvocationResult<TResult> Create(bool isSuccess, StringBuilder messagesBuilder, TResult result) => Create(isSuccess, messagesBuilder.ToString(), result);
    internal static InvocationResult<TResult> Create(bool isSuccess, string messages, TResult result) => new()
    {
        IsSuccess = isSuccess,
        Messages = messages,
        Result = result
    };
}