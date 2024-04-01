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
var script = options.Mode == ScriptInputMode.Stream ? new StringBuilder() : new StringBuilder(options.Script);

while (options.Mode == ScriptInputMode.Stream && Console.ReadLine() is { } ln && ln != options.Postamble)
    script.AppendLine(ln);

if (options.Mode == ScriptInputMode.Stream) // If the input mode was set to stream, the "finish" token will be written to the console at the bottom of the script. This will overwrite it
{
    ConsoleExtensions.BlankOutLastLine(options.Postamble.Length);
    ConsoleExtensions.ShiftCursorUp(1);
}

if (script.Length > 0)
{
    var compileResult = await services.GetRequiredKeyedService<IScriptCompiler>(options.TargetKey).Compile(script.ToString());
    var consoleWriter = compileResult.IsSuccess ? Console.Out : Console.Error;
    consoleWriter.WriteLine(compileResult.Messages);
    if (compileResult.IsSuccess)
    {
        var invokeResult = await services.GetRequiredKeyedService<IScriptInvoker>(options.TargetKey).Run<object>(compileResult);
        consoleWriter = invokeResult.IsSuccess ? Console.Out : Console.Error;
        consoleWriter.WriteLine(invokeResult.Messages);
    }
}
else
    Console.WriteLine("Failed to receive script from caller!");
