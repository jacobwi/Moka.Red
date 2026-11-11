using System.Globalization;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Every word a chart writes, English by default. Set the ones to change: <c>MokaChartText.Default with { TableView =
///     "Tabelle" }</c>. The sentences are what a screen reader hears when the legend or the pin changes something.
/// </summary>
public sealed record MokaChartText
{
	/// <summary>The words in English.</summary>
	public static MokaChartText Default { get; } = new();

	/// <summary>The view switch's chart option. Defaults to "Chart".</summary>
	public string ChartView { get; init; } = "Chart";

	/// <summary>The view switch's table option. Defaults to "Table".</summary>
	public string TableView { get; init; } = "Table";

	/// <summary>The view switch's name. Defaults to "Show as".</summary>
	public string ViewLabel { get; init; } = "Show as";

	/// <summary>The export menu's name. Defaults to "Export".</summary>
	public string Export { get; init; } = "Export";

	/// <summary>The PNG export. Defaults to "PNG image".</summary>
	public string ExportPng { get; init; } = "PNG image";

	/// <summary>The SVG export. Defaults to "SVG drawing".</summary>
	public string ExportSvg { get; init; } = "SVG drawing";

	/// <summary>The CSV export. Defaults to "CSV data".</summary>
	public string ExportCsv { get; init; } = "CSV data";

	/// <summary>The button that undoes a zoom. Defaults to "Reset zoom".</summary>
	public string ResetZoom { get; init; } = "Reset zoom";

	/// <summary>The button that brings hidden series back. Defaults to "Show all".</summary>
	public string ShowAll { get; init; } = "Show all";

	/// <summary>The chip while new data loads. Defaults to "Updating".</summary>
	public string Updating { get; init; } = "Updating";

	/// <summary>The chip of a live chart. Defaults to "Live".</summary>
	public string Live { get; init; } = "Live";

	/// <summary>The chip of a live chart holding still. Defaults to "Paused".</summary>
	public string Paused { get; init; } = "Paused";

	/// <summary>The chip of a live chart holding still with samples waiting. Defaults to "Paused · 3 new".</summary>
	public Func<int, string> PausedWaiting { get; init; } =
		waiting => $"Paused · {waiting.ToString(CultureInfo.CurrentCulture)} new";

	/// <summary>The button that asks for the data again. Defaults to "Try again".</summary>
	public string TryAgain { get; init; } = "Try again";

	/// <summary>The tag on a pinned tooltip. Defaults to "Pinned".</summary>
	public string Pinned { get; init; } = "Pinned";

	/// <summary>The button that lets a pinned tooltip go. Defaults to "Unpin".</summary>
	public string Unpin { get; init; } = "Unpin";

	/// <summary>What an empty chart says. Defaults to "No data".</summary>
	public string NoData { get; init; } = "No data";

	/// <summary>The total row of a stacked tooltip. Defaults to "Total".</summary>
	public string Total { get; init; } = "Total";

	/// <summary>What the small pieces folded together are called. Defaults to "Other".</summary>
	public string Other { get; init; } = "Other";

	/// <summary>The hint when the wheel scrolls past a zoomable chart. Defaults to "Hold Ctrl and scroll to zoom".</summary>
	public string ZoomHint { get; init; } = "Hold Ctrl and scroll to zoom";

	/// <summary>The range presets' name. Defaults to "Time range".</summary>
	public string RangeLabel { get; init; } = "Time range";

	/// <summary>How the keys work on a line chart, read after its name.</summary>
	public string LineKeys { get; init; } = "Left and Right move along a line, Up and Down change line.";

	/// <summary>How the keys work on a bar chart, read after its name.</summary>
	public string BarKeys { get; init; } = "Left and Right move between bars.";

	/// <summary>How the keys work on a chart of rows, read after its name.</summary>
	public string RowKeys { get; init; } = "Up and Down move between rows.";

	/// <summary>How the keys work on a chart of parts, such as a donut or a treemap, read after its name.</summary>
	public string PartKeys { get; init; } = "Left and Right move between parts.";

	/// <summary>Said when a series is hidden: its name, how many show, and how many there are.</summary>
	public Func<string, int, int, string> SeriesHidden { get; init; } =
		(name, shown, total) => $"{name} hidden. {Count(shown)} of {Count(total)} series shown.";

	/// <summary>Said when a series is shown again: its name, how many show, and how many there are.</summary>
	public Func<string, int, int, string> SeriesShown { get; init; } =
		(name, shown, total) => $"{name} shown. {Count(shown)} of {Count(total)} series shown.";

	/// <summary>Said when one series is shown alone.</summary>
	public Func<string, string> SeriesIsolated { get; init; } =
		name => $"Showing {name} only. Do it again, or press Show all, to bring the rest back.";

	/// <summary>Said when every series shows again: how many there are.</summary>
	public Func<int, string> AllShown { get; init; } = total => $"All {Count(total)} series shown.";

	/// <summary>Said when a point is pinned: where.</summary>
	public Func<string, string> PinnedAt { get; init; } = at => $"Pinned at {at}.";

	/// <summary>Said when the pin is let go.</summary>
	public string Unpinned { get; init; } = "Unpinned.";

	/// <summary>A tooltip note for a value past a threshold: by how much, and the line's value.</summary>
	public Func<string, string, string> OverThreshold { get; init; } = (by, line) => $"{by} over the {line} line";

	/// <summary>A tooltip note for a value inside the target band.</summary>
	public string InsideTarget { get; init; } = "Inside target";

	/// <summary>A tooltip note for a value above the target band: by how much.</summary>
	public Func<string, string> AboveTarget { get; init; } = by => $"{by} above target";

	/// <summary>A tooltip note for a value below the target band: by how much.</summary>
	public Func<string, string> BelowTarget { get; init; } = by => $"{by} below target";

	/// <summary>A tooltip note for a band's spread: its low and its high.</summary>
	public Func<string, string, string> Spread { get; init; } = (low, high) => $"5th {low} to 95th {high}";

	/// <summary>The table view's heading for names. Defaults to "Name".</summary>
	public string Name { get; init; } = "Name";

	/// <summary>The table view's heading for values. Defaults to "Value".</summary>
	public string Value { get; init; } = "Value";

	/// <summary>The table view's heading for shares of a whole. Defaults to "Share".</summary>
	public string Share { get; init; } = "Share";

	/// <summary>The table view's heading for where a flow starts. Defaults to "From".</summary>
	public string From { get; init; } = "From";

	/// <summary>The table view's heading for where a flow ends. Defaults to "To".</summary>
	public string To { get; init; } = "To";

	/// <summary>A span between two times or values, such as an event's: its start and its end.</summary>
	public Func<string, string, string> Between { get; init; } = (from, to) => $"{from} to {to}";

	/// <summary>Shown when an export fails.</summary>
	public string ExportFailed { get; init; } = "Couldn't save the chart. Try again.";

	/// <summary>Shown in the error state when the chart has no words of its own for the failure.</summary>
	public string LoadFailed { get; init; } = "Couldn't load the data.";

	/// <summary>The name of the legend's button that shows the entries folded away: how many.</summary>
	public Func<int, string> MoreSeries { get; init; } =
		count => count == 1 ? "Show 1 more series" : $"Show {Count(count)} more series";

	/// <summary>The legend's button that folds the extra entries away again.</summary>
	public string FewerSeries { get; init; } = "Show fewer series";

	/// <summary>
	///     The tooltip's note under the changes since the pinned point, while the pointer is somewhere else: the pinned
	///     point's label. Defaults to "Change since 02:30".
	/// </summary>
	public Func<string, string> ChangeSince { get; init; } = at => $"Change since {at}";

	/// <summary>The readout's word for the newest values, shown before their time while nothing is pointed at.</summary>
	public string Latest { get; init; } = "Latest";

	/// <summary>The table view's heading for times. Defaults to "Time".</summary>
	public string Time { get; init; } = "Time";

	/// <summary>The table view's heading for the bottom of a series' band: the series' name. Defaults to "LAN low".</summary>
	public Func<string, string> Low { get; init; } = name => $"{name} low";

	/// <summary>The table view's heading for the top of a series' band: the series' name. Defaults to "LAN high".</summary>
	public Func<string, string> High { get; init; } = name => $"{name} high";

	/// <summary>The table view's heading for an indexed series' index: the series' name. Defaults to "LAN index".</summary>
	public Func<string, string> Index { get; init; } = name => $"{name} index";

	/// <summary>The legend's name for screen readers. Defaults to "Series".</summary>
	public string Series { get; init; } = "Series";

	private static string Count(int count) => count.ToString(CultureInfo.CurrentCulture);
}
