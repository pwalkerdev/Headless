namespace Headless.Targeting.CSharp.Extensions;

internal static class LanguageVersionExtensions
{
    public static bool TryResolveLanguageVersion(this string input, out LanguageVersion result) => Enum.TryParse(input switch
    {
        "7.1" => "701",
        "7.2" => "702",
        "7.3" => "703",
        "8" => "800",
        "9" => "900",
        "10" => "1000",
        "11" => "1100",
        "12" => "1200",

        _ => input
    }, true, out result);

    public static LanguageVersion ResolveLanguageVersion(this string input) =>
        TryResolveLanguageVersion(input, out var languageVersion)
            ? languageVersion
            : throw new InvalidOperationException($"Specified language version \"{input} is unrecognised or unsupported");
}