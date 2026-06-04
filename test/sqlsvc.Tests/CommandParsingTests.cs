using sqlsvc.Commands;
using sqlsvc.Helpers;

namespace sqlsvc.Tests;

public class CommandParsingTests
{
    [Fact]
    public void StartCommand_EmptyArgs_ReturnsError()
    {
        var result = StartCommand.Execute([]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StartCommand_NoServiceName_ReturnsError()
    {
        var result = StartCommand.Execute(["--timeout", "10"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StopCommand_EmptyArgs_ReturnsError()
    {
        var result = StopCommand.Execute([]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StopCommand_NoServiceName_ReturnsError()
    {
        var result = StopCommand.Execute(["--timeout", "10"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StartupCommand_MissingArgs_ReturnsError()
    {
        var result = StartupCommand.Execute([]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StartupCommand_MissingType_ReturnsError()
    {
        var result = StartupCommand.Execute(["MSSQLSERVER"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StartupCommand_InvalidType_ReturnsError()
    {
        var result = StartupCommand.Execute(["MSSQLSERVER", "invalid"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void ParseFormat_Default_ReturnsTable()
    {
        var result = OutputFormatter.ParseFormat([]);
        Assert.Equal(OutputFormat.Table, result);
    }

    [Fact]
    public void ParseFormat_JsonFlag_ReturnsJson()
    {
        var result = OutputFormatter.ParseFormat(["--json"]);
        Assert.Equal(OutputFormat.Json, result);
    }

    [Fact]
    public void ParseFormat_CsvFlag_ReturnsCsv()
    {
        var result = OutputFormatter.ParseFormat(["--csv"]);
        Assert.Equal(OutputFormat.Csv, result);
    }

    [Fact]
    public void ParseFormat_JsonAmongArgs_ReturnsJson()
    {
        var result = OutputFormatter.ParseFormat(["MSSQLSERVER", "--json"]);
        Assert.Equal(OutputFormat.Json, result);
    }
}
