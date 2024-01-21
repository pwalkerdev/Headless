namespace Headless.Extensions;

internal static class ConsoleExtensions
{
    public static void BlankOutLine(int index)
    {
        var startingPosition = (Console.CursorLeft, Console.CursorTop);
        Console.SetCursorPosition(0, index);
        Console.Write(new string(' ', Console.BufferWidth)); 
        Console.SetCursorPosition(startingPosition.CursorLeft, startingPosition.CursorTop);
    }

    public static void BlankOutLastLine() => BlankOutLine(Console.CursorTop - 1);
}