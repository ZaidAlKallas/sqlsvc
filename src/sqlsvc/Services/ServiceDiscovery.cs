using System.ServiceProcess;
using sqlsvc.Models;

namespace sqlsvc.Services;

internal static class ServiceDiscovery
{
    public static List<SqlServiceInfo> GetSqlServices()
    {
        return ServiceController.GetServices()
            .Where(s => IsSqlServerService(s.ServiceName))
            .Select(SqlServiceInfo.FromController)
            .OrderBy(s => s.ServiceName)
            .ToList();
    }

    internal static bool IsSqlServerService(string serviceName)
    {
        return serviceName.StartsWith("MSSQL", StringComparison.OrdinalIgnoreCase)
            || serviceName.StartsWith("SQLSERVERAGENT", StringComparison.OrdinalIgnoreCase)
            || serviceName.StartsWith("SQLAGENT$", StringComparison.OrdinalIgnoreCase)
            || serviceName.Equals("SQLBrowser", StringComparison.OrdinalIgnoreCase)
            || serviceName.Equals("SQLWriter", StringComparison.OrdinalIgnoreCase)
            || serviceName.StartsWith("ReportServer", StringComparison.OrdinalIgnoreCase)
            || serviceName.StartsWith("MsDtsServer", StringComparison.OrdinalIgnoreCase)
            || serviceName.StartsWith("SQLTELEMETRY", StringComparison.OrdinalIgnoreCase)
            || serviceName.StartsWith("MSOLAP", StringComparison.OrdinalIgnoreCase);
    }
}
