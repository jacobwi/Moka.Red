using Moka.Red.Core.Icons;

namespace Moka.Red.Data.Chart;

/// <summary>What a series is drawn as, which decides its legend key.</summary>
internal enum ChartMark
{
	Line,
	Area,
	Box,
	Dot,
	Dash
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
	string? CurrentText)
{
	/// <summary>An icon the legend draws in its color in place of the key, for a series that means a state.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>The key of the series it is drawn with, such as a comparison's, so a highlight of that series keeps it lit.</summary>
	public string? With { get; init; }
}

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
	bool Emphasis)
{
	/// <summary>An icon drawn in its color in place of the key, for a row that names a state.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>How far the value is from the series it is compared with, such as "+12% vs Last week".</summary>
	public string? CompareText { get; init; }

	/// <summary>Whether the row's line is drawn dashed, as a comparison or a trend is, so its key is a dash.</summary>
	public bool Dashed { get; init; }
}

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

/// <summary>How a chart's parts are numbered across its rows, for the keys that step between rows.</summary>
internal enum ChartPartOrder
{
	/// <summary>Row after row: part <c>row * columns + column</c>, such as a heatmap grid.</summary>
	Rows,

	/// <summary>Column after column: part <c>column * rows + row</c>, such as the days of a calendar, week after week.</summary>
	Columns,

	/// <summary>Row after row, each row holding its own number of parts at places along it, such as a state timeline's spans.</summary>
	Listed
}

/// <summary>
///     Where a chart of parts has each part, by row and by place along the row, so the arrow keys step along a row and
///     between rows. A regular layout is worked out from its size; a listed one carries where each row starts and each
///     part's span, which the script pulls once a geometry version, the first time the keys need it.
/// </summary>
/// <param name="Order">How the parts are numbered.</param>
/// <param name="Rows">How many rows.</param>
/// <param name="Columns">How many columns a regular layout has; the most parts a row holds in a listed one.</param>
/// <param name="Count">How many parts there are.</param>
internal sealed record ChartPartLayout(ChartPartOrder Order, int Rows, int Columns, int Count)
{
	/// <summary>
	///     Whether Up and Down follow the numbering past a column's ends, as days run on into the next week. Only a layout
	///     numbered by columns flows.
	/// </summary>
	public bool Flow { get; init; }

	/// <summary>A listed layout's first part of each row, in row order.</summary>
	public int[]? RowStarts { get; init; }

	/// <summary>Where each part of a listed layout starts along its row, as a share of the row, 0 to 1.</summary>
	public float[]? From { get; init; }

	/// <summary>Where each part of a listed layout ends along its row, as a share of the row.</summary>
	public float[]? To { get; init; }

	/// <summary>A part's row and its place in that row, both from 0; null out of range.</summary>
	/// <param name="part">The part.</param>
	public (int Row, int Column)? PlaceOf(int part)
	{
		if (part < 0 || part >= Count)
		{
			return null;
		}

		switch (Order)
		{
			case ChartPartOrder.Rows:
				return Columns > 0 ? (part / Columns, part % Columns) : null;
			case ChartPartOrder.Columns:
				return Rows > 0 ? (part % Rows, part / Rows) : null;
		}

		if (RowStarts is not { Length: > 0 } starts)
		{
			return null;
		}

		int index = Array.BinarySearch(starts, part);
		int row = index >= 0 ? index : ~index - 1;
		while (row + 1 < starts.Length && starts[row + 1] == part)
		{
			row++;
		}

		return row >= 0 ? (row, part - starts[row]) : null;
	}
}
