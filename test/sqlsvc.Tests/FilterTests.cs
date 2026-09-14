using sqlsvc.Services;

namespace sqlsvc.Tests;

public class FilterTests
{
    [Theory]
    [InlineData("MSSQLSERVER", true)]
    [InlineData("MSSQL$SQLEXPRESS", true)]
    [InlineData("MSSQLFDLauncher", true)]
    [InlineData("MSSQLFDLauncher$SQLEXPRESS", true)]
    [InlineData("MSSQLLaunchpad", true)]
    [InlineData("SQLSERVERAGENT", true)]
    [InlineData("SQLAGENT$SQLEXPRESS", true)]
    [InlineData("SQLBrowser", true)]
    [InlineData("SQLWriter", true)]
    [InlineData("ReportServer", true)]
    [InlineData("ReportServer$SQLEXPRESS", true)]
    [InlineData("MsDtsServer150", true)]
    [InlineData("MsDtsServer160", true)]
    [InlineData("MSOLAP$SQL2022", true)]
    [InlineData("SQLTELEMETRY", true)]
    [InlineData("SQLTELEMETRY$SQL2022", true)]
    [InlineData("WSearch", false)]
    [InlineData("Dhcp", false)]
    [InlineData("SQLAnywhere", false)]
    [InlineData("MySQL80", false)]
    [InlineData("Tomcat", false)]
    [InlineData("", false)]
    public void IsSqlServerService_Returns_Correctly(string serviceName, bool expected)
    {
        var result = ServiceDiscovery.IsSqlServerService(serviceName);
        Assert.Equal(expected, result);
    }
}
