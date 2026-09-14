using System.ServiceProcess;

namespace sqlsvc.Models;

internal sealed record SqlServiceInfo
{
    public required string ServiceName { get; init; }
    public required string DisplayName { get; init; }
    public required string Status { get; init; }
    public required string StartupType { get; init; }

    public static SqlServiceInfo FromController(ServiceController sc) => new()
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
}