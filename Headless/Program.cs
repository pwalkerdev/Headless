var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(config => config.AddCommandLine(args, new Dictionary<string, string>
    {
        { "-m", "mode" },
        { "-s", "script" },
        { "-t", "postamble" }
    }))
    .ConfigureServices((context, services) =>
    {
        services.Configure<CommandLineOptions>(context.Configuration);
    })
    .Build();

var services = host.Services;
var options = services.GetRequiredService<IOptions<CommandLineOptions>>().Value;
var script = options.Mode == ScriptInputMode.Stream ? new StringBuilder() : new StringBuilder(options.Script);

while (options.Mode == ScriptInputMode.Stream && Console.ReadLine() is { } ln && ln != options.Postamble)
    script.AppendLine(ln);

if (script.Length > 0)
{
    var headless = new Headless.Core.HeadlessService();

    var compileResult = await headless.ResolveCompiler("CSharp").Compile(script.ToString());
    var consoleWriter = compileResult.IsSuccess ? Console.Out : Console.Error;
    consoleWriter.WriteLine(compileResult.Messages);
    if (compileResult.IsSuccess)
    {
        var invokeResult = await headless.ResolveInvoker("CSharp").Run<object>(compileResult);
        consoleWriter = invokeResult.IsSuccess ? Console.Out : Console.Error;
        consoleWriter.WriteLine(invokeResult.Messages);
    }
}
else
    Console.WriteLine("Failed to receive script from caller!");
