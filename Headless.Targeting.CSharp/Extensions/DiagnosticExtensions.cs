namespace Headless.Targeting.CSharp.Extensions;

internal static class DiagnosticExtensions
{
    public static bool IsError(this Diagnostic item) => item.Severity == DiagnosticSeverity.Error;
    public static bool IsNotError(this Diagnostic item) => item.Severity != DiagnosticSeverity.Error;

    public static IEnumerable<Diagnostic> Errors(this IEnumerable<Diagnostic> results) => results.Where(IsError);
    public static bool Success(this IEnumerable<Diagnostic> results) => results.Errors().ToArray().Length == 0;
    public static bool Fail(this IEnumerable<Diagnostic> results) => !results.Success();

#if NET
    public static string Summarise(this ImmutableArray<Diagnostic> results) => new StringBuilder().AppendJoin(Environment.NewLine, results).ToString();
#else
    public static string Summarise(this ImmutableArray<Diagnostic> results) => new StringBuilder().Append(string.Join(Environment.NewLine, results)).ToString();
#endif
}