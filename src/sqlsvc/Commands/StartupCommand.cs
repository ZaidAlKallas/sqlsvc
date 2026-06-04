using System.ComponentModel;
using sqlsvc.Helpers;
using sqlsvc.Services;

namespace sqlsvc.Commands;

internal static class StartupCommand
{
    public static int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            ConsoleEx.WriteErrorLine("Usage: sqlsvc startup <auto|manual|disabled> (<service> [<service>...] | --all)");
            return 1;
        }

        var startupType = args[0].ToLowerInvariant();

        if (startupType is not ("auto" or "automatic" or "manual" or "disabled"))
        {
            ConsoleEx.WriteErrorLine("Startup type must be auto, manual, or disabled.");
            return 1;
        }

        var canonicalType = startupType switch
        {
            "auto" or "automatic" => "auto",
            "manual" => "manual",
            "disabled" => "disabled",
            _ => throw new InvalidOperationException("Unreachable")
        };

        var remaining = args[1..];
        var hasAll = remaining.Any(a => string.Equals(a, "--all", StringComparison.OrdinalIgnoreCase));
        var serviceNames = remaining.Where(a => !a.StartsWith("--")).ToList();

        if (hasAll && serviceNames.Count > 0)
        {
            ConsoleEx.WriteErrorLine("Cannot use --all with specific service names.");
            return 1;
        }

        List<string> targets;

        if (hasAll)
        {
            var all = ServiceDiscovery.GetSqlServices();

            if (all.Count == 0)
            {
                ConsoleEx.WriteWarningLine("No SQL Server services found.");
                return 0;
            }

            targets = all.Select(s => s.ServiceName).ToList();
        }
        else if (serviceNames.Count > 0)
        {
            targets = serviceNames;
        }
        else
        {
            ConsoleEx.WriteErrorLine("Specify at least one service name or use --all.");
            return 1;
        }

        ServiceManager.WarnIfNotAdministrator();
        var hasError = false;

        foreach (var name in targets)
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

                ServiceManager.ChangeStartupType(sc, canonicalType);
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
            catch (ArgumentException ex)
            {
                ConsoleEx.WriteErrorLine(ex.Message);
                hasError = true;
            }
        }

        return hasError ? 1 : 0;
    }
}
