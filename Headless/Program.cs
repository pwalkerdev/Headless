var host = Host.CreateDefaultBuilder()
    .ConfigureSerilog()
    .ConfigureAppConfiguration(ConfigurationBuilderExtensions.AddHeadlessCommandLineMappings)
    .ConfigureServices(ServiceCollectionExtensions.AddHeadlessServices)
    .Build();

using var scope = host.Services.CreateScope();
var options = scope.ServiceProvider.GetRequiredService<CommandLineOptions>();
var script = options.InputMode switch
{
    ScriptInputMode.File => new StringBuilder(File.ReadAllText(options.Script)),
    ScriptInputMode.Stream => new Func<StringBuilder>(() =>
    {
        var result = new StringBuilder();
        while (Console.ReadLine() is { } ln && ln != options.Postamble)
            result.AppendLine(ln);

        ConsoleExtensions.BlankOutLastLine(options.Postamble.Length); // If the input is streamed, the "finish" token is written to the console at the end. This hides it
        ConsoleExtensions.ShiftCursorUp(1);
        return result;
    })(),
    _ => new StringBuilder(options.Script),
};

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
if (script.Length == 0)
{
    logger.LogError("Failed to receive script from caller!");
    return;
}

logger.LogInformation("-------------HEADLESS-----------");

var compileTaskTimedResult = await scope.ServiceProvider.GetRequiredKeyedService<IScriptCompiler>(options.TargetKey).Compile(script.ToString()).WithTimer();
if (compileTaskTimedResult.TaskResult.IsSuccess)
{
    var invokeTaskTimedResult = await scope.ServiceProvider.GetRequiredKeyedService<IScriptInvoker>(options.TargetKey).Run<object>(compileTaskTimedResult.TaskResult).WithTimer();
    if (invokeTaskTimedResult.TaskResult.IsSuccess)
    {
        logger.LogInformation("COMPILED IN: {CompileDuration}s", compileTaskTimedResult.TaskDuration.ToString("ss\\.fffffff"));
        logger.LogInformation("EXECUTED IN: {InvocationDuration}s", invokeTaskTimedResult.TaskDuration.ToString("ss\\.fffffff"));
        logger.LogInformation("RESULT VALUE: {Result}", invokeTaskTimedResult.TaskResult.Result);
    }
    else
        logger.LogError("{ErrorMessasges}", invokeTaskTimedResult.TaskResult.Messages);
}
else
    logger.LogError("{ErrorMessasges}", compileTaskTimedResult.TaskResult.Messages);

logger.LogInformation("-------------FINISH-------------");
