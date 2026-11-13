using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A category line chart: one or more named series drawn as polylines over a common y-axis,
///     with grid lines, tick labels and a legend. The plot fills its box's width at the box's height, the labels and
///     dots keep their shape however wide that is, and the category labels thin out as the chart narrows. It also
///     draws a live window of samples, such as a transfer rate fed once a second: see <see cref="Live" />.
/// </summary>
public partial class MokaLineChart : MokaCartesianChartBase
{
	private const int MaxTicks = 50;

	// Past this many values a classic series draws the lowest and highest value of each of ClassicBuckets runs, as
	// every chart now does, rather than a path vertex and a dot per value.
	private const int ClassicReduceAbove = 4000;
	private const int ClassicBuckets = 800;

	// The plot's own svg covers the viewBox from here right and from the top down to the x-axis, and stretches to fill
	// its cell. The tick labels are HTML beside and under it, so text never stretches with it.
	private const double PlotLeft = PadLeft;
	private const double PlotSpan = ViewWidth - PlotLeft - PadRight;
	private const double PlotBoxWidth = PlotSpan + PadRight;
	private const double PlotBoxHeight = ViewHeight - PadBottom;

	private readonly string _clipId = $"moka-linechart-clip-{Guid.NewGuid():N}";

	private static readonly HashSet<string> ClassicDeciding = new(StringComparer.Ordinal)
	{
		nameof(Title), nameof(Subtitle), nameof(TitleContent), nameof(Actions), nameof(Controls), nameof(State), nameof(View)
	};

	private MokaLineSeries[] _seriesSnapshot = [];
	private string[]? _labelsSnapshot;
	private double[]? _unitsSnapshot;
	private DateTimeOffset[]? _timesSnapshot;
	private bool _classic = true;
	private long? _builtVersion;
	private bool _valuesChanged;
	private bool _built;
	private IReadOnlyList<ChartSeriesInfo> _seriesInfo = [];

	private IReadOnlyList<MokaLineSeries> _visible = [];
	private int _categoryCount;
	private bool _hasValues;
	private IReadOnlyList<YTick> _grid = [];
	private IReadOnlyList<XTick> _xLabels = [];
	private string _widestTick = string.Empty;
	private IReadOnlyList<SeriesGeometry> _geometry = [];
	private IReadOnlyList<PointGeometry> _lastPoints = [];
	private IReadOnlyList<LegendEntry> _legend = [];
	private string? _peakText;
	private string _summary = "Line chart";
	private string? _slideClass;
	private string? _slideStyle;

	/// <summary>
	///     The series to draw. Null or empty renders the axes alone. A <c>double.NaN</c> in a series is a gap: its
	///     line and area break there and no point is drawn.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaLineSeries>? Series { get; set; }

	/// <summary>Whether a dot marks every value. Defaults to true. Turn it off for a live window of many samples.</summary>
	[Parameter]
	public bool ShowPoints { get; set; } = true;

	/// <summary>Whether the area between each line and the x-axis is filled. Defaults to false.</summary>
	[Parameter]
	public bool ShowArea { get; set; }

	/// <summary>Whether the legend shows each series' latest value beside its name. Defaults to false.</summary>
	[Parameter]
	public bool ShowCurrent { get; set; }

	/// <summary>Whether the legend ends with the highest value any series reaches. Defaults to false.</summary>
	[Parameter]
	public bool ShowPeak { get; set; }

	/// <summary>The word before the peak in the legend. Defaults to "Peak".</summary>
	[Parameter]
	public string PeakLabel { get; set; } = "Peak";

	/// <summary>
	///     Rounds the ends of the y-axis out to a step of 1, 2, 2.5 or 5 times a power of ten, so every tick is a round
	///     number. The step gives about <see cref="MokaCartesianChartBase.YTicks" /> divisions. A fixed
	///     <see cref="MokaCartesianChartBase.Min" /> or <see cref="MokaCartesianChartBase.Max" /> stays as set. Defaults to false.
	/// </summary>
	[Parameter]
	public bool AutoScale { get; set; }

	/// <summary>
	///     With <see cref="AutoScale" />, the units <see cref="MokaChartBase.Formatter" /> writes values in, such as 1, 1024 and
	///     1048576 for B/s, KiB/s and MiB/s. The step is rounded in the largest unit the top of the axis reaches, so
	///     the ticks read 0.5 MiB/s rather than 524288 B/s. Null rounds in plain numbers.
	/// </summary>
	[Parameter]
	public IReadOnlyList<double>? ScaleUnits { get; set; }

	/// <summary>
	///     The least the top of the y-axis reaches when <see cref="MokaCartesianChartBase.Max" /> isn't set. Higher values
	///     still raise it. Keeps an idle chart from stretching noise to full height.
	/// </summary>
	[Parameter]
	public double? SuggestedMax { get; set; }

	/// <summary>Whether lines are drawn as smooth curves rather than straight segments. Defaults to false.</summary>
	[Parameter]
	public bool Smooth { get; set; }

	/// <summary>Whether a ringed dot marks each series' latest value. Defaults to false.</summary>
	[Parameter]
	public bool ShowLastPoint { get; set; }

	/// <summary>
	///     For a rolling window of samples: when the values change and the count stays the same, the lines slide one
	///     step left over <see cref="LiveInterval" /> instead of jumping. Pass the whole window each time, padded at
	///     the start with <c>double.NaN</c> until it fills. Defaults to false.
	/// </summary>
	[Parameter]
	public bool Live { get; set; }

	/// <summary>How long the slide between two samples takes with <see cref="Live" />. Defaults to one second.</summary>
	[Parameter]
	public TimeSpan LiveInterval { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	///     Writes the start of a point's tooltip for a category index, such as the full time of a sample when the axis
	///     labels are short. An empty string leaves the start off. Null uses <see cref="MokaCartesianChartBase.Labels" />.
	/// </summary>
	[Parameter]
	public Func<int, string>? TooltipLabel { get; set; }

	/// <summary>
	///     Stacks the series so the top edge is their total and each band is one part, such as bandwidth by zone. The
	///     tooltip lists each series and the total. A gap counts as a gap in its own band and as nothing for the bands
	///     above it. Defaults to false.
	/// </summary>
	[Parameter]
	public bool Stacked { get; set; }

	/// <summary>
	///     How the lines run between their values: straight, smooth, or held flat until the next value, for counts that
	///     jump rather than drift. A series' own <see cref="MokaLineSeries.Curve" /> wins. Null takes the look's, or
	///     <see cref="Smooth" />.
	/// </summary>
	[Parameter]
	public MokaLineCurve? Curve { get; set; }

	/// <summary>
	///     Where dots are drawn: nowhere, where you point and at each line's end, or on every value. Past 400 drawn
	///     values a line gets dots only where you point. Null takes the look's; <see cref="ShowPoints" /> off means none.
	/// </summary>
	[Parameter]
	public MokaChartPoints? Points { get; set; }

	/// <summary>
	///     How the area under each line is filled: a flat tint, or a tint that fades to nothing at the baseline. Null fills
	///     with the look's tint when <see cref="ShowArea" /> is on.
	/// </summary>
	[Parameter]
	public MokaChartArea? Area { get; set; }

	/// <summary>
	///     Names, latest values or both written beside each line's end, in text colors, with a short leader where ends
	///     crowd. Labels that can't fit hide and leave the legend to name the lines, as they do on a narrow chart. Null
	///     takes the look's for up to four lines, or names the highlighted series when <see cref="MokaChartBase.Highlight" />
	///     is set.
	/// </summary>
	[Parameter]
	public MokaDirectLabels? DirectLabels { get; set; }

	/// <summary>
	///     Draws one small chart per series, all on one scale so their heights compare, with one crosshair moving through
	///     all of them and each one's value in its header. For four or more series, or lines that cross a lot.
	/// </summary>
	[Parameter]
	public bool Split { get; set; }

	/// <summary>
	///     With <see cref="Split" />, the most panels side by side. Null fits as many as the width allows. Panels are never
	///     narrower than about 16em, so a phone shows one column whatever this says.
	/// </summary>
	[Parameter]
	public int? SplitColumns { get; set; }

	/// <summary>
	///     Divides each series by its first value and multiplies by this, usually 100, so measures of very different size
	///     compare by growth on one axis. The tooltip still says each raw value. The honest replacement for a second y
	///     axis.
	/// </summary>
	[Parameter]
	public double? IndexTo { get; set; }

	/// <summary>
	///     With <see cref="MokaCartesianChartBase.Times" />, two samples further apart than this break the line instead of
	///     joining across the time nothing came in.
	/// </summary>
	[Parameter]
	public TimeSpan? BreakGapsOver { get; set; }

	/// <summary>
	///     With <see cref="Live" />, holds the chart still while the pointer or the keyboard focus is on it, so the value
	///     being read doesn't slide away. A chip counts the samples that wait, and the chart catches up when it is let go.
	/// </summary>
	[Parameter]
	public bool PauseWhilePointing { get; set; }

	/// <summary>
	///     A trend drawn over each series as a thin dashed line in its color, listed after it in the tooltip and the table:
	///     its moving average, or the straight line nearest to it. Worked out from all of the series, so a zoom doesn't
	///     move it, and from the values as drawn, so it follows <see cref="IndexTo" />. Not drawn on a stacked chart, whose
	///     bands sit on each other, nor for a comparison. Defaults to none.
	/// </summary>
	[Parameter]
	public MokaChartTrend Trend { get; set; }

	/// <summary>How many values each mean of a <see cref="MokaChartTrend.MovingAverage" /> trend covers, the value itself included. Defaults to 7.</summary>
	[Parameter]
	public int TrendWindow { get; set; } = 7;

	/// <inheritdoc />
	protected override string RootClass => "moka-linechart";

	/// <inheritdoc />
	protected override string CssClass => _classic
		? new CssBuilder(RootClass)
			.AddClass($"moka-linechart--{SizeToKebab(Size)}")
			.AddClass(Class)
			.Build()
		: new CssBuilder(RootClass)
			.AddClass($"moka-linechart--{SizeToKebab(Size)}")
			.AddClass(ModernRootClasses)
			.AddClass("moka-linechart--modern")
			.AddClass("moka-linechart--split", Split)
			.AddClass("moka-linechart--points-none", _modern.Points == MokaChartPoints.None)
			.AddClass(Class)
			.Build();

	private static string PlotViewBox => $"{Num(PlotLeft)} 0 {Num(PlotBoxWidth)} {Num(PlotBoxHeight)}";

	private string? FrameStyle => new StyleBuilder()
		.AddStyle("height", Height)
		.Build();

	private string FrameClass => new CssBuilder("moka-linechart-frame")
		.AddClass("moka-linechart-frame--fixed", FrameStyle is not null)
		.AddClass("moka-linechart-frame--no-x", !ShowXLabels)
		.Build();

	// Geometry leaves the component as strings: every number goes through Num, so an SVG attribute
	// never carries a culture's decimal comma.
	private sealed record PointGeometry(string Cx, string Cy, string Title, string ColorClass);

	/// <summary>A grid line's y in the viewBox, and its label placed at the same height, as a share of the plot's.</summary>
	private sealed record YTick(string Y, string? Style, string Text);

	/// <summary>A category label placed under its point, as a share of the plot's width, with its classes.</summary>
	private sealed record XTick(string? Style, string Text, string Class);

	private sealed record SeriesGeometry(MokaLineSeries Series, string ColorClass, string Line, string? Area,
		IReadOnlyList<PointGeometry> Points);

	private sealed record LegendEntry(string Name, string ColorClass, string? Value);

	/// <inheritdoc />
	internal override bool IsClassic => _classic;

	/// <inheritdoc />
	internal override string DefaultLabel => "Line chart";

	/// <inheritdoc />
	internal override bool HasData => _classic ? _hasValues : _modern.HasValues;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _classic ? _seriesInfo : ModernSeriesInfo;

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn) =>
		_classic ? null : DescribeModern(panel, series, drawn, pinnedDrawn);

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel) =>
		_classic ? ClassicValueRange() : (_modern.ValueMin, _modern.ValueMax);

	private (double Min, double Max) ClassicValueRange()
	{
		double min = double.NaN;
		double max = double.NaN;
		foreach (MokaLineSeries series in _visible)
		{
			foreach (double value in series.Values)
			{
				if (double.IsFinite(value))
				{
					min = double.IsNaN(min) ? value : Math.Min(min, value);
					max = double.IsNaN(max) ? value : Math.Max(max, value);
				}
			}
		}

		return (min, max);
	}

	/// <inheritdoc />
	internal override int CategoryCount => _classic ? _categoryCount : _count;

	// The series, the labels, the times and the units are compared by value with what was last drawn, so a list changed
	// in place still draws again and an unchanged one costs no render.
	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name switch
	{
		nameof(Series) => SameSeries(after as IReadOnlyList<MokaLineSeries>),
		nameof(Labels) => ChartSnapshot.SameItems(after as IReadOnlyList<string>, _labelsSnapshot),
		nameof(ScaleUnits) => ChartSnapshot.SameItems(after as IReadOnlyList<double>, _unitsSnapshot),
		nameof(Times) => ChartSnapshot.SameItems(after as IReadOnlyList<DateTimeOffset>, _timesSnapshot),
		_ => base.SameValue(name, before, after)
	};

	/// <inheritdoc />
	internal override void Rebuild()
	{
		MokaLineSeries[] previous = TakeSnapshot();
		_classic = ClassicNow();
		if (_classic)
		{
			Build(previous);
		}
		else
		{
			BuildModern(previous);
		}

		_built = true;
	}

	/// <inheritdoc />
	/// <remarks>
	///     A listener or a tooltip template doesn't move the geometry, but it does take the chart off the classic path,
	///     and back on it once it goes.
	/// </remarks>
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (_built && ClassicNow() != _classic)
		{
			Redraw();
		}
	}

	// The classic chart rebuilds on any change, as it always has: the live slide is worked out per build, so a build
	// skipped for a class change would leave the last slide's class where the classic markup drops it. The modern one
	// rebuilds for what it draws from, and for the chrome that decides whether it is still modern.
	/// <inheritdoc />
	internal override bool AffectsGeometry(string parameterName) =>
		_classic || ClassicDeciding.Contains(parameterName) || base.AffectsGeometry(parameterName);

	private bool SameSeries(IReadOnlyList<MokaLineSeries>? series)
	{
		int count = series?.Count ?? 0;
		if (count != _seriesSnapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < count; i++)
		{
			MokaLineSeries current = series![i];
			MokaLineSeries snapshot = _seriesSnapshot[i];
			if (current.Name != snapshot.Name || current.Color != snapshot.Color || current.Key != snapshot.Key
			    || current.Slot != snapshot.Slot || current.ColorValue != snapshot.ColorValue || current.Curve != snapshot.Curve
			    || snapshot.Values is not double[] values || !ChartSnapshot.SameItems(current.Values, values)
			    || !ChartSnapshot.SameItems(current.Low, snapshot.Low) || !ChartSnapshot.SameItems(current.High, snapshot.High))
			{
				return false;
			}
		}

		return true;
	}

	// Copies what the chart draws from, so a list changed in place still counts as changed, and gives back what it
	// replaced. With a DataVersion nothing is copied: the version says when the data moved.
	private MokaLineSeries[] TakeSnapshot()
	{
		MokaLineSeries[] previous = _seriesSnapshot;
		if (DataVersion is { } version)
		{
			_seriesSnapshot = [.. Series ?? []];
			_labelsSnapshot = null;
			_unitsSnapshot = null;
			_timesSnapshot = null;
			_valuesChanged = version != _builtVersion || !ChartSnapshot.SameReferences(previous, _seriesSnapshot);
		}
		else
		{
			_seriesSnapshot = [.. (Series ?? []).Select(s => s with
			{
				Values = s.Values.ToArray(),
				Low = s.Low is null ? null : s.Low.ToArray(),
				High = s.High is null ? null : s.High.ToArray()
			})];
			_labelsSnapshot = Labels?.ToArray();
			_unitsSnapshot = ScaleUnits?.ToArray();
			_timesSnapshot = Times?.ToArray();
			_valuesChanged = !SameValues(previous, _seriesSnapshot);
		}

		_builtVersion = DataVersion;
		return previous;
	}

	private void Build(MokaLineSeries[] previous)
	{
		_visible = [.. _seriesSnapshot.Where(s => s.Values.Count > 0)];
		_categoryCount = _visible.Count > 0 ? _visible[0].Values.Count : 0;
		_hasValues = _visible.Any(s => s.Values.Any(double.IsFinite));

		(double min, double max, double? step) = Scale();
		IReadOnlyList<double> ticks = step is { } every ? SteppedTicks(min, max, every) : Ticks(min, max);
		string[] tickTexts = [.. ticks.Select(FormatValue)];
		_widestTick = tickTexts.Length == 0 ? string.Empty : tickTexts.MaxBy(t => t.Length)!;

		_grid = _categoryCount > 0
			? [.. ticks.Select((tick, i) =>
			{
				double y = Y(tick, min, max);
				return new YTick(Num(y), Placed("top", y / PlotBoxHeight), tickTexts[i]);
			})]
			: [];
		_xLabels = XLabelsFor(_categoryCount);
		_geometry = GeometryFor(min, max);
		_lastPoints = ShowLastPoint ? LastPointsFor(min, max) : [];
		_legend = [.. _visible.Select((series, s) => new LegendEntry(
			series.Name,
			MokaChartRotation.ClassFor(series.Color, s),
			ShowCurrent && LastFinite(series.Values) is { } latest ? FormatValue(latest) : null))];
		_peakText = ShowPeak && _hasValues ? FormatValue(Peak()) : null;
		_seriesInfo = [.. _visible.Select((series, s) => new ChartSeriesInfo(series.Name, series.Name, s + 1,
			_legend[s].ColorClass, null, ChartMark.Line, false, false, _legend[s].Value))];
		_summary = Summary();
		SetSlide(previous);
	}

	private static bool SameValues(MokaLineSeries[] before, MokaLineSeries[] after) =>
		before.Length == after.Length
		&& before.Zip(after).All(pair => ChartSnapshot.SameItems(pair.First.Values, pair.Second.Values));

	// Slides only when the window kept its length: then each value moved one place left. A first build, a window
	// still filling or a new set of series just appears.
	private void SetSlide(MokaLineSeries[] previous)
	{
		bool slide = Live && _built && _valuesChanged && _categoryCount > 1
		             && previous.Length > 0 && previous[0].Values.Count == _categoryCount;
		if (!slide)
		{
			_slideClass = null;
			_slideStyle = null;
			return;
		}

		_slideClass = _slideClass == "moka-linechart-slide--a" ? "moka-linechart-slide--b" : "moka-linechart-slide--a";
		double milliseconds = Math.Max(0, LiveInterval.TotalMilliseconds);
		_slideStyle = new StyleBuilder()
			.AddStyle("--_slide-shift", $"{Num(PlotSpan / (_categoryCount - 1))}px")
			.AddStyle("--_slide-time", $"{milliseconds.ToString("0", CultureInfo.InvariantCulture)}ms")
			.Build();
	}

	private (double Min, double Max, double? Step) Scale()
	{
		List<double> all = [];
		foreach (MokaLineSeries series in _visible)
		{
			all.AddRange(series.Values);
		}

		(double min, double max) = all.Count > 0 ? Domain(all) : (0, 1);
		if (Max is null && SuggestedMax is { } suggested && double.IsFinite(suggested) && suggested > max)
		{
			max = suggested;
		}

		if (!AutoScale)
		{
			return (min, max, null);
		}

		double unit = UnitFor(Math.Max(Math.Abs(min), Math.Abs(max)));
		double raw = (max - min) / unit / Math.Max(1, YTicks);
		double magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
		double normalized = raw / magnitude;
		double nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 2.5 ? 2.5 : normalized <= 5 ? 5 : 10;
		double step = nice * magnitude * unit;

		double niceMin = Min ?? Math.Floor(min / step + 1e-9) * step;
		double niceMax = Max ?? Math.Ceiling(max / step - 1e-9) * step;
		return niceMax - niceMin < double.Epsilon ? (min, max, null) : (niceMin, niceMax, step);
	}

	private double UnitFor(double top)
	{
		double unit = 1;
		double smallest = double.MaxValue;
		bool reached = false;
		foreach (double candidate in ScaleUnits ?? [])
		{
			if (candidate <= 0 || !double.IsFinite(candidate))
			{
				continue;
			}

			smallest = Math.Min(smallest, candidate);
			if (top >= candidate && (!reached || candidate > unit))
			{
				unit = candidate;
				reached = true;
			}
		}

		return reached ? unit : smallest < double.MaxValue ? smallest : 1;
	}

	// A fixed Min or Max off the step still gets a tick at each end.
	private static List<double> SteppedTicks(double min, double max, double step)
	{
		List<double> ticks = [min];
		double first = Math.Ceiling(min / step + 1e-9) * step;
		for (double tick = first; tick < max - step * 1e-6 && ticks.Count < MaxTicks; tick += step)
		{
			if (tick > min + step * 1e-6)
			{
				ticks.Add(tick);
			}
		}

		ticks.Add(max);
		return ticks;
	}

	private string FormatValue(double value) =>
		Formatter is not null ? Formatter(value) : value.ToString(TickFormat, CultureInfo.CurrentCulture);

	private static double XAt(int index, int count) =>
		PlotLeft + (count <= 1 ? PlotSpan / 2 : (double)index / (count - 1) * PlotSpan);

	private List<XTick> XLabelsFor(int count) =>
	[
		.. AxisLabels(count, i => (XAt(i, count) - PlotLeft) / PlotBoxWidth, "moka-linechart-xtick")
			.Select(label => new XTick(
				Placed("left", label.Share),
				label.Text,
				new CssBuilder("moka-linechart-tick moka-linechart-xtick").AddClass(label.WidthClass).Build()))
	];

	// A label's place as a percentage of the plot box, which is what the stretched plot keeps.
	private static string? Placed(string side, double share) => new StyleBuilder()
		.AddStyle(side, FormattableString.Invariant($"{share * 100:0.###}%"))
		.Build();

	private List<SeriesGeometry> GeometryFor(double min, double max)
	{
		List<SeriesGeometry> geometry = [];
		string bottom = Num(PadTop + PlotHeight);

		for (int s = 0; s < _visible.Count; s++)
		{
			MokaLineSeries series = _visible[s];
			string colorClass = MokaChartRotation.ClassFor(series.Color, s);
			if (series.Values.Count > ClassicReduceAbove)
			{
				geometry.Add(ReducedGeometry(series, colorClass, bottom, min, max));
				continue;
			}

			IReadOnlyList<(int First, int Last)> runs = MokaSeriesRuns.Of(series.Values);
			string line = string.Join(' ', runs.Select(run => RunPath(series, run, min, max)));
			string? area = ShowArea && runs.Count > 0
				? string.Join(' ', runs.Select(run => AreaPath(series, run, bottom, min, max)))
				: null;

			List<PointGeometry> points = [];
			if (ShowPoints)
			{
				for (int i = 0; i < series.Values.Count; i++)
				{
					if (double.IsFinite(series.Values[i]))
					{
						points.Add(PointAt(series, i, min, max, colorClass));
					}
				}
			}

			geometry.Add(new SeriesGeometry(series, colorClass, line, area, points));
		}

		return geometry;
	}

	private bool ClassicSmooth => Smooth || Curve == MokaLineCurve.Smooth;

	// A long classic series: the path and dots run through the kept values only, and a kept gap still breaks the line.
	private SeriesGeometry ReducedGeometry(MokaLineSeries series, string colorClass, string bottom, double min, double max)
	{
		double[] values = series.Values as double[] ?? [.. series.Values];
		int[] kept = MokaDownsampler.MinMax(values, ClassicBuckets);
		List<List<int>> runs = [];
		List<int>? run = null;
		foreach (int index in kept)
		{
			if (!double.IsFinite(values[index]))
			{
				run = null;
				continue;
			}

			if (run is null)
			{
				run = [];
				runs.Add(run);
			}

			run.Add(index);
		}

		StringBuilder line = new();
		StringBuilder? area = ShowArea ? new StringBuilder() : null;
		foreach (List<int> stretch in runs)
		{
			string path = ReducedPath(series, stretch, min, max);
			if (line.Length > 0)
			{
				line.Append(' ');
			}

			line.Append(path);
			if (area is not null)
			{
				if (area.Length > 0)
				{
					area.Append(' ');
				}

				area.Append("M ").Append(Num(XAt(stretch[0], values.Length))).Append(' ').Append(bottom).Append(' ')
					.Append(path[2..]).Append(" L ").Append(Num(XAt(stretch[^1], values.Length))).Append(' ').Append(bottom)
					.Append(" Z");
			}
		}

		List<PointGeometry> points = ShowPoints
			? [.. kept.Where(i => double.IsFinite(values[i])).Select(i => PointAt(series, i, min, max, colorClass))]
			: [];
		return new SeriesGeometry(series, colorClass, line.ToString(), area?.ToString(), points);
	}

	private string ReducedPath(MokaLineSeries series, List<int> stretch, double min, double max)
	{
		StringBuilder sb = new();
		if (ClassicSmooth && stretch.Count > 1)
		{
			(double X, double Y)[] points = [.. stretch.Select(i => (XAt(i, series.Values.Count), Y(series.Values[i], min, max)))];
			MokaChartPaths.AppendMonotone(sb, points);
			return sb.ToString();
		}

		for (int k = 0; k < stretch.Count; k++)
		{
			sb.Append(k == 0 ? "M " : " L ");
			AppendPoint(sb, series, stretch[k], min, max);
		}

		if (stretch.Count == 1)
		{
			sb.Append(" L ");
			AppendPoint(sb, series, stretch[0], min, max);
		}

		return sb.ToString();
	}

	private List<PointGeometry> LastPointsFor(double min, double max)
	{
		List<PointGeometry> points = [];
		for (int s = 0; s < _visible.Count; s++)
		{
			MokaLineSeries series = _visible[s];
			for (int i = series.Values.Count - 1; i >= 0; i--)
			{
				if (double.IsFinite(series.Values[i]))
				{
					points.Add(PointAt(series, i, min, max, MokaChartRotation.ClassFor(series.Color, s)));
					break;
				}
			}
		}

		return points;
	}

	private PointGeometry PointAt(MokaLineSeries series, int index, double min, double max, string colorClass) =>
		new(Num(XAt(index, series.Values.Count)), Num(Y(series.Values[index], min, max)), PointTitle(series, index), colorClass);

	private string RunPath(MokaLineSeries series, (int First, int Last) run, double min, double max)
	{
		StringBuilder sb = new();
		if (ClassicSmooth && run.Last > run.First)
		{
			(double X, double Y)[] points = new (double, double)[run.Last - run.First + 1];
			for (int i = run.First; i <= run.Last; i++)
			{
				points[i - run.First] = (XAt(i, series.Values.Count), Y(series.Values[i], min, max));
			}

			MokaChartPaths.AppendMonotone(sb, points);
			return sb.ToString();
		}

		for (int i = run.First; i <= run.Last; i++)
		{
			sb.Append(i == run.First ? "M " : " L ");
			AppendPoint(sb, series, i, min, max);
		}

		// A lone value between two gaps needs a segment for the round cap to draw.
		if (run.First == run.Last && series.Values.Count > 1)
		{
			sb.Append(" L ");
			AppendPoint(sb, series, run.First, min, max);
		}

		return sb.ToString();
	}

	private static void AppendPoint(StringBuilder sb, MokaLineSeries series, int index, double min, double max)
	{
		sb.Append(Num(XAt(index, series.Values.Count)));
		sb.Append(' ');
		sb.Append(Num(Y(series.Values[index], min, max)));
	}

	// One stretch of the line closed along the x-axis. A stretch that reaches a side of the plot closes on that side.
	private string AreaPath(MokaLineSeries series, (int First, int Last) run, string bottom, double min, double max)
	{
		int count = series.Values.Count;
		double left = run.First == 0 ? PlotLeft : XAt(run.First, count);
		double right = run.Last == count - 1 ? PlotLeft + PlotSpan : XAt(run.Last, count);
		return $"M {Num(left)} {bottom} {RunPath(series, run, min, max)[2..]} L {Num(right)} {bottom} Z";
	}

	private string PointTitle(MokaLineSeries series, int index)
	{
		string value = $"{series.Name}: {FormatValue(series.Values[index])}";
		IReadOnlyList<string>? labels = (IReadOnlyList<string>?)_labelsSnapshot ?? (DataVersion is null ? null : Labels);
		string? category = TooltipLabel is not null
			? TooltipLabel(index) is { Length: > 0 } custom ? custom : null
			: labels is not null && index < labels.Count
				? labels[index]
				: null;
		return string.IsNullOrWhiteSpace(category) ? value : $"{category} · {value}";
	}

	private static double? LastFinite(IReadOnlyList<double> values)
	{
		for (int i = values.Count - 1; i >= 0; i--)
		{
			if (double.IsFinite(values[i]))
			{
				return values[i];
			}
		}

		return null;
	}

	private double Peak() =>
		_visible.SelectMany(series => series.Values).Where(double.IsFinite).DefaultIfEmpty(0).Max();

	// The current values go into the name only when the legend shows them, so a screen reader hears what the eye sees.
	private string Summary()
	{
		string label = Label ?? "Line chart";
		List<string> parts = [.. _legend.Where(entry => entry.Value is not null).Select(entry => $"{entry.Name} {entry.Value}")];
		if (_peakText is not null)
		{
			parts.Add($"{PeakLabel} {_peakText}");
		}

		return parts.Count == 0 ? label : $"{label}. {string.Join(", ", parts)}";
	}
}
