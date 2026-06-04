using System.Diagnostics;
using System.ServiceProcess;

namespace sqlsvc.Services;

internal static class ServiceManager
{
    public const int DefaultTimeoutSeconds = 30;

    public static ServiceController GetService(string name)
    {
        try
        {
            var sc = new ServiceController(name);
            _ = sc.Status;
            return sc;
        }
        catch (InvalidOperationException)
        {
            throw new ServiceNotFoundException(name);
        }
    }

    public static void Start(ServiceController sc, int timeoutSeconds)
    {
        if (sc.Status == ServiceControllerStatus.Running)
        {
            Console.WriteLine($"Service '{sc.ServiceName}' is already running.");
            return;
        }

        Console.Write($"Starting '{sc.ServiceName}'... ");
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSeconds));
        Console.WriteLine("Started.");
    }

    public static void Stop(ServiceController sc, int timeoutSeconds)
    {
        if (sc.Status == ServiceControllerStatus.Stopped)
        {
            Console.WriteLine($"Service '{sc.ServiceName}' is already stopped.");
            return;
        }

        Console.Write($"Stopping '{sc.ServiceName}'... ");
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSeconds));
        Console.WriteLine("Stopped.");
    }

    public static void ChangeStartupType(ServiceController sc, string startupType)
    {
        var scArg = startupType.ToLowerInvariant() switch
        {
            "auto" => "auto",
            "automatic" => "auto",
            "manual" => "demand",
            "disabled" => "disabled",
            _ => throw new ArgumentException($"Invalid startup type: '{startupType}'. Use auto, manual, or disabled.")
        };

        Console.Write($"Setting startup type of '{sc.ServiceName}' to {startupType}... ");

        var psi = new ProcessStartInfo("sc", $"config \"{sc.ServiceName}\" start={scArg}")
        {
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi);

        if (process is null)
        {
            Console.Error.WriteLine("Failed to start sc.exe.");
            return;
        }

        process.WaitForExit(30000);

        Console.WriteLine(process.ExitCode == 0 ? "Done." : "Failed.");
    }
}

internal sealed class ServiceNotFoundException(string serviceName) : Exception
{
    public string ServiceName { get; } = serviceName;
}
