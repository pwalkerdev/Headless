namespace Headless.Options;

public class CommandLineOptions
{
    public ScriptInputMode Mode { get; set; }
    public string Script { get; set; } = string.Empty;
    public string Postamble { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;
    public string LanguageVersion { get; set; } = "latest";
    public string RuntimeVersion { get; set; } = "any";
    public string TargetKey => $"{Language}.{LanguageVersion}.{RuntimeVersion}";
}