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

        var controllers = BuildControllers(serviceNames.Select(s => s.ServiceName).ToList());

        // Topological sort: services that others depend on come last in stop order
        // Without this, ServiceController.Stop() throws InvalidOperationException
        // when a dependent service (e.g. SQLSERVERAGENT) is still running.
        var ordered = TopologicalSort(controllers);
        ordered.Reverse(); // reverse start order → stop order (dependents first)

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

    private static Dictionary<string, ServiceController> BuildControllers(List<string> names)
    {
        var controllers = new Dictionary<string, ServiceController>(names.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            try
            {
                var sc = new ServiceController(name);
                _ = sc.Status;
                controllers[name] = sc;
            }
            catch (InvalidOperationException)
            {
                ConsoleEx.WriteErrorLine($"Service '{name}' was not found. Skipping.");
            }
        }

        return controllers;
    }

    private static List<ServiceController> TopologicalSort(Dictionary<string, ServiceController> controllers)
    {
        var adj = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in controllers.Keys)
        {
            adj[name] = [];
            inDegree[name] = 0;
        }

        foreach (var (name, sc) in controllers)
        {
            foreach (var dep in sc.ServicesDependedOn)
            {
                if (controllers.ContainsKey(dep.ServiceName))
                {
                    adj[dep.ServiceName].Add(name);
                    inDegree[name]++;
                }
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var sorted = new List<string>(controllers.Count);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            sorted.Add(node);

            foreach (var neighbor in adj.GetValueOrDefault(node, []))
            {
                if (--inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        foreach (var (name, degree) in inDegree)
        {
            if (degree > 0)
                sorted.Add(name);
        }

        return sorted.Select(n => controllers[n]).ToList();
    }
}
