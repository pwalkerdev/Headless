using Headless.Core.Attributes;

namespace Headless.Core.Options;

public class CommandLineOptions
{
    public ScriptInputMode InputMode { get; set; }
    public string Script { get; set; } = string.Empty;
    public RunMode RunMode { get; set; }
    public string Postamble { get; set; } = string.Empty;
    // TODO: add command line option for compiler result verbosity, ie.  hints, warnings, errors, etc.

    public string Language { get; set; } = string.Empty;
    public string LanguageVersion { get; set; } = "latest";
    public string RuntimeVersion { get; set; } = "any";
    public string TargetKey => $"{Language}{SupportedTargetsAttribute.KeyDelimiter}{LanguageVersion}{SupportedTargetsAttribute.KeyDelimiter}{RuntimeVersion}".ToLower();

    // TODO: These should not live in this project. Because they are target specific, they should be their corresponding target's project
    public CSharpScriptInterpreterOptions CSharpScriptInterpreter { get; set; } = new();
    public JavaScriptInterpreterOptions JavaScriptInterpreter { get; set; } = new();
}