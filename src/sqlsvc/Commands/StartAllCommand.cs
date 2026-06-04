using System.ComponentModel;
using sqlsvc.Helpers;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class StartAllCommand
{
    public static int Execute(string[] args)
    {
        var timeout = ParseTimeout(args);
        var services = ServiceDiscovery.GetSqlServices();

        if (services.Count == 0)
        {
            ConsoleEx.WriteWarningLine("No SQL Server services found.");
            return 0;
        }

        ServiceManager.WarnIfNotAdministrator();
        var hasError = false;

        foreach (var svc in services)
        {
            try
            {
                using var sc = ServiceManager.GetService(svc.ServiceName);
                ServiceManager.Start(sc, timeout);
            }
            catch (ServiceNotFoundException)
            {
                ConsoleEx.WriteErrorLine($"Service '{svc.ServiceName}' was not found.");
                hasError = true;
            }
            catch (Win32Exception)
            {
                ConsoleEx.WriteErrorLine("Access denied. Run as administrator.");
                return 1;
            }
            catch (TimeoutException)
            {
                ConsoleEx.WriteErrorLine($"Operation timed out after {timeout} seconds.");
                hasError = true;
            }
        }

        return hasError ? 1 : 0;
    }

    private static int ParseTimeout(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--timeout", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out var seconds) && seconds > 0)
                    return seconds;
            }
        }

        return ServiceManager.DefaultTimeoutSeconds;
    }
}
