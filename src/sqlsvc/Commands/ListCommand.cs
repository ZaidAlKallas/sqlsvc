using System.ServiceProcess;
using sqlsvc.Helpers;
using sqlsvc.Models;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class ListCommand
{
    public static int Execute(string[] args)
    {
        var format = OutputFormatter.ParseFormat(args);
        var serviceName = args.FirstOrDefault(a => !a.StartsWith("--"));

        if (serviceName is null)
        {
            var services = ServiceDiscovery.GetSqlServices();

            if (services.Count == 0)
            {
                ConsoleEx.WriteWarningLine("No SQL Server services found.");
                return 0;
            }

            OutputFormatter.Print(services, format);
            return 0;
        }

        try
        {
            using var sc = new ServiceController(serviceName);

            if (!ServiceDiscovery.IsSqlServerService(sc.ServiceName))
            {
                ConsoleEx.WriteErrorLine($"'{serviceName}' is not a SQL Server service.");
                return 1;
            }

            var info = SqlServiceInfo.FromController(sc);

            OutputFormatter.Print(new List<SqlServiceInfo> { info }, format);
            return 0;
        }
        catch (InvalidOperationException)
        {
            ConsoleEx.WriteErrorLine($"Service '{serviceName}' was not found.");
            return 1;
        }
        catch (ArgumentException)
        {
            ConsoleEx.WriteErrorLine($"Service '{serviceName}' was not found.");
            return 1;
        }
    }
}
