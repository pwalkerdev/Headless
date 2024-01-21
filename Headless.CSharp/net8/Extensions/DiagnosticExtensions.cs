#if NET8_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace Headless.CSharp.Extensions;

internal static class DiagnosticExtensions
{
    public static bool IsError(this Diagnostic item) => item.Severity == DiagnosticSeverity.Error;
    public static bool IsNotError(this Diagnostic item) => item.Severity != DiagnosticSeverity.Error;

    public static IEnumerable<Diagnostic> Errors(this IEnumerable<Diagnostic> results) => results.Where(DiagnosticExtensions.IsError);
    public static bool Success(this IEnumerable<Diagnostic> results) => results.Errors().ToArray().Length == 0;
    public static bool Fail(this IEnumerable<Diagnostic> results) => !results.Success();

    public static string Summarise(this ImmutableArray<Diagnostic> results) => new System.Text.StringBuilder(results.Length).AppendJoin(Environment.NewLine, results).ToString();
}
#endif