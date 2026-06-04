using System.ComponentModel;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class StopCommand
{
    public static int Execute(string[] args)
    {
        if (args.Length == 0 || args[0].StartsWith("--"))
        {
            Console.Error.WriteLine("Usage: sqlsvc stop <service>");
            return 1;
        }

        var serviceName = args[0];
        var timeout = DefaultTimeout(args);

        try
        {
            using var sc = ServiceManager.GetService(serviceName);

            if (!ServiceDiscovery.IsSqlServerService(sc.ServiceName))
            {
                Console.Error.WriteLine($"'{serviceName}' is not a SQL Server service.");
                return 1;
            }

            ServiceManager.Stop(sc, timeout);
            return 0;
        }
        catch (ServiceNotFoundException)
        {
            Console.Error.WriteLine($"Service '{serviceName}' was not found.");
            return 1;
        }
        catch (Win32Exception)
        {
            Console.Error.WriteLine("Access denied. Run as administrator.");
            return 1;
        }
        catch (System.TimeoutException)
        {
            Console.Error.WriteLine($"Operation timed out after {timeout} seconds.");
            return 1;
        }
    }

    private static int DefaultTimeout(string[] args)
    {
        for (int i = 1; i < args.Length; i++)
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
