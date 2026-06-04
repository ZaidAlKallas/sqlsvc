using System.ComponentModel;
using sqlsvc.Helpers;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class StopCommand
{
    public static int Execute(string[] args)
    {
        var (timeout, serviceNames) = ParseArgs(args);

        if (serviceNames.Count == 0)
        {
            ConsoleEx.WriteErrorLine("Usage: sqlsvc stop <service> [<service>...]");
            return 1;
        }

        ServiceManager.WarnIfNotAdministrator();
        var hasError = false;

        foreach (var name in serviceNames)
        {
            try
            {
                using var sc = ServiceManager.GetService(name);

                if (!ServiceDiscovery.IsSqlServerService(sc.ServiceName))
                {
                    ConsoleEx.WriteErrorLine($"'{name}' is not a SQL Server service.");
                    hasError = true;
                    continue;
                }

                ServiceManager.Stop(sc, timeout);
            }
            catch (ServiceNotFoundException)
            {
                ConsoleEx.WriteErrorLine($"Service '{name}' was not found.");
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

    private static (int timeout, List<string> services) ParseArgs(string[] args)
    {
        var timeout = ServiceManager.DefaultTimeoutSeconds;
        var services = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--timeout", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out var seconds) && seconds > 0)
                {
                    timeout = seconds;
                    i++;
                }
            }
            else if (!args[i].StartsWith("--"))
            {
                services.Add(args[i]);
            }
        }

        return (timeout, services);
    }
}
