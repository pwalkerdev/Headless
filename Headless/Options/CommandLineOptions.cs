namespace Headless.Options;

public class CommandLineOptions
{
    public ScriptInputMode Mode { get; set; }
    public string Script { get; set; } = string.Empty;
    public string Postamble { get; set; } = string.Empty;
}