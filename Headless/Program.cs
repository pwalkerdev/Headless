var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(ConfigurationBuilderExtensions.AddHeadlessCommandLineMappings)
    .ConfigureServices(ServiceCollectionExtensions.AddHeadlessServices)
    .Build();

using var scope = host.Services.CreateScope();
var options = scope.ServiceProvider.GetRequiredService<CommandLineOptions>();
var script = options.Mode switch
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

if (script.Length == 0)
{
    Console.Error.WriteLine("Failed to receive script from caller!");
    return;
}

Console.Out.WriteLine($"{Environment.NewLine}-------------OUTPUT-------------{Environment.NewLine}");

var compileTaskTimedResult = await TimedTask.Run(() => scope.ServiceProvider.GetRequiredKeyedService<IScriptCompiler>(options.TargetKey).Compile(script.ToString()));
if (compileTaskTimedResult.TaskResult.IsSuccess)
{
    var invokeTaskTimedResult = await TimedTask.Run(() => scope.ServiceProvider.GetRequiredKeyedService<IScriptInvoker>(options.TargetKey).Run<object>(compileTaskTimedResult.TaskResult));
    if (invokeTaskTimedResult.TaskResult.IsSuccess)
    {
        Console.Out.WriteLine($"COMPILED IN: {compileTaskTimedResult.TaskDuration.TotalMilliseconds:N2} ms");
        Console.Out.WriteLine($"EXECUTED IN: {invokeTaskTimedResult.TaskDuration.TotalMilliseconds:N2} ms{Environment.NewLine}");
        Console.Out.WriteLine($"RESULT VALUE: {invokeTaskTimedResult.TaskResult.Result}");
    }
    else
        Console.Error.WriteLine(invokeTaskTimedResult.TaskResult.Messages);
}
else
    Console.Error.WriteLine($"{compileTaskTimedResult.TaskResult.Messages}");

Console.Out.WriteLine($"{Environment.NewLine}-------------FINISH-------------");