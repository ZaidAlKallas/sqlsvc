using sqlsvc.Helpers;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class ListCommand
{
    public static int Execute(string[] args)
    {
        var format = OutputFormatter.ParseFormat(args);
        var services = ServiceDiscovery.GetSqlServices();

        if (services.Count == 0)
        {
            Console.WriteLine("No SQL Server services found.");
            return 0;
        }

        OutputFormatter.Print(services, format);
        return 0;
    }
}
