var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(config => config.AddCommandLine(args, new Dictionary<string, string>
    {
        { "-l", "language" },
        { "-lv", "languageVersion" },
        { "-lr", "targetRuntime" },
        { "-m", "mode" },
        { "-s", "script" },
        { "-t", "postamble" }
    }))
    .ConfigureServices((context, services) =>
    {
        services.Configure<CommandLineOptions>(context.Configuration);
        services.AddHeadlessService();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var options = services.GetRequiredService<IOptions<CommandLineOptions>>().Value;
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
    Console.WriteLine("Failed to receive script from caller!");
    return;
    
}

var stopwatch = Stopwatch.StartNew();
var compileResult = await services.GetRequiredKeyedService<IScriptCompiler>(options.TargetKey).Compile(script.ToString());
var timeSpentCompilingMs = stopwatch.ElapsedTicks;
if (!compileResult.IsSuccess)
{
    Console.Error.WriteLine(compileResult.Messages);
    return;
}

stopwatch.Restart();
var invokeResult = await services.GetRequiredKeyedService<IScriptInvoker>(options.TargetKey).Run<object>(compileResult);
stopwatch.Stop();
if (!compileResult.IsSuccess)
{
    Console.Error.WriteLine(invokeResult.Messages);
    return;
}

Console.Out.WriteLine($"{Environment.NewLine}{string.Join("", Enumerable.Repeat('-', 13))}OUTPUT{string.Join("", Enumerable.Repeat('-', 13))}{Environment.NewLine}");
Console.Out.WriteLine($"COMPILED IN: {TimeSpan.FromTicks(timeSpentCompilingMs).TotalSeconds:N4}s"); 
Console.Out.WriteLine($"EXECUTED IN: {TimeSpan.FromTicks(stopwatch.ElapsedTicks).TotalSeconds:N4}s{Environment.NewLine}");
Console.Out.WriteLine($"RESULT VALUE: {invokeResult.Result}");