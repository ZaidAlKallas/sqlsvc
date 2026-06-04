using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using sqlsvc.Helpers;

namespace sqlsvc.Services;

internal static class ServiceManager
{
    public const int DefaultTimeoutSeconds = 30;

    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void WarnIfNotAdministrator()
    {
        if (!IsAdministrator())
        {
            ConsoleEx.WriteWarningLine("Warning: Not running as administrator. This command may fail.");
        }
    }

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
            ConsoleEx.WriteWarningLine($"Service '{sc.ServiceName}' is already running.");
            return;
        }

        Console.Write($"Starting '{sc.ServiceName}'... ");
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(timeoutSeconds));
        ConsoleEx.WriteSuccessLine("Started.");
    }

    public static void Stop(ServiceController sc, int timeoutSeconds)
    {
        if (sc.Status == ServiceControllerStatus.Stopped)
        {
            ConsoleEx.WriteWarningLine($"Service '{sc.ServiceName}' is already stopped.");
            return;
        }

        Console.Write($"Stopping '{sc.ServiceName}'... ");
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(timeoutSeconds));
        ConsoleEx.WriteSuccessLine("Stopped.");
    }

    public static bool TryChangeStartupType(ServiceController sc, string startupType)
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
            ConsoleEx.WriteErrorLine("Failed to start sc.exe.");
            return false;
        }

        process.WaitForExit(30000);

        if (process.ExitCode == 0)
        {
            ConsoleEx.WriteSuccessLine("Done.");
            return true;
        }

        ConsoleEx.WriteErrorLine("Failed.");
        return false;
    }

    public static void ChangeStartupType(ServiceController sc, string startupType)
    {
        TryChangeStartupType(sc, startupType);
    }
}

internal sealed class ServiceNotFoundException(string serviceName) : Exception
{
    public string ServiceName { get; } = serviceName;
}
