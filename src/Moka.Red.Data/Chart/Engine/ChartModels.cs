namespace Moka.Red.Data.Chart;

/// <summary>What a series is drawn as, which decides its legend key.</summary>
internal enum ChartMark
{
	Line,
	Area,
	Box,
	Dot
}

/// <summary>One series as the chart's chrome sees it: legend order, color, and whether it is hidden or muted.</summary>
/// <param name="Key">Its identity for hiding, highlighting and colors.</param>
/// <param name="Name">What it is called.</param>
/// <param name="Slot">Its palette slot, counting from 1.</param>
/// <param name="ColorClass">The class that colors it.</param>
/// <param name="ColorStyle">An inline <c>--moka-chart-color</c> when its color isn't a class.</param>
/// <param name="Mark">What it is drawn as.</param>
/// <param name="Hidden">Whether the legend hid it.</param>
/// <param name="Muted">Whether a highlight elsewhere mutes it.</param>
/// <param name="CurrentText">Its latest value, written, when the legend shows values.</param>
internal sealed record ChartSeriesInfo(
	string Key,
	string Name,
	int Slot,
	string ColorClass,
	string? ColorStyle,
	ChartMark Mark,
	bool Hidden,
	bool Muted,
	string? CurrentText);

/// <summary>One row of a tooltip or readout.</summary>
/// <param name="Name">The series' name.</param>
/// <param name="ColorClass">The class that colors its key.</param>
/// <param name="ColorStyle">An inline color for its key.</param>
/// <param name="ValueText">The value, written.</param>
/// <param name="DeltaText">The change since the pin, written.</param>
/// <param name="Emphasis">Whether the row is the one in focus.</param>
internal sealed record ChartActiveRow(
	string Name,
	string ColorClass,
	string? ColorStyle,
	string ValueText,
	string? DeltaText,
	bool Emphasis);

/// <summary>What the surface shows for the point or column under the pointer or the keyboard.</summary>
/// <param name="Heading">The tooltip's heading, such as the time.</param>
/// <param name="Rows">One row per visible series.</param>
/// <param name="Notes">Lines about thresholds and bands.</param>
/// <param name="CellText">What the keyboard grid's cell reads, such as "LAN, 14:30, 412 Mbps".</param>
/// <param name="Context">What a <c>TooltipTemplate</c> gets.</param>
internal sealed record ChartActiveInfo(
	string? Heading,
	IReadOnlyList<ChartActiveRow> Rows,
	IReadOnlyList<string> Notes,
	string CellText,
	MokaChartTooltipContext Context);

/// <summary>The table view and the CSV export of a chart.</summary>
/// <param name="Headers">The column headings.</param>
/// <param name="RowCount">How many rows.</param>
/// <param name="Display">A cell as the table shows it, by row and column.</param>
/// <param name="Raw">A cell for CSV: invariant numbers and ISO 8601 times.</param>
internal sealed record ChartTableModel(
	IReadOnlyList<string> Headers,
	int RowCount,
	Func<int, int, string> Display,
	Func<int, int, string> Raw);

/// <summary>
///     Where the drawn points sit, for the script that finds the point under the pointer. Every position is a share of
///     the plot, 0 to 1; NaN is a gap. Every series shares one x list, sorted, so a chart whose series have x values of
///     their own (scatter) merges them and leaves each series a gap where another series has the point.
/// </summary>
/// <param name="XMin">The x value at the plot's left edge.</param>
/// <param name="XMax">The x value at the plot's right edge.</param>
/// <param name="X">Each drawn point's x share.</param>
/// <param name="Y">Each series' y shares, one per drawn point.</param>
internal sealed record ChartHoverData(
	double XMin,
	double XMax,
	ReadOnlyMemory<float> X,
	IReadOnlyList<ReadOnlyMemory<float>> Y);
