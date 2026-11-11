using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

public abstract partial class MokaCartesianChartBase
{
	private const int VirtualizeAbove = 200;
	private const double DefaultCategoryEm = 2.75;
	private const double NamesAboveLeastEm = 24;
	private const double NamesAboveMostEm = 60;
	private const double NameShare = 0.4;
	private const int NumberTicks = 6;
	private const int CategoryLabelsLaidOut = 512;

	private readonly Dictionary<int, (long Version, ColumnPlotModel Model)> _columnModels = [];
	private readonly Dictionary<int, (long Version, ChartXFrame Frame)> _xFrames = [];
	private (long Version, RowPlotModel Model)? _rowModel;
	private (long Version, (double From, double To) Span)? _timeSpan;

	[Inject]
	private IServiceProvider ChartServices { get; set; } = default!;

	/// <summary>The lowest and highest value a panel draws, hidden series left out. NaN when there are none.</summary>
	/// <param name="panel">The panel.</param>
	internal virtual (double Min, double Max) ValueRange(int panel) => (double.NaN, double.NaN);

	/// <summary>
	///     The value axis's options as a column plot draws them: <see cref="YAxis" />, unless a chart folds parameters of
	///     its own into them, as the line chart does with <c>AutoScale</c> and <c>SuggestedMax</c>.
	/// </summary>
	internal virtual MokaChartAxis? ValueAxisOptions => YAxis;

	/// <summary>How many categories the column plot has.</summary>
	internal virtual int CategoryCount => Labels?.Count ?? 0;

	/// <summary>How many rows the row plot has.</summary>
	internal virtual int RowCount => CategoryCount;

	/// <summary>Whether the chart draws rows (names down, values across) rather than columns.</summary>
	internal virtual bool DrawsRows => false;

	/// <summary>Where a category's label is centred, as a share of the plot's width. Points at the ends by default.</summary>
	/// <param name="index">The category.</param>
	/// <param name="count">How many there are.</param>
	internal virtual double CategoryShare(int index, int count) =>
		count <= 1 ? 0.5 : (double)index / (count - 1);

	/// <summary>A category's label: its entry in <see cref="Labels" />, else its number.</summary>
	/// <param name="index">The category.</param>
	internal virtual string CategoryText(int index) =>
		Labels is not null && index >= 0 && index < Labels.Count ? Labels[index] : index.ToString(CultureInfo.CurrentCulture);

	/// <summary>A row's name, drawn whole on one line and kept whole in a tooltip when it is cut.</summary>
	/// <param name="index">The row.</param>
	internal virtual string RowLabel(int index) => CategoryText(index);

	/// <summary>What a row reads at its end, such as its value, or null for nothing.</summary>
	/// <param name="index">The row.</param>
	internal virtual string? RowFigure(int index) => null;

	/// <summary>The lowest and highest x a panel draws, for a chart whose x axis counts in numbers; null for categories.</summary>
	/// <param name="panel">The panel.</param>
	internal virtual (double Min, double Max)? NumericXRange(int panel) => null;

	/// <summary>The span a chart of rows draws when its values are times, such as a gantt chart; null when they are numbers.</summary>
	internal virtual (DateTimeOffset From, DateTimeOffset To)? RowTimeRange => null;

	/// <summary>
	///     The value axis's options as the chart wants them, such as a dot plot that spans only its data rather than
	///     starting at zero. Asked for the axis the values are read on; the parameters still win where they are set.
	/// </summary>
	/// <param name="options">The options worked out from the parameters and the look.</param>
	internal virtual MokaScaleOptions ValueScaleOptions(MokaScaleOptions options) => options;

	/// <summary>Each row's height in pixels when <see cref="RowHeight" /> isn't set: the look's, unless a chart needs more room.</summary>
	internal virtual double DefaultRowHeight => Spec.RowHeight;

	/// <summary>The x axis's title when <see cref="XAxis" /> names none, such as a scatter chart's measure.</summary>
	internal virtual string? DefaultXTitle => null;

	/// <summary>The value axis's title when its axis names none.</summary>
	internal virtual string? DefaultValueTitle => null;

	/// <summary>
	///     The tallest a column plot grows while it keeps its proportion, as a CSS length: what the classic chart's frame
	///     held at each <c>Size</c>, less a line of labels. An app's <c>--moka-chart-plot-max</c> wins over it, and a set
	///     <c>Height</c> takes the plot out of proportion altogether.
	/// </summary>
	internal virtual string PlotMaxHeight => Size switch
	{
		MokaSize.Xs => "6rem",
		MokaSize.Sm => "9rem",
		MokaSize.Lg => "19rem",
		_ => "14rem"
	};

	/// <summary>Whether the value axis reaches zero. A chart whose values sit far from it, such as prices, says false.</summary>
	internal virtual bool ValueAxisReachesZero => true;

	/// <summary>Whether the value axis rounds its ends and steps when its own options don't say: the look's, unless a chart always does.</summary>
	internal virtual bool NiceValueAxis => Spec.NiceTicks;

	/// <summary>The unit the value axis takes when its own options name none: <see cref="Unit" />, unless a chart reads that elsewhere.</summary>
	internal virtual MokaChartUnit? ValueAxisUnit => Unit;

	/// <summary>What writes the value axis when its own options name nothing: the chart's formatter, unless it writes something else.</summary>
	internal virtual Func<double, string>? ValueAxisFormatter => Formatter;

	/// <summary>The zone time axes are aligned and written in.</summary>
	internal TimeZoneInfo ResolvedTimeZone =>
		TimeZone ?? ChartServices?.GetService<TimeProvider>()?.LocalTimeZone ?? TimeZoneInfo.Local;

	/// <summary>
	///     The x axis as it is drawn now: what it counts in, the values at its two ends, and how a value maps to a share
	///     of the plot. Built once per geometry version, so read it while rendering. While rebuilding geometry, use
	///     <see cref="CreateXFrame" />, since the version only moves once the rebuild is done.
	/// </summary>
	/// <param name="panel">The panel.</param>
	internal ChartXFrame XFrame(int panel = 0)
	{
		if (_xFrames.TryGetValue(panel, out (long Version, ChartXFrame Frame) cached) && cached.Version == GeometryVersion)
		{
			return cached.Frame;
		}

		ChartXFrame frame = CreateXFrame(panel);
		_xFrames[panel] = (GeometryVersion, frame);
		return frame;
	}

	/// <summary>Works out the x axis from the parameters as they are now. One pass over <see cref="Times" /> for a time axis.</summary>
	/// <param name="panel">The panel.</param>
	internal ChartXFrame CreateXFrame(int panel = 0)
	{
		ChartXKind kind = XKindOf(panel, out (double Min, double Max)? numbers);
		bool reverse = XAxis?.Reverse ?? false;
		(double From, double To)? zoom = ZoomSpan();

		switch (kind)
		{
			case ChartXKind.Time:
			{
				(double from, double to) = zoom ?? MeasureTimeSpan();
				TimeSpan span = TimeSpan.FromMilliseconds(Math.Max(1, to - from));
				MokaTimeTickOptions options = TimeOptions();
				return new ChartXFrame(kind, from, to, null, reverse, null, value => TimeText(value, span, options));
			}
			case ChartXKind.Number:
			{
				(double min, double max) = numbers ?? (0, 1);
				MokaScale scale = zoom is { } window
					? ValueScale((XAxis ?? new MokaChartAxis()) with { Min = window.From, Max = window.To, Nice = false }, window.From,
						window.To, false)
					: ValueScale(XAxis, min, max, false);
				return new ChartXFrame(kind, scale.Min, scale.Max, scale, reverse, null, value => WriteValue(value, XAxis, false));
			}
			default:
			{
				int count = CategoryCount;
				(double from, double to) = zoom ?? (0, Math.Max(1, count - 1));
				Func<double, double>? share = zoom is null ? index => InterpolatedCategoryShare(index, count) : null;
				return new ChartXFrame(kind, from, to, null, reverse, share,
					index => CategoryText((int)Math.Round(index)));
			}
		}
	}

	/// <summary>Where a value sits along the x axis as it is drawn now, as a share of the plot's width.</summary>
	/// <param name="x">A time as Unix milliseconds, a number, or a category's index.</param>
	internal double XShare(double x) => XFrame().Share(x);

	/// <summary>
	///     How far a time axis reaches past the first and last times, in milliseconds, so marks with a width, such as
	///     bars, sit whole inside the plot. An end pinned by <see cref="MokaChartAxis.Min" /> or <see cref="MokaChartAxis.Max" />
	///     isn't moved. Read while the frame is built, so a chart sets what it reads before it builds one.
	/// </summary>
	internal virtual double TimeEdgePad => 0;

	/// <summary>
	///     The span a time axis covers when it isn't zoomed, in Unix milliseconds: from the first time to the last, moved by
	///     the x axis's <see cref="MokaChartAxis.Min" />, <see cref="MokaChartAxis.Max" /> and suggested ends, and padded by
	///     <see cref="TimeEdgePad" />. Worked out once per geometry version, so read it while rendering; null without times.
	/// </summary>
	internal (double From, double To)? TimeAxisSpan()
	{
		if (Times is not { Count: > 0 })
		{
			return null;
		}

		if (_timeSpan is { } cached && cached.Version == GeometryVersion)
		{
			return cached.Span;
		}

		(double From, double To) span = MeasureTimeSpan();
		_timeSpan = (GeometryVersion, span);
		return span;
	}

	/// <summary>The time options every time axis of the chart writes with: its zone, culture, formats and clock.</summary>
	internal MokaTimeTickOptions TimeOptions()
	{
		Func<double, string>? formatter = XAxis?.Formatter;
		return new MokaTimeTickOptions
		{
			Zone = ResolvedTimeZone,
			Culture = CultureInfo.CurrentCulture,
			Format = XAxis?.TimeFormat,
			Formatter = formatter is null ? null : time => formatter(MokaChartX.FromDateTimeOffset(time).Value),
			Hour12 = XAxis?.Hour12 ?? MokaTimeTicks.WritesHour12(XAxis?.TimeFormat),
			TooltipFormat = XAxis?.TooltipTimeFormat
		};
	}

	/// <summary>
	///     A time written for a tooltip heading, the table view or an event marker: the x axis's
	///     <see cref="MokaChartAxis.TooltipTimeFormat" />, else its <see cref="MokaChartAxis.Formatter" />, else the time with
	///     as much of the date as the span needs, on the axis's clock.
	/// </summary>
	/// <param name="value">The time, as Unix milliseconds.</param>
	/// <param name="span">The time the axis covers.</param>
	/// <param name="options">The axis's time options, when the caller already has them.</param>
	internal string TimeText(double value, TimeSpan span, MokaTimeTickOptions? options = null) =>
		XAxis is { TooltipTimeFormat: null or "", Formatter: { } formatter }
			? formatter(value)
			: MokaTimeTicks.TooltipText(new MokaChartX(value, true).Time, span, options ?? TimeOptions());

	/// <summary>
	///     <see cref="Times" /> as Unix milliseconds, the units every time on the x axis is counted in. A new array each
	///     call: keep it for the rebuild.
	/// </summary>
	internal double[] TimeValues()
	{
		if (Times is not { Count: > 0 } times)
		{
			return [];
		}

		double[] values = new double[times.Count];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = MokaChartX.FromDateTimeOffset(times[i]).Value;
		}

		return values;
	}

	/// <summary>
	///     The points to draw when the chart is zoomed: those inside <see cref="CurrentRange" />, padded by one on each side
	///     so a line runs to the plot's edges. Everything when it isn't zoomed.
	/// </summary>
	/// <param name="x">Each point's place on the x axis, in order.</param>
	internal (int Start, int End) XWindow(ReadOnlySpan<double> x)
	{
		if (x.IsEmpty)
		{
			return (0, -1);
		}

		return ZoomSpan() is { } zoom ? MokaDownsampler.Window(x, zoom.From, zoom.To) : (0, x.Length - 1);
	}

	/// <summary>The categories to draw when the chart is zoomed by index, padded by one on each side; all of them otherwise.</summary>
	/// <param name="count">How many categories there are.</param>
	internal (int Start, int End) CategoryWindow(int count)
	{
		if (count <= 0)
		{
			return (0, -1);
		}

		if (ZoomSpan() is not { } zoom)
		{
			return (0, count - 1);
		}

		int start = Math.Clamp((int)Math.Floor(zoom.From) - 1, 0, count - 1);
		int end = Math.Clamp((int)Math.Ceiling(zoom.To) + 1, 0, count - 1);
		return (start, Math.Max(start, end));
	}

	/// <summary>Whether a window that breaks the line covers this place on the x axis, so a value there is drawn as a gap.</summary>
	/// <param name="x">A time as Unix milliseconds, a number, or a category's index.</param>
	internal bool BreaksLineAt(double x) => ChartAnnotations.Breaks(Annotations, x);

	/// <summary>
	///     What a tooltip adds for a value: how far over each threshold, whether inside the target and each band, and the
	///     events and windows its place falls in, in the chart's words.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="x">Its place on the x axis, or null.</param>
	/// <param name="xTolerance">How near an event a point has to be to name it, such as half the step between samples.</param>
	internal IReadOnlyList<string> AnnotationNotes(double value, double? x = null, double xTolerance = 0) =>
		ChartAnnotations.Notes(Annotations, TargetLow, TargetHigh, value, x, xTolerance, ValueText, Words);

	/// <summary>
	///     A value written as the value axis writes it, with its unit, at the value's own precision rather than the tick
	///     step's. A chart of rows whose values are times writes the plain number.
	/// </summary>
	/// <param name="value">The value.</param>
	internal string ValueText(double value)
	{
		if (DrawsRows)
		{
			return RowTimeRange is null ? WriteValue(value, XAxis) : FormatPlain(value);
		}

		return WriteValue(value, ValueAxisOptions);
	}

	/// <summary>The axes, labels, grid and annotations around a column chart's marks, worked out once per geometry version.</summary>
	/// <param name="panel">The panel.</param>
	internal ColumnPlotModel ColumnPlotModel(int panel)
	{
		if (_columnModels.TryGetValue(panel, out (long Version, ColumnPlotModel Model) cached) && cached.Version == GeometryVersion)
		{
			return cached.Model;
		}

		ColumnPlotModel model = BuildColumnModel(panel);
		_columnModels[panel] = (GeometryVersion, model);
		return model;
	}

	/// <summary>The value axis and the box around a row chart's rows, worked out once per geometry version.</summary>
	internal RowPlotModel RowPlotModel()
	{
		if (_rowModel is { } cached && cached.Version == GeometryVersion)
		{
			return cached.Model;
		}

		RowPlotModel model = BuildRowModel();
		_rowModel = (GeometryVersion, model);
		return model;
	}

	/// <summary>The value axis for data from <paramref name="min" /> to <paramref name="max" />, with this axis's options.</summary>
	/// <param name="axis">The axis's options, if any.</param>
	/// <param name="min">The lowest value, or NaN.</param>
	/// <param name="max">The highest value, or NaN.</param>
	/// <param name="valueAxis">
	///     Whether it is the axis the values are read on, which takes the chart's <see cref="Unit" />, reaches its
	///     thresholds and bands, and starts at zero; false for a numeric x axis.
	/// </param>
	internal MokaScale ValueScale(MokaChartAxis? axis, double min, double max, bool valueAxis = true)
	{
		bool compact = axis?.Compact ?? Spec.CompactAbove is not null;
		MokaScaleOptions options = new()
		{
			Min = axis?.Min ?? (valueAxis ? Min : null),
			Max = axis?.Max ?? (valueAxis ? Max : null),
			SuggestedMin = axis?.SuggestedMin,
			SuggestedMax = axis?.SuggestedMax,
			TickCount = Math.Max(1, axis?.TickCount ?? (valueAxis ? YTicks : NumberTicks)),
			TickValues = axis?.TickValues,
			Nice = axis?.Nice ?? (NiceValueAxis || !valueAxis),
			Unit = axis?.Unit ?? (valueAxis ? ValueAxisUnit : null) ?? (compact ? MokaChartUnit.Compact : null),
			IncludeZero = axis?.IncludeZero ?? (valueAxis && ValueAxisReachesZero),
			Reach = valueAxis ? Reach() : null
		};
		if (valueAxis)
		{
			// A chart's own preference gives way to what the axis says outright.
			MokaScaleOptions chart = ValueScaleOptions(options);
			options = chart with
			{
				IncludeZero = axis?.IncludeZero ?? chart.IncludeZero,
				Nice = axis?.Nice ?? chart.Nice
			};
		}

		return axis?.Scale == MokaAxisScale.Log
			? MokaLogScale.Create(min, max, options)
			: MokaNiceScale.Linear(min, max, options);
	}

	/// <summary>Writes a value as the axis does.</summary>
	/// <param name="scale">The axis's scale.</param>
	/// <param name="axis">The axis's options, if any.</param>
	/// <param name="value">The value.</param>
	/// <param name="valueAxis">Whether the chart's own <see cref="MokaChartBase.Formatter" /> applies, as it does on the value axis.</param>
	internal string TickText(MokaScale scale, MokaChartAxis? axis, double value, bool valueAxis = true)
	{
		ArgumentNullException.ThrowIfNull(scale);

		if ((axis?.Formatter ?? (valueAxis ? ValueAxisFormatter : null)) is { } formatter)
		{
			return formatter(value);
		}

		if (axis?.Format is { Length: > 0 } format)
		{
			return value.ToString(format, CultureInfo.CurrentCulture);
		}

		if (scale is { Kind: MokaScaleKind.Log, ChartUnit: { } unit } && WholeIn(unit, value, axis?.ShowUnit ?? true) is { } whole)
		{
			return whole;
		}

		return scale.ChartUnit is not null || scale.Kind == MokaScaleKind.Log || (axis?.Nice ?? (NiceValueAxis || !valueAxis))
			? scale.Format(value, axis?.ShowUnit ?? true)
			: value.ToString(TickFormat, CultureInfo.CurrentCulture);
	}

	/// <summary>
	///     A value written for a tooltip, a table cell or a printed label: as the axis writes it, but at the value's own
	///     precision rather than the tick step's, so 26.4 GB never reads 26 GB while the ticks step by 10 GB. The axis's
	///     formatter or format win, then the chart's formatter on the value axis, then the unit in the step the value's
	///     size reaches, else a plain number with as many decimals as it needs to keep three significant digits.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <param name="axis">The axis's options, if any.</param>
	/// <param name="valueAxis">Whether the chart's own formatter and unit apply, as they do on the value axis.</param>
	/// <returns>The value as text, or empty for one that isn't a number.</returns>
	internal string WriteValue(double value, MokaChartAxis? axis, bool valueAxis = true)
	{
		if (!double.IsFinite(value))
		{
			return string.Empty;
		}

		if ((axis?.Formatter ?? (valueAxis ? ValueAxisFormatter : null)) is { } formatter)
		{
			return formatter(value);
		}

		if (axis?.Format is { Length: > 0 } format)
		{
			return value.ToString(format, CultureInfo.CurrentCulture);
		}

		MokaChartUnit? unit = axis?.Unit ?? (valueAxis ? ValueAxisUnit : null) ?? (axis?.Compact == true ? MokaChartUnit.Compact : null);
		return unit is not null
			? unit.Format(value, null, CultureInfo.CurrentCulture)
			: value.ToString(PlainPattern(value), CultureInfo.CurrentCulture);
	}

	// Two decimals from 1 up; below it, enough to keep three significant digits, so 0.0042 doesn't read 0.
	private static string PlainPattern(double value)
	{
		double size = Math.Abs(value);
		if (size == 0 || size >= 1)
		{
			return "#,##0.##";
		}

		int decimals = Math.Clamp(2 - (int)Math.Floor(Math.Log10(size)), 2, 6);
		return "#,##0." + new string('#', decimals);
	}

	// A power of ten on a log axis reads best in the largest step it is a whole number of: 100 s rather than 1.67 min.
	private static string? WholeIn(MokaChartUnit unit, double value, bool withUnit)
	{
		if (!double.IsFinite(value) || value <= 0)
		{
			return null;
		}

		for (int i = unit.Steps.Count - 1; i >= 0; i--)
		{
			MokaUnitStep step = unit.Steps[i];
			double count = step.Factor > 0 ? value / step.Factor : double.NaN;
			if (count >= 1 && Math.Abs(count - Math.Round(count)) < 1e-9 * Math.Max(1, count))
			{
				string number = Math.Round(count).ToString("#,##0", CultureInfo.CurrentCulture);
				return withUnit ? unit.Join(number, step.Suffix) : number;
			}
		}

		return null;
	}

	private ChartXKind XKindOf(int panel, out (double Min, double Max)? numbers)
	{
		numbers = null;
		bool hasTimes = Times is { Count: > 0 };
		switch (XAxis?.Scale)
		{
			case MokaAxisScale.Category:
				return ChartXKind.Category;
			case MokaAxisScale.Time:
				return hasTimes ? ChartXKind.Time : ChartXKind.Category;
		}

		if (hasTimes)
		{
			return ChartXKind.Time;
		}

		numbers = NumericXRange(panel);
		return numbers is null ? ChartXKind.Category : ChartXKind.Number;
	}

	private (double From, double To)? ZoomSpan()
	{
		if (CurrentRange is not { } range)
		{
			return null;
		}

		double from = Math.Min(range.From.Value, range.To.Value);
		double to = Math.Max(range.From.Value, range.To.Value);
		return double.IsFinite(from) && double.IsFinite(to) && to > from ? (from, to) : null;
	}

	/// <summary>
	///     <see cref="TimeAxisSpan" /> worked out now, for a rebuild, whose geometry version hasn't moved yet: the data's
	///     first and last times, moved by the axis's ends. A fixed end wins and a suggested one only widens. A span with no
	///     width gets half an hour either side, so a single point sits in the middle of an hour.
	/// </summary>
	internal (double From, double To) MeasureTimeSpan()
	{
		double first = double.PositiveInfinity;
		double last = double.NegativeInfinity;
		foreach (DateTimeOffset time in Times ?? [])
		{
			double value = MokaChartX.FromDateTimeOffset(time).Value;
			first = Math.Min(first, value);
			last = Math.Max(last, value);
		}

		double pad = TimeEdgePad is var edge && double.IsFinite(edge) && edge > 0 ? edge : 0;
		double from = Finite(XAxis?.Min) ?? Math.Min(first - pad, Finite(XAxis?.SuggestedMin) ?? double.PositiveInfinity);
		double to = Finite(XAxis?.Max) ?? Math.Max(last + pad, Finite(XAxis?.SuggestedMax) ?? double.NegativeInfinity);
		if (!double.IsFinite(from) || !double.IsFinite(to))
		{
			(from, to) = double.IsFinite(from) ? (from, from) : double.IsFinite(to) ? (to, to) : (0, 0);
		}

		if (to < from)
		{
			(from, to) = (to, from);
		}

		if (to == from)
		{
			double half = TimeSpan.FromMinutes(30).TotalMilliseconds;
			(from, to) = (from - half, to + half);
		}

		return (from, to);
	}

	private static double? Finite(double? value) => value is { } number && double.IsFinite(number) ? number : null;

	private double InterpolatedCategoryShare(double index, int count)
	{
		if (count <= 0 || !double.IsFinite(index))
		{
			return double.NaN;
		}

		int below = (int)Math.Floor(index);
		if (below == index && below >= 0 && below < count)
		{
			return CategoryShare(below, count);
		}

		int low = Math.Clamp(below, 0, Math.Max(0, count - 2));
		int high = Math.Min(count - 1, low + 1);
		double first = CategoryShare(low, count);
		double next = high == low ? first + 1 : CategoryShare(high, count);
		return first + ((index - low) * (next - first));
	}

	private ColumnPlotModel BuildColumnModel(int panel)
	{
		MokaChartAxis? axis = ValueAxisOptions;
		(double min, double max) = ValueRange(panel);
		MokaScale scale = ValueScale(axis, min, max);
		List<PlotTick> ticks = TicksOf(scale, axis, true, true);
		ChartXFrame frame = XFrame(panel);
		bool showX = ShowXLabels && (XAxis?.Visible ?? true);
		MokaAxisLabelFit fit = XAxis?.Fit ?? LabelFit ?? MokaAxisLabelFit.Auto;
		double? scroll = fit == MokaAxisLabelFit.Scroll && frame.Kind == ChartXKind.Category
			? Math.Max(1, CategoryCount) * PositiveOr(XAxis?.MinCategoryEm, DefaultCategoryEm)
			: null;
		bool reverse = axis?.Reverse ?? false;
		PlotAnnotations annotations = ChartAnnotations.Build(Annotations, TargetLow, TargetHigh,
			value => Flip(scale.Share(value), reverse), value => WriteValue(value, axis), frame, Words);

		return new ColumnPlotModel(
			scale,
			ticks,
			Widest(ticks),
			frame.Kind,
			XAxisOf(frame, fit, showX, scroll ?? 0),
			axis?.Grid ?? Grid ?? Spec.Grid,
			axis?.Side ?? ValueSide ?? Spec.ValueSide,
			axis?.TickLabels ?? TickLabels ?? Spec.TickLabels,
			axis?.Visible ?? true,
			showX,
			AxisTitle(axis, scale) ?? DefaultValueTitle,
			XAxis?.Title ?? DefaultXTitle,
			scroll,
			annotations,
			axis?.TickTemplate,
			XAxis?.TickTemplate,
			BaselineShare(scale, axis));
	}

	private RowPlotModel BuildRowModel()
	{
		MokaChartAxis? axis = XAxis;
		int rows = Math.Max(0, RowCount);
		double rowHeight = RowHeight is { } height && double.IsFinite(height) && height > 0 ? height : DefaultRowHeight;
		bool virtualize = rows > 0 && (Virtualize ?? rows > VirtualizeAbove) && RendererInfo.IsInteractive;
		bool reverse = axis?.Reverse ?? false;

		MokaScale? scale = null;
		List<PlotTick> ticks;
		PlotAxis labels;
		PlotAnnotations annotations = PlotAnnotations.Empty;
		if (RowTimeRange is { } span && span.To > span.From)
		{
			ticks = TimeTicksAcross(span.From, span.To, reverse, out labels);
			double start = MokaChartX.FromDateTimeOffset(span.From).Value;
			double width = Math.Max(1, MokaChartX.FromDateTimeOffset(span.To).Value - start);
			TimeSpan whole = span.To - span.From;
			MokaTimeTickOptions options = TimeOptions();
			annotations = ChartAnnotations.Build(Annotations, TargetLow, TargetHigh, value => Flip((value - start) / width, reverse),
				value => TimeText(value, whole, options), null, Words);
		}
		else
		{
			(double min, double max) = ValueRange(0);
			MokaScale values = ValueScale(axis, min, max);
			scale = values;
			ticks = TicksOf(values, axis, true, false);
			labels = FlatAxis(ticks.Select(tick => (tick.Text, tick.Share, tick.Tick, tick.Minor ? 1 : 2)).ToList(), axis?.MaxLabelEm,
				ticks.Count);
			annotations = ChartAnnotations.Build(Annotations, TargetLow, TargetHigh, value => Flip(values.Share(value), reverse),
				value => WriteValue(value, axis), null, Words);
		}

		// A line down the rows hides with its label, so a narrow chart isn't striped with lines that read nothing.
		Dictionary<int, string> labelClasses = labels.Labels.ToDictionary(label => label.Tick.Index, label => label.Class);
		ticks = [.. ticks.Select(tick => tick with { HideClass = HideOf(labelClasses.GetValueOrDefault(tick.Tick.Index)) })];

		double nameEm = 0;
		double figureEm = 0;
		bool figures = false;
		double? longestName = YAxis?.MaxLabelEm;
		for (int i = 0; i < rows; i++)
		{
			nameEm = Math.Max(nameEm, ChartAxisLayout.TextEm(RowLabel(i), longestName));
			if (RowFigure(i) is { } figure)
			{
				figures = true;
				figureEm = Math.Max(figureEm, ChartAxisLayout.TextEm(figure, null));
			}
		}

		int? above = ChartAxisLayout.WidthFor(Math.Min(NamesAboveMostEm, Math.Max(NamesAboveLeastEm, nameEm / NameShare)));

		return new RowPlotModel(
			scale,
			ticks,
			labels,
			rows,
			rowHeight,
			virtualize,
			MaxHeight,
			axis?.Grid ?? Grid ?? Spec.Grid,
			nameEm,
			figureEm,
			figures,
			above,
			axis?.Visible ?? true,
			(scale is null ? axis?.Title : AxisTitle(axis, scale)) ?? DefaultValueTitle,
			annotations,
			axis?.TickTemplate,
			scale is null ? double.NaN : BaselineShare(scale, axis));
	}

	private static string HideOf(string? labelClass)
	{
		if (labelClass is null)
		{
			return "moka-chart-plot__xtick--hide";
		}

		foreach (string part in labelClass.Split(' ', StringSplitOptions.RemoveEmptyEntries))
		{
			if (part.StartsWith("moka-chart-plot__xtick--w", StringComparison.Ordinal) || part == "moka-chart-plot__xtick--hide")
			{
				return part;
			}
		}

		return string.Empty;
	}

	private PlotAxis XAxisOf(ChartXFrame frame, MokaAxisLabelFit fit, bool showLabels, double knownWidthEm)
	{
		switch (frame.Kind)
		{
			case ChartXKind.Time:
				TimeTicksAcross(new MokaChartX(frame.From, true).Time, new MokaChartX(frame.To, true).Time, frame.Reverse,
					out PlotAxis time);
				return showLabels ? time : time with { Labels = [] };
			case ChartXKind.Number when frame.Scale is { } scale:
				if (!showLabels)
				{
					return PlotAxis.Empty;
				}

				List<PlotTick> ticks = TicksOf(scale, XAxis, false, false);
				return FlatAxis([.. ticks.Select(tick => (tick.Text, tick.Share, tick.Tick, tick.Minor ? 1 : 2))], XAxis?.MaxLabelEm,
					ticks.Count);
			default:
				return showLabels ? CategoryAxis(frame, fit, knownWidthEm) : PlotAxis.Empty;
		}
	}

	private PlotAxis CategoryAxis(ChartXFrame frame, MokaAxisLabelFit fit, double knownWidthEm)
	{
		int count = CategoryCount;
		(int start, int end) = CategoryWindow(count);
		List<(string Text, double Share)> labels = [];
		List<int> indices = [];
		int stride = CategoryStride(end - start + 1, knownWidthEm);
		for (int block = start; block <= end; block += stride)
		{
			// The first label with text in each block of the stride; past the last block, the axis's last label.
			int last = Math.Min(end, block + stride - 1);
			for (int i = block; i <= last; i++)
			{
				if (AddCategoryLabel(frame, i, labels, indices))
				{
					break;
				}
			}
		}

		if (stride > 1 && (indices.Count == 0 || indices[^1] != end))
		{
			for (int i = end; i > (indices.Count == 0 ? start - 1 : indices[^1]); i--)
			{
				if (AddCategoryLabel(frame, i, labels, indices))
				{
					break;
				}
			}
		}

		CategoryAxisLayout layout = ChartAxisLayout.Categories(labels, fit, XAxis?.WrapLines ?? 2, XAxis?.MaxLabelEm, knownWidthEm);
		List<PlotLabel> placed =
		[
			.. layout.Labels.Select(spot => new PlotLabel(labels[spot.Index].Text, labels[spot.Index].Share, spot.Class, spot.Style,
				spot.MayBeCut ? labels[spot.Index].Text : null,
				new MokaAxisTick(indices[spot.Index], labels[spot.Index].Text, indices[spot.Index], null, MokaTimeTickLevel.Major)))
		];
		return new PlotAxis(placed, layout.AxisClass, layout.AxisStyle, []);
	}

	// Labels laid out one by one cost time for every category, and the widest axis the stylesheet knows shows a few
	// dozen at most. So past a few hundred, only the first label of every block of a power-of-two stride is laid out:
	// those are the labels the ranking would keep anyway, so the axis thins the same way at a fraction of the cost. A
	// scrolling axis wide enough for every label keeps them all.
	private static int CategoryStride(int count, double knownWidthEm)
	{
		double widestEm = Math.Max(ChartAxisLayout.Widths[^1], knownWidthEm);
		int fits = (int)Math.Ceiling(widestEm / (ChartAxisLayout.GapEm + ChartAxisLayout.CharEm)) + 1;
		int laidOut = Math.Max(CategoryLabelsLaidOut, 2 * fits);
		if (count <= laidOut)
		{
			return 1;
		}

		int stride = 1;
		while ((long)count > (long)stride * laidOut)
		{
			stride <<= 1;
		}

		return stride;
	}

	private bool AddCategoryLabel(ChartXFrame frame, int index, List<(string Text, double Share)> labels, List<int> indices)
	{
		string text = CategoryText(index);
		double share = frame.Share(index);
		if (string.IsNullOrWhiteSpace(text) || !double.IsFinite(share) || share < -0.0001 || share > 1.0001)
		{
			return false;
		}

		labels.Add((text, Math.Clamp(share, 0, 1)));
		indices.Add(index);
		return true;
	}

	private List<PlotTick> TimeTicksAcross(DateTimeOffset from, DateTimeOffset to, bool reverse, out PlotAxis axis)
	{
		IReadOnlyList<MokaTimeTick> ticks = MokaTimeTicks.For(from, to, TimeOptions());
		double start = MokaChartX.FromDateTimeOffset(from).Value;
		double span = Math.Max(1, MokaChartX.FromDateTimeOffset(to).Value - start);

		List<AxisSpot> spots = [];
		List<MokaAxisTick> stands = [];
		List<double> layers = [];
		List<PlotTick> grid = [];
		List<PlotLine> lines = [];
		for (int i = 0; i < ticks.Count; i++)
		{
			MokaTimeTick tick = ticks[i];
			double value = MokaChartX.FromDateTimeOffset(tick.At).Value;
			double share = Flip((value - start) / span, reverse);
			string text = tick.Text;
			MokaAxisTick stand = new(value, text, i, tick.At, tick.Level);
			stands.Add(stand);
			spots.Add(new AxisSpot(i, text, share, ChartAxisLayout.TextEm(text, XAxis?.MaxLabelEm) / 2, 0));
			layers.Add(TimeLayer(tick));
			grid.Add(new PlotTick(value, share, text, null, false, tick.Level == MokaTimeTickLevel.Minor, stand));
			if (tick.Level is MokaTimeTickLevel.Day or MokaTimeTickLevel.Month or MokaTimeTickLevel.Year && share > 0 && share < 1)
			{
				lines.Add(new PlotLine(share, $"moka-chart-plot__vline moka-chart-plot__vline--{LevelName(tick.Level)}"));
			}
		}

		List<PlotLabel> labels = [];
		foreach (AxisLabelSpot spot in ChartAxisLayout.Flat(spots, XAxis?.MaxLabelEm, layers))
		{
			MokaTimeTick tick = ticks[spot.Index];
			string classes = tick.Level is MokaTimeTickLevel.Minor or MokaTimeTickLevel.Major
				? spot.Class
				: $"{spot.Class} moka-chart-plot__xtick--{LevelName(tick.Level)}";
			labels.Add(new PlotLabel(stands[spot.Index].Text, spots[spot.Index].Share, classes, spot.Style,
				spot.MayBeCut ? stands[spot.Index].Text : null, stands[spot.Index]));
		}

		axis = new PlotAxis(labels, "moka-chart-plot__x--fit-thin moka-chart-plot__x--time", null, lines);
		return grid;
	}

	private static PlotAxis FlatAxis(List<(string Text, double Share, MokaAxisTick Tick, int Weight)> ticks, double? maxLabelEm,
		int count)
	{
		List<AxisSpot> spots = [];
		int majors = ticks.Count(tick => tick.Weight > 1);
		int majorPosition = 0;
		for (int i = 0; i < ticks.Count; i++)
		{
			(string text, double share, _, int weight) = ticks[i];
			int rank = weight > 1 ? ChartAxisLayout.MajorRank(ChartAxisLayout.RankOf(majorPosition++, majors)) : Math.Min(i, 1 << 29);
			spots.Add(new AxisSpot(i, text, share, ChartAxisLayout.TextEm(text, maxLabelEm) / 2, rank));
		}

		List<PlotLabel> labels =
		[
			.. ChartAxisLayout.Flat(spots, maxLabelEm).Select(spot => new PlotLabel(ticks[spot.Index].Text, ticks[spot.Index].Share,
				spot.Class, spot.Style, spot.MayBeCut ? ticks[spot.Index].Text : null, ticks[spot.Index].Tick))
		];
		return new PlotAxis(labels, count > 0 ? "moka-chart-plot__x--fit-thin" : string.Empty, null, []);
	}

	private List<PlotTick> TicksOf(MokaScale scale, MokaChartAxis? axis, bool valueAxis, bool vertical)
	{
		bool reverse = axis?.Reverse ?? false;
		bool log = scale.Kind == MokaScaleKind.Log;
		int count = scale.Ticks.Count;
		double[] shares = new double[count];
		bool[] majors = new bool[count];
		for (int i = 0; i < count; i++)
		{
			double tick = scale.Ticks[i];
			shares[i] = Flip(scale.Share(tick), reverse);
			majors[i] = !log || IsPowerOfTen(tick);
		}

		string?[] heights = vertical ? ChartAxisLayout.TickHeightClasses(shares, majors) : new string?[count];
		double baseline = BaselineOf(scale);
		double tolerance = Math.Abs(scale.Max - scale.Min) * 1e-9;
		int baselineIndex = -1;
		for (int i = 0; i < count; i++)
		{
			if (Math.Abs(scale.Ticks[i] - baseline) <= tolerance)
			{
				baselineIndex = i;
			}
		}

		List<PlotTick> ticks = [];
		for (int i = 0; i < count; i++)
		{
			double value = scale.Ticks[i];
			string text = TickText(scale, axis, value, valueAxis);
			bool minor = !majors[i];
			ticks.Add(new PlotTick(value, shares[i], text, heights[i], i == baselineIndex, minor,
				new MokaAxisTick(value, text, i, null, minor ? MokaTimeTickLevel.Minor : MokaTimeTickLevel.Major)));
		}

		return ticks;
	}

	// The line the values stand on: zero when the axis crosses it, else its bottom.
	private static double BaselineOf(MokaScale scale) =>
		scale.Kind != MokaScaleKind.Log && scale.Min <= 0 && scale.Max >= 0 ? 0 : scale.Min;

	private static double BaselineShare(MokaScale scale, MokaChartAxis? axis) =>
		Flip(scale.Share(BaselineOf(scale)), axis?.Reverse ?? false);

	private static string? AxisTitle(MokaChartAxis? axis, MokaScale scale)
	{
		string? unit = axis is { ShowUnit: false } && scale.Kind != MokaScaleKind.Log && !string.IsNullOrEmpty(scale.Unit.Suffix)
			? scale.Unit.Suffix
			: null;
		return (axis?.Title, unit) switch
		{
			({ Length: > 0 } title, { } suffix) => $"{title} ({suffix})",
			({ Length: > 0 } title, null) => title,
			(_, { } suffix) => suffix,
			_ => null
		};
	}

	private string FormatPlain(double value) =>
		Formatter?.Invoke(value) ?? value.ToString(TickFormat, CultureInfo.CurrentCulture);

	private static double TimeLayer(MokaTimeTick tick) => tick.Level switch
	{
		MokaTimeTickLevel.Year => -3,
		MokaTimeTickLevel.Month => -2,
		MokaTimeTickLevel.Day => -1,
		_ => Math.Round(tick.NeedEm, 3)
	};

	private static string LevelName(MokaTimeTickLevel level) => level switch
	{
		MokaTimeTickLevel.Year => "year",
		MokaTimeTickLevel.Month => "month",
		_ => "day"
	};

	private static bool IsPowerOfTen(double value)
	{
		if (value <= 0 || !double.IsFinite(value))
		{
			return false;
		}

		double exponent = Math.Log10(value);
		return Math.Abs(exponent - Math.Round(exponent)) < 1e-9;
	}

	private static double Flip(double share, bool reverse) => reverse ? 1 - share : share;

	private static double PositiveOr(double? value, double fallback) =>
		value is { } v && double.IsFinite(v) && v > 0 ? v : fallback;

	private List<double> Reach()
	{
		List<double> reach = [];
		foreach (MokaChartAnnotation annotation in Annotations ?? [])
		{
			switch (annotation)
			{
				case MokaChartThreshold threshold:
					reach.Add(threshold.Value);
					break;
				case MokaChartBand band:
					reach.Add(band.Low);
					reach.Add(band.High);
					break;
			}
		}

		if (TargetLow is { } low)
		{
			reach.Add(low);
		}

		if (TargetHigh is { } high)
		{
			reach.Add(high);
		}

		return reach;
	}

	private static string Widest(List<PlotTick> ticks) =>
		ticks.Count == 0 ? string.Empty : ticks.MaxBy(tick => tick.Text.Length)!.Text;
}

/// <summary>
///     A column chart's x axis as it is drawn: what it counts in, the values at the plot's two edges, and how a value
///     maps to a share of the plot's width. Times count in Unix milliseconds, categories by index.
/// </summary>
internal sealed class ChartXFrame
{
	private readonly Func<double, double>? _categoryShare;
	private readonly Func<double, string> _text;

	/// <summary>Builds a frame.</summary>
	/// <param name="kind">What the axis counts in.</param>
	/// <param name="from">The value at the plot's start.</param>
	/// <param name="to">The value at the plot's end.</param>
	/// <param name="scale">The scale of a numeric axis.</param>
	/// <param name="reverse">Whether the axis runs the other way.</param>
	/// <param name="categoryShare">Where an index sits, for categories that aren't zoomed.</param>
	/// <param name="text">A value written for a tooltip or a marker.</param>
	public ChartXFrame(ChartXKind kind, double from, double to, MokaScale? scale, bool reverse,
		Func<double, double>? categoryShare, Func<double, string> text)
	{
		Kind = kind;
		From = from;
		To = to;
		Scale = scale;
		Reverse = reverse;
		_categoryShare = categoryShare;
		_text = text;
	}

	/// <summary>What the axis counts in.</summary>
	public ChartXKind Kind { get; }

	/// <summary>The value at the plot's start: the left edge, or the right when reversed.</summary>
	public double From { get; }

	/// <summary>The value at the plot's end.</summary>
	public double To { get; }

	/// <summary>The scale of a numeric axis; null otherwise.</summary>
	public MokaScale? Scale { get; }

	/// <summary>Whether the axis runs the other way.</summary>
	public bool Reverse { get; }

	/// <summary>Where a value sits, as a share of the plot's width: 0 at the start, 1 at the end, not clamped.</summary>
	/// <param name="x">A time as Unix milliseconds, a number, or a category's index.</param>
	public double Share(double x)
	{
		double share = _categoryShare is not null
			? _categoryShare(x)
			: Scale is { Kind: MokaScaleKind.Log } log
				? log.Share(x)
				: To > From ? (x - From) / (To - From) : 0.5;
		return Reverse ? 1 - share : share;
	}

	/// <summary>A value written as a tooltip or a marker says it: the time with as much of the date as the span needs.</summary>
	/// <param name="x">The value.</param>
	public string Text(double x) => _text(x);
}
