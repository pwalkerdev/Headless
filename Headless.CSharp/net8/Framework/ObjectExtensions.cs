// ReSharper disable once CheckNamespace
namespace Headless.CSharp.Framework;

public static class ObjectExtensions
{
    public static void Dump(this object? value)
    {
        var str = value?.ToString() ?? "(null)";
        Console.WriteLine("|> " + string.Join("\r\n--", str.Split(new [] { "\r\n", "\n" }, StringSplitOptions.None)));
    }
}