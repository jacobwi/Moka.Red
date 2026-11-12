using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A category bar chart: one or more named series as bars, side by side or stacked, over a common value axis, in
///     columns or, with <see cref="Horizontal" />, in rows. Shares of a whole (<see cref="Percent" />), bars either side of
///     zero (<see cref="Diverging" />), a running total built from changes (<see cref="Waterfall" />), sorting, printed
///     values, a target band and emphasis are all choices; so are the chart looks, the legend, the tooltip and the keyboard.
/// </summary>
/// <remarks>
///     Left to its defaults on the Standard look it draws exactly what it always has: an svg of bars with native
///     tooltips. Any of the newer choices draws it in HTML instead, which gives exact bar caps and gaps, hatching, values
///     printed where they fit, and the shared chart frame, tooltip and keyboard. Past <see cref="MaxColumns" />
///     categories, or more than the plot has room for, columns are merged; rows draw only as they scroll into view.
/// </remarks>
public partial class MokaBarChart : MokaCartesianChartBase
{
	// The plot's own svg covers the viewBox from here right and from the top down to the x-axis, and stretches to fill
	// its cell. The tick labels are HTML beside and under it, so text never stretches with it.
	private const double PlotLeft = PadLeft;
	private const double PlotBoxWidth = ViewWidth - PlotLeft;
	private const double PlotBoxHeight = ViewHeight - PadBottom;

	private IReadOnlyList<MokaBarSeries> _visible = [];
	private int _categoryCount;
	private IReadOnlyList<YTick> _grid = [];
	private string _widestTick = string.Empty;
	private IReadOnlyList<XTick> _xLabels = [];
	private IReadOnlyList<BarGeometry> _bars = [];
	private IReadOnlyList<SeriesLegend> _legend = [];
	private MokaBarSeries[] _seriesSnapshot = [];
	private string[]? _labelsSnapshot;
	private DateTimeOffset[]? _timesSnapshot;
	private IReadOnlyList<ChartSeriesInfo> _seriesInfo = [];
	private bool _classic = true;

	/// <summary>The series to draw. Null or empty renders the axes alone.</summary>
	[Parameter]
	public IReadOnlyList<MokaBarSeries>? Series { get; set; }

	/// <summary>Whether the series stack onto one bar per category instead of standing side by side.</summary>
	[Parameter]
	public bool Stacked { get; set; }

	/// <summary>
	///     Whether the bars run across, one row per category, with the names whole beside them, or above them in a narrow
	///     chart. Long lists draw only the rows in view.
	/// </summary>
	[Parameter]
	public bool Horizontal { get; set; }

	/// <summary>Whether each category is drawn as one whole split into its series' shares, so categories compare by their mix. It stacks the series.</summary>
	[Parameter]
	public bool Percent { get; set; }

	/// <summary>
	///     Whether bars grow either side of a zero line in the up and down colors, by their sign rather than their series:
	///     faster or slower, over or under budget.
	/// </summary>
	[Parameter]
	public bool Diverging { get; set; }

	/// <summary>
	///     With <see cref="Diverging" /> or <see cref="Waterfall" />, whether a value above zero is the good direction. False
	///     draws values above zero in the down color, for a change in latency or cost. Defaults to true.
	/// </summary>
	[Parameter]
	public bool UpIsGood { get; set; } = true;

	/// <summary>
	///     With <see cref="Diverging" />, what the legend and the tooltip call a value above zero. Defaults to "Above zero";
	///     with <see cref="Waterfall" />, to <see cref="MokaChartText.Increase" />.
	/// </summary>
	[Parameter]
	public string? PositiveText { get; set; }

	/// <summary>
	///     With <see cref="Diverging" />, what the legend and the tooltip call a value below zero. Defaults to "Below zero";
	///     with <see cref="Waterfall" />, to <see cref="MokaChartText.Decrease" />.
	/// </summary>
	[Parameter]
	public string? NegativeText { get; set; }

	/// <summary>
	///     Whether the first series is drawn as a waterfall, from where it starts to where it ends up: each value is a
	///     change, and its bar floats from the running total before it to the running total after it, in the up color for
	///     an increase and the down color for a decrease, with a hairline joining each bar to the next. It wins over
	///     <see cref="Stacked" />, <see cref="Percent" />, <see cref="Diverging" /> and <see cref="Sort" />, and other
	///     series aren't drawn.
	/// </summary>
	[Parameter]
	public bool Waterfall { get; set; }

	/// <summary>
	///     With <see cref="Waterfall" />, the positions, from 0, drawn as totals: a bar from zero to the value there, read as
	///     a level rather than a change, so the changes after it count on from it. <c>double.NaN</c> at a total's position
	///     draws the running total instead, so a subtotal can be left to the chart. A start value is a total at 0.
	/// </summary>
	[Parameter]
	public IReadOnlyList<int>? TotalsAt { get; set; }

	/// <summary>With <see cref="Waterfall" />, whether a last bar shows where the changes end up, from zero to the running total.</summary>
	[Parameter]
	public bool ShowTotal { get; set; }

	/// <summary>With <see cref="ShowTotal" />, the last bar's category name. Defaults to <see cref="MokaChartText.Total" />.</summary>
	[Parameter]
	public string? TotalLabel { get; set; }

	/// <summary>The order categories are drawn in, by their total. Null sorts largest first when <see cref="Horizontal" />, else keeps the order given.</summary>
	[Parameter]
	public MokaChartSort? Sort { get; set; }

	/// <summary>Where values are printed. Null takes what suits the chart and the look.</summary>
	[Parameter]
	public MokaValueLabels? ValueLabels { get; set; }

	/// <summary>How the bars' ends are drawn. Null takes the look's: rounded at the data end.</summary>
	[Parameter]
	public MokaBarEnds? BarEnds { get; set; }

	/// <summary>
	///     The most columns drawn. More categories than this, or than the plot has room for at three pixels a column, are
	///     merged into buckets by <see cref="ColumnAggregate" />; <c>LabelFit="MokaAxisLabelFit.Scroll"</c> scrolls them
	///     instead. Rows are never merged. A <see cref="Waterfall" /> adds up the changes it merges and never merges a total.
	///     Defaults to 400.
	/// </summary>
	[Parameter]
	public int MaxColumns { get; set; } = 400;

	/// <summary>How merged columns combine their values. Defaults to the sum.</summary>
	[Parameter]
	public MokaChartAggregate ColumnAggregate { get; set; }

	/// <summary>
	///     Writes a bar's share of the whole for the tooltip, given the share and the total, such as
	///     <c>(share, total) =&gt; $"{share} of the {total} online"</c>. Defaults to "38% of 104".
	/// </summary>
	[Parameter]
	public Func<string, string, string>? ShareText { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-barchart";

	/// <inheritdoc />
	protected override string CssClass => _classic
		? new CssBuilder(RootClass)
			.AddClass($"moka-barchart--{SizeToKebab(Size)}")
			.AddClass(Class)
			.Build()
		: new CssBuilder(RootClass)
			.AddClass($"moka-barchart--{SizeToKebab(Size)}")
			.AddClass("moka-barchart--modern")
			.AddClass("moka-barchart--horizontal", Horizontal)
			.AddClass(ModernRootClasses)
			.AddClass(Class)
			.Build();

	private static string PlotViewBox => $"{Num(PlotLeft)} 0 {Num(PlotBoxWidth)} {Num(PlotBoxHeight)}";

	private string? FrameStyle => new StyleBuilder()
		.AddStyle("height", Height)
		.Build();

	private string FrameClass => new CssBuilder("moka-barchart-frame")
		.AddClass("moka-barchart-frame--fixed", FrameStyle is not null)
		.AddClass("moka-barchart-frame--no-x", !ShowXLabels)
		.Build();

	private sealed record BarGeometry(string Path, string ColorClass, string Title);
	private sealed record SeriesLegend(string Name, string ColorClass);

	/// <summary>A grid line's y in the viewBox, and its label placed at the same height, as a share of the plot's.</summary>
	private sealed record YTick(string Y, string? Style, string Text);

	/// <summary>A category label placed under its slot, as a share of the plot's width, with its classes.</summary>
	private sealed record XTick(string? Style, string Text, string Class);

	/// <inheritdoc />
	internal override bool IsClassic => _classic;

	/// <inheritdoc />
	internal override string DefaultLabel => _classic || _modernInfo.Count == 0
		? "Bar chart"
		: _stepSeries is { } steps
			? $"Waterfall chart: {steps.Name}"
			: $"Bar chart: {string.Join(", ", _modernInfo.Where(s => !s.Hidden).Select(s => s.Name))}";

	/// <inheritdoc />
	internal override bool HasData => _classic ? _categoryCount > 0 : _rawCount > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _classic ? _seriesInfo : _modernInfo;

	/// <inheritdoc />
	internal override int CategoryCount => _classic ? _categoryCount : _columnCount;

	// The series, the labels and the times are compared by value with what was last drawn, so a list changed in place
	// still draws again and an unchanged one costs no render.
	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name switch
	{
		nameof(Series) => SameSeries(after as IReadOnlyList<MokaBarSeries>),
		nameof(Labels) => ChartSnapshot.SameItems(after as IReadOnlyList<string>, _labelsSnapshot),
		nameof(Times) => ChartSnapshot.SameItems(after as IReadOnlyList<DateTimeOffset>, _timesSnapshot),
		_ => base.SameValue(name, before, after)
	};

	/// <inheritdoc />
	internal override bool ComparesItems(string name) => name == nameof(TotalsAt) || base.ComparesItems(name);

	/// <inheritdoc />
	internal override void Rebuild()
	{
		bool versioned = DataVersion is not null;
		_seriesSnapshot = versioned ? [] : [.. (Series ?? []).Select(s => s with { Values = s.Values.ToArray(), Colors = s.Colors?.ToArray() })];
		_labelsSnapshot = versioned ? null : Labels?.ToArray();
		_timesSnapshot = versioned ? null : Times?.ToArray();
		_seriesHaveOwnColors = (Series ?? []).Any(s => s.Key is not null || s.Slot is not null || s.ColorValue is not null
		                                             || s.Colors is not null);

		_classic = ClassicNow();
		if (_classic)
		{
			ClearModern();
			RebuildClassic();
			return;
		}

		ClearClassic();
		RebuildModern();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Chrome such as a title doesn't move the geometry, but it does take the chart off the classic path.
		if (ClassicNow() != _classic)
		{
			Redraw();
		}
	}

	private void RebuildClassic()
	{
		_visible = [.. (Series ?? []).Where(s => s.Values.Count > 0)];
		_categoryCount = _visible.Count > 0 ? _visible.Max(s => s.Values.Count) : 0;

		(double min, double max) = YDomain();
		IReadOnlyList<double> ticks = Ticks(min, max);
		string[] tickTexts = [.. ticks.Select(tick => tick.ToString(TickFormat, CultureInfo.CurrentCulture))];
		_widestTick = tickTexts.Length == 0 ? string.Empty : tickTexts.MaxBy(t => t.Length)!;
		_grid = _categoryCount > 0
			? [.. ticks.Select((tick, i) =>
			{
				double y = Y(tick, min, max);
				return new YTick(Num(y), Placed("top", y / PlotBoxHeight), tickTexts[i]);
			})]
			: [];

		double slot = _categoryCount > 0 ? PlotWidth / _categoryCount : 0;
		_xLabels =
		[
			.. AxisLabels(_categoryCount, i => slot * (i + 0.5) / PlotBoxWidth, "moka-barchart-xtick")
				.Select(label => new XTick(
					Placed("left", label.Share),
					label.Text,
					new CssBuilder("moka-barchart-tick moka-barchart-xtick").AddClass(label.WidthClass).Build()))
		];
		_bars = BarsFor(min, max);
		_legend = [.. _visible.Select((series, i) => new SeriesLegend(series.Name, MokaChartRotation.ClassFor(series.Color, i)))];
		_seriesInfo = [.. _legend.Select((entry, i) => new ChartSeriesInfo(entry.Name, entry.Name, i + 1, entry.ColorClass, null,
			ChartMark.Box, false, false, null))];
	}

	private void ClearClassic()
	{
		_visible = [];
		_categoryCount = 0;
		_grid = [];
		_widestTick = string.Empty;
		_xLabels = [];
		_bars = [];
		_legend = [];
		_seriesInfo = [];
	}

	// What the chart always drew stays exactly as it was until something asks for more: a look, a palette, texture, a
	// newer parameter, chrome, a tooltip or the keyboard.
	private bool ClassicNow() =>
		ResolvedLook == MokaChartLook.Standard && !Textured && Print == MokaChartPrint.AsShown
		&& (ChartTheme is not { Look: not MokaLook.Matrix } theme || theme.ChartPalette == MokaChartPalette.Standard)
		&& !Horizontal && !Percent && !Diverging && Sort is null && ValueLabels is null && BarEnds is null && MaxColumns == 400
		&& ColumnAggregate == MokaChartAggregate.Sum && ShareText is null
		&& !Waterfall && TotalsAt is null && !ShowTotal && TotalLabel is null
		&& Title is null && Subtitle is null && TitleContent is null && Actions is null && Colors is null && Texture is null
		&& Legend is null && LegendKey is null && LegendActions is null && HiddenSeries is null && Highlight is null
		&& HighlightSeries is null && Tooltip is null && TooltipTemplate is null && Pinnable is null && Keyboard is null
		&& SyncGroup is null && !OnPointClick.HasDelegate && !ActiveChanged.HasDelegate && Controls is null
		&& ToolbarControl is null && View == MokaChartView.Chart && State == MokaChartState.Auto && EmptyText is null
		&& EmptyContent is null && ErrorContent is null && ErrorText is null && !OnRetry.HasDelegate && ExportName is null
		&& Formatter is null && Times is null && XAxis is null && YAxis is null && Unit is null && LabelFit is null
		&& Grid is null && TickLabels is null && ValueSide is null && Annotations is null && TargetLow is null
		&& TargetHigh is null && Range is null && Zoom is null && RangePresets is null && Virtualize is null
		&& RowHeight is null && MaxHeight is null && !_seriesHaveOwnColors;

	private bool SameSeries(IReadOnlyList<MokaBarSeries>? series)
	{
		int count = series?.Count ?? 0;
		if (count != _seriesSnapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < count; i++)
		{
			MokaBarSeries current = series![i];
			MokaBarSeries snapshot = _seriesSnapshot[i];
			if (current.Name != snapshot.Name || current.Color != snapshot.Color || current.Key != snapshot.Key
			    || current.Slot != snapshot.Slot || current.ColorValue != snapshot.ColorValue
			    || !ChartSnapshot.SameItems(current.Values, snapshot.Values) || !ChartSnapshot.SameItems(current.Colors, snapshot.Colors))
			{
				return false;
			}
		}

		return true;
	}

	// A label's place as a percentage of the plot box, which is what the stretched plot keeps.
	private static string? Placed(string side, double share) => new StyleBuilder()
		.AddStyle(side, FormattableString.Invariant($"{share * 100:0.###}%"))
		.Build();

	// Stacked bars are as tall as the category's totals, so they size the axis, not each value.
	private (double Min, double Max) YDomain()
	{
		if (_visible.Count == 0 || _categoryCount == 0)
		{
			return (0, 1);
		}

		if (!Stacked)
		{
			return Domain([.. _visible.SelectMany(s => s.Values)]);
		}

		List<double> totals = [];
		for (int category = 0; category < _categoryCount; category++)
		{
			double positive = 0;
			double negative = 0;
			foreach (MokaBarSeries series in _visible)
			{
				if (category >= series.Values.Count)
				{
					continue;
				}

				double value = series.Values[category];
				if (value >= 0)
				{
					positive += value;
				}
				else
				{
					negative += value;
				}
			}

			totals.Add(positive);
			totals.Add(negative);
		}

		return Domain(totals);
	}

	private List<BarGeometry> BarsFor(double min, double max)
	{
		List<BarGeometry> bars = [];
		if (_visible.Count == 0 || _categoryCount == 0)
		{
			return bars;
		}

		double zero = Y(0, min, max);
		double slot = PlotWidth / _categoryCount;

		for (int category = 0; category < _categoryCount; category++)
		{
			if (Stacked)
			{
				double width = slot * 0.55;
				double x = PadLeft + slot * category + (slot - width) / 2;
				double positiveBase = 0;
				double negativeBase = 0;

				for (int s = 0; s < _visible.Count; s++)
				{
					MokaBarSeries series = _visible[s];
					if (category >= series.Values.Count)
					{
						continue;
					}

					double value = series.Values[category];
					if (value == 0)
					{
						continue;
					}

					double from = value >= 0 ? positiveBase : negativeBase;
					double to = from + value;
					if (value >= 0)
					{
						positiveBase = to;
					}
					else
					{
						negativeBase = to;
					}

					bars.Add(new BarGeometry(
						BarPath(x, Y(to, min, max), width, Y(from, min, max)),
						MokaChartRotation.ClassFor(series.Color, s),
						BarTitle(series, category)));
				}
			}
			else
			{
				int drawn = _visible.Count;
				double groupWidth = slot * 0.7;
				double barWidth = groupWidth / drawn;
				double x = PadLeft + slot * category + (slot - groupWidth) / 2;

				for (int s = 0; s < drawn; s++)
				{
					MokaBarSeries series = _visible[s];
					if (category >= series.Values.Count)
					{
						continue;
					}

					double value = series.Values[category];
					bars.Add(new BarGeometry(
						BarPath(x + s * barWidth, Y(value, min, max), barWidth, zero),
						MokaChartRotation.ClassFor(series.Color, s),
						BarTitle(series, category)));
				}
			}
		}

		return bars;
	}

	// A bar with rounded ends on the side it points to: the top for positive values, the bottom for
	// negative ones, and the plain edge against the axis either way.
	private static string BarPath(double x, double top, double width, double bottom)
	{
		double height = Math.Abs(bottom - top);
		double radius = Math.Clamp(Math.Min(3, width / 2), 0, height);
		string right = Num(x + width);

		if (bottom >= top)
		{
			return string.Create(CultureInfo.InvariantCulture,
				$"M {Num(x)} {Num(bottom)} L {Num(x)} {Num(top + radius)} Q {Num(x)} {Num(top)} {Num(x + radius)} {Num(top)} " +
				$"L {Num(x + width - radius)} {Num(top)} Q {right} {Num(top)} {right} {Num(top + radius)} L {right} {Num(bottom)} Z");
		}

		return string.Create(CultureInfo.InvariantCulture,
			$"M {Num(x)} {Num(top)} L {Num(x)} {Num(bottom - radius)} Q {Num(x)} {Num(bottom)} {Num(x + radius)} {Num(bottom)} " +
			$"L {Num(x + width - radius)} {Num(bottom)} Q {right} {Num(bottom)} {right} {Num(bottom - radius)} L {right} {Num(top)} Z");
	}

	private string BarTitle(MokaBarSeries series, int index) =>
		Labels is not null && index < Labels.Count && !string.IsNullOrWhiteSpace(Labels[index])
			? $"{Labels[index]} · {series.Name}: {series.Values[index].ToString(TickFormat, CultureInfo.CurrentCulture)}"
			: $"{series.Name}: {series.Values[index].ToString(TickFormat, CultureInfo.CurrentCulture)}";
}
