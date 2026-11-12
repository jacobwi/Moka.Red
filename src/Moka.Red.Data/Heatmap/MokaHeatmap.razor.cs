using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Heatmap;

/// <summary>
///     A calendar heatmap, the contribution-graph view: one small square per day, colored by how
///     much happened that day, laid out weeks across and days of the week down.
/// </summary>
/// <remarks>
///     <see cref="Layout" /> <see cref="MokaHeatmapLayout.Grid" /> draws rows by columns instead, such as hour by
///     weekday from <see cref="HourByWeekday" />. With only its own parameters it draws exactly what it always has, each
///     cell keeping a <c>title</c> tooltip. A title, a tooltip of its own, the keyboard, a point listener, the toolbar, a
///     state or the table view put it in the shared chart frame: cells then carry <c>data-i</c>, the chart's own tooltip
///     and pin follow the pointer, and the arrow keys walk the cells by row and column.
/// </remarks>
public partial class MokaHeatmap : MokaChartBase
{
	private const int StepCount = 5;
	private const double MinCell = 0.625;
	private const double MaxCell = 1.125;
	private const double CellGap = 0.125;
	private const string ValueKey = "value";

	private static readonly int[] Widths = [6, 8, 10, 12, 15, 19, 24, 30, 38, 48, 60, 75, 95, 120];
	private static readonly int[] FlipWidths = [15, 19, 24, 30, 38, 48, 60];

	// Whether it is still drawn the old way is decided by these too, so they rebuild it even on the modern path.
	private static readonly HashSet<string> ClassicDeciding = new(StringComparer.Ordinal)
	{
		nameof(Title), nameof(Subtitle), nameof(TitleContent), nameof(Actions), nameof(Controls), nameof(State), nameof(View),
		nameof(TooltipTemplate)
	};

	private List<GridCell> _gridCells = [];
	private List<GridLabel> _columnLabels = [];
	private List<GridLabel> _rowLabels = [];
	private double[][] _gridData = [];
	private int _gridRows;
	private int _gridColumns;
	private string? _flipClass;
	private double _gridMax;
	private bool _hasFigures;

	private List<Cell> _cells = [];
	private List<MonthLabel> _months = [];
	private List<MonthLabel> _chartMonths = [];
	private int _dayCount;
	private DateTime _builtToday;

	private Dictionary<DateTime, int>? _dataSnapshot;
	private double[]?[]? _gridSnapshot;
	private ChartPartLayout? _layout;
	private bool _classic = true;
	private bool _built;
	private bool _nativeTitles = true;

	/// <summary>
	///     How the cells are arranged: a calendar of days from <see cref="Data" />, or rows by columns from
	///     <see cref="GridValues" />. Defaults to the calendar.
	/// </summary>
	[Parameter]
	public MokaHeatmapLayout Layout { get; set; }

	/// <summary>The grid's values, a list of rows each holding its columns. A <c>double.NaN</c> is a cell nobody knows about.</summary>
	[Parameter]
	public IReadOnlyList<IReadOnlyList<double>>? GridValues { get; set; }

	/// <summary>A name per row of the grid, such as <see cref="WeekdayNames" />.</summary>
	[Parameter]
	public IReadOnlyList<string>? RowLabels { get; set; }

	/// <summary>
	///     A name per column of the grid, such as <see cref="HourNames" />. When the grid is too narrow for all of them,
	///     every second, third or sixth stays, by container query.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? ColumnLabels { get; set; }

	/// <summary>
	///     A grid cell's tooltip from its row, its column and its value. It is asked for unknown cells too, with
	///     <c>double.NaN</c>. Null, or a null answer, writes "Tue 04: 412", or the row and column and
	///     <see cref="UnknownText" /> for an unknown cell. In the chart's own tooltip the answer is a line under the value,
	///     the keyboard reads it, and the table view lists it.
	/// </summary>
	[Parameter]
	public Func<int, int, double, string?>? CellText { get; set; }

	/// <summary>
	///     How large a grid cell grows when there is room, a CSS length such as <c>1.5rem</c> or a <c>var()</c>. Null is
	///     1.125rem. A cell never shrinks below 0.625rem, or this size if it is smaller; past that a grid wider than tall
	///     turns on its side, and any other scrolls. Column names are thinned for it when it is in rem or px. Ignored with
	///     <see cref="Fill" />.
	/// </summary>
	[Parameter]
	public string? CellSize { get; set; }

	/// <summary>
	///     Whether the grid's cells share the heatmap's whole width, staying square however large that makes them, such
	///     as 24 hours by 7 days across a wide card. On its side the columns share the width too. Wins over
	///     <see cref="CellSize" />.
	/// </summary>
	[Parameter]
	public bool Fill { get; set; }

	/// <summary>
	///     A figure per row of the grid, such as a day's total, set beside the row's name: at the end of the name
	///     column, and under the name when the grid turns on its side.
	/// </summary>
	[Parameter]
	public IReadOnlyList<string>? RowFigures { get; set; }

	/// <summary>Rows of the grid to mark, such as today, by position: their name stands out with an accent bar beside it.</summary>
	[Parameter]
	public IReadOnlyCollection<int>? MarkedRows { get; set; }

	/// <summary>Grid cells to ring, such as the hour now.</summary>
	[Parameter]
	public IReadOnlyCollection<MokaHeatmapCell>? MarkedCells { get; set; }

	/// <summary>Where the key from <see cref="LessText" /> to <see cref="MoreText" /> sits. Defaults to the end of the heatmap's width.</summary>
	[Parameter]
	public MokaHeatmapScalePlacement ScalePlacement { get; set; }

	/// <summary>The value a grid cell must reach for the deepest color. Null = the grid's largest value.</summary>
	[Parameter]
	public double? MaxGridValue { get; set; }

	/// <summary>
	///     The colors the cells step through. Null steps through five solid shades for the grid and for any chart look
	///     but Standard, and keeps the accent's four on today's calendar.
	/// </summary>
	[Parameter]
	public MokaHeatmapRamp? Ramp { get; set; }

	/// <summary>Whether a key from <see cref="LessText" /> to <see cref="MoreText" /> sits under the cells.</summary>
	[Parameter]
	public bool ShowScale { get; set; }

	/// <summary>The key's low end. Defaults to "Less".</summary>
	[Parameter]
	public string LessText { get; set; } = "Less";

	/// <summary>The key's high end. Defaults to "More".</summary>
	[Parameter]
	public string MoreText { get; set; } = "More";

	/// <summary>How many days each date had. Dates are matched by their day; time is ignored.</summary>
	[Parameter]
	public IReadOnlyDictionary<DateTime, int>? Data { get; set; }

	/// <summary>Number of week columns to show. Defaults to 53, the trailing year.</summary>
	[Parameter]
	public int Weeks { get; set; } = 53;

	/// <summary>The last day shown. Defaults to today. The grid still ends on that week's Sunday.</summary>
	[Parameter]
	public DateTime? EndDate { get; set; }

	/// <summary>
	///     The count a day must reach for the deepest color. Null = the largest count in
	///     <see cref="Data" />, so the busiest day is always deepest.
	/// </summary>
	[Parameter]
	public int? MaxValue { get; set; }

	/// <summary>Whether month names run along the top. Defaults to true.</summary>
	[Parameter]
	public bool ShowMonthLabels { get; set; } = true;

	/// <summary>Whether weekday names run down the side. Defaults to true.</summary>
	[Parameter]
	public bool ShowWeekdayLabels { get; set; } = true;

	/// <summary>
	///     Days nobody knows about, such as days before tracking began or while a collector was down, which aren't the
	///     same as days with nothing in them. They are drawn hollow and hatched rather than as an empty day. Matched by
	///     day; time is ignored. A date that is also in <see cref="Data" /> counts as known.
	/// </summary>
	[Parameter]
	public IReadOnlyCollection<DateTime>? Unknown { get; set; }

	/// <summary>
	///     What an unknown day's tooltip says before its date, and what an unknown cell's value reads in the chart's own
	///     tooltip and its table. Defaults to "No data".
	/// </summary>
	[Parameter]
	public string UnknownText { get; set; } = "No data";

	/// <summary>Monday first, the short day names a grid from <see cref="HourByWeekday" /> is labelled with.</summary>
	/// <param name="culture">The culture to name them in. Null is the current culture.</param>
	public static IReadOnlyList<string> WeekdayNames(CultureInfo? culture = null)
	{
		DateTimeFormatInfo format = (culture ?? CultureInfo.CurrentCulture).DateTimeFormat;
		return [.. Enumerable.Range(1, 7).Select(day => format.GetAbbreviatedDayName((DayOfWeek)(day % 7)))];
	}

	/// <summary>The hours of a day as two digits, "00" to "23", for a grid from <see cref="HourByWeekday" />.</summary>
	public static IReadOnlyList<string> HourNames { get; } =
		[.. Enumerable.Range(0, 24).Select(hour => hour.ToString("00", CultureInfo.InvariantCulture))];

	/// <summary>
	///     Counts times into seven rows, Monday first, of 24 hours each, for <see cref="GridValues" />: when things happen
	///     in a week, such as blocked connections or logins.
	/// </summary>
	/// <param name="times">When each thing happened.</param>
	/// <param name="zone">The zone whose days and hours they are counted in. Null is the local zone, the server's on Blazor Server.</param>
	public static IReadOnlyList<IReadOnlyList<double>> HourByWeekday(IEnumerable<DateTimeOffset> times, TimeZoneInfo? zone = null)
	{
		ArgumentNullException.ThrowIfNull(times);

		double[][] grid = [.. Enumerable.Range(0, 7).Select(_ => new double[24])];
		TimeZoneInfo local = zone ?? TimeZoneInfo.Local;
		foreach (DateTimeOffset time in times)
		{
			DateTimeOffset at = TimeZoneInfo.ConvertTime(time, local);
			grid[((int)at.DayOfWeek + 6) % 7][at.Hour]++;
		}

		return grid;
	}

	/// <inheritdoc />
	protected override string RootClass => "moka-heatmap";

	/// <inheritdoc />
	protected override string CssClass => _classic
		? new CssBuilder(RootClass)
			.AddClass(Class)
			.Build()
		: new CssBuilder(RootClass)
			.AddClass("moka-heatmap--chart")
			.AddClass(ModernRootClasses)
			.AddClass(Class)
			.Build();

	/// <inheritdoc />
	internal override bool IsClassic => _classic;

	/// <inheritdoc />
	internal override string DefaultLabel => Layout == MokaHeatmapLayout.Grid ? "Heatmap" : "Calendar heatmap";

	/// <inheritdoc />
	internal override bool HasData => Layout == MokaHeatmapLayout.Grid ? _gridCells.Count > 0 : _dayCount > 0;

	/// <inheritdoc />
	/// <remarks>A heatmap is one measure across its cells, keyed by its scale, so it lists no series.</remarks>
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => [];

	/// <inheritdoc />
	internal override bool DrawsOwnLegend => true;

	/// <inheritdoc />
	/// <remarks>The cell under the pointer is the point: each carries <c>data-i</c>, and the arrow keys walk them by row and column.</remarks>
	internal override bool HitsMarks => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => Layout == MokaHeatmapLayout.Grid ? _gridCells.Count : _dayCount;

	/// <inheritdoc />
	internal override ChartPartLayout? PartLayout(int panel) => _classic ? null : _layout;

	/// <inheritdoc />
	internal override string? OwnKeysText => Layout == MokaHeatmapLayout.Grid ? Words.CellKeys : Words.DayKeys;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => Layout == MokaHeatmapLayout.Grid ? GridTable() : CalendarTable();

	private sealed record MonthLabel(string Text);

	/// <summary>A day: its count and color step, its tooltip, and its place among the days the chart can point at.</summary>
	private sealed record Cell(DateTime Date, int Count, int Level, string CssClass, string Title, string? Drawn, bool IsUnknown);

	private sealed record WeekdayLabel(string Text);

	/// <summary>A grid cell: its color step, where it sits, its tooltip and its place among the cells.</summary>
	private sealed record GridCell(string Class, string? Style, string? Title, string Drawn, string Step);

	/// <summary>A row or column name, with the classes that hide it when the grid runs out of room, and a row's figure.</summary>
	private sealed record GridLabel(string Text, string Class, string? Style, string? Figure = null);

	private DateTime End => EndDate?.Date ?? DateTime.Today;

	// The grid runs Monday to Sunday and always includes the whole week the end day sits in, so
	// the last column holds it and the days after it. GridEnd is exclusive: the Monday after.
	private DateTime GridEnd => End.AddDays(7 - ((int)End.DayOfWeek + 6) % 7);

	// Counting back Weeks whole weeks from that Monday lands on a Monday again, so every column is
	// a full week.
	private DateTime Start => GridEnd.AddDays(-Math.Max(1, Weeks) * 7);

	private IReadOnlyList<MonthLabel> Months => _months;

	// Monday, Wednesday, Friday: the three that name the rows without crowding them.
	private static IReadOnlyList<WeekdayLabel> Weekdays =>
	[
		new(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Monday)),
		new(""),
		new(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Wednesday)),
		new(""),
		new(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Friday)),
		new(""),
		new("")
	];

	private IReadOnlyList<Cell> Cells => _cells;

	private MokaHeatmapRamp ResolvedRamp =>
		Ramp ?? (Layout == MokaHeatmapLayout.Grid || ResolvedLook != MokaChartLook.Standard
			? MokaHeatmapRamp.Steps
			: MokaHeatmapRamp.Accent);

	private bool Stepped => ResolvedRamp == MokaHeatmapRamp.Steps;

	private int Levels => Stepped ? StepCount : 4;

	private string LevelPrefix => Stepped ? "s" : "l";

	private IEnumerable<string> ScaleClasses => Enumerable.Range(1, Levels)
		.Select(level => $"moka-heatmap-cell moka-heatmap-cell--{LevelPrefix}{level.ToString(CultureInfo.InvariantCulture)}");

	private string GridClass => new CssBuilder(RootClass)
		.AddClass("moka-heatmap--grid")
		.AddClass("moka-fill-width")
		.AddClass(_flipClass)
		.AddClass("moka-heatmap--figures", _hasFigures)
		.AddClass(Class)
		.Build();

	private string? GridStyle => GridVars(SpacingStyle())
		.AddStyle(Style)
		.Build();

	// The grid as the frame holds it: the same classes and sizes as the drawing on its own, on an inner element.
	private string GridViewClass => new CssBuilder("moka-heatmap__view")
		.AddClass("moka-heatmap--grid")
		.AddClass("moka-fill-width")
		.AddClass(_flipClass)
		.AddClass("moka-heatmap--figures", _hasFigures)
		.Build();

	private string? GridViewStyle => GridVars(new StyleBuilder()).Build();

	private bool ScaleInGrid => Layout == MokaHeatmapLayout.Grid && ScalePlacement == MokaHeatmapScalePlacement.GridEnd;

	private string ScaleClass => new CssBuilder("moka-heatmap__scale")
		.AddClass("moka-heatmap__scale--start", ScalePlacement == MokaHeatmapScalePlacement.Start)
		.AddClass("moka-heatmap__scale--center", ScalePlacement == MokaHeatmapScalePlacement.Center)
		.AddClass("moka-heatmap__scale--grid", ScaleInGrid)
		.Build();

	private string? ResolvedCellSize => CssValues.IsLengthOrVariable(CellSize) ? CssValues.WithPixels(CellSize.Trim()) : null;

	// The most a cell grows to, in rem, which decides where a column name fits. A size in another unit, or a var(), can't
	// be measured here, so names are thinned as though cells never grow past their smallest: they show late, never on top
	// of each other.
	private double CellCapRem
	{
		get
		{
			if (Fill)
			{
				return double.PositiveInfinity;
			}

			if (ResolvedCellSize is not { } size)
			{
				return MaxCell;
			}

			return RemOf(size) is { } rem ? Math.Max(rem, 0) : MinCell;
		}
	}

	/// <inheritdoc />
	/// <remarks>
	///     A listener or the template doesn't move the cells, but it does take the heatmap off its old drawing, and back
	///     onto it once it goes. A calendar that ends today moves to the next day when a parameter set arrives after
	///     midnight.
	/// </remarks>
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (_built && (ClassicNow() != _classic || (EndDate is null && Layout == MokaHeatmapLayout.Calendar && DateTime.Today != _builtToday)))
		{
			Redraw();
		}
	}

	// The old drawing rebuilds on every change, as it always rendered on every one; the chart rebuilds for what it draws
	// from and for what decides whether it is still a chart.
	/// <inheritdoc />
	internal override bool AffectsGeometry(string parameterName) =>
		_classic || ClassicDeciding.Contains(parameterName) || base.AffectsGeometry(parameterName);

	/// <inheritdoc />
	internal override bool ComparesItems(string name) =>
		name is nameof(RowLabels) or nameof(ColumnLabels) or nameof(RowFigures) or nameof(MarkedRows) or nameof(MarkedCells)
			or nameof(Unknown)
		|| base.ComparesItems(name);

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name switch
	{
		nameof(Data) => SameData(after as IReadOnlyDictionary<DateTime, int>),
		nameof(GridValues) => SameGrid(after as IReadOnlyList<IReadOnlyList<double>>),
		_ => base.SameValue(name, before, after)
	};

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_dataSnapshot = DataVersion is null && Data is not null ? new Dictionary<DateTime, int>(Data) : null;
		_gridSnapshot = DataVersion is null && GridValues is not null ? [.. GridValues.Select(row => row?.ToArray())] : null;
		_classic = ClassicNow();
		_nativeTitles = _classic || ResolvedTooltip == MokaChartTooltip.Native;
		_built = true;
		if (Layout == MokaHeatmapLayout.Grid)
		{
			BuildGrid();
			_layout = new ChartPartLayout(ChartPartOrder.Rows, _gridRows, _gridColumns, _gridCells.Count);
		}
		else
		{
			BuildCalendar();
			_layout = new ChartPartLayout(ChartPartOrder.Columns, 7, Math.Max(1, Weeks), _dayCount) { Flow = true };
		}
	}

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn) =>
		Layout == MokaHeatmapLayout.Grid ? DescribeCell(drawn, pinnedDrawn) : DescribeDay(drawn, pinnedDrawn);

	// Anything from the shared chart chrome that a heatmap draws moves it into the frame. The look doesn't: it has always
	// picked the ramp. Nor do Formatter, Text and DataVersion, which change no markup the old drawing has.
	private bool ClassicNow() =>
		Title is null && Subtitle is null && TitleContent is null && Actions is null
		&& Tooltip is null or MokaChartTooltip.Native && TooltipTemplate is null
		&& Pinnable != true && Keyboard != true && string.IsNullOrEmpty(SyncGroup) && !ListensForPoints
		&& Controls is null && View == MokaChartView.Chart && State == MokaChartState.Auto
		&& EmptyText is null && EmptyContent is null;

	private bool SameData(IReadOnlyDictionary<DateTime, int>? data)
	{
		if (data is null || _dataSnapshot is null)
		{
			return data is null && _dataSnapshot is null;
		}

		if (data.Count != _dataSnapshot.Count)
		{
			return false;
		}

		foreach (KeyValuePair<DateTime, int> pair in data)
		{
			if (!_dataSnapshot.TryGetValue(pair.Key, out int count) || count != pair.Value)
			{
				return false;
			}
		}

		return true;
	}

	private bool SameGrid(IReadOnlyList<IReadOnlyList<double>>? grid)
	{
		if (grid is null || _gridSnapshot is null)
		{
			return grid is null && _gridSnapshot is null;
		}

		if (grid.Count != _gridSnapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < grid.Count; i++)
		{
			if (!ChartSnapshot.SameItems(grid[i], _gridSnapshot[i]))
			{
				return false;
			}
		}

		return true;
	}

	private void BuildCalendar()
	{
		_builtToday = DateTime.Today;
		Dictionary<DateTime, int> counts = [];
		if (Data is not null)
		{
			foreach (KeyValuePair<DateTime, int> pair in Data)
			{
				counts[pair.Key.Date] = pair.Value;
			}
		}

		int max = MaxValue.HasValue ? Math.Max(1, MaxValue.Value) : Math.Max(1, counts.Values.DefaultIfEmpty(0).Max());
		HashSet<DateTime> unknown = [.. (Unknown ?? []).Select(day => day.Date)];
		DateTime end = End;
		DateTime start = Start;
		DateTime gridEnd = GridEnd;
		string pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
		int levels = Levels;
		string prefix = LevelPrefix;

		List<Cell> cells = [];
		int drawn = 0;
		for (DateTime day = start; day < gridEnd; day = day.AddDays(1))
		{
			bool future = day > end;
			string? index = future ? null : (drawn++).ToString(CultureInfo.InvariantCulture);
			if (!future && unknown.Contains(day) && !counts.ContainsKey(day))
			{
				cells.Add(new Cell(day, 0, 0, "moka-heatmap-cell moka-heatmap-cell--unknown",
					$"{UnknownText} on {day.ToString(pattern, CultureInfo.CurrentCulture)}", index, true));
				continue;
			}

			int count = future ? 0 : counts.GetValueOrDefault(day);
			int level = count <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(count * (double)levels / max), 1, levels);

			cells.Add(new Cell(day, count, level,
				$"moka-heatmap-cell moka-heatmap-cell--{prefix}{level.ToString(CultureInfo.InvariantCulture)}{(future ? " moka-heatmap-cell--future" : "")}",
				$"{WriteCount(count)} on {day.ToString(pattern, CultureInfo.CurrentCulture)}", index, false));
		}

		_cells = cells;
		_dayCount = drawn;
		_months = MonthsFrom(start, gridEnd);
		_chartMonths = MonthsThatFit(_months);
	}

	// In the frame a month's name sits over its first week and runs on over the next, so a name with no second week
	// before the next name or the end is left out, as the first one often is.
	private static List<MonthLabel> MonthsThatFit(List<MonthLabel> months)
	{
		List<MonthLabel> fit = [.. months];
		int next = fit.Count;
		for (int i = fit.Count - 1; i >= 0; i--)
		{
			if (fit[i].Text.Length == 0)
			{
				continue;
			}

			if (next - i < 2)
			{
				fit[i] = new MonthLabel(string.Empty);
			}
			else
			{
				next = i;
			}
		}

		return fit;
	}

	private static List<MonthLabel> MonthsFrom(DateTime start, DateTime gridEnd)
	{
		List<MonthLabel> labels = [];
		DateTime previousMonth = start.Month == 1 ? new DateTime(start.Year - 1, 12, 1) : new DateTime(start.Year, start.Month - 1, 1);

		for (DateTime week = start; week < gridEnd; week = week.AddDays(7))
		{
			// A label where a month begins inside the week, so each month is named once, at the
			// column where it starts.
			if (week.Month != previousMonth.Month || week.Year != previousMonth.Year)
			{
				labels.Add(new MonthLabel(
					CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(week.Month)));
				previousMonth = week;
			}
			else
			{
				labels.Add(new MonthLabel(""));
			}
		}

		return labels;
	}

	private void BuildGrid()
	{
		IReadOnlyList<IReadOnlyList<double>> values = GridValues ?? [];
		_gridRows = values.Count;
		_gridColumns = values.Count == 0 ? 0 : values.Max(row => row?.Count ?? 0);
		_gridMax = MaxGridValue is { } fixedMax && double.IsFinite(fixedMax) && fixedMax > 0
			? fixedMax
			: values.SelectMany(row => row ?? []).Where(double.IsFinite).DefaultIfEmpty(0).Max();

		HashSet<MokaHeatmapCell> marked = MarkedCells is null ? [] : [.. MarkedCells];
		List<GridCell> cells = [];
		double[][] data = new double[_gridRows][];
		for (int r = 0; r < _gridRows; r++)
		{
			IReadOnlyList<double> row = values[r] ?? [];
			data[r] = new double[_gridColumns];
			for (int c = 0; c < _gridColumns; c++)
			{
				double value = c < row.Count ? row[c] : 0;
				data[r][c] = value;
				string place = new StyleBuilder()
					.AddStyle("--r", r.ToString(CultureInfo.InvariantCulture))
					.AddStyle("--c", c.ToString(CultureInfo.InvariantCulture))
					.Build()!;
				string step = !double.IsFinite(value)
					? "unknown"
					: $"{LevelPrefix}{Level(value).ToString(CultureInfo.InvariantCulture)}";
				string? title = _nativeTitles ? GridTitle(r, c, value) : null;
				string css = new CssBuilder("moka-heatmap-cell")
					.AddClass($"moka-heatmap-cell--{step}")
					.AddClass("moka-heatmap-cell--marked", marked.Count > 0 && marked.Contains(new MokaHeatmapCell(r, c)))
					.Build();
				cells.Add(new GridCell(css, place, title, (r * _gridColumns + c).ToString(CultureInfo.InvariantCulture), step));
			}
		}

		_gridCells = cells;
		_gridData = data;
		_hasFigures = RowFigures is { Count: > 0 } figures && figures.Take(_gridRows).Any(figure => !string.IsNullOrEmpty(figure));
		HashSet<int> markedRows = MarkedRows is null ? [] : [.. MarkedRows];
		_rowLabels = [.. Enumerable.Range(0, _gridRows).Select(r => new GridLabel(LabelAt(RowLabels, r),
			new CssBuilder("moka-heatmap__row").AddClass("moka-heatmap__row--marked", markedRows.Contains(r)).Build(),
			new StyleBuilder().AddStyle("--r", r.ToString(CultureInfo.InvariantCulture)).Build(),
			_hasFigures ? LabelAt(RowFigures, r) : null))];

		double rowLabelRem = _rowLabels.Count == 0 ? 0 : _rowLabels.Max(RowLabelRem);
		_columnLabels = [.. Enumerable.Range(0, _gridColumns).Select(c => ColumnLabel(c, rowLabelRem))];

		// A grid wider than tall turns on its side once its cells would shrink under 0.75rem, so the long run goes down.
		double needRem = rowLabelRem + 0.5 + _gridColumns * (0.75 + CellGap);
		_flipClass = _gridRows < _gridColumns ? $"moka-heatmap--flip-w{Snap(needRem, FlipWidths).ToString(CultureInfo.InvariantCulture)}" : null;
	}

	private string GridTitle(int row, int column, double value) =>
		CellText?.Invoke(row, column, value) ?? (double.IsFinite(value)
			? $"{Name(row, column)}: {WriteValue(value)}"
			: $"{Name(row, column)}: {UnknownText}");

	private StyleBuilder GridVars(StyleBuilder builder) => builder
		.AddStyle("--_hm-rows", _gridRows.ToString(CultureInfo.InvariantCulture))
		.AddStyle("--_hm-cols", _gridColumns.ToString(CultureInfo.InvariantCulture))
		.AddStyle("--_hm-cell-max", Fill ? "1fr" : ResolvedCellSize)
		.AddStyle("--_hm-cell-min", Fill || ResolvedCellSize is null ? null : $"min({Rem(MinCell)}, {ResolvedCellSize})")
		.AddStyle("--_hm-flip-max", Fill ? "1fr" : null);

	// A column's name shows once a cell, times its stride, is as wide as the name. Strides run in a chain where each
	// divides the next (1, 3, 6, 12 for a count of columns three divides, such as hours or months; 1, 2, 4, 8
	// otherwise), and a name's stride is the largest one its position divides, so whatever the width the names left
	// are evenly spaced. The width that takes is snapped up to the container query widths the stylesheet knows. A name
	// too long for any stride never shows; the tooltip keeps it. On its side the grid keeps every name but the minor ones.
	private GridLabel ColumnLabel(int column, double rowLabelRem)
	{
		string text = LabelAt(ColumnLabels, column);
		int stride = StrideOf(column, _gridColumns);
		double perCell = TextRem(text) / stride;
		double cap = CellCapRem;
		double need = rowLabelRem + 0.5 + _gridColumns * (perCell + CellGap);
		string? width = perCell > cap || need > Widths[^1]
			? "moka-heatmap__col--hide"
			: perCell <= Math.Min(MinCell, cap)
				? null
				: $"moka-heatmap__col--w{Snap(need, Widths).ToString(CultureInfo.InvariantCulture)}";
		string css = new CssBuilder("moka-heatmap__col")
			.AddClass(width)
			.AddClass("moka-heatmap__col--minor", stride == 1)
			.Build();
		return new GridLabel(text, css, new StyleBuilder().AddStyle("--c", column.ToString(CultureInfo.InvariantCulture)).Build());
	}

	private static int StrideOf(int column, int columns)
	{
		int step = columns % 3 == 0 ? 3 : 2;
		if (column == 0)
		{
			return int.MaxValue;
		}

		if (column % step != 0)
		{
			return 1;
		}

		int stride = step;
		while (column % (stride * 2) == 0)
		{
			stride *= 2;
		}

		return stride;
	}

	private ChartActiveInfo? DescribeCell(int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _gridCells.Count || _gridColumns <= 0)
		{
			return null;
		}

		int row = drawn / _gridColumns;
		int column = drawn % _gridColumns;
		double value = _gridData[row][column];
		bool known = double.IsFinite(value);
		string heading = Name(row, column);
		string valueText = known ? WriteValue(value) : UnknownText;
		string? own = CellText?.Invoke(row, column, value);
		double? since = known && pinnedDrawn is { } pin && pin != drawn && pin >= 0 && pin < _gridCells.Count
		                && _gridData[pin / _gridColumns][pin % _gridColumns] is var pinned && double.IsFinite(pinned)
			? value - pinned
			: null;

		MokaChartPoint point = new(ValueKey, Words.Value, 0, drawn, value, valueText)
		{
			Category = heading,
			Row = row,
			Column = column,
			SincePin = since
		};
		return Describe(heading, _gridCells[drawn].Step, valueText, since, own, point);
	}

	private ChartActiveInfo? DescribeDay(int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _dayCount || drawn >= _cells.Count)
		{
			return null;
		}

		Cell cell = _cells[drawn];
		string heading = cell.Date.ToString("D", CultureInfo.CurrentCulture);
		string valueText = cell.IsUnknown ? UnknownText : WriteCount(cell.Count);
		double? since = !cell.IsUnknown && pinnedDrawn is { } pin && pin != drawn && pin >= 0 && pin < _dayCount
		                && !_cells[pin].IsUnknown
			? cell.Count - _cells[pin].Count
			: null;
		DateTimeOffset day = new(DateTime.SpecifyKind(cell.Date, DateTimeKind.Unspecified), TimeSpan.Zero);
		MokaChartPoint point = new(ValueKey, Words.Value, 0, drawn, cell.IsUnknown ? double.NaN : cell.Count, valueText)
		{
			Category = heading,
			Time = day,
			X = day.ToUnixTimeMilliseconds(),
			Row = drawn % 7,
			Column = drawn / 7,
			SincePin = since
		};
		string step = cell.IsUnknown ? "unknown" : $"{LevelPrefix}{cell.Level.ToString(CultureInfo.InvariantCulture)}";
		return Describe(heading, step, valueText, since, null, point);
	}

	// The tooltip and the keyboard's reading of a cell: its value first, keyed by its color step, then what the app wrote
	// about it, and the change since a pinned cell.
	private ChartActiveInfo Describe(string heading, string step, string valueText, double? since, string? own, MokaChartPoint point)
	{
		string? delta = since is { } change ? ChartRows.Signed(WriteValue(Math.Abs(change)), change) : null;
		IReadOnlyList<string> notes = own is { Length: > 0 } ? [own] : [];
		ChartActiveRow row = new(string.Empty, step == "unknown" ? "moka-chart-muted" : string.Empty, StepColor(step), valueText,
			delta, true);
		string cell = own is { Length: > 0 } ? own : $"{heading}, {valueText}";
		return new ChartActiveInfo(heading, [row], notes, cell,
			new MokaChartTooltipContext(point.Index, heading, [point], point, null, notes));
	}

	// The color a step is drawn in, for the tooltip's key, as the stylesheet draws the cell.
	private static string? StepColor(string step) => step switch
	{
		"l1" => "--moka-chart-color: color-mix(in srgb, var(--moka-color-primary) 25%, transparent)",
		"l2" => "--moka-chart-color: color-mix(in srgb, var(--moka-color-primary) 50%, transparent)",
		"l3" => "--moka-chart-color: color-mix(in srgb, var(--moka-color-primary) 75%, transparent)",
		"l4" or "s5" => "--moka-chart-color: var(--moka-chart-seq-5, var(--moka-color-primary))",
		"s1" => "--moka-chart-color: var(--moka-chart-seq-1, color-mix(in srgb, var(--moka-color-primary) 20%, transparent))",
		"s2" => "--moka-chart-color: var(--moka-chart-seq-2, color-mix(in srgb, var(--moka-color-primary) 40%, transparent))",
		"s3" => "--moka-chart-color: var(--moka-chart-seq-3, color-mix(in srgb, var(--moka-color-primary) 60%, transparent))",
		"s4" => "--moka-chart-color: var(--moka-chart-seq-4, color-mix(in srgb, var(--moka-color-primary) 80%, transparent))",
		"unknown" => null,
		_ => "--moka-chart-color: var(--moka-chart-zero, var(--moka-color-outline-variant))"
	};

	private ChartTableModel GridTable()
	{
		int columns = Math.Max(1, _gridColumns);
		bool detail = CellText is not null;
		IReadOnlyList<string> headers = detail
			? [Words.Row, Words.Column, Words.Value, Words.Detail]
			: [Words.Row, Words.Column, Words.Value];
		return new ChartTableModel(headers, _gridCells.Count,
			(index, column) => GridCellText(index / columns, index % columns, column, false),
			(index, column) => GridCellText(index / columns, index % columns, column, true));
	}

	private string GridCellText(int row, int column, int field, bool raw)
	{
		double value = _gridData[row][column];
		return field switch
		{
			0 => RowName(row),
			1 => ColumnName(column),
			2 => double.IsFinite(value)
				? raw ? value.ToString(CultureInfo.InvariantCulture) : WriteValue(value)
				: raw ? string.Empty : UnknownText,
			_ => CellText?.Invoke(row, column, value) ?? string.Empty
		};
	}

	private ChartTableModel CalendarTable() => new([Words.Date, Words.Value], _dayCount,
		(index, column) => column == 0
			? _cells[index].Date.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture)
			: _cells[index].IsUnknown ? UnknownText : WriteCount(_cells[index].Count),
		(index, column) => column == 0
			? _cells[index].Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
			: _cells[index].IsUnknown ? string.Empty : _cells[index].Count.ToString(CultureInfo.InvariantCulture));

	private string WriteValue(double value) => Formatter?.Invoke(value) ?? value.ToString("#,0.##", CultureInfo.CurrentCulture);

	private string WriteCount(int count) => Formatter?.Invoke(count) ?? count.ToString(CultureInfo.CurrentCulture);

	private static int Snap(double rem, int[] widths) => widths.FirstOrDefault(width => rem <= width, widths[^1]);

	// The extra-small text the labels are set in runs about 0.42rem a character, plus room either side.
	private static double TextRem(string text) => text.Length * 0.42 + 0.4;

	// A row's name and its figure share the name column, a small gap apart.
	private static double RowLabelRem(GridLabel label) =>
		string.IsNullOrEmpty(label.Figure) ? TextRem(label.Text) : TextRem(label.Text) + TextRem(label.Figure) + 0.25;

	private int Level(double value) =>
		value <= 0 || _gridMax <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(value * Levels / _gridMax), 1, Levels);

	private static string Rem(double rem) => rem.ToString("0.###", CultureInfo.InvariantCulture) + "rem";

	private static double? RemOf(string size)
	{
		string value = size.Trim();
		(string number, double factor) = value.EndsWith("rem", StringComparison.OrdinalIgnoreCase)
			? (value[..^3], 1d)
			: value.EndsWith("px", StringComparison.OrdinalIgnoreCase)
				? (value[..^2], 1 / 16d)
				: (string.Empty, 0d);
		return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && factor > 0
			? parsed * factor
			: null;
	}

	private static string LabelAt(IReadOnlyList<string>? labels, int index) =>
		labels is not null && index < labels.Count ? labels[index] ?? string.Empty : string.Empty;

	private string RowName(int row) => LabelAt(RowLabels, row) is { Length: > 0 } name
		? name
		: (row + 1).ToString(CultureInfo.CurrentCulture);

	private string ColumnName(int column) => LabelAt(ColumnLabels, column) is { Length: > 0 } name
		? name
		: (column + 1).ToString(CultureInfo.CurrentCulture);

	private string Name(int row, int column) =>
		string.Join(' ', new[] { LabelAt(RowLabels, row), LabelAt(ColumnLabels, column) }.Where(part => part.Length > 0)) is
			{ Length: > 0 } name
			? name
			: $"{(row + 1).ToString(CultureInfo.CurrentCulture)}, {(column + 1).ToString(CultureInfo.CurrentCulture)}";
}
