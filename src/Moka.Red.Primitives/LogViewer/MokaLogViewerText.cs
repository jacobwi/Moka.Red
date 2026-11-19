namespace Moka.Red.Primitives.LogViewer;

/// <summary>Every word a <see cref="MokaLogViewer" /> shows or says to a screen reader.</summary>
public sealed record MokaLogViewerText
{
	/// <summary>The words a log viewer uses unless it is given others.</summary>
	public static MokaLogViewerText Default { get; } = new();

	/// <summary>The search box's placeholder.</summary>
	public string SearchPlaceholder { get; init; } = "Search logs...";

	/// <summary>Names the search box for screen readers, since it has no visible label.</summary>
	public string SearchLabel { get; init; } = "Search logs";

	/// <summary>Shown when there are no entries.</summary>
	public string NoEntries { get; init; } = "No log entries";

	/// <summary>Names the scrolling list of rows, which takes focus under <see cref="MokaLogViewer.Virtualize" /> so the keyboard can scroll it.</summary>
	public string EntriesLabel { get; init; } = "Log entries";

	/// <summary>Names the button that opens a row's detail.</summary>
	public string ShowDetails { get; init; } = "Show details";

	/// <summary>Names the button that closes a row's detail.</summary>
	public string HideDetails { get; init; } = "Hide details";

	/// <summary>Names the button that closes the detail pane under <see cref="MokaLogViewer.Virtualize" />.</summary>
	public string CloseDetails { get; init; } = "Close details";
}
