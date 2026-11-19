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

	/// <summary>Names the search button, under <see cref="MokaLogSearchMode.OnSubmit" />, and its tooltip.</summary>
	public string SearchButton { get; init; } = "Search";

	/// <summary>Names the level list under <see cref="MokaLogLevelFilter.AtLeast" />, which has no visible label.</summary>
	public string MinimumLevelLabel { get; init; } = "Minimum level";

	/// <summary>
	///     The text of each choice in the level list under <see cref="MokaLogLevelFilter.AtLeast" />, given the lowest level
	///     it shows. English: "All levels", "Debug and above" up to "Fatal only".
	/// </summary>
	public Func<MokaLogLevel, string> AtLeast { get; init; } = level => level switch
	{
		MokaLogLevel.Trace => "All levels",
		MokaLogLevel.Fatal => "Fatal only",
		_ => $"{level} and above"
	};

	/// <summary>The button at the old end of the list while <see cref="MokaLogViewer.HasOlder" /> is set.</summary>
	public string ShowOlder { get; init; } = "Show older lines";

	/// <summary>Said to screen readers while older lines load.</summary>
	public string LoadingOlder { get; init; } = "Loading older lines";
}
