using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A radar chart: a few series of values across a handful of axes, each series a closed shape around the center, for
///     seeing an item's profile at a glance, such as a product's scores or where a server's load sits across its
///     resources.
/// </summary>
/// <remarks>
///     A shape is hard to read exactly: each value is a length along a spoke at its own angle, and the area grows with
///     the square of the values, so neighbouring axes change how big a shape looks. To compare values, a grouped or
///     horizontal <see cref="MokaBarChart" /> or a <see cref="MokaDotPlot" /> reads more exactly. A radar earns its place
///     when the shape is the point, with three to twelve axes and up to three series. The axes are drawn clockwise from
///     twelve o'clock, in the order given. Pointing at an axis, or stepping to it with the arrow keys, reads every
///     series' value on it.
/// </remarks>
public partial class MokaRadarChart : MokaChartBase
{
	private const double Center = 100;
	private const double Radius = 100;
	private const double WedgeReach = 1.1;
	private const double AxisFloor = 3;
	private const double CharWidthEm = 0.62;
	private const double SideLabelMaxEm = 9;
	private const double SideLabelMinEm = 3;
	private const double SideReach = 0.2;
	private const int MaxRings = 10;

	private readonly string _uid = Guid.NewGuid().ToString("N")[..12];
	private MokaRadarAxis[] _axes = [];
	private MokaRadarSeries[] _series = [];
	private MokaRadarAxis[]? _axesSnapshot;
	private MokaRadarSeries[]? _seriesSnapshot;
	private MokaScale[] _scales = [];
	private Spoke[] _spokes = [];
	private Shape[] _shapes = [];
	private Ring[] _rings = [];
	private TickLabel[] _ticks = [];
	private IReadOnlyList<ChartSeriesInfo> _seriesInfo = [];
	private string _sideWidth = "3em";
	private bool _hasData;

	/// <summary>The axes, drawn clockwise from twelve o'clock in this order. Three to twelve read well; fewer than three draw nothing.</summary>
	[Parameter]
	public IReadOnlyList<MokaRadarAxis>? Axes { get; set; }

	/// <summary>The shapes, each with one value per axis. Up to three read well; past that they cover each other.</summary>
	[Parameter]
	public IReadOnlyList<MokaRadarSeries>? Series { get; set; }

	/// <summary>Whether every axis shares one scale, or each has its own. Defaults to one shared scale.</summary>
	[Parameter]
	public MokaRadarScale Scale { get; set; }

	/// <summary>
	///     The value at the center on a shared scale. Null starts at zero, or lower when a value is below zero. Each axis
	///     sets its own under <see cref="MokaRadarScale.PerAxis" />.
	/// </summary>
	[Parameter]
	public double? Min { get; set; }

	/// <summary>
	///     The value at the edge on a shared scale, such as 10 for scores out of 10. Null takes a round number at or above
	///     the largest value. Each axis sets its own under <see cref="MokaRadarScale.PerAxis" />.
	/// </summary>
	[Parameter]
	public double? Max { get; set; }

	/// <summary>
	///     About how many rings mark the scale. On a shared scale they fall on round values, so the count can differ by
	///     one; on scales per axis they mark equal parts of each axis. Defaults to 4, at most 10.
	/// </summary>
	[Parameter]
	public int Rings { get; set; } = 4;

	/// <summary>The rings' shape: straight lines between the axes, or circles. Defaults to straight lines.</summary>
	[Parameter]
	public MokaRadarGridShape GridShape { get; set; }

	/// <summary>
	///     How the rings are drawn. Null takes the look's. <see cref="MokaChartGrid.Baseline" /> draws the outer ring alone,
	///     and <see cref="MokaChartGrid.None" /> none; the spokes are the axes and always show.
	/// </summary>
	[Parameter]
	public MokaChartGrid? Grid { get; set; }

	/// <summary>Where a dot marks each value. Null takes the look's: on every value, or only on the axis pointed at.</summary>
	[Parameter]
	public MokaChartPoints? Points { get; set; }

	/// <summary>
	///     Whether each shape is filled with a light tint of its color. Null fills it.
	///     <see cref="MokaChartArea.None" /> draws outlines only, which reads better past two series.
	/// </summary>
	[Parameter]
	public MokaChartArea? Area { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-radarchart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-radarchart--{SizeToKebab(Size)}")
		.AddClass(GridClass)
		.AddClass("moka-radarchart--points-always", ResolvedPoints == MokaChartPoints.Always)
		.AddClass("moka-radarchart--points-none", ResolvedPoints == MokaChartPoints.None)
		.AddClass(ModernRootClasses)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--moka-radarchart-height", HeightValue)
		.AddStyle("--_radar-side", _sideWidth)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => _shapes.Length == 0
		? Words.RadarChart
		: Words.Described(Words.RadarChart, string.Join(", ", _shapes.Select(shape => shape.Info.Name)));

	/// <inheritdoc />
	internal override bool HasData => _hasData;

	/// <inheritdoc />
	internal override bool HitsMarks => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override string? OwnKeysText => Words.RadarKeys;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _hasData ? _spokes.Length : 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _seriesInfo;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => _hasData
		? new ChartTableModel([Words.Axis, .. _shapes.Select(shape => shape.Info.Name)], _spokes.Length,
			(row, column) => column == 0 ? _axes[row].Name : ValueText(row, _shapes[column - 1].Values[row]),
			(row, column) => column == 0
				? _axes[row].Name
				: double.IsFinite(_shapes[column - 1].Values[row])
					? _shapes[column - 1].Values[row].ToString(CultureInfo.InvariantCulture)
					: string.Empty)
		: null;

	/// <summary>One axis as drawn: its spoke, the wedge that takes the pointer for it, and its label.</summary>
	private sealed record Spoke(int Index, string Name, string X, string Y, string Wedge, string LabelClass, string LabelStyle,
		string Title);

	/// <summary>One visible series as drawn: its fill, its outline, and a dot per value.</summary>
	private sealed record Shape(
		ChartSeriesInfo Info,
		int Position,
		IReadOnlyList<double> Values,
		string? Fill,
		string Outline,
		IReadOnlyList<Dot> Dots);

	/// <summary>A dot on a value, carrying its axis so pointing at its axis lights it.</summary>
	private sealed record Dot(int Axis, string Path);

	/// <summary>A ring of the grid: a polygon's points, or a circle's radius.</summary>
	private sealed record Ring(string? Path, string? CircleRadius);

	/// <summary>A value on the shared scale, written along the top axis.</summary>
	private sealed record TickLabel(string Text, string Style);

	private MokaChartGrid ResolvedGrid => Grid ?? Spec.Grid;

	private MokaChartPoints ResolvedPoints => Points ?? Spec.Points;

	private bool Filled => (Area ?? MokaChartArea.Flat) != MokaChartArea.None;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	private string? SvgHidden => Interactive ? "true" : null;

	private string GridClass => ResolvedGrid switch
	{
		MokaChartGrid.Hairlines => "moka-radarchart--grid-hairlines",
		MokaChartGrid.Dotted => "moka-radarchart--grid-dotted",
		MokaChartGrid.Baseline => "moka-radarchart--grid-baseline",
		MokaChartGrid.None => "moka-radarchart--grid-none",
		_ => "moka-radarchart--grid-lines"
	};

	private string PatternId(int position) => $"moka-radar-{_uid}-h{position.ToString(CultureInfo.InvariantCulture)}";

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name switch
	{
		nameof(Axes) => ChartSnapshot.SameItems(after as IReadOnlyList<MokaRadarAxis>, _axesSnapshot),
		nameof(Series) => SameSeries(after as IReadOnlyList<MokaRadarSeries>),
		_ => base.SameValue(name, before, after)
	};

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (!_hasData || drawn < 0 || drawn >= _spokes.Length)
		{
			return null;
		}

		string heading = _axes[drawn].Name;
		List<ChartActiveRow> rows = new(_shapes.Length);
		List<MokaChartPoint> points = new(_shapes.Length);
		foreach (Shape shape in _shapes)
		{
			double value = shape.Values[drawn];
			string text = ValueText(drawn, value);
			rows.Add(new ChartActiveRow(shape.Info.Name, shape.Info.ColorClass, shape.Info.ColorStyle, text, null,
				shape.Position == series));
			points.Add(new MokaChartPoint(shape.Info.Key, shape.Info.Name, shape.Position, drawn, value, text)
			{
				Category = heading
			});
		}

		string cell = rows.Count == 0
			? heading
			: $"{heading}, {string.Join(", ", rows.Select(row => $"{row.Name} {row.ValueText}"))}";
		MokaChartPoint? focus = series >= 0 ? points.FirstOrDefault(point => point.Series == series) : null;
		return new ChartActiveInfo(heading, rows, [], cell,
			new MokaChartTooltipContext(drawn, heading, points, focus, pinnedDrawn, []));
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		bool versioned = DataVersion is not null;
		_axes = [.. Axes ?? []];
		_series = [.. Series ?? []];
		_axesSnapshot = versioned ? null : _axes;
		_seriesSnapshot = versioned ? null : [.. _series.Select(s => s with { Values = [.. s.Values] })];

		List<ChartSeriesInfo> info = new(_series.Length);
		List<(MokaRadarSeries Series, int Position, ChartSeriesInfo Info)> shown = [];
		for (int i = 0; i < _series.Length; i++)
		{
			MokaRadarSeries one = _series[i];
			string key = one.Key ?? one.Name;
			int slot = SlotFor(i, one.Slot);
			bool hidden = IsHidden(key);
			bool muted = IsMuted(key);
			ChartSeriesInfo series = new(key, one.Name, slot, SeriesClass(slot, one.Color, muted),
				SeriesStyle(slot, one.ColorValue), Filled ? ChartMark.Area : ChartMark.Line, hidden, muted, null);
			info.Add(series);
			if (!hidden)
			{
				shown.Add((one, i, series));
			}
		}

		_seriesInfo = info;
		int count = _axes.Length;
		_hasData = count >= AxisFloor
		           && shown.Any(s => Enumerable.Range(0, count).Any(axis => double.IsFinite(ValueAt(s.Series, axis))));
		if (!_hasData)
		{
			_scales = [];
			_spokes = [];
			_shapes = [];
			_rings = [];
			_ticks = [];
			_sideWidth = Em(SideLabelMinEm);
			return;
		}

		_scales = BuildScales(shown.Select(s => s.Series).ToArray());
		_spokes = BuildSpokes();
		_shapes = [.. shown.Select(s => BuildShape(s.Series, s.Position, s.Info))];
		_rings = BuildRings();
		_ticks = BuildTicks();
	}

	private bool SameSeries(IReadOnlyList<MokaRadarSeries>? series)
	{
		if (series is null || _seriesSnapshot is null)
		{
			return series is null && _seriesSnapshot is null;
		}

		if (series.Count != _seriesSnapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < series.Count; i++)
		{
			MokaRadarSeries now = series[i];
			MokaRadarSeries before = _seriesSnapshot[i];
			if (now.Name != before.Name || now.Key != before.Key || now.Slot != before.Slot || now.Color != before.Color
			    || now.ColorValue != before.ColorValue || !ChartSnapshot.SameItems(now.Values, before.Values))
			{
				return false;
			}
		}

		return true;
	}

	private static double ValueAt(MokaRadarSeries series, int axis) =>
		axis < series.Values.Count ? series.Values[axis] : double.NaN;

	// One scale for every axis, or one each; either way a scale per axis, so the rest of the chart reads one list.
	private MokaScale[] BuildScales(MokaRadarSeries[] shown)
	{
		int rings = Math.Clamp(Rings, 1, MaxRings);
		if (Scale == MokaRadarScale.Shared)
		{
			double[] values = [.. shown.SelectMany(s => Enumerable.Range(0, _axes.Length).Select(axis => ValueAt(s, axis)))];
			MokaScale shared = MokaNiceScale.Linear(values, new MokaScaleOptions { Min = Min, Max = Max, TickCount = rings });
			return [.. _axes.Select(_ => shared)];
		}

		return
		[
			.. _axes.Select((axis, index) =>
			{
				double[] values = [.. shown.Select(s => ValueAt(s, index))];
				return MokaNiceScale.Linear(values, new MokaScaleOptions { Min = axis.Min, Max = axis.Max, TickCount = rings });
			})
		];
	}

	// How far out a value sits, 0 at the center and 1 at the edge, with a reversed axis running the other way. A value
	// past the ends sits on them; the tooltip and the table still give it.
	private double Reach(int axis, double value)
	{
		double share = Math.Clamp(_scales[axis].Share(value), 0, 1);
		return _axes[axis].Reversed ? 1 - share : share;
	}

	private Spoke[] BuildSpokes()
	{
		int count = _axes.Length;
		double half = Math.PI / count;
		double side = SideLabelMinEm;
		Spoke[] spokes = new Spoke[count];
		for (int i = 0; i < count; i++)
		{
			double angle = AngleOf(i, count);
			(double sin, double cos) = Math.SinCos(angle);
			(double x, double y) = Point(angle, 1);
			(double ax, double ay) = Point(angle - half, WedgeReach);
			(double bx, double by) = Point(angle, WedgeReach);
			(double cx, double cy) = Point(angle + half, WedgeReach);
			string wedge = $"M{Num(Center)} {Num(Center)}L{Num(ax)} {Num(ay)}L{Num(bx)} {Num(by)}L{Num(cx)} {Num(cy)}Z";

			bool atSide = Math.Abs(sin) > SideReach;
			if (atSide)
			{
				side = Math.Max(side, Math.Min(SideLabelMaxEm, _axes[i].Name.Length * CharWidthEm));
			}

			string labelClass = new CssBuilder("moka-radarchart__label")
				.AddClass(sin > SideReach ? "moka-radarchart__label--start"
					: sin < -SideReach ? "moka-radarchart__label--end"
					: "moka-radarchart__label--center")
				.AddClass(-cos < -SideReach ? "moka-radarchart__label--above"
					: -cos > SideReach ? "moka-radarchart__label--below"
					: "moka-radarchart__label--middle")
				.Build();
			string labelStyle = new StyleBuilder()
				.AddStyle("--_x", Num(sin, "0.####"))
				.AddStyle("--_y", Num(-cos, "0.####"))
				.Build()!;
			spokes[i] = new Spoke(i, _axes[i].Name, Num(x), Num(y), wedge, labelClass, labelStyle, AxisTitle(i));
		}

		_sideWidth = Em(side);
		return spokes;
	}

	private Shape BuildShape(MokaRadarSeries series, int position, ChartSeriesInfo info)
	{
		int count = _axes.Length;
		double[] values = new double[count];
		(double X, double Y)?[] corners = new (double X, double Y)?[count];
		List<Dot> dots = new(count);
		for (int axis = 0; axis < count; axis++)
		{
			double value = ValueAt(series, axis);
			values[axis] = value;
			if (!double.IsFinite(value))
			{
				continue;
			}

			(double x, double y) = Point(AngleOf(axis, count), Reach(axis, value));
			corners[axis] = (x, y);
			dots.Add(new Dot(axis, $"M{Num(x)} {Num(y)}h0"));
		}

		bool whole = corners.All(corner => corner is not null);
		string outline = whole ? Closed(corners) : Broken(corners);
		return new Shape(info, position, values, whole && Filled ? outline : null, outline, dots);
	}

	// Every value is there: one closed shape.
	private static string Closed((double X, double Y)?[] corners)
	{
		StringBuilder path = new();
		for (int i = 0; i < corners.Length; i++)
		{
			(double x, double y) = corners[i]!.Value;
			path.Append(i == 0 ? 'M' : 'L').Append(Num(x)).Append(' ').Append(Num(y));
		}

		return path.Append('Z').ToString();
	}

	// A value is missing: the outline breaks there rather than dropping to the center, which would read as zero. Each
	// run of values starts just after a gap and runs on round the shape to the next one.
	private static string Broken((double X, double Y)?[] corners)
	{
		int count = corners.Length;
		int start = Array.FindIndex(corners, corner => corner is null);
		StringBuilder path = new();
		bool open = false;
		for (int step = 1; step <= count; step++)
		{
			(double X, double Y)? corner = corners[(start + step) % count];
			if (corner is not { } point)
			{
				open = false;
				continue;
			}

			path.Append(open ? 'L' : 'M').Append(Num(point.X)).Append(' ').Append(Num(point.Y));
			open = true;
		}

		return path.ToString();
	}

	private Ring[] BuildRings()
	{
		if (ResolvedGrid == MokaChartGrid.None)
		{
			return [];
		}

		IEnumerable<double> shares = Scale == MokaRadarScale.Shared
			? _scales[0].Ticks.Select(tick => _scales[0].Share(tick))
			: Enumerable.Range(1, Math.Clamp(Rings, 1, MaxRings)).Select(ring => ring / (double)Math.Clamp(Rings, 1, MaxRings));
		double[] reaches = [.. shares.Where(share => share > 1e-9 && share <= 1 + 1e-9).Distinct().Order()];
		if (ResolvedGrid == MokaChartGrid.Baseline)
		{
			reaches = [1];
		}
		else if (reaches.Length == 0 || reaches[^1] < 1 - 1e-9)
		{
			reaches = [.. reaches, 1];
		}

		int count = _axes.Length;
		return
		[
			.. reaches.Select(reach => GridShape == MokaRadarGridShape.Circle
				? new Ring(null, Num(Radius * reach))
				: new Ring(Closed([.. Enumerable.Range(0, count).Select(axis => ((double, double)?)Point(AngleOf(axis, count), reach))]),
					null))
		];
	}

	// The shared scale's values, written up the top axis. Scales per axis differ, so the tooltip and table give theirs.
	private TickLabel[] BuildTicks()
	{
		if (Scale != MokaRadarScale.Shared || ResolvedGrid is MokaChartGrid.None or MokaChartGrid.Baseline)
		{
			return [];
		}

		MokaScale scale = _scales[0];
		bool reversed = _axes[0].Reversed;
		return
		[
			.. scale.Ticks
				.Select(tick => (Tick: tick, Share: scale.Share(tick)))
				.Where(t => t.Share > 1e-9 && t.Share <= 1 + 1e-9)
				.Select(t => new TickLabel(Formatter?.Invoke(t.Tick) ?? scale.Format(t.Tick),
					new StyleBuilder().AddStyle("--_s", Num(reversed ? 1 - t.Share : t.Share, "0.####")).Build()!))
		];
	}

	private string AxisTitle(int axis)
	{
		string values = string.Join(", ", _series
			.Where(s => !IsHidden(s.Key ?? s.Name))
			.Select(s => $"{s.Name} {ValueText(axis, ValueAt(s, axis))}"));
		return values.Length == 0 ? _axes[axis].Name : $"{_axes[axis].Name}: {values}";
	}

	private string ValueText(int axis, double value)
	{
		if (!double.IsFinite(value))
		{
			return Words.NoData;
		}

		Func<double, string>? formatter = _axes[axis].Formatter ?? Formatter;
		return formatter is not null ? formatter(value) : PlainValue(value);
	}

	// Axis 0 points up and the rest follow clockwise.
	private static double AngleOf(int axis, int count) => 2 * Math.PI * axis / count;

	private static (double X, double Y) Point(double angle, double reach)
	{
		(double sin, double cos) = Math.SinCos(angle);
		return (Center + Radius * reach * sin, Center - Radius * reach * cos);
	}

	private static string SeriesGroupClass(Shape shape) => new CssBuilder("moka-radarchart__series")
		.AddClass(shape.Info.ColorClass)
		.Build();

	private string AreaClass => new CssBuilder("moka-radarchart__area")
		.AddClass("moka-radarchart__area--hatched", Textured)
		.Build();

	private string? AreaStyle(Shape shape) => Textured
		? new StyleBuilder().AddStyle("fill", $"url(#{PatternId(shape.Position)})").Build()
		: null;

	// Hatching runs at 45 degrees for odd slots and 135 for even ones, wider from the third slot on, as on the donut.
	private static string HatchTransform(Shape shape) => shape.Info.Slot % 2 == 1 ? "rotate(45)" : "rotate(135)";

	private static string HatchSize(Shape shape) => shape.Info.Slot >= 3 ? "9" : "6";

	private static string Index(int value) => value.ToString(CultureInfo.InvariantCulture);

	private static string Em(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "em";

	// Geometry leaves the component as invariant strings, so an SVG attribute never carries a decimal comma.
	private static string Num(double value, string format = "0.##") =>
		value.ToString(format, CultureInfo.InvariantCulture) is var text && text == "-0" ? "0" : text;
}
