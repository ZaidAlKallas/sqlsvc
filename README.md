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
| `list [service]` | List all or a specific SQL Server service |
| `start <service...> [--enable]` | Start one or more services |
| `stop <service...>` | Stop one or more services |
| `start-all [--enable]` | Start all SQL Server services |
| `stop-all` | Stop all SQL Server services |
| `startup <auto\|manual\|disabled> (<service...> \| --all)` | Set startup type for one or all services |

### Command Options

| Option | Applies to | Description |
|---|---|---|
| `--enable` | `start`, `start-all` | Automatically enable disabled services before starting |
| `--timeout <sec>` | `start`, `stop`, `start-all`, `stop-all` | Operation timeout in seconds (default: 30) |

### Global Options

| Option | Description |
|---|---|
| `--json` | Output in JSON format |
| `--csv` | Output in CSV format |
| `--help`, `-h` | Show help |
| `--version` | Show version |

### Output

- Table, JSON, and CSV output (use `--json` or `--csv` with list commands)
- Status values are color-coded in table output: **Running** (green), **Stopped** (yellow), **Disabled** (red)
- Success, warning, and error messages use green, yellow, and red respectively

### Examples

```powershell
# List all SQL Server services
sqlsvc list

# List services as JSON
sqlsvc list --json

# Show a specific service
sqlsvc list MSSQLSERVER

# Start a service (requires administrator)
sqlsvc start MSSQLSERVER

# Start multiple services (requires administrator)
sqlsvc start SQLSERVERAGENT SQLBrowser SQLWriter

# Start a disabled service (automatically enables it first)
sqlsvc start MSSQLSERVER --enable

# Start all SQL Server services
sqlsvc start-all

# Start all services, automatically enabling disabled ones
sqlsvc start-all --enable

# Stop services (requires administrator)
sqlsvc stop SQLSERVERAGENT MSSQLSERVER

# Stop all SQL Server services (dependency-aware)
sqlsvc stop-all

# Configure a service to start automatically (requires administrator)
sqlsvc startup auto MSSQLSERVER

# Configure multiple services to manual start
sqlsvc startup manual SQLSERVERAGENT SQLBrowser

# Disable a service
sqlsvc startup disabled SQLSERVERAGENT

# Apply a startup type to all SQL Server services
sqlsvc startup auto --all

# Use a custom timeout for long-running operations
sqlsvc stop-all --timeout 60
```

## Requirements

- Windows 10/11
- .NET 10 SDK or runtime
- Administrator privileges for `start`, `stop`, `startup`, `start-all`, and `stop-all` commands

## Building from Source

```powershell
dotnet build
```

## Testing

```powershell
dotnet test
```

## Packaging

```powershell
dotnet pack
dotnet tool install --global --add-source ./nupkg sqlsvc
```

## License

MIT
