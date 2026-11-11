using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;

namespace Moka.Red.Data.Chart;

/// <summary>What a column chart's x axis counts in.</summary>
internal enum ChartXKind
{
	/// <summary>Categories, one per value, in order.</summary>
	Category,

	/// <summary>Times: x is proportional to time and the axis steps in clock and calendar units.</summary>
	Time,

	/// <summary>Numbers: x is proportional to the value, on nice ticks.</summary>
	Number
}

/// <summary>
///     A tick on a value axis: its value, where it sits as a share of the axis (0 at the bottom or start), its text, the
///     class that hides it on an axis too short for it, and what it is.
/// </summary>
/// <param name="Value">The value.</param>
/// <param name="Share">Where it sits.</param>
/// <param name="Text">What it reads.</param>
/// <param name="HideClass">The class that hides its text on a short axis, or null when it always shows.</param>
/// <param name="Baseline">Whether its grid line is the baseline: zero when the axis crosses it, else the bottom.</param>
/// <param name="Minor">Whether it is a log axis's tick between two powers of ten.</param>
/// <param name="Tick">The tick as a <see cref="MokaChartAxis.TickTemplate" /> gets it.</param>
internal sealed record PlotTick(
	double Value,
	double Share,
	string Text,
	string? HideClass,
	bool Baseline,
	bool Minor,
	MokaAxisTick Tick);

/// <summary>
///     A label along a horizontal axis: its text, where it is centred as a share of the axis, its classes and variables
///     from <see cref="ChartAxisLayout" />, the whole text when the stylesheet may cut it, and the tick it stands for.
/// </summary>
/// <param name="Text">What it reads.</param>
/// <param name="Share">Where it is centred.</param>
/// <param name="Class">Its classes, which hide it, stagger it or mark a day boundary.</param>
/// <param name="Style">Its variables: share, half width, rotated reach, slot and longest length.</param>
/// <param name="Title">The whole text, for a label that may be cut; null otherwise.</param>
/// <param name="Tick">The tick as a <see cref="MokaChartAxis.TickTemplate" /> gets it.</param>
internal sealed record PlotLabel(string Text, double Share, string Class, string Style, string? Title, MokaAxisTick Tick);

/// <summary>A line down the plot at a share of its width, such as the start of a day on a time axis.</summary>
/// <param name="Share">Where it runs.</param>
/// <param name="Class">What it marks.</param>
internal sealed record PlotLine(double Share, string Class);

/// <summary>A horizontal axis laid out: its labels, its stage classes and variables, and the lines it draws down the plot.</summary>
/// <param name="Labels">The labels that show on some width.</param>
/// <param name="AxisClass">The classes that stagger, rotate, wrap or cut its labels by its own width.</param>
/// <param name="AxisStyle">Its variables.</param>
/// <param name="Lines">The lines down the plot: day, month and year boundaries on a time axis.</param>
internal sealed record PlotAxis(IReadOnlyList<PlotLabel> Labels, string AxisClass, string? AxisStyle, IReadOnlyList<PlotLine> Lines)
{
	/// <summary>No labels at all.</summary>
	public static PlotAxis Empty { get; } = new([], "moka-chart-plot__x--fit-thin", null, []);
}

/// <summary>What <see cref="MokaChartPlot" /> draws around a column chart's marks: both axes, the grid and the annotations.</summary>
/// <param name="Scale">The value axis.</param>
/// <param name="ValueTicks">Its ticks, bottom first.</param>
/// <param name="WidestValueText">The longest tick text, which sizes the axis column.</param>
/// <param name="XKind">What the x axis counts in.</param>
/// <param name="X">The x axis's labels and lines.</param>
/// <param name="Grid">The lines behind the marks.</param>
/// <param name="ValueSide">Which side the value axis is on.</param>
/// <param name="TickLabels">Where the value axis's numbers sit.</param>
/// <param name="ShowValueAxis">Whether the value axis's numbers are drawn.</param>
/// <param name="ShowCategoryAxis">Whether the labels along the bottom are drawn.</param>
/// <param name="ValueTitle">The value axis's title, with its unit when the ticks leave it out.</param>
/// <param name="XTitle">The x axis's title.</param>
/// <param name="ScrollMinEm">With <see cref="MokaAxisLabelFit.Scroll" />, the least width of the plot in em; null otherwise.</param>
/// <param name="Annotations">The thresholds, bands, events and windows.</param>
/// <param name="ValueTemplate">Draws each value tick, when the y axis has a template.</param>
/// <param name="XTemplate">Draws each x label, when the x axis has a template.</param>
/// <param name="BaselineShare">Where the baseline runs: zero when the value axis crosses it, else its bottom.</param>
internal sealed record ColumnPlotModel(
	MokaScale Scale,
	IReadOnlyList<PlotTick> ValueTicks,
	string WidestValueText,
	ChartXKind XKind,
	PlotAxis X,
	MokaChartGrid Grid,
	MokaAxisSide ValueSide,
	MokaTickPlacement TickLabels,
	bool ShowValueAxis,
	bool ShowCategoryAxis,
	string? ValueTitle,
	string? XTitle,
	double? ScrollMinEm,
	PlotAnnotations Annotations,
	RenderFragment<MokaAxisTick>? ValueTemplate,
	RenderFragment<MokaAxisTick>? XTemplate,
	double BaselineShare);
