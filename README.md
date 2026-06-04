# sqlsvc

A .NET CLI tool for managing local SQL Server services on Windows.

## Installation

```powershell
dotnet tool install --global sqlsvc
```

Or install locally:

```powershell
dotnet tool install sqlsvc
```

## Usage

```
sqlsvc <command> [options]
```

### Commands

| Command | Description |
|---|---|
| `list` | List all SQL Server services |
| `status [service]` | Show status of all or a specific service |
| `start <service>` | Start a service |
| `stop <service>` | Stop a service |
| `startup <service> <auto\|manual\|disabled>` | Set service startup type |

### Global Options

| Option | Description |
|---|---|
| `--json` | Output in JSON format |
| `--csv` | Output in CSV format |
| `--help`, `-h` | Show help |
| `--version` | Show version |

### Examples

```powershell
# List all SQL Server services
sqlsvc list

# List services as JSON
sqlsvc list --json

# Check status of a specific service
sqlsvc status MSSQLSERVER

# Start a service (requires administrator)
sqlsvc start MSSQLSERVER

# Stop a service (requires administrator)
sqlsvc stop MSSQLSERVER

# Configure service to start automatically (requires administrator)
sqlsvc startup MSSQLSERVER auto

# Configure service to manual start (requires administrator)
sqlsvc startup MSSQLSERVER manual

# Disable a service (requires administrator)
sqlsvc startup MSSQLSERVER disabled
```

## Requirements

- Windows 10/11
- .NET 10 SDK or runtime
- Administrator privileges for `start`, `stop`, and `startup` commands

## Building from Source

```powershell
dotnet build
```

## Packaging

```powershell
dotnet pack
dotnet tool install --global --add-source ./nupkg sqlsvc
```

## License

MIT
