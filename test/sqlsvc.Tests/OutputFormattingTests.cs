using sqlsvc.Helpers;
using sqlsvc.Models;

namespace sqlsvc.Tests;

[Collection("Sequential")]
public class OutputFormattingTests
{
    private readonly List<SqlServiceInfo> _services =
    [
        new() { ServiceName = "MSSQLSERVER", DisplayName = "SQL Server (MSSQLSERVER)", Status = "Running", StartupType = "Automatic" },
        new() { ServiceName = "SQLBrowser", DisplayName = "SQL Server Browser", Status = "Stopped", StartupType = "Manual" },
    ];

    [Fact]
    public void PrintTable_IncludesHeaders()
    {
        var output = CaptureOut(() => OutputFormatter.PrintTable(_services));
        Assert.Contains("ServiceName", output);
        Assert.Contains("DisplayName", output);
        Assert.Contains("Status", output);
        Assert.Contains("StartupType", output);
    }

    [Fact]
    public void PrintTable_IncludesServiceData()
    {
        var output = CaptureOut(() => OutputFormatter.PrintTable(_services));
        Assert.Contains("MSSQLSERVER", output);
        Assert.Contains("SQLBrowser", output);
        Assert.Contains("Running", output);
        Assert.Contains("Stopped", output);
        Assert.Contains("Manual", output);
    }

    [Fact]
    public void PrintTable_SingleService_DoesNotThrow()
    {
        var single = _services.Take(1).ToList();
        var output = CaptureOut(() => OutputFormatter.PrintTable(single));
        Assert.Contains("MSSQLSERVER", output);
    }

    [Fact]
    public void PrintJson_ValidJson()
    {
        var output = CaptureOut(() => OutputFormatter.PrintJson(_services));
        Assert.Contains("\"ServiceName\": \"MSSQLSERVER\"", output);
        Assert.Contains("\"Status\": \"Running\"", output);
    }

    [Fact]
    public void PrintCsv_IncludesHeader()
    {
        var output = CaptureOut(() => OutputFormatter.PrintCsv(_services));
        Assert.StartsWith("ServiceName,DisplayName,Status,StartupType", output.Trim());
    }

    [Fact]
    public void PrintCsv_IncludesQuotedValues()
    {
        var output = CaptureOut(() => OutputFormatter.PrintCsv(_services));
        Assert.Contains("\"MSSQLSERVER\"", output);
        Assert.Contains("\"SQL Server (MSSQLSERVER)\"", output);
    }

    private static string CaptureOut(Action action)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            action();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
