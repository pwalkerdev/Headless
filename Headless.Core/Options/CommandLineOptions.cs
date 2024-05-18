using Headless.Core.Attributes;

namespace Headless.Core.Options;

public class CommandLineOptions
{
    public ScriptInputMode InputMode { get; set; }
    public string Script { get; set; } = string.Empty;
    public RunMode RunMode { get; set; }
    public string Postamble { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;
    public string LanguageVersion { get; set; } = "latest";
    public string RuntimeVersion { get; set; } = "any";
    public string TargetKey => $"{Language}{SupportedTargetsAttribute.KeyDelimiter}{LanguageVersion}{SupportedTargetsAttribute.KeyDelimiter}{RuntimeVersion}".ToLower();

    public JavaScriptInterpreterOptions JavaScriptInterpreter { get; set; } = new();
}