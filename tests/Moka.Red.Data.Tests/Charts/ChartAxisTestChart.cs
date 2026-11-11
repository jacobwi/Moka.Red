using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     A cartesian chart built only for the axis, row and annotation tests, through InternalsVisibleTo, since the real
///     charts draw on <see cref="MokaChartPlot" /> in their own packages. It draws the plot around an empty svg, or rows
///     of plain bars, and answers the plot's questions from its parameters.
/// </summary>
public sealed class ChartAxisTestChart : MokaCartesianChartBase
{
	/// <summary>The values, one per category or row.</summary>
	[Parameter]
	public IReadOnlyList<double>? Values { get; set; }

	/// <summary>Whether it draws rows, names down and values across.</summary>
	[Parameter]
	public bool Rows { get; set; }

	/// <summary>Whether categories sit in the middle of equal slots, as bars do, rather than at points from end to end.</summary>
	[Parameter]
	public bool Slots { get; set; }

	/// <summary>What each row reads at its end.</summary>
	[Parameter]
	public IReadOnlyList<string>? Figures { get; set; }

	/// <summary>The x range of a numeric x axis.</summary>
	[Parameter]
	public (double Min, double Max)? NumberRange { get; set; }

	/// <summary>A time span for the value axis of rows, as a gantt chart has.</summary>
	[Parameter]
	public (DateTimeOffset From, DateTimeOffset To)? TimeSpanAcross { get; set; }

	/// <summary>How many times the geometry was rebuilt.</summary>
	public int Rebuilds { get; private set; }

	/// <summary>The frame the chart worked out during its last rebuild.</summary>
	internal ChartXFrame? FrameAtRebuild { get; private set; }

	/// <summary>The chart as the base sees it.</summary>
	internal MokaCartesianChartBase Base => this;

	/// <inheritdoc />
	protected override string RootClass => "test-axis-chart";

	internal override bool HasData => Values is { Count: > 0 };

	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => [];

	internal override bool DrawsRows => Rows;

	internal override int CategoryCount => Labels?.Count ?? Values?.Count ?? 0;

	internal override int RowCount => CategoryCount;

	internal override (DateTimeOffset From, DateTimeOffset To)? RowTimeRange => TimeSpanAcross;

	internal override void Rebuild()
	{
		Rebuilds++;
		FrameAtRebuild = CreateXFrame();
	}

	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn) => null;

	internal override (double Min, double Max) ValueRange(int panel) =>
		Values is { Count: > 0 } values ? (values.Where(double.IsFinite).DefaultIfEmpty(double.NaN).Min(),
			values.Where(double.IsFinite).DefaultIfEmpty(double.NaN).Max()) : (double.NaN, double.NaN);

	internal override double CategoryShare(int index, int count) =>
		Slots ? (index + 0.5) / Math.Max(1, count) : base.CategoryShare(index, count);

	internal override string? RowFigure(int index) => Figures is not null && index < Figures.Count ? Figures[index] : null;

	internal override (double Min, double Max)? NumericXRange(int panel) => NumberRange;

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", CssClass);
		builder.AddAttribute(2, "style", CssStyle);
		builder.AddElementReferenceCapture(3, element => RootElement = element);
		builder.OpenComponent<MokaChartPlot>(4);
		builder.AddComponentParameter(5, nameof(MokaChartPlot.Chart), this);
		if (Rows)
		{
			builder.AddComponentParameter(6, nameof(MokaChartPlot.Row), (RenderFragment<int>)(row => bar =>
			{
				double value = Values is not null && row < Values.Count ? Values[row] : 0;
				bar.OpenElement(0, "span");
				bar.AddAttribute(1, "class", "test-bar");
				bar.AddAttribute(2, "style", FormattableString.Invariant($"width:{RowShare(value) * 100:0.##}%"));
				bar.CloseElement();
			}));
		}
		else
		{
			builder.AddComponentParameter(7, nameof(MokaChartPlot.Marks), (RenderFragment)(marks =>
			{
				marks.OpenElement(0, "svg");
				marks.AddAttribute(1, "class", "test-marks");
				marks.CloseElement();
			}));
		}

		builder.CloseComponent();
		builder.CloseElement();
	}

	private double RowShare(double value) => RowPlotModel().Scale?.Share(value) is { } share && double.IsFinite(share)
		? Math.Clamp(share, 0, 1)
		: 0;

	/// <summary>Labels "A1" to "An", or whatever <paramref name="text" /> makes of each position.</summary>
	public static string[] Names(int count, Func<int, string>? text = null) =>
		[.. Enumerable.Range(0, count).Select(i => text?.Invoke(i) ?? $"A{(i + 1).ToString(CultureInfo.InvariantCulture)}")];
}
