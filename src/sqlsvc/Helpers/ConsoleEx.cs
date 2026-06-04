namespace sqlsvc.Helpers;

internal static class ConsoleEx
{
    public static void WriteSuccessLine(string message) => WriteLine(message, ConsoleColor.Green, Console.Out);

    public static void WriteWarningLine(string message) => WriteLine(message, ConsoleColor.Yellow, Console.Error);

    public static void WriteErrorLine(string message) => WriteLine(message, ConsoleColor.Red, Console.Error);

    public static void WriteColored(string text, ConsoleColor color) => Write(text, color, Console.Out);

    private static void WriteLine(string message, ConsoleColor color, TextWriter writer)
    {
        if (!IsRedirected(writer))
        {
            var orig = Console.ForegroundColor;
            Console.ForegroundColor = color;
            writer.WriteLine(message);
            Console.ForegroundColor = orig;
        }
        else
        {
            writer.WriteLine(message);
        }
    }

    private static void Write(string text, ConsoleColor color, TextWriter writer)
    {
        if (!IsRedirected(writer))
        {
            var orig = Console.ForegroundColor;
            Console.ForegroundColor = color;
            writer.Write(text);
            Console.ForegroundColor = orig;
        }
        else
        {
            writer.Write(text);
        }
    }

    private static bool IsRedirected(TextWriter writer)
    {
        if (writer == Console.Out) return Console.IsOutputRedirected;
        if (writer == Console.Error) return Console.IsErrorRedirected;
        return false;
    }
}
