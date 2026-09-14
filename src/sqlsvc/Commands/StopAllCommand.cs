using System.ComponentModel;
using System.ServiceProcess;
using sqlsvc.Helpers;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class StopAllCommand
{
    public static int Execute(string[] args)
    {
        var timeout = ParseTimeout(args);
        var serviceNames = ServiceDiscovery.GetSqlServices();

        if (serviceNames.Count == 0)
        {
            ConsoleEx.WriteWarningLine("No SQL Server services found.");
            return 0;
        }

        ServiceManager.WarnIfNotAdministrator();
        var hasError = false;

        var controllers = ServiceManager.BuildControllers(serviceNames.Select(s => s.ServiceName));
        var ordered = ServiceManager.OrderForStop(controllers);

        foreach (var sc in ordered)
        {
            try
            {
                ServiceManager.Stop(sc, timeout);
            }
            catch (ServiceNotFoundException)
            {
                ConsoleEx.WriteErrorLine($"Service '{sc.ServiceName}' was not found.");
                hasError = true;
            }
            catch (Win32Exception)
            {
                ConsoleEx.WriteErrorLine("Access denied. Run as administrator.");
                return 1;
            }
            catch (System.TimeoutException)
            {
                ConsoleEx.WriteErrorLine($"Operation timed out after {timeout} seconds.");
                hasError = true;
            }
            finally
            {
                sc.Dispose();
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
