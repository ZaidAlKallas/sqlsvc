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
    public void StartupCommand_NewSyntax_TypeWithService_ReturnsError()
    {
        var result = StartupCommand.Execute(["manual", "NonexistentService"]);
        Assert.Equal(1, result); // service not found
    }

    [Fact]
    public void StartupCommand_NoServiceOrAll_ReturnsError()
    {
        var result = StartupCommand.Execute(["manual"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StartupCommand_AllAndServiceNames_ReturnsError()
    {
        var result = StartupCommand.Execute(["manual", "MSSQLSERVER", "--all"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StartCommand_WithEnableFlag_ReturnsErrorForMissingService()
    {
        var result = StartCommand.Execute(["NonexistentService", "--enable"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StartCommand_MultipleServiceNames_ParsesAll()
    {
        // Should not throw; will fail at ServiceController but parsing succeeds
        var result = StartCommand.Execute(["Svc1", "Svc2", "Svc3"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void StopCommand_MultipleServiceNames_ParsesAll()
    {
        var result = StopCommand.Execute(["Svc1", "Svc2", "--timeout", "15"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void RestartCommand_EmptyArgs_ReturnsError()
    {
        var result = RestartCommand.Execute([]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void RestartCommand_NoServiceName_ReturnsError()
    {
        var result = RestartCommand.Execute(["--timeout", "10"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void RestartCommand_MultipleServiceNames_ParsesAll()
    {
        var result = RestartCommand.Execute(["Svc1", "Svc2", "Svc3"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void RestartCommand_WithEnableFlag_ReturnsErrorForMissingService()
    {
        var result = RestartCommand.Execute(["NonexistentService", "--enable"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void ListCommand_EmptyArgs_ReturnsOk()
    {
        // No SQL services on CI → returns 0 with warning
        var result = ListCommand.Execute([]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ListCommand_SpecificService_NonExistent_ReturnsError()
    {
        var result = ListCommand.Execute(["NonexistentService"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void ListCommand_WithJsonFlag_StillParses()
    {
        var result = ListCommand.Execute(["--json"]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ListCommand_JsonFlagBeforeServiceName_TargetsService()
    {
        var result = ListCommand.Execute(["--json", "NonexistentService"]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void ListCommand_ServiceNameBeforeJsonFlag_TargetsService()
    {
        var result = ListCommand.Execute(["NonexistentService", "--json"]);
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
