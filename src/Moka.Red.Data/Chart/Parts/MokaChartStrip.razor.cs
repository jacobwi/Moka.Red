using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.RangeBar;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Shared chart chrome: the range strip, a range bar over an overview of the whole series that zooms the chart along
///     its x axis. The plot draws it under its plot box, so its track lines up with the marks; the frame draws it under
///     the body of a chart with more than one plot. On a time axis the overview places each value at its time. Not meant to
///     be used on its own, and draws nothing without a chart that has a strip.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class MokaChartStrip : ComponentBase
{
	/// <summary>The chart this strip zooms.</summary>
	[Parameter]
	public MokaChartBase? Chart { get; set; }

	private ChartSeriesInfo? OverviewSeries => Chart?.SeriesInfo.FirstOrDefault(series => !series.Hidden);

	private string OverviewClass => new CssBuilder("moka-chart-strip__overview-host")
		.AddClass(OverviewSeries is { } series ? MokaChartBase.SeriesClass(series.Slot, null, false) : "moka-chart-c1")
		.Build();

	// The strip changes the chart, which redraws itself; the strip never renders for an event.
	Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

	private double Start(MokaChartBase.ChartStrip strip) =>
		Chart?.CurrentRange is { } range ? Math.Clamp(Math.Min(range.From.Value, range.To.Value), strip.Min, strip.Max) : strip.Min;

	private double End(MokaChartBase.ChartStrip strip) =>
		Chart?.CurrentRange is { } range ? Math.Clamp(Math.Max(range.From.Value, range.To.Value), strip.Min, strip.Max) : strip.Max;

	private Task Changed(MokaRange range) => Chart?.ChooseRangeAsync(range.Start, range.End) ?? Task.CompletedTask;

	private string Text(double value) => Chart?.StripText(value) ?? value.ToString(CultureInfo.CurrentCulture);
}
