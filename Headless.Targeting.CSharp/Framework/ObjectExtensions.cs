namespace Headless.Targeting.CSharp.Framework;

public static class ObjectExtensions
{
    public static void Dump(this object? value)
    {
        var str = value?.ToString() ?? "(null)";
        Console.WriteLine("|> " + string.Join("\r\n--", str.Split(["\r\n", "\n"], StringSplitOptions.None)));
    }
}