using sqlsvc.Commands;

var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "";

switch (cmd)
{
    case "list":
        return ListCommand.Execute(args[1..]);

    case "status":
        return StatusCommand.Execute(args[1..]);

    case "--help" or "-h" or "":
        PrintUsage();
        return 0;

    case "--version":
        var version = typeof(Program).Assembly.GetName().Version;
        Console.WriteLine(version is not null ? version.ToString(3) : "0.1.0");
        return 0;

    default:
        Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintUsage();
        return 1;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Usage: sqlsvc <command> [options]

        Commands:
          list              List all SQL Server services
          status [service]  Show service status
          start <service>   Start a service
          stop <service>    Stop a service
          startup <service> <auto|manual|disabled>  Set startup type

        Global options:
          --json            Output in JSON format
          --csv             Output in CSV format
          --help, -h        Show this help
          --version         Show version
        """);
}
