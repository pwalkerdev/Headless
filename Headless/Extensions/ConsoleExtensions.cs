namespace Headless.Extensions;

internal static class ConsoleExtensions
{
    public static void BlankOutLine(int index, int width)
    {
        var startingPosition = (Console.CursorLeft, Console.CursorTop);
        Console.SetCursorPosition(0, index);
        Console.Write(new string(' ', width)); 
        Console.SetCursorPosition(startingPosition.CursorLeft, startingPosition.CursorTop);
    }

    public static void BlankOutLastLine(int? width = null)
    {
        if (!Console.IsInputRedirected)
            BlankOutLine(Console.CursorTop - 1, width ?? Console.BufferWidth);
    }

    public static void ShiftCursorUp(int linesToMoveUp)
    {
        if (!Console.IsInputRedirected)
            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - linesToMoveUp);
    }
}