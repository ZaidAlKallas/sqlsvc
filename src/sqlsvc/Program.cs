using sqlsvc.Commands;

var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "";

switch (cmd)
{
    case "list":
        return ListCommand.Execute(args[1..]);

    case "start":
        return StartCommand.Execute(args[1..]);

    case "stop":
        return StopCommand.Execute(args[1..]);

    case "start-all":
        return StartAllCommand.Execute(args[1..]);

    case "stop-all":
        return StopAllCommand.Execute(args[1..]);

    case "startup":
        return StartupCommand.Execute(args[1..]);

    case "--help" or "-h" or "":
        PrintUsage();
        return 0;

    case "--version":
        var version = typeof(Program).Assembly.GetName().Version;
        Console.WriteLine(version is not null ? version.ToString(3) : "0.1.0");
        return 0;

    default:
        await Console.Error.WriteLineAsync($"Unknown command: {args[0]}");
        PrintUsage();
        return 1;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Usage: sqlsvc <command> [options]

        Commands:
          list [service]                        List all or a specific SQL Server service
          start <service> [<service>...]        Start one or more services
          stop  <service> [<service>...]        Stop one or more services
          start-all                             Start all SQL Server services
          stop-all                              Stop all SQL Server services
          startup <auto|manual|disabled>        Set startup type
                 (<service> [<service>...] | --all)

        Command options:
          --enable         Automatically enable disabled services before starting (start, start-all)
          --timeout <sec>  Operation timeout in seconds (default: 30)

        Global options:
          --json            Output in JSON format
          --csv             Output in CSV format
          --help, -h        Show this help
          --version         Show version
        """);
}
