using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;

namespace Moka.Red.Data.Chart;

/// <summary>What <see cref="MokaChartPlot" /> draws around a row chart's rows: the value axis on top and the rows' box.</summary>
/// <param name="Scale">The value axis, or null when it counts in time.</param>
/// <param name="ValueTicks">The value axis's ticks, start first, for the grid.</param>
/// <param name="Axis">The value axis's labels along the top.</param>
/// <param name="RowCount">How many rows.</param>
/// <param name="RowHeight">Each row's height in pixels, exact, as <c>Virtualize</c> needs it.</param>
/// <param name="Virtualize">Whether rows are drawn only as they scroll into view.</param>
/// <param name="MaxHeight">The height the rows scroll in, as a CSS length, or null.</param>
/// <param name="Grid">The lines behind the rows.</param>
/// <param name="NameEm">The longest name's estimated width, in em, which sizes the name column of virtualized rows.</param>
/// <param name="FigureEm">The longest figure's estimated width, in em, likewise.</param>
/// <param name="HasFigure">Whether any row reads something at its end.</param>
/// <param name="NamesAboveBelow">The chart width, in em, below which names move above their rows; null to keep them beside.</param>
/// <param name="ShowValueAxis">Whether the value axis's labels are drawn.</param>
/// <param name="ValueTitle">The value axis's title.</param>
/// <param name="Annotations">The thresholds and bands, drawn down the rows.</param>
/// <param name="ValueTemplate">Draws each value tick, when the value axis has a template.</param>
/// <param name="BaselineShare">Where the baseline runs, or NaN on a time axis.</param>
internal sealed record RowPlotModel(
	MokaScale? Scale,
	IReadOnlyList<PlotTick> ValueTicks,
	PlotAxis Axis,
	int RowCount,
	double RowHeight,
	bool Virtualize,
	string? MaxHeight,
	MokaChartGrid Grid,
	double NameEm,
	double FigureEm,
	bool HasFigure,
	int? NamesAboveBelow,
	bool ShowValueAxis,
	string? ValueTitle,
	PlotAnnotations Annotations,
	RenderFragment<MokaAxisTick>? ValueTemplate,
	double BaselineShare);
