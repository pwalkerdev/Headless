namespace Headless.Extensions;

internal class TimedResult<TResult>(TResult taskResult, TimeSpan taskDuration)
{
    public TResult TaskResult { get; } = taskResult;
    public TimeSpan TaskDuration { get; } = taskDuration;
}

internal static class TimedTask
{
    public static async Task<TimedResult<TResult>> WithTimer<TResult>(this Task<TResult> task)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await task;
        stopwatch.Stop();

        return new TimedResult<TResult>(result, stopwatch.Elapsed);
    }

    public static Task<TimedResult<TResult>> Run<TResult>(Func<Task<TResult>> task) => Task.Run(task).WithTimer();
}