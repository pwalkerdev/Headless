var host = Host.CreateDefaultBuilder().ConfigureAppConfiguration(config =>
{
    // some arguments' values can be assumed if no corresponding value was provided in the command line. For example: the `--js-strict` argument is a boolean. It can be defined as `--js-strict True` or `--js-strict False`
    // However, because it is just a boolean, rather than explicitly specifying the value in the command line, we can assume the value based on whether the argument itself was defined - making the command line more concise.
    // Other arguments may in future be implemented that are used in a similar `switch` type manner, where the argument value is unnecessary or redundant. Unfortunately though, the Microsoft.Extensions.Configuration.CommandLine
    // NuGet package doesn't natively support this behavior, so we have to hack it in here. This code will be moved to a more suitable home at a later date.
    args = args.SelectMany((arg, i) =>
    {
        return arg switch
        {
            "--js-strict" when i + 1 == args.Length || !bool.TryParse(args[i + 1], out _)  => ["--js-strict", "True"],
            _ => new [] { arg }
        };
    }).ToArray();

    config.AddCommandLine(args, new Dictionary<string, string>
    {
        { "-l", "language" },
        { "-lv", "languageVersion" },
        { "-lr", "targetRuntime" },
        { "-m", "mode" },
        { "-s", "script" },
        { "-t", "postamble" },
        { "--js-strict", "JavaScriptInterpreter:StrictMode" }
    });
}).ConfigureServices((context, services) =>
{
    services.Configure<CommandLineOptions>(context.Configuration);
    services.Configure<JavaScriptInterpreterOptions>(context.Configuration);
    services.AddSingleton<CommandLineOptions>(provider => provider.GetRequiredService<IOptions<CommandLineOptions>>().Value);
    services.AddSingleton<JavaScriptInterpreterOptions>(provider => provider.GetRequiredService<IOptions<CommandLineOptions>>().Value.JavaScriptInterpreter);
    services.AddHeadlessService();
}).Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;
var options = services.GetRequiredService<CommandLineOptions>();
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
    Console.Error.WriteLine($"{Environment.NewLine}{compileResult.Messages}");
    return;
}

stopwatch.Restart();
var invokeResult = await services.GetRequiredKeyedService<IScriptInvoker>(options.TargetKey).Run<object>(compileResult);
stopwatch.Stop();

Console.Out.WriteLine($"{Environment.NewLine}-------------OUTPUT-------------{Environment.NewLine}");

if (invokeResult.IsSuccess)
{
    Console.Out.WriteLine($"COMPILED IN: {TimeSpan.FromTicks(timeSpentCompilingMs).TotalSeconds:N4}s"); 
    Console.Out.WriteLine($"EXECUTED IN: {TimeSpan.FromTicks(stopwatch.ElapsedTicks).TotalSeconds:N4}s{Environment.NewLine}");
    Console.Out.WriteLine($"RESULT VALUE: {invokeResult.Result}");
    
}
else
    Console.Error.WriteLine(invokeResult.Messages);

Console.Out.WriteLine($"{Environment.NewLine}-------------FINISH-------------");