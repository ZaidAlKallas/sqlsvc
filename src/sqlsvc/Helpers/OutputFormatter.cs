using System.Text.Json;
using sqlsvc.Models;

namespace sqlsvc.Helpers;

internal enum OutputFormat
{
    Table,
    Json,
    Csv
}

internal static class OutputFormatter
{
    public static OutputFormat ParseFormat(string[] args)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase))
                return OutputFormat.Json;
            if (string.Equals(arg, "--csv", StringComparison.OrdinalIgnoreCase))
                return OutputFormat.Csv;
        }
        return OutputFormat.Table;
    }

    public static void Print(List<SqlServiceInfo> services, OutputFormat format)
    {
        switch (format)
        {
            case OutputFormat.Json:
                PrintJson(services);
                break;
            case OutputFormat.Csv:
                PrintCsv(services);
                break;
            default:
                PrintTable(services);
                break;
        }
    }

    public static void PrintTable(List<SqlServiceInfo> services)
    {
        var nameWidth = Math.Max(services.Max(s => s.ServiceName.Length) + 2, 14);
        var displayWidth = Math.Max(services.Max(s => s.DisplayName.Length) + 2, 14);
        var statusWidth = 12;
        var startupWidth = 12;

        var line = new string('-', nameWidth + displayWidth + statusWidth + startupWidth + 9);

        Console.WriteLine(line);
        Console.WriteLine(
            $"| {"ServiceName".PadRight(nameWidth - 1)}" +
            $"| {"DisplayName".PadRight(displayWidth - 1)}" +
            $"| {"Status".PadRight(statusWidth - 1)}" +
            $"| {"StartupType".PadRight(startupWidth - 1)}|");
        Console.WriteLine(line);

        foreach (var svc in services)
        {
            Console.WriteLine(
                $"| {svc.ServiceName.PadRight(nameWidth - 1)}" +
                $"| {svc.DisplayName.PadRight(displayWidth - 1)}" +
                $"| {svc.Status.PadRight(statusWidth - 1)}" +
                $"| {svc.StartupType.PadRight(startupWidth - 1)}|");
        }

        Console.WriteLine(line);
    }

    public static void PrintJson(List<SqlServiceInfo> services)
    {
        var json = JsonSerializer.Serialize(services, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
    }

    public static void PrintCsv(List<SqlServiceInfo> services)
    {
        Console.WriteLine("ServiceName,DisplayName,Status,StartupType");
        foreach (var svc in services)
        {
            Console.WriteLine($"\"{svc.ServiceName}\",\"{svc.DisplayName}\",\"{svc.Status}\",\"{svc.StartupType}\"");
        }
    }
}
