using Spectre.Console;

namespace dis.Features.TrimSlider;

public static class DisplayStrings 
{
    private static Table GetTimeInputTable(string currentInput)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Time Input").Centered().PadLeft(1).PadRight(1))
            .HideHeaders();

        var currentValue = string.IsNullOrEmpty(currentInput) 
            ? "█" 
            : $"[underline]{currentInput}[/]█";

        table.AddRow($"[blue]Enter time value:[/] {currentValue}");
        table.AddRow(new Text("(ss) or (mm:ss) or (mm:ss.ms)", new Style(foreground: Color.Grey)));
        table.AddRow(new Markup("[green]Enter[/] to confirm, [red]Esc[/] to cancel"));

        return table;
    }

    public static Table GetTimeInput(string currentInput) => GetTimeInputTable(currentInput);

    private static Table GetControlsTable()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Key").PadLeft(1).PadRight(1))
            .AddColumn(new TableColumn("Action").PadLeft(1).PadRight(1));

        table.AddRow("[aqua]1[/] / [aqua]2[/]", "Select [aqua]start/end[/] position");
        table.AddRow("[yellow]←[/] / [yellow]→[/]", "Adjust by seconds");
        table.AddRow("[yellow]↑[/] / [yellow]↓[/]", "Adjust by minutes");
        table.AddRow("[lime]Shift[/] + [yellow]←[/] / [yellow]→[/]", "Adjust by [blue]milliseconds[/]");
        table.AddRow("[aqua]Space[/]", "Enter exact time");
        table.AddRow("[green]Enter[/]", "Confirm");
        table.AddRow("[red]Esc[/]", "Cancel");

        return table;
    }

    public static Table Controls => GetControlsTable();
}
