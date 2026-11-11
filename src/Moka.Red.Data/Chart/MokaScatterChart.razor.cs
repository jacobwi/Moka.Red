using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Each dot is one thing placed by two measures, such as devices by connections and data used. A
///     <see cref="MokaScatterPoint.Size" /> on the points makes it a bubble chart, whose areas follow a third measure.
///     The tooltip finds the dot nearest the pointer, so small dots need no aiming. Three series at most read well,
///     since any two dots can touch and more colors can't all be told apart.
/// </summary>
/// <remarks>
///     Dots are one svg path per series, so a series of a hundred thousand points costs one element. Past 2,000 points a
///     series keeps one dot per 2 pixel cell of the plot, which keeps its shape and the dots the pointer finds are the
///     ones drawn. Bubbles are a line each, largest first so the small ones stay on top; past
///     <see cref="MaxBubbles" /> the largest are kept and the summary says so. Pass
///     <see cref="MokaChartBase.DataVersion" /> for large series, so they are compared by version rather than copied.
/// </remarks>
public partial class MokaScatterChart : MokaCartesianChartBase
{
	private const int DedupeAbove = 2000;
	private const int NativeMost = 1000;
	private const double CellPixels = 2;
	private const double DefaultPlotWidth = 800;
	private const double ViewUnits = 10000;

	private SeriesCopy[]? _snapshot;
	private List<ChartSeriesInfo> _series = [];
	private DrawnPoint[] _drawn = [];
	private int[] _bubbleOrder = [];
	private string[] _paths = [];
	private bool[] _dense = [];
	private bool _bubbles;
	private (double Min, double Max)? _xRange;
	private (double Min, double Max) _yRange = (double.NaN, double.NaN);
	private (double From, double To)? _xExtent;
	private int _bubbleTotal;
	private bool _measures;

	/// <summary>The series, each a group of points in one color. Three at most read well.</summary>
	[Parameter]
	public IReadOnlyList<MokaScatterSeries>? Series { get; set; }

	/// <summary>What the measure across is called, in the tooltip and under the x axis, such as "Connections".</summary>
	[Parameter]
	public string? XName { get; set; }

	/// <summary>What the measure up is called, in the tooltip and over the y axis, such as "Data used".</summary>
	[Parameter]
	public string? YName { get; set; }

	/// <summary>What the bubbles' size measures, in the tooltip, such as "CPU cores".</summary>
	[Parameter]
	public string? SizeName { get; set; }

	/// <summary>Writes a bubble's size for the tooltip and the table, such as <c>v =&gt; $"{v:0} cores"</c>.</summary>
	[Parameter]
	public Func<double, string>? SizeFormatter { get; set; }

	/// <summary>The smallest bubble's diameter in pixels. Defaults to 4.</summary>
	[Parameter]
	public double MinSize { get; set; } = 4;

	/// <summary>The largest bubble's diameter in pixels; the others' areas follow their size. Defaults to 28.</summary>
	[Parameter]
	public double MaxSize { get; set; } = 28;

	/// <summary>The most bubbles drawn. Past it the largest are kept and the chart's summary says so. Defaults to 2000.</summary>
	[Parameter]
	public int MaxBubbles { get; set; } = 2000;

	/// <summary>The table's heading for the series a point belongs to. Defaults to "Series".</summary>
	[Parameter]
	public string SeriesLabel { get; set; } = "Series";

	/// <summary>
	///     Says that only the largest bubbles are drawn, given how many are drawn and how many there are, under the plot
	///     and in the summary. Defaults to "The 2,000 largest of 5,400 are drawn".
	/// </summary>
	[Parameter]
	public Func<int, int, string>? LargestText { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-scatter";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(ModernRootClasses)
		.AddClass("moka-scatter--bubbles", _bubbles)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel
	{
		get
		{
			string names = string.Join(", ", _series.Where(s => !s.Hidden).Select(s => s.Name));
			string label = names.Length == 0 ? "Scatter chart" : $"Scatter chart: {names}";
			return Capped is { } capped ? $"{label}. {capped}" : label;
		}
	}

	/// <inheritdoc />
	internal override bool HasData => _drawn.Length > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _series;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Nearest;

	/// <inheritdoc />
	internal override MokaChartZoom ZoomGestures => (Zoom ?? MokaChartZoom.None) & ~MokaChartZoom.Strip;

	/// <inheritdoc />
	internal override bool SyncsByX => false;

	/// <inheritdoc />
	internal override bool MeasuresWidth => _measures;

	/// <inheritdoc />
	internal override int CategoryCount => 0;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _drawn.Length;

	/// <inheritdoc />
	internal override int LatestDrawn(int panel) => _drawn.Length - 1;

	/// <inheritdoc />
	internal override (double From, double To)? XExtent(int panel) => _xExtent;

	/// <inheritdoc />
	internal override string? DefaultXTitle => XName;

	/// <inheritdoc />
	internal override string? DefaultValueTitle => YName;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel
	{
		get
		{
			List<(MokaScatterSeries Series, int Offset)> shown = [];
			int rows = 0;
			foreach (MokaScatterSeries series in Series ?? [])
			{
				if (!IsHidden(KeyOf(series)))
				{
					shown.Add((series, rows));
					rows += series.Points?.Count ?? 0;
				}
			}

			bool sized = _bubbles;
			List<string> headers = [Words.Name, SeriesLabel, XName ?? "x", YName ?? "y"];
			if (sized)
			{
				headers.Add(SizeName ?? "Size");
			}

			return new ChartTableModel(headers, rows,
				(row, column) => Cell(shown, row, column, false),
				(row, column) => Cell(shown, row, column, true));
		}
	}

	/// <summary>The sentence that says only the largest bubbles are drawn, or null when they all are.</summary>
	internal string? Capped => _bubbles && _bubbleTotal > _drawn.Length
		? LargestText?.Invoke(_drawn.Length, _bubbleTotal)
		  ?? $"The {_drawn.Length.ToString("#,0", CultureInfo.CurrentCulture)} largest of {_bubbleTotal.ToString("#,0", CultureInfo.CurrentCulture)} are drawn"
		: null;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	// A dot each with its own native tooltip, while there are few enough for that to be cheap.
	private bool DotsAsLines => _bubbles || (NativeTooltips && _drawn.Length <= NativeMost);

	/// <summary>A drawn point: its series, its index there, where it sits as shares of the plot, and a bubble's diameter.</summary>
	private readonly record struct DrawnPoint(int Series, int Index, float X, float Y, float Diameter);

	/// <summary>A copy of a series as it was drawn, points included, so a list changed in place still counts.</summary>
	private sealed record SeriesCopy(MokaScatterSeries Series, MokaScatterPoint[]? Points);

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Series)
		? SameSeries(after as IReadOnlyList<MokaScatterSeries>, _snapshot)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel) => _yRange;

	/// <inheritdoc />
	internal override (double Min, double Max)? NumericXRange(int panel) => _xRange ?? (0, 1);

	/// <inheritdoc />
	internal override ChartHoverData? HoverData(int panel)
	{
		if (_drawn.Length == 0)
		{
			return null;
		}

		float[] x = new float[_drawn.Length];
		float[][] y = new float[_series.Count][];
		for (int s = 0; s < y.Length; s++)
		{
			y[s] = new float[_drawn.Length];
			Array.Fill(y[s], float.NaN);
		}

		for (int i = 0; i < _drawn.Length; i++)
		{
			DrawnPoint point = _drawn[i];
			x[i] = point.X;
			if (point.Series < y.Length)
			{
				y[point.Series][i] = point.Y;
			}
		}

		ChartXFrame frame = XFrame();
		return new ChartHoverData(frame.From, frame.To, x, [.. y.Select(values => (ReadOnlyMemory<float>)values)]);
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		IReadOnlyList<MokaScatterSeries> all = Series ?? [];
		_snapshot = DataVersion is null ? [.. all.Select(series => new SeriesCopy(series, ChartSnapshot.Copy(series.Points)))] : null;

		_series = [];
		for (int i = 0; i < all.Count; i++)
		{
			MokaScatterSeries series = all[i];
			string key = KeyOf(series);
			int slot = SlotFor(i, series.Slot);
			bool muted = IsMuted(key);
			_series.Add(new ChartSeriesInfo(key, series.Name, slot, SeriesClass(slot, series.Color, muted),
				muted ? null : SeriesStyle(slot, series.ColorValue), ChartMark.Dot, IsHidden(key), muted, null));
		}

		bool logX = XAxis?.Scale == MokaAxisScale.Log;
		bool logY = YAxis?.Scale == MokaAxisScale.Log;
		_bubbles = all.Any(series => !IsHidden(KeyOf(series)) && (series.Points ?? []).Any(point => point.Size is not null));
		_xRange = RangeOf(all, point => point.X, logX);
		_xExtent = _xRange;
		ChartXFrame frame = CreateXFrame();

		// The value axis fits the points the x window shows, as a zoom would expect.
		_yRange = RangeOf(all, point => point.Y, logY, point => frame.Share(point.X) is var share && share >= 0 && share <= 1)
		          ?? (double.NaN, double.NaN);
		MokaScale yScale = ValueScale(YAxis, _yRange.Min, _yRange.Max);
		bool reverseY = YAxis?.Reverse ?? false;

		double width = PlotWidthBucket is > 0 and var measured ? measured : DefaultPlotWidth;
		int columns = Math.Max(1, (int)(width / CellPixels));
		int rows = Math.Max(1, (int)(width * ViewRatio / CellPixels));

		List<DrawnPoint> drawn = [];
		_measures = false;
		_dense = new bool[all.Count];
		for (int s = 0; s < all.Count; s++)
		{
			if (IsHidden(_series[s].Key) || all[s].Points is not { Count: > 0 } points)
			{
				continue;
			}

			double[] xs = new double[points.Count];
			double[] ys = new double[points.Count];
			for (int i = 0; i < points.Count; i++)
			{
				MokaScatterPoint point = points[i];
				double x = frame.Share(point.X);
				double y = yScale.Share(point.Y);
				bool inside = double.IsFinite(x) && double.IsFinite(y) && x >= 0 && x <= 1 && y >= -1e-9 && y <= 1 + 1e-9;
				xs[i] = inside ? x : double.NaN;
				ys[i] = inside ? (reverseY ? 1 - y : y) : double.NaN;
			}

			IEnumerable<int> kept;
			if (!_bubbles && points.Count > DedupeAbove)
			{
				_measures = true;
				_dense[s] = true;
				kept = MokaDownsampler.GridDedupe(xs, ys, columns, rows);
			}
			else
			{
				kept = Enumerable.Range(0, points.Count).Where(i => double.IsFinite(xs[i]));
			}

			foreach (int i in kept)
			{
				drawn.Add(new DrawnPoint(s, i, (float)xs[i], (float)ys[i], 0));
			}
		}

		_bubbleTotal = 0;
		if (_bubbles)
		{
			_bubbleTotal = drawn.Count;
			drawn = [.. drawn.OrderByDescending(point => SizeOf(all, point)).Take(Math.Max(1, MaxBubbles))];
			double largest = drawn.Select(point => SizeOf(all, point)).Where(size => size > 0).DefaultIfEmpty(1).Max();
			double least = Math.Max(1, MinSize);
			double most = Math.Max(least, MaxSize);
			for (int i = 0; i < drawn.Count; i++)
			{
				double size = SizeOf(all, drawn[i]);
				double diameter = size > 0 ? Math.Max(least, most * Math.Sqrt(size / largest)) : least;
				drawn[i] = drawn[i] with { Diameter = (float)diameter };
			}
		}

		_drawn = [.. drawn.OrderBy(point => point.X)];
		_bubbleOrder = _bubbles
			? [.. Enumerable.Range(0, _drawn.Length).OrderByDescending(i => _drawn[i].Diameter).ThenBy(i => i)]
			: [];
		_paths = DotsAsLines ? [] : Paths(_drawn, all.Count);
	}

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _drawn.Length || Series is not { } all)
		{
			return null;
		}

		DrawnPoint at = _drawn[drawn];
		MokaScatterSeries owner = all[at.Series];
		MokaScatterPoint point = owner.Points[at.Index];
		ChartSeriesInfo info = _series[at.Series];
		string yText = YText(point.Y);
		string xText = XText(point.X);
		List<ChartActiveRow> rows =
		[
			new(YName ?? string.Empty, info.ColorClass, info.ColorStyle, yText, null, false),
			new(XName ?? string.Empty, string.Empty, ChartRows.NoKey, xText, null, false)
		];
		if (_bubbles)
		{
			rows.Add(new ChartActiveRow(SizeName ?? string.Empty, string.Empty, ChartRows.NoKey, SizeText(point.Size ?? double.NaN), null,
				false));
		}

		string heading = HeadingOf(owner, point);
		List<string> notes = [.. AnnotationNotes(point.Y, point.X)];
		MokaChartPoint chartPoint = new(info.Key, owner.Name, at.Series, at.Index, point.Y, yText)
		{
			Category = point.Label,
			X = point.X,
			Size = point.Size
		};
		return ChartRows.Describe(drawn, heading, rows, notes, [chartPoint]);
	}

	private string PointClass(DrawnPoint point) => new CssBuilder("moka-scatter__point")
		.AddClass("moka-scatter__point--bubble", _bubbles)
		.AddClass(point.Series < _series.Count ? _series[point.Series].ColorClass : null)
		.Build();

	// A bubble's diameter is its stroke's width, which ignores the plot's stretch, so it is pixels on screen.
	private string? PointStyle(DrawnPoint point) => new StyleBuilder()
		.AddStyle(point.Series < _series.Count ? _series[point.Series].ColorStyle : null)
		.AddStyle("stroke-width", _bubbles ? $"{point.Diameter.ToString("0.#", CultureInfo.InvariantCulture)}px" : null)
		.Build();

	// A series thinned to one dot per cell draws smaller, fainter dots, so where it is dense still reads as dense.
	private string DotsClass(int series) => new CssBuilder("moka-scatter__dots")
		.AddClass("moka-scatter__dots--dense", series < _dense.Length && _dense[series])
		.AddClass(_series[series].ColorClass)
		.Build();

	private string PointKey(DrawnPoint point) => point.Series < _series.Count ? _series[point.Series].Key : string.Empty;

	private string? NativeTitle(DrawnPoint at)
	{
		if (!NativeTooltips || Series is not { } all)
		{
			return null;
		}

		MokaScatterPoint point = all[at.Series].Points[at.Index];
		StringBuilder text = new(HeadingOf(all[at.Series], point));
		text.Append(": ").Append(Named(YName, YText(point.Y)));
		text.Append(", ").Append(Named(XName, XText(point.X)));
		if (_bubbles)
		{
			text.Append(", ").Append(Named(SizeName, SizeText(point.Size ?? double.NaN)));
		}

		return text.ToString();
	}

	private string HeadingOf(MokaScatterSeries series, MokaScatterPoint point) => point.Label is { Length: > 0 } label
		? (Series?.Count ?? 0) > 1 ? $"{label}, {series.Name}" : label
		: series.Name;

	private string YText(double value) => WriteValue(value, YAxis);

	private string XText(double value) => WriteValue(value, XAxis, false);

	private string SizeText(double size) => !double.IsFinite(size)
		? string.Empty
		: SizeFormatter?.Invoke(size) ?? size.ToString("#,0.##", CultureInfo.CurrentCulture);

	private static string Named(string? name, string value) => string.IsNullOrEmpty(name) ? value : $"{value} {name}";

	private string Cell(List<(MokaScatterSeries Series, int Offset)> shown, int row, int column, bool raw)
	{
		int at = shown.Count - 1;
		while (at > 0 && shown[at].Offset > row)
		{
			at--;
		}

		(MokaScatterSeries series, int offset) = shown[at];
		MokaScatterPoint point = series.Points[row - offset];
		return column switch
		{
			0 => point.Label ?? string.Empty,
			1 => series.Name,
			2 => raw ? Raw(point.X) : XText(point.X),
			3 => raw ? Raw(point.Y) : YText(point.Y),
			_ => raw ? Raw(point.Size ?? double.NaN) : SizeText(point.Size ?? double.NaN)
		};
	}

	private static string[] Paths(DrawnPoint[] drawn, int seriesCount)
	{
		StringBuilder[] paths = new StringBuilder[seriesCount];
		foreach (DrawnPoint point in drawn)
		{
			StringBuilder path = paths[point.Series] ??= new StringBuilder();
			path.Append('M').Append(Coordinate(point.X)).Append(' ').Append(Coordinate(1 - point.Y)).Append("h0");
		}

		return [.. paths.Select(path => path?.ToString() ?? string.Empty)];
	}

	private static string KeyOf(MokaScatterSeries series) => series.Key ?? series.Name;

	// The plot keeps the look's 600 by 260 shape unless a height is set, which only changes how many rows of cells
	// there are, so the estimate is safe either way.
	private static double ViewRatio => 260d / 600d;

	private static string Coordinate(double share) => Math.Round(share * ViewUnits).ToString("0", CultureInfo.InvariantCulture);

	private static double SizeOf(IReadOnlyList<MokaScatterSeries> all, DrawnPoint point) =>
		all[point.Series].Points[point.Index].Size is { } size && double.IsFinite(size) ? size : 0;

	private static string Raw(double value) => double.IsFinite(value) ? value.ToString(CultureInfo.InvariantCulture) : string.Empty;

	private (double Min, double Max)? RangeOf(IReadOnlyList<MokaScatterSeries> all, Func<MokaScatterPoint, double> pick, bool log,
		Func<MokaScatterPoint, bool>? keep = null)
	{
		double min = double.PositiveInfinity;
		double max = double.NegativeInfinity;
		foreach (MokaScatterSeries series in all)
		{
			if (IsHidden(KeyOf(series)))
			{
				continue;
			}

			foreach (MokaScatterPoint point in series.Points ?? [])
			{
				double value = pick(point);
				if (!double.IsFinite(value) || (log && value <= 0) || (keep is not null && !keep(point)))
				{
					continue;
				}

				min = Math.Min(min, value);
				max = Math.Max(max, value);
			}
		}

		return min <= max ? (min, max) : null;
	}

	private static bool SameSeries(IReadOnlyList<MokaScatterSeries>? series, SeriesCopy[]? snapshot)
	{
		if (series is null || snapshot is null)
		{
			return series is null && snapshot is null;
		}

		if (series.Count != snapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < series.Count; i++)
		{
			MokaScatterSeries one = series[i];
			SeriesCopy copy = snapshot[i];
			if (one.Name != copy.Series.Name || one.Color != copy.Series.Color || one.ColorValue != copy.Series.ColorValue
			    || one.Key != copy.Series.Key || one.Slot != copy.Series.Slot || !ChartSnapshot.SameItems(one.Points, copy.Points))
			{
				return false;
			}
		}

		return true;
	}
}
