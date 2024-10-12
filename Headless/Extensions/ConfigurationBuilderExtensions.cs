namespace Headless.Extensions;

internal static class ConfigurationBuilderExtensions
{
    public static void AddHeadlessCommandLineMappings(this IConfigurationBuilder builder)
    {
        // Some arguments' values can be assumed if no corresponding value was provided in the command line. For example: `--js-strict` is an optional boolean. It can be declared as `--js-strict True` or `--js-strict False`
        // However because it is a bool, rather than explicitly specifying the value in the args, we can assume the value based on whether the argument itself was specified - making the overall arguments string more concise.
        // More arguments may be added going forward with similar behavior; where the argument value is either unnecessary or redundant.
        // HACK: Unfortunately the `Microsoft.Extensions.Configuration.CommandLine` package doesn't natively support the aforementioned behavior, so I've added this hack. Seems to work 👍
        var args = Environment.GetCommandLineArgs().SelectMany((arg, i) =>
        {
            return arg switch
            {
                "--js-strict" when i + 1 == Environment.GetCommandLineArgs().Length || !bool.TryParse(Environment.GetCommandLineArgs()[i + 1], out _)  => ["--js-strict", "True"],
                _ => new [] { arg }
            };
        }).ToArray();

        builder.AddCommandLine(args, new Dictionary<string, string>
        {
            { "-i", "inputMode" },
            { "-s", "script" },
            { "-m", "runMode" },
            { "-t", "postamble" },
            { "-l", "language" },
            { "-lv", "languageVersion" },
            { "-lr", "targetRuntime" },
            { "--cs-file-name", "CSharpScriptInterpreter:FileName" },
            { "--cs-impl-scheme", "CSharpScriptInterpreter:ImplementationScheme" },
            { "--js-strict", "JavaScriptInterpreter:StrictMode" }
        });
    }
}
