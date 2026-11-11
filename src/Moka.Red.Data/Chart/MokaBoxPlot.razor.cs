using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     One row per group of values: the box covers the middle half, the bar inside is the median and the whiskers reach
///     the 5th and 95th percentiles (or as <see cref="Whiskers" /> says). With <see cref="ShowPoints" /> the values
///     themselves are drawn as faint dots behind the box.
/// </summary>
/// <remarks>
///     The box, the median and the whiskers are HTML. The points are one small svg per row holding one path, so a row
///     costs the same whatever its count; past <see cref="MaxPoints" /> an even stride of the sorted values is drawn,
///     the lowest and the highest among them. Working the boxes out sorts each group's values, so for a million values
///     pass a precomputed <see cref="MokaBoxPlotGroup.Summary" /> or set <see cref="MokaChartBase.DataVersion" />.
/// </remarks>
public partial class MokaBoxPlot : MokaCartesianChartBase
{
	// The share of a row the points spread over, top to bottom, so they never touch the next row.
	private const double PointSpread = 0.62;
	private const double GoldenRatio = 0.6180339887498949;

	private GroupCopy[]? _snapshot;
	private Box[] _boxes = [];
	private readonly Dictionary<int, (long Version, string Path)> _paths = [];

	/// <summary>The groups, one row each, in order.</summary>
	[Parameter]
	public IReadOnlyList<MokaBoxPlotGroup>? Groups { get; set; }

	/// <summary>Where the whiskers end. Defaults to the 5th and the 95th percentile.</summary>
	[Parameter]
	public MokaWhiskers Whiskers { get; set; } = MokaWhiskers.Percentile5To95;

	/// <summary>Draws the values themselves as faint dots behind each box.</summary>
	[Parameter]
	public bool ShowPoints { get; set; }

	/// <summary>The most points a row draws. A row with more draws an even stride of its sorted values. Defaults to 200.</summary>
	[Parameter]
	public int MaxPoints { get; set; } = 200;

	/// <summary>Whether a key under the rows says what the box, the median, the whiskers and the dots mean. Defaults to true.</summary>
	[Parameter]
	public bool ShowKey { get; set; } = true;

	/// <summary>What the median is called, in the tooltip, the key and the table. Defaults to "Median".</summary>
	[Parameter]
	public string MedianText { get; set; } = "Median";

	/// <summary>What the box is called. Defaults to "Middle half".</summary>
	[Parameter]
	public string MiddleText { get; set; } = "Middle half";

	/// <summary>
	///     What the whiskers are called. Null names them by <see cref="Whiskers" />: "5th to 95th", "Lowest to highest" or
	///     "Whiskers".
	/// </summary>
	[Parameter]
	public string? WhiskerText { get; set; }

	/// <summary>What one dot is called in the key. Defaults to "One value".</summary>
	[Parameter]
	public string PointText { get; set; } = "One value";

	/// <summary>Writes how many values a group has, for the tooltip. Defaults to "1,204 values".</summary>
	[Parameter]
	public Func<int, string>? CountText { get; set; }

	/// <summary>The table's heading for where the low whisker ends. Defaults to "Low".</summary>
	[Parameter]
	public string LowText { get; set; } = "Low";

	/// <summary>The table's heading for where the high whisker ends. Defaults to "High".</summary>
	[Parameter]
	public string HighText { get; set; } = "High";

	/// <summary>The table's heading for how many values a group has. Defaults to "Count".</summary>
	[Parameter]
	public string CountLabel { get; set; } = "Count";

	/// <inheritdoc />
	protected override string RootClass => "moka-boxplot";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(ModernRootClasses)
		.AddClass("moka-boxplot--points", ShowPoints)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => _boxes.Length == 0
		? "Box plot"
		: $"Box plot: {string.Join(", ", _boxes.Take(6).Select(box => $"{box.Group.Name} {MedianText} {Write(box.Summary.Median)}"))}{(_boxes.Length > 6 ? ", ..." : "")}";

	/// <inheritdoc />
	internal override bool HasData => _boxes.Length > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => [];

	/// <inheritdoc />
	internal override bool DrawsRows => true;

	/// <inheritdoc />
	internal override bool SurfaceRows => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int RowCount => _boxes.Length;

	/// <inheritdoc />
	internal override int CategoryCount => _boxes.Length;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _boxes.Length;

	/// <inheritdoc />
	internal override int LatestDrawn(int panel) => _boxes.Length > 0 ? 0 : -1;

	/// <inheritdoc />
	internal override double DefaultRowHeight => Math.Round(Spec.RowHeight * 8 / 7);

	/// <inheritdoc />
	internal override string RowLabel(int index) => index >= 0 && index < _boxes.Length ? _boxes[index].Group.Name : string.Empty;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => new(
		[Words.Name, LowText, "Q1", MedianText, "Q3", HighText, CountLabel], _boxes.Length,
		(row, column) => column switch
		{
			0 => _boxes[row].Group.Name,
			6 => _boxes[row].Summary.Count.ToString("#,0", CultureInfo.CurrentCulture),
			_ => Write(Column(_boxes[row].Summary, column))
		},
		(row, column) => column switch
		{
			0 => _boxes[row].Group.Name,
			6 => _boxes[row].Summary.Count.ToString(CultureInfo.InvariantCulture),
			_ => Raw(Column(_boxes[row].Summary, column))
		});

	private string WhiskerName => WhiskerText ?? Whiskers switch
	{
		MokaWhiskers.MinMax => "Lowest to highest",
		MokaWhiskers.Tukey => "Whiskers",
		_ => "5th to 95th"
	};

	// Tukey whiskers leave values past them, which are drawn as dots even without the points.
	private bool DrawsOutliers => !ShowPoints && Whiskers == MokaWhiskers.Tukey;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	/// <summary>A drawn row: its group, its box, the values it draws as dots, and its colors.</summary>
	private sealed record Box(int Source, MokaBoxPlotGroup Group, MokaBoxSummary Summary, double[] Points, string ColorClass,
		string? ColorStyle);

	/// <summary>A copy of a group as it was drawn, values included, so a list changed in place still counts.</summary>
	private sealed record GroupCopy(MokaBoxPlotGroup Group, double[] Values);

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Groups)
		? SameGroups(after as IReadOnlyList<MokaBoxPlotGroup>, _snapshot)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override (double Min, double Max) ValueRange(int panel)
	{
		double min = double.PositiveInfinity;
		double max = double.NegativeInfinity;
		foreach (Box box in _boxes)
		{
			foreach (double value in (ReadOnlySpan<double>)[box.Summary.Low, box.Summary.High])
			{
				if (double.IsFinite(value))
				{
					min = Math.Min(min, value);
					max = Math.Max(max, value);
				}
			}

			if (box.Points.Length > 0)
			{
				min = Math.Min(min, box.Points[0]);
				max = Math.Max(max, box.Points[^1]);
			}
		}

		return min <= max ? (min, max) : (double.NaN, double.NaN);
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		IReadOnlyList<MokaBoxPlotGroup> groups = Groups ?? [];
		_snapshot = DataVersion is null ? [.. groups.Select(group => new GroupCopy(group, [.. group.Values ?? []]))] : null;
		_paths.Clear();

		string? slotStyle = SeriesStyle(1, null);
		List<Box> boxes = [];
		for (int i = 0; i < groups.Count; i++)
		{
			MokaBoxPlotGroup group = groups[i];
			double[] sorted = ShowPoints || group.Summary is null ? Sorted(group.Values) : [];
			MokaBoxSummary summary = group.Summary ?? MokaChartStats.Box(sorted, Whiskers);
			if (summary.Count == 0 || !double.IsFinite(summary.Median))
			{
				continue;
			}

			double[] points = ShowPoints
				? Stride(sorted, MaxPoints)
				: DrawsOutliers
					? Stride([.. summary.Outliers], MaxPoints)
					: [];
			bool muted = IsMuted(group.Key ?? group.Name);
			string colorClass = SeriesClass(1, group.Color, muted);
			boxes.Add(new Box(i, group, summary, points, colorClass, muted || group.Color is not null ? null : slotStyle));
		}

		_boxes = [.. boxes];
	}

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _boxes.Length)
		{
			return null;
		}

		Box box = _boxes[drawn];
		MokaBoxSummary summary = box.Summary;
		string median = Write(summary.Median);
		List<ChartActiveRow> rows =
		[
			new(MedianText, box.ColorClass, box.ColorStyle, median, null, false),
			new(MiddleText, string.Empty, ChartRows.NoKey, Words.Between(Write(summary.Q1), Write(summary.Q3)), null, false),
			new(WhiskerName, string.Empty, ChartRows.NoKey, Words.Between(Write(summary.Low), Write(summary.High)), null, false)
		];
		List<string> notes = [Count(summary.Count)];
		notes.AddRange(AnnotationNotes(summary.Median));
		MokaChartPoint point = new(box.Group.Key ?? box.Group.Name, box.Group.Name, 0, box.Source, summary.Median, median)
		{
			Category = box.Group.Name,
			Low = summary.Low,
			High = summary.High
		};
		return ChartRows.Describe(drawn, box.Group.Name, rows, notes, [point]);
	}

	private static string LaneClass(Box box) => new CssBuilder("moka-boxplot__lane")
		.AddClass(box.ColorClass)
		.Build();

	private string? LaneStyle(Box box)
	{
		MokaBoxSummary s = box.Summary;
		return new StyleBuilder()
			.AddStyle(box.ColorStyle)
			.AddStyle("--_bx-lo", Css(s.Low))
			.AddStyle("--_bx-q1", Css(s.Q1))
			.AddStyle("--_bx-md", Css(s.Median))
			.AddStyle("--_bx-q3", Css(s.Q3))
			.AddStyle("--_bx-hi", Css(s.High))
			.Build();
	}

	private string? NativeTitle(Box box) => NativeTooltips
		? $"{box.Group.Name}: {MedianText} {Write(box.Summary.Median)}, {MiddleText} {Words.Between(Write(box.Summary.Q1), Write(box.Summary.Q3))}, {WhiskerName} {Words.Between(Write(box.Summary.Low), Write(box.Summary.High))}, {Count(box.Summary.Count)}"
		: null;

	// One path of zero-length segments in a stretched 1000 by 100 box: each a round dot once the stroke ignores the
	// stretch. The dots spread down the row by the golden ratio, so they never line up and never move between renders.
	private string PointsPath(int row)
	{
		if (_paths.TryGetValue(row, out (long Version, string Path) cached) && cached.Version == GeometryVersion)
		{
			return cached.Path;
		}

		Box box = _boxes[row];
		StringBuilder path = new(box.Points.Length * 14);
		for (int k = 0; k < box.Points.Length; k++)
		{
			double share = Share(box.Points[k]);
			if (!double.IsFinite(share))
			{
				continue;
			}

			double spread = (k * GoldenRatio) % 1 - 0.5;
			path.Append('M')
				.Append((Math.Clamp(share, 0, 1) * 1000).ToString("0.#", CultureInfo.InvariantCulture))
				.Append(' ')
				.Append((50 + spread * PointSpread * 100).ToString("0.#", CultureInfo.InvariantCulture))
				.Append("h0");
		}

		string text = path.ToString();
		_paths[row] = (GeometryVersion, text);
		return text;
	}

	private string? Css(double value) => Share(value) is var share && double.IsFinite(share) ? ChartRows.Css(share) : null;

	private double Share(double value)
	{
		if (RowPlotModel().Scale is not { } scale || !double.IsFinite(value))
		{
			return double.NaN;
		}

		double share = scale.Share(value);
		return XAxis?.Reverse == true ? 1 - share : share;
	}

	private string Count(int count) =>
		CountText?.Invoke(count) ?? $"{count.ToString("#,0", CultureInfo.CurrentCulture)} values";

	private string Write(double value) => ChartRows.Write(this, value);

	private static string Raw(double value) => double.IsFinite(value) ? value.ToString(CultureInfo.InvariantCulture) : string.Empty;

	private static double Column(MokaBoxSummary summary, int column) => column switch
	{
		1 => summary.Low,
		2 => summary.Q1,
		3 => summary.Median,
		4 => summary.Q3,
		_ => summary.High
	};

	private static double[] Sorted(IReadOnlyList<double>? values)
	{
		if (values is null)
		{
			return [];
		}

		double[] finite = [.. values.Where(double.IsFinite)];
		Array.Sort(finite);
		return finite;
	}

	// An even stride of sorted values that keeps the lowest and the highest.
	private static double[] Stride(double[] sorted, int most)
	{
		most = Math.Max(2, most);
		if (sorted.Length <= most)
		{
			return sorted;
		}

		double[] picked = new double[most];
		for (int i = 0; i < most; i++)
		{
			picked[i] = sorted[(int)Math.Round((double)i * (sorted.Length - 1) / (most - 1))];
		}

		return picked;
	}

	private static bool SameGroups(IReadOnlyList<MokaBoxPlotGroup>? groups, GroupCopy[]? snapshot)
	{
		if (groups is null || snapshot is null)
		{
			return groups is null && snapshot is null;
		}

		if (groups.Count != snapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < groups.Count; i++)
		{
			MokaBoxPlotGroup group = groups[i];
			GroupCopy copy = snapshot[i];
			if (group.Name != copy.Group.Name || group.Summary != copy.Group.Summary || group.Color != copy.Group.Color
			    || group.Key != copy.Group.Key || !ChartSnapshot.SameItems(group.Values, copy.Values))
			{
				return false;
			}
		}

		return true;
	}
}
