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

        if (args.Length == 0 || args[0].StartsWith("--"))
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

        var serviceName = args[0];

        try
        {
            using var sc = new ServiceController(serviceName);

            if (!ServiceDiscovery.IsSqlServerService(sc.ServiceName))
            {
                ConsoleEx.WriteErrorLine($"'{serviceName}' is not a SQL Server service.");
                return 1;
            }

            var info = new SqlServiceInfo
            {
                ServiceName = sc.ServiceName,
                DisplayName = sc.DisplayName,
                Status = sc.Status switch
                {
                    ServiceControllerStatus.Running => "Running",
                    ServiceControllerStatus.Stopped => "Stopped",
                    ServiceControllerStatus.Paused => "Paused",
                    ServiceControllerStatus.StartPending => "StartPending",
                    ServiceControllerStatus.StopPending => "StopPending",
                    _ => "Unknown"
                },
                StartupType = sc.StartType switch
                {
                    ServiceStartMode.Automatic => "Automatic",
                    ServiceStartMode.Manual => "Manual",
                    ServiceStartMode.Disabled => "Disabled",
                    ServiceStartMode.Boot => "Boot",
                    ServiceStartMode.System => "System",
                    _ => "Unknown"
                }
            };

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
