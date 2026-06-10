using Spectre.Console;
using Spectre.Console.Rendering;
using sqlsvc.Helpers;
using sqlsvc.Models;
using sqlsvc.Services;
using System.ComponentModel;
using System.ServiceProcess;

namespace sqlsvc.Commands;

internal static class TuiCommand {
    private sealed record AppState(
        List<SqlServiceInfo> AllServices,
        List<SqlServiceInfo> Filtered,
        int SelectedIndex,
        int ScrollOffset,
        string FilterText,
        string StatusMessage,
        bool StatusIsError,
        SqlServiceInfo? DetailService,
        bool ShowHelp,
        bool NeedsRefresh,
        int Width,
        int Height,
        int ContentRows
    );

    public static int Execute() {
        if (!IsConsoleAvailable()) {
            ConsoleEx.WriteErrorLine("TUI mode requires an interactive console.");
            return 1;
        }

        var originalTitle = Console.Title;
        var originalCursorVisible = Console.CursorVisible;

        Console.Title = "sqlsvc - SQL Server Service Manager";
        Console.CursorVisible = false;

        try {
            Console.Clear();

            var services = ServiceDiscovery.GetSqlServices();

            var state = new AppState(
                AllServices: services,
                Filtered: services,
                SelectedIndex: 0,
                ScrollOffset: 0,
                FilterText: "",
                StatusMessage: "",
                StatusIsError: false,
                DetailService: null,
                ShowHelp: false,
                NeedsRefresh: false,
                Width: Math.Min(Console.WindowWidth, 120),
                Height: Console.WindowHeight,
                ContentRows: Console.WindowHeight - 7
            );

            var exitCode = 0;

            AnsiConsole.Live(BuildScreen(state))
                .AutoClear(false)
                .Start(ctx => {
                    while (true) {
                        if (state.NeedsRefresh) {
                            services = ServiceDiscovery.GetSqlServices();
                            state = state with {
                                AllServices = services,
                                Filtered = FilterServices(services, state.FilterText),
                                NeedsRefresh = false,
                                StatusMessage = "Refreshed.",
                                StatusIsError = false
                            };
                        }

                        var filtered = state.Filtered;
                        var sel = state.SelectedIndex;

                        if (filtered.Count > 0 && sel >= filtered.Count)
                            sel = filtered.Count - 1;
                        if (sel < 0)
                            sel = 0;

                        if (sel < state.ScrollOffset)
                            state = state with { ScrollOffset = sel, SelectedIndex = sel };
                        else if (sel >= state.ScrollOffset + state.ContentRows)
                            state = state with { ScrollOffset = sel - state.ContentRows + 1, SelectedIndex = sel };
                        else if (sel != state.SelectedIndex)
                            state = state with { SelectedIndex = sel };

                        var width = Math.Min(Console.WindowWidth, 120);
                        var height = Console.WindowHeight;
                        var hasDetail = state.DetailService is not null;
                        var contentRows = height - 7 - (hasDetail ? 6 : 0);

                        if (width != state.Width || height != state.Height || contentRows != state.ContentRows)
                            state = state with { Width = width, Height = height, ContentRows = contentRows };

                        ctx.UpdateTarget(BuildScreen(state));
                        ctx.Refresh();

                        var statusMsg = state.StatusMessage;
                        if (!string.IsNullOrEmpty(statusMsg))
                            state = state with { StatusMessage = "" };

                        if (state.ShowHelp || state.DetailService is not null) {
                            Console.ReadKey(true);
                            state = state with { ShowHelp = false, DetailService = null };
                            continue;
                        }

                        var k = Console.ReadKey(true);

                        if (k.Key == ConsoleKey.Enter && filtered.Count > 0) {
                            state = state with { DetailService = filtered[state.SelectedIndex] };
                            continue;
                        }

                        if (k.KeyChar == '/' || k.Key == ConsoleKey.F5) {
                            if (k.Key == ConsoleKey.F5) {
                                state = state with { NeedsRefresh = true };
                                continue;
                            }

                            var newFilter = ReadFilterInput(state.FilterText);
                            state = state with {
                                FilterText = newFilter,
                                Filtered = FilterServices(state.AllServices, newFilter),
                                SelectedIndex = 0,
                                ScrollOffset = 0
                            };
                            continue;
                        }

                        switch (k.Key) {
                            case ConsoleKey.UpArrow when state.SelectedIndex > 0:
                                state = state with { SelectedIndex = state.SelectedIndex - 1 };
                                break;

                            case ConsoleKey.DownArrow when state.SelectedIndex < filtered.Count - 1:
                                state = state with { SelectedIndex = state.SelectedIndex + 1 };
                                break;

                            case ConsoleKey.PageUp:
                                state = state with { SelectedIndex = Math.Max(0, state.SelectedIndex - contentRows) };
                                break;

                            case ConsoleKey.PageDown:
                                state = state with { SelectedIndex = Math.Min(filtered.Count - 1, state.SelectedIndex + contentRows) };
                                break;

                            case ConsoleKey.Home:
                                state = state with { SelectedIndex = 0 };
                                break;

                            case ConsoleKey.End:
                                state = state with { SelectedIndex = Math.Max(0, filtered.Count - 1) };
                                break;

                            case ConsoleKey.F1:
                                state = state with { ShowHelp = true };
                                break;

                            case ConsoleKey.S when filtered.Count > 0:
                                var (msgS, errS) = DoAction(filtered[state.SelectedIndex].ServiceName, sc => {
                                    if (sc.StartType == ServiceStartMode.Disabled) {
                                        if (!ServiceManager.TryChangeStartupType(sc, "manual"))
                                            return ($"Failed to enable '{sc.ServiceName}'.", true);
                                    }
                                    ServiceManager.Start(sc, ServiceManager.DefaultTimeoutSeconds);
                                    return ($"'{sc.ServiceName}' started.", false);
                                });
                                state = state with { StatusMessage = msgS, StatusIsError = errS, NeedsRefresh = !errS };
                                break;

                            case ConsoleKey.T when filtered.Count > 0:
                                var (msgT, errT) = DoAction(filtered[state.SelectedIndex].ServiceName, sc => {
                                    ServiceManager.Stop(sc, ServiceManager.DefaultTimeoutSeconds);
                                    return ($"'{sc.ServiceName}' stopped.", false);
                                });
                                state = state with { StatusMessage = msgT, StatusIsError = errT, NeedsRefresh = !errT };
                                break;

                            case ConsoleKey.A when filtered.Count > 0:
                                var (msgA, errA) = DoAction(filtered[state.SelectedIndex].ServiceName, sc => {
                                    ServiceManager.ChangeStartupType(sc, "auto");
                                    return ($"'{sc.ServiceName}' set to Automatic.", false);
                                });
                                state = state with { StatusMessage = msgA, StatusIsError = errA, NeedsRefresh = !errA };
                                break;

                            case ConsoleKey.M when filtered.Count > 0:
                                var (msgM, errM) = DoAction(filtered[state.SelectedIndex].ServiceName, sc => {
                                    ServiceManager.ChangeStartupType(sc, "manual");
                                    return ($"'{sc.ServiceName}' set to Manual.", false);
                                });
                                state = state with { StatusMessage = msgM, StatusIsError = errM, NeedsRefresh = !errM };
                                break;

                            case ConsoleKey.D when filtered.Count > 0:
                                var (msgD, errD) = DoAction(filtered[state.SelectedIndex].ServiceName, sc => {
                                    ServiceManager.ChangeStartupType(sc, "disabled");
                                    return ($"'{sc.ServiceName}' set to Disabled.", false);
                                });
                                state = state with { StatusMessage = msgD, StatusIsError = errD, NeedsRefresh = !errD };
                                break;

                            case ConsoleKey.R:
                                state = state with { NeedsRefresh = true };
                                break;

                            case ConsoleKey.Q:
                            case ConsoleKey.Escape:
                                exitCode = 0;
                                return;
                        }
                    }
                });

            return exitCode;
        }
        finally {
            Console.CursorVisible = originalCursorVisible;
            Console.Title = originalTitle;
            Console.ResetColor();
            Console.Clear();
            Console.WriteLine("Exited sqlsvc TUI.");
        }
    }

    private static Rows BuildScreen(AppState state) {
        var rows = new List<IRenderable>();
        var width = state.Width;

        var header = new Panel(
            new Markup("[cyan]sqlsvc - SQL Server Service Manager[/]"))
            .Border(BoxBorder.Heavy)
            .BorderStyle(Style.Parse("grey"))
            .Padding(new Padding(2, 0, 2, 0))
            .Header(new PanelHeader("", Justify.Left));
        rows.Add(header);

        if (state.ShowHelp) {
            rows.Add(BuildHelpPanel());
        } else {
            rows.Add(BuildServiceTable(state));

            if (state.DetailService is not null)
                rows.Add(BuildDetailPanel(state));
        }

        rows.Add(BuildFooter(state));

        return new Rows(rows);
    }

    private static Table BuildServiceTable(AppState state) {
        var table = new Table()
            .Border(TableBorder.Simple)
            .Expand()
            .AddColumn(new TableColumn("Service Name").Padding(1, 0, 0, 0))
            .AddColumn(new TableColumn("Status").Padding(1, 0, 1, 0))
            .AddColumn(new TableColumn("Startup").Padding(1, 0, 1, 0));

        var visible = state.Filtered
            .Skip(state.ScrollOffset)
            .Take(state.ContentRows)
            .ToList();

        for (var i = 0; i < state.ContentRows; i++) {
            if (i < visible.Count) {
                var svc = visible[i];
                var idx = state.ScrollOffset + i;
                var isSelected = idx == state.SelectedIndex;

                var statusColor = svc.Status switch {
                    "Running" => "green",
                    "Stopped" => "yellow",
                    "Disabled" => "red",
                    _ => "default"
                };

                if (isSelected) {
                    table.AddRow(
                        new Markup($"[bold cyan]▌[/] " +
                        $"[bold white on gray]{svc.ServiceName.EscapeMarkup()}[/]"),
                        new Markup($"[bold][{statusColor} on gray]{svc.Status.EscapeMarkup()}[/][/]"),
                        new Markup($"[bold white on gray]{svc.StartupType.EscapeMarkup()}[/]")
                    );
                } else {
                    table.AddRow(
                        $"  {svc.ServiceName.EscapeMarkup()}",
                        $"[{statusColor}]{svc.Status.EscapeMarkup()}[/]",
                        svc.StartupType.EscapeMarkup()
                    );
                }
            } else {
                table.AddRow("", "", "");
            }
        }

        return table;
    }

    private static Panel BuildDetailPanel(AppState state) {
        var svc = state.DetailService!;

        var statusColor = svc.Status switch {
            "Running" => "green",
            "Stopped" => "yellow",
            "Disabled" => "red",
            _ => "default"
        };

        var detail = new Table();
        detail.Border(TableBorder.None);
        detail.AddColumn(new TableColumn("").Padding(0, 0, 0, 0));
        detail.AddColumn(new TableColumn("").Padding(0, 0, 0, 0));

        detail.AddRow(
            new Markup("[bold][cyan]Details:[/][/]"),
            new Markup(svc.ServiceName.EscapeMarkup())
        );
        detail.AddRow(
            new Markup("[dim]Display:[/]"),
            new Markup(svc.DisplayName.EscapeMarkup())
        );
        detail.AddRow(
            new Markup("[dim]Status:[/]"),
            new Markup($"[{statusColor}]{svc.Status.EscapeMarkup()}[/]")
        );
        detail.AddRow(
            new Markup("[dim]Startup:[/]"),
            new Markup(svc.StartupType.EscapeMarkup())
        );
        detail.AddRow(
            new Markup("[grey]Press any key to close[/]"),
            new Markup("")
        );

        return new Panel(detail)
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("grey"))
            .Padding(new Padding(1, 0, 1, 0));
    }

    private static Panel BuildFooter(AppState state) {
        var lines = new List<IRenderable>();

        if (!string.IsNullOrEmpty(state.FilterText)) {
            lines.Add(new Markup(
                $"┆ [yellow]Filter:[/] {state.FilterText.EscapeMarkup()} [yellow]({state.Filtered.Count} total)[/]    [[Esc]] Clear  [[Q]]uit ┆"));
        } else {
            lines.Add(new Markup(
                "┆ [green]S[/]tart  s[yellow]T[/]op  [cyan]A[/]uto  [cyan]M[/]anual  [red]D[/]isable  [grey]R[/]efresh  [[/]]Filter  [[F1]]Help  [[Q]]uit ┆"));
        }

        if (!string.IsNullOrEmpty(state.StatusMessage)) {
            var color = state.StatusIsError ? "red" : "lime";
            lines.Add(new Markup($"┆ [{color}]{state.StatusMessage.EscapeMarkup()}[/] ┆"));
        }

        var scrollHint = state.ScrollOffset > 0 || state.ScrollOffset + state.ContentRows < state.Filtered.Count;
        if (scrollHint && !state.ShowHelp) {
            var hasAbove = state.ScrollOffset > 0;
            var hasBelow = state.ScrollOffset + state.ContentRows < state.Filtered.Count;
            var hint = (hasAbove, hasBelow) switch {
                (true, true) => "↑ More · Down ↓",
                (true, false) => "↑ More above",
                (false, true) => "More below ↓",
                _ => ""
            };
            lines.Add(new Markup($"┆ [grey]{hint}[/] ┆"));
        }

        return new Panel(new Rows(lines))
            .Border(BoxBorder.Heavy)
            .BorderStyle(Style.Parse("grey"))
            .Padding(new Padding(1, 0, 1, 0));
    }

    private static Panel BuildHelpPanel() {
        var nav = new Grid();
        nav.AddColumn(new GridColumn().Padding(new Padding(0, 0, 2, 0)));
        nav.AddColumn(new GridColumn());
        nav.AddRow("[grey]F1[/]", "Show this help");
        nav.AddRow("[grey]↑/↓[/]", "Navigate services");
        nav.AddRow("[grey]PgUp/PgDn[/]", "Scroll by page");
        nav.AddRow("[grey]Home/End[/]", "Jump to first/last");
        nav.AddRow("[grey]/[/]", "Filter services ([grey]F5[/] to refresh)");
        nav.AddRow("[grey]Enter[/]", "Show service details");

        var actions = new Grid();
        actions.AddColumn(new GridColumn().Padding(new Padding(0, 0, 2, 0)));
        actions.AddColumn(new GridColumn());
        actions.AddRow("[green]S[/]", "Start selected service");
        actions.AddRow("[yellow]T[/]", "Stop selected service");
        actions.AddRow("[cyan]A[/]", "Set startup to [cyan]Automatic[/]");
        actions.AddRow("[cyan]M[/]", "Set startup to [cyan]Manual[/]");
        actions.AddRow("[red]D[/]", "Set startup to [red]Disabled[/]");
        actions.AddRow("[grey]R[/]", "Refresh service list");
        actions.AddRow("[grey]Q/Esc[/]", "Quit");

        var layout = new Grid();
        layout.AddColumn(new GridColumn().Padding(new Padding(2, 0, 0, 0)));
        layout.AddColumn(new GridColumn());
        layout.AddRow(
            new Panel(nav).Border(BoxBorder.None).Header(new PanelHeader("[bold]Navigation[/]", Justify.Left)).Padding(0, 0, 0, 0),
            new Panel(actions).Border(BoxBorder.None).Header(new PanelHeader("[bold]Actions[/]", Justify.Left)).Padding(0, 0, 0, 0)
        );

        return new Panel(layout)
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("grey"))
            .Padding(new Padding(1, 0, 1, 0))
            .Header(new PanelHeader("[cyan]Help[/]", Justify.Center));
    }

    private static string ReadFilterInput(string currentFilter) {
        var input = currentFilter;
        Console.CursorVisible = true;
        while (true) {
            Console.SetCursorPosition(3, Console.WindowHeight - 1);
            Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - 3)));
            Console.SetCursorPosition(3, Console.WindowHeight - 1);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Filter: ");
            Console.ResetColor();
            Console.Write(input);

            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Enter)
                break;
            if (k.Key == ConsoleKey.Escape) { input = ""; break; }
            if (k.Key == ConsoleKey.Backspace && input.Length > 0)
                input = input[..^1];
            else if (k.KeyChar is >= (char)32 and < (char)127)
                input += k.KeyChar;
        }
        Console.CursorVisible = false;
        return input;
    }

    private static List<SqlServiceInfo> FilterServices(List<SqlServiceInfo> services, string filter) {
        if (string.IsNullOrWhiteSpace(filter))
            return services;
        return services
            .Where(s => s.ServiceName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsConsoleAvailable() {
        try { return !Console.IsOutputRedirected && Console.WindowHeight > 10; }
        catch { return false; }
    }

    private static (string message, bool isError) DoAction(string serviceName, Func<ServiceController, (string, bool)> action) {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try {
            using var silentOut = new StringWriter();
            Console.SetOut(silentOut);
            Console.SetError(silentOut);

            ServiceManager.WarnIfNotAdministrator();
            using var sc = ServiceManager.GetService(serviceName);
            return !ServiceDiscovery.IsSqlServerService(sc.ServiceName) ?
                ((string message, bool isError))($"'{serviceName}' is not a SQL Server service.", true) :
                ((string message, bool isError))action(sc);
        }
        catch (ServiceNotFoundException) { return ($"Service '{serviceName}' not found.", true); }
        catch (Win32Exception) { return ("Access denied. Run as administrator.", true); }
        catch (InvalidOperationException ex) { return ($"Error: {ex.Message}", true); }
        catch (System.TimeoutException) { return ("Operation timed out.", true); }
        catch (Exception ex) { return ($"Error: {ex.Message}", true); }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
