using System.Text.Json;
using sqlsvc.Models;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class ListCommand
{
    public static int Execute(string[] args)
    {
        var format = ParseFormat(args);

        var services = ServiceDiscovery.GetSqlServices();

        if (services.Count == 0)
        {
            Console.WriteLine("No SQL Server services found.");
            return 0;
        }

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

        return 0;
    }

    private static OutputFormat ParseFormat(string[] args)
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

    private static void PrintTable(List<SqlServiceInfo> services)
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

    private static void PrintJson(List<SqlServiceInfo> services)
    {
        var json = JsonSerializer.Serialize(services, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
    }

    private static void PrintCsv(List<SqlServiceInfo> services)
    {
        Console.WriteLine("ServiceName,DisplayName,Status,StartupType");
        foreach (var svc in services)
        {
            Console.WriteLine($"\"{svc.ServiceName}\",\"{svc.DisplayName}\",\"{svc.Status}\",\"{svc.StartupType}\"");
        }
    }

    private enum OutputFormat
    {
        Table,
        Json,
        Csv
    }
}
