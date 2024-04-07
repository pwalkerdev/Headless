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
