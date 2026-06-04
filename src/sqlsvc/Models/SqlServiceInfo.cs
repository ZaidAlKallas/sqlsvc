namespace sqlsvc.Models;

internal sealed record SqlServiceInfo
{
    public required string ServiceName { get; init; }
    public required string DisplayName { get; init; }
    public required string Status { get; init; }
    public required string StartupType { get; init; }
}
