using System.ComponentModel;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class StartupCommand
{
    public static int Execute(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: sqlsvc startup <service> <auto|manual|disabled>");
            return 1;
        }

        var serviceName = args[0];
        var startupType = args[1].ToLowerInvariant();

        if (startupType is not ("auto" or "automatic" or "manual" or "disabled"))
        {
            Console.Error.WriteLine("Startup type must be auto, manual, or disabled.");
            return 1;
        }

        try
        {
            using var sc = ServiceManager.GetService(serviceName);

            if (!ServiceDiscovery.IsSqlServerService(sc.ServiceName))
            {
                Console.Error.WriteLine($"'{serviceName}' is not a SQL Server service.");
                return 1;
            }

            ServiceManager.WarnIfNotAdministrator();
            ServiceManager.ChangeStartupType(sc, startupType);
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
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
