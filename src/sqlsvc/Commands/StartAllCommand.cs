using System.ComponentModel;
using System.ServiceProcess;
using sqlsvc.Helpers;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class StartAllCommand
{
    public static int Execute(string[] args)
    {
        var (timeout, enable) = ParseArgs(args);
        var services = ServiceDiscovery.GetSqlServices();

        if (services.Count == 0)
        {
            ConsoleEx.WriteWarningLine("No SQL Server services found.");
            return 0;
        }

        ServiceManager.WarnIfNotAdministrator();
        var hasError = false;

        var controllers = ServiceManager.BuildControllers(services.Select(s => s.ServiceName));
        var ordered = ServiceManager.OrderForStart(controllers);

        foreach (var sc in ordered)
        {
            try
            {
                using (sc)
                {
                    if (sc.StartType == ServiceStartMode.Disabled)
                    {
                        if (!enable)
                        {
                            ConsoleEx.WriteErrorLine($"Service '{sc.ServiceName}' is disabled. Use --enable to automatically enable it before starting.");
                            hasError = true;
                            continue;
                        }

                        if (!ServiceManager.TryChangeStartupType(sc, "manual"))
                        {
                            ConsoleEx.WriteErrorLine($"Failed to enable service '{sc.ServiceName}'.");
                            hasError = true;
                            continue;
                        }
                    }

                    ServiceManager.Start(sc, timeout);
                }
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
        }

        return hasError ? 1 : 0;
    }

    private static (int timeout, bool enable) ParseArgs(string[] args)
    {
        var timeout = ServiceManager.DefaultTimeoutSeconds;
        var enable = false;

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
            else if (string.Equals(args[i], "--enable", StringComparison.OrdinalIgnoreCase))
            {
                enable = true;
            }
        }

        return (timeout, enable);
    }
}
