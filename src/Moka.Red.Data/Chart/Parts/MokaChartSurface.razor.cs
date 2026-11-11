using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Shared chart chrome: the interaction surface over a plot. The chart's script draws the crosshair, the dots, the
///     brush and the pin line in its empty overlay and finds the point under the pointer; the surface draws the keyboard
///     grid, the tooltip, the pin card, the live chip and a status region through a host that renders alone when the
///     point changes. It sits over the plot box, which is its containing block. It draws only in an interactive render of
///     a chart that needs it, and nothing without a chart.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class MokaChartSurface : ComponentBase
{
	private RenderFragment<ChartActiveState>? _hostContent;

	/// <summary>The chart this surface belongs to.</summary>
	[Parameter]
	public MokaChartBase? Chart { get; set; }

	/// <summary>The panel it covers.</summary>
	[Parameter]
	public int Panel { get; set; }

	private string VersionText => Number(Chart?.GeometryVersion ?? 0);

	private string PanelText => Number(Panel);

	// Handlers here change the chart, which redraws what it needs; the surface never renders for an event.
	Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

	private Task UnpinClicked() => Chart?.UnpinAsync(Panel) ?? Task.CompletedTask;

	private static string SurfaceClass(MokaChartBase chart) => new CssBuilder("moka-chart-surface")
		.AddClass($"moka-chart-surface--{ModeName(chart.SurfaceMode)}")
		.AddClass("moka-chart-surface--rows", chart.SurfaceRows)
		.AddClass("moka-chart-surface--hit", chart.HitsMarks)
		.AddClass("moka-chart-surface--brush", chart.ZoomGestures.HasFlag(MokaChartZoom.Brush))
		.Build();

	private string? ExtentFrom(MokaChartBase chart) => chart.XExtent(Panel) is { } extent ? Number(extent.From) : null;

	private string? ExtentTo(MokaChartBase chart) => chart.XExtent(Panel) is { } extent ? Number(extent.To) : null;

	private string? ColumnCount(MokaChartBase chart) =>
		chart.SurfaceRows ? "1" : chart.DrawnCount(Panel) is var count and > 0 ? Number(count) : null;

	private string? CellIdOf(MokaChartBase chart, ChartActiveState state) =>
		state.Info is null ? null : chart.CellId(Panel, state.Series, state.Drawn);

	private static int RowCount(MokaChartBase chart, IReadOnlyList<ChartSeriesInfo> series) =>
		chart.SurfaceMode == MokaChartTooltip.Column ? 1 : Math.Max(1, series.Count);

	private static int CellRow(MokaChartTooltip mode, ChartActiveState state) =>
		mode == MokaChartTooltip.Column ? 0 : Math.Max(0, state.Series);

	private static bool HasTip(MokaChartTooltip mode) =>
		mode is MokaChartTooltip.Crosshair or MokaChartTooltip.Nearest or MokaChartTooltip.Column;

	private static string TipClass(MokaChartTooltip mode) => new CssBuilder("moka-chart-tip")
		.AddClass($"moka-chart-tip--{ModeName(mode)}")
		.Build();

	private static string ModeName(MokaChartTooltip mode) => mode switch
	{
		MokaChartTooltip.Crosshair => "crosshair",
		MokaChartTooltip.Nearest => "nearest",
		MokaChartTooltip.Readout => "readout",
		MokaChartTooltip.Column => "column",
		_ => "none"
	};

	private static IEnumerable<ChartActiveRow> ShownRows(IReadOnlyList<ChartActiveRow> rows, bool focusOnly) =>
		focusOnly && rows.Any(row => row.Emphasis) ? rows.Where(row => row.Emphasis) : rows;

	private static string RowClass(ChartActiveRow row) => new CssBuilder("moka-chart-tip__row")
		.AddClass("moka-chart-tip__row--focus", row.Emphasis)
		.Build();

	private static string KeyClass(ChartActiveRow row, bool box) => new CssBuilder("moka-chart-tip__key")
		.AddClass("moka-chart-tip__key--box", box)
		.AddClass(row.ColorClass)
		.Build();

	private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

	private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
