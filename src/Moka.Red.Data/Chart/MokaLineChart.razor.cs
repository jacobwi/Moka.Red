using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A category line chart: one or more named series drawn as polylines over a common y-axis,
///     with grid lines, tick labels and a legend. Pure SVG, so it themes and scales with the page.
///     It also draws a live window of samples, such as a transfer rate fed once a second: see <see cref="Live" />.
/// </summary>
public partial class MokaLineChart : MokaCartesianChartBase
{
	// About how wide one character of an 11 px tick label is in viewBox units, to keep long labels inside the svg.
	private const double TickCharWidth = 6.6;
	private const double TickGap = 10;
	private const int MaxTicks = 50;

	private readonly string _clipId = $"moka-linechart-clip-{Guid.NewGuid():N}";

	private IReadOnlyDictionary<string, object?>? _lastParameters;
	private MokaLineSeries[] _seriesSnapshot = [];
	private string[]? _labelsSnapshot;
	private double[]? _unitsSnapshot;
	private bool _dirty = true;
	private bool _valuesChanged;
	private bool _built;

	private IReadOnlyList<MokaLineSeries> _visible = [];
	private int _categoryCount;
	private bool _hasValues;
	private double _plotLeft = PadLeft;
	private IReadOnlyList<GridLine> _grid = [];
	private IReadOnlyList<XLabel> _xLabels = [];
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

	/// <summary>What the chart shows, for assistive technology.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Writes a value as text, for the y-axis ticks, the point tooltips and the legend, such as
	///     <c>v =&gt; $"{v / 1e6:0.#} MB/s"</c>. Null uses <see cref="MokaCartesianChartBase.TickFormat" />.
	/// </summary>
	[Parameter]
	public Func<double, string>? Formatter { get; set; }

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
	///     With <see cref="AutoScale" />, the units <see cref="Formatter" /> writes values in, such as 1, 1024 and
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

	/// <summary>Text drawn in the middle of the plot while there are no values, such as "Waiting for traffic".</summary>
	[Parameter]
	public string? EmptyText { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-linechart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-linechart--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	private double PlotSpan => ViewWidth - _plotLeft - PadRight;

	// Geometry leaves the component as strings: every number goes through Num, so an SVG attribute
	// never carries a culture's decimal comma.
	private sealed record PointGeometry(string Cx, string Cy, string Title, string ColorClass);

	private sealed record SeriesGeometry(MokaLineSeries Series, string ColorClass, string Line, string? Area,
		IReadOnlyList<PointGeometry> Points);

	private sealed record LegendEntry(string Name, string ColorClass, string? Value);

	/// <inheritdoc />
	/// <remarks>
	///     A parent that renders every second hands the chart its parameters every second. They are compared with the
	///     last set, the series by value, and the chart draws again only when something differs, so a list changed in
	///     place still redraws and an unchanged one costs no render.
	/// </remarks>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		IReadOnlyDictionary<string, object?> next = parameters.ToDictionary();
		if (!SameParameters(next))
		{
			_dirty = true;
		}

		_lastParameters = next;
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (_dirty)
		{
			Build();
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender()
	{
		bool render = _dirty;
		_dirty = false;
		return render;
	}

	// Blazor doesn't ask ShouldRender before the first render, so that render is marked as drawn here.
	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		if (firstRender)
		{
			_dirty = false;
		}
	}

	private bool SameParameters(IReadOnlyDictionary<string, object?> next)
	{
		if (_lastParameters is null || _lastParameters.Count != next.Count)
		{
			return false;
		}

		foreach ((string name, object? value) in next)
		{
			if (!_lastParameters.TryGetValue(name, out object? previous))
			{
				return false;
			}

			bool same = name switch
			{
				nameof(Series) => SameSeries(value as IReadOnlyList<MokaLineSeries>),
				nameof(Labels) => SameItems(value as IReadOnlyList<string>, _labelsSnapshot),
				nameof(ScaleUnits) => SameItems(value as IReadOnlyList<double>, _unitsSnapshot),
				_ => Equals(previous, value)
			};

			if (!same)
			{
				return false;
			}
		}

		return true;
	}

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
			if (current.Name != snapshot.Name || current.Color != snapshot.Color
			    || !SameItems(current.Values, (double[])snapshot.Values))
			{
				return false;
			}
		}

		return true;
	}

	// Equals rather than ==, so NaN, the gap marker, matches NaN.
	private static bool SameItems<T>(IReadOnlyList<T>? items, T[]? snapshot)
	{
		if (items is null || snapshot is null)
		{
			return items is null && snapshot is null;
		}

		if (items.Count != snapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < snapshot.Length; i++)
		{
			if (!EqualityComparer<T>.Default.Equals(items[i], snapshot[i]))
			{
				return false;
			}
		}

		return true;
	}

	private void Build()
	{
		MokaLineSeries[] previous = _seriesSnapshot;
		_seriesSnapshot = [.. (Series ?? []).Select(s => s with { Values = s.Values.ToArray() })];
		_labelsSnapshot = Labels?.ToArray();
		_unitsSnapshot = ScaleUnits?.ToArray();
		_valuesChanged = !SameValues(previous, _seriesSnapshot);

		_visible = [.. _seriesSnapshot.Where(s => s.Values.Count > 0)];
		_categoryCount = _visible.Count > 0 ? _visible[0].Values.Count : 0;
		_hasValues = _visible.Any(s => s.Values.Any(double.IsFinite));

		(double min, double max, double? step) = Scale();
		IReadOnlyList<double> ticks = step is { } every ? SteppedTicks(min, max, every) : Ticks(min, max);
		string[] tickTexts = [.. ticks.Select(FormatValue)];
		int widest = tickTexts.Length == 0 ? 0 : tickTexts.Max(t => t.Length);
		_plotLeft = Math.Max(PadLeft, widest * TickCharWidth + TickGap);

		_grid = _categoryCount > 0
			? [.. ticks.Select((tick, i) =>
			{
				double y = Y(tick, min, max);
				return new GridLine(Num(y), Num(y + 4), tickTexts[i]);
			})]
			: [];
		_xLabels = XLabelsFor(_categoryCount);
		_geometry = GeometryFor(min, max);
		_lastPoints = ShowLastPoint ? LastPointsFor(min, max) : [];
		_legend = [.. _visible.Select((series, s) => new LegendEntry(
			series.Name,
			MokaChartPalette.ClassFor(series.Color, s),
			ShowCurrent && LastFinite(series.Values) is { } latest ? FormatValue(latest) : null))];
		_peakText = ShowPeak && _hasValues ? FormatValue(Peak()) : null;
		_summary = Summary();
		SetSlide(previous);
		_built = true;
	}

	private static bool SameValues(MokaLineSeries[] before, MokaLineSeries[] after) =>
		before.Length == after.Length
		&& before.Zip(after).All(pair => SameItems(pair.First.Values, (double[])pair.Second.Values));

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

	private double XAt(int index, int count) =>
		_plotLeft + (count <= 1 ? PlotSpan / 2 : (double)index / (count - 1) * PlotSpan);

	private List<XLabel> XLabelsFor(int count)
	{
		List<XLabel> labels = [];
		for (int i = 0; i < count; i += LabelStep(count))
		{
			string text = Labels is not null && i < Labels.Count
				? Labels[i]
				: i.ToString(CultureInfo.CurrentCulture);
			labels.Add(new XLabel(Num(XAt(i, count)), text));
		}

		return labels;
	}

	private List<SeriesGeometry> GeometryFor(double min, double max)
	{
		List<SeriesGeometry> geometry = [];
		string bottom = Num(PadTop + PlotHeight);

		for (int s = 0; s < _visible.Count; s++)
		{
			MokaLineSeries series = _visible[s];
			string colorClass = MokaChartPalette.ClassFor(series.Color, s);
			IReadOnlyList<(int First, int Last)> runs = MokaChartRuns.Of(series.Values);
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
					points.Add(PointAt(series, i, min, max, MokaChartPalette.ClassFor(series.Color, s)));
					break;
				}
			}
		}

		return points;
	}

	private PointGeometry PointAt(MokaLineSeries series, int index, double min, double max, string colorClass) =>
		new(Num(XAt(index, series.Values.Count)), Num(Y(series.Values[index], min, max)), Tooltip(series, index), colorClass);

	private string RunPath(MokaLineSeries series, (int First, int Last) run, double min, double max)
	{
		StringBuilder sb = new();
		if (Smooth && run.Last > run.First)
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

	private void AppendPoint(StringBuilder sb, MokaLineSeries series, int index, double min, double max)
	{
		sb.Append(Num(XAt(index, series.Values.Count)));
		sb.Append(' ');
		sb.Append(Num(Y(series.Values[index], min, max)));
	}

	// One stretch of the line closed along the x-axis. A stretch that reaches a side of the plot closes on that side.
	private string AreaPath(MokaLineSeries series, (int First, int Last) run, string bottom, double min, double max)
	{
		int count = series.Values.Count;
		double left = run.First == 0 ? _plotLeft : XAt(run.First, count);
		double right = run.Last == count - 1 ? _plotLeft + PlotSpan : XAt(run.Last, count);
		return $"M {Num(left)} {bottom} {RunPath(series, run, min, max)[2..]} L {Num(right)} {bottom} Z";
	}

	private string Tooltip(MokaLineSeries series, int index) =>
		_labelsSnapshot is not null && index < _labelsSnapshot.Length
			? $"{_labelsSnapshot[index]} · {series.Name}: {FormatValue(series.Values[index])}"
			: $"{series.Name}: {FormatValue(series.Values[index])}";

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
