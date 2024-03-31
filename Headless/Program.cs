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

if (options.Mode == ScriptInputMode.Stream)
    ConsoleExtensions.BlankOutLastLine();

if (script.Length > 0)
{
    var compileResult = await services.GetRequiredKeyedService<IReadScripts>(options.TargetKey).Compile(script.ToString());
    var consoleWriter = compileResult.IsSuccess ? Console.Out : Console.Error;
    consoleWriter.WriteLine(compileResult.Messages);
    if (compileResult.IsSuccess)
    {
        var invokeResult = await services.GetRequiredKeyedService<IRunScripts>(options.TargetKey).Run<object>(compileResult);
        consoleWriter = invokeResult.IsSuccess ? Console.Out : Console.Error;
        consoleWriter.WriteLine(invokeResult.Messages);
    }
}
else
    Console.WriteLine("Failed to receive script from caller!");
