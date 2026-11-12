using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Data.Chart;

/// <summary>
///     The "is it up" chart: one row per thing (a service, a device, a link) with a band along a shared clock, colored by the
///     state it was in, and a line at now. States mean good and bad, so each takes a status color with an icon and a word,
///     and the legend lists the states, hiding and isolating them.
/// </summary>
/// <remarks>
///     Spans are resolved once per data change: a span without an end lasts until the next one without an end starts and
///     the last until <see cref="Now" />, so a span with an end (an incident, a maintenance window) sits over the state
///     around it; the more severe state wins where spans overlap, and time no span covers is unknown, drawn hatched. Where
///     a row holds more stretches than the track has room for, neighbours shorter than a quantum of time (about two
///     pixels) merge into one mark showing the most severe state among them, so a short outage never disappears, and its
///     tooltip says what it holds. Past 200 rows the rows are drawn only as they scroll into view.
/// </remarks>
public partial class MokaStateTimeline : MokaCartesianChartBase
{
	/// <summary>The built-in state for working: the success color.</summary>
	public const string UpKey = "up";

	/// <summary>The built-in state for working badly: the warning color.</summary>
	public const string DegradedKey = "degraded";

	/// <summary>The built-in state for not working: the error color.</summary>
	public const string DownKey = "down";

	/// <summary>The built-in state for planned work: the info color.</summary>
	public const string MaintenanceKey = "maintenance";

	/// <summary>The built-in state for no data, hatched, which time no span covers is in.</summary>
	public const string UnknownKey = "unknown";

	private const int MeasureAbove = 100;
	private const double PixelsPerMark = 2;
	private const double DefaultWidth = 800;
	private const double GapQuanta = 3;

	// The grid the marks are placed on starts at a whole number of these quanta, so it stays put while a window slides
	// across thousands of quanta, and the numbers written stay small enough for CSS to place to the pixel.
	private const double OriginBlock = 4096;
	private const int TitledMarksPerRow = 120;

	// A width this close to the one the marks were laid out for keeps them: the first measurement after a render at the
	// default width would otherwise send every mark again for a quantum a fraction shorter.
	private const double SettleRatio = 1.25;

	private readonly List<StateInfo> _states = [];
	private readonly Dictionary<string, int> _stateIndex = new(StringComparer.Ordinal);
	private readonly List<int> _severity = [];
	private int _unknown;

	private MokaStateRow[]? _rowsSnapshot;
	private MokaStateSpan[]?[]? _spansSnapshot;
	private bool _resolved;
	private IReadOnlyList<MokaStateRow>? _resolvedRows;
	private MokaTimelineState[]? _resolvedStates;
	private DateTimeOffset? _resolvedNowParameter;
	private long? _resolvedVersion;

	private RowData[] _data = [];
	private RowView[] _views = [];
	private Mark[] _marks = [];
	private List<ChartSeriesInfo> _series = [];
	private DateTimeOffset _from;
	private DateTimeOffset _to;
	private double _nowMs;
	private double? _nowShare;
	private int _widest;
	private double? _layoutWidth;
	private double _quantum = 1;
	private double _origin;
	private double _axisOffset;
	private string? _axisScale;
	private (long Version, (int Row, int Piece)[] Index)? _table;

	[Inject]
	private IServiceProvider TimelineServices { get; set; } = default!;

	/// <summary>The rows, one per thing, in order.</summary>
	[Parameter]
	public IReadOnlyList<MokaStateRow>? Rows { get; set; }

	/// <summary>
	///     What the spans' state keys mean, in legend order. Null uses the built-in states: <see cref="UpKey" />,
	///     <see cref="DegradedKey" />, <see cref="DownKey" />, <see cref="MaintenanceKey" /> and <see cref="UnknownKey" />. A key
	///     not listed is a built-in state when it is one, else a state named by its key in the muted color.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaTimelineState>? States { get; set; }

	/// <summary>Where the shared clock starts. Null takes the x axis's <see cref="MokaChartAxis.MinTime" />, else the earliest span.</summary>
	[Parameter]
	public DateTimeOffset? From { get; set; }

	/// <summary>
	///     Where the shared clock ends. Null takes the x axis's <see cref="MokaChartAxis.MaxTime" />, else the latest span's end,
	///     which is now for a span still open.
	/// </summary>
	[Parameter]
	public DateTimeOffset? To { get; set; }

	/// <summary>
	///     The time now, where open spans end, where unknown time stops and the line is drawn. Null reads a registered
	///     <see cref="TimeProvider" />, else the clock, whenever the data changes.
	/// </summary>
	[Parameter]
	public DateTimeOffset? Now { get; set; }

	/// <summary>Whether a line marks now when it falls on the clock. Defaults to true.</summary>
	[Parameter]
	public bool ShowNow { get; set; } = true;

	/// <summary>
	///     Whether each row ends with its uptime: the share of its known time on the clock, up to now, spent in a
	///     <see cref="MokaStateTone.Good" /> state. Unknown time is left out, and the tooltip says how much there was.
	/// </summary>
	[Parameter]
	public bool ShowUptime { get; set; }

	/// <summary>
	///     Writes an uptime, a share from 0 to 1. Null writes a percentage with two decimals that never rounds up to 100%, such
	///     as "99.95%", so a column of them lines up.
	/// </summary>
	[Parameter]
	public Func<double, string>? UptimeFormatter { get; set; }

	/// <summary>Writes how long a span lasted, such as "41 min". Null writes seconds, minutes, hours and days in English.</summary>
	[Parameter]
	public Func<TimeSpan, string>? DurationText { get; set; }

	/// <summary>The words the chart writes beyond the shared ones. Null is English.</summary>
	[Parameter]
	public MokaStateTimelineText? TimelineText { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-state-timeline";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-state-timeline--reverse", XAxis?.Reverse == true)
		.AddClass(ModernRootClasses)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	/// <remarks>
	///     Marks are placed in quanta of time from a fixed origin, and the axis's start and scale are written here once, so a
	///     window that slides moves this and leaves the marks as they were.
	/// </remarks>
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--_st-o", _views.Length > 0 ? Number(_axisOffset) : null)
		.AddStyle("--_st-k", _views.Length > 0 ? _axisScale : null)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => "State timeline";

	/// <inheritdoc />
	internal override bool HasData => _views.Length > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _series;

	/// <inheritdoc />
	internal override bool DrawsRows => true;

	/// <inheritdoc />
	/// <remarks>The mark under the pointer is the span: each carries <c>data-i</c>, and the arrows walk them row by row.</remarks>
	internal override bool HitsMarks => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override bool SyncsByX => false;

	/// <inheritdoc />
	internal override bool MeasuresWidth => _widest > MeasureAbove;

	/// <inheritdoc />
	internal override int RowCount => _views.Length;

	/// <inheritdoc />
	internal override int CategoryCount => _views.Length;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _marks.Length;

	/// <inheritdoc />
	/// <remarks>The first row's newest mark, which is where it stands now.</remarks>
	internal override int LatestDrawn(int panel) =>
		_views.Length > 0 && _views[0].MarkCount > 0 ? _views[0].FirstMark + _views[0].MarkCount - 1 : -1;

	/// <inheritdoc />
	internal override (DateTimeOffset From, DateTimeOffset To)? RowTimeRange => _views.Length > 0 ? (_from, _to) : null;

	/// <inheritdoc />
	internal override string RowLabel(int index) => index >= 0 && index < _views.Length ? _views[index].Data.Name : string.Empty;

	/// <inheritdoc />
	internal override string? RowFigure(int index) =>
		ShowUptime && index >= 0 && index < _views.Length ? _views[index].UptimeText : null;

	/// <summary>The time now, over the names, where it stays while the rows scroll; the line down the rows is unlabeled.</summary>
	internal override string? DefaultValueTitle => _nowShare is null ? null : $"{StateWords.Now} {When(_nowMs)}";

	/// <inheritdoc />
	internal override ChartTableModel? TableModel
	{
		get
		{
			(int Row, int Piece)[] index = TableIndex();
			return new ChartTableModel([Words.Name, StateWords.State, Words.From, Words.To, StateWords.Duration], index.Length,
				(row, column) => TableCell(index[row], column, false), (row, column) => TableCell(index[row], column, true));
		}
	}

	/// <summary>The words, English unless <see cref="TimelineText" /> is set.</summary>
	internal MokaStateTimelineText StateWords => TimelineText ?? MokaStateTimelineText.Default;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	/// <inheritdoc />
	internal override bool ComparesItems(string name) => name == nameof(States) || base.ComparesItems(name);

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Rows)
		? SameRows(after as IReadOnlyList<MokaStateRow>)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override void Rebuild()
	{
		if (!_resolved || DataChanged())
		{
			Resolve();
		}

		(_from, _to) = Span();
		double from = MokaChartX.FromDateTimeOffset(_from).Value;
		double to = MokaChartX.FromDateTimeOffset(_to).Value;
		_nowShare = ShowNow && _data.Length > 0 && _nowMs >= from && _nowMs <= to ? Flip((_nowMs - from) / (to - from)) : null;
		LayOut(from, to);
	}

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn) =>
		Describe(drawn, ShowUptime);

	private ChartActiveInfo? Describe(int drawn, bool uptime)
	{
		if (drawn < 0 || drawn >= _marks.Length)
		{
			return null;
		}

		Mark mark = _marks[drawn];
		RowView view = _views[mark.Row];
		List<string> notes = [];
		List<ChartActiveRow> rows = [];
		List<MokaChartPoint> points = [];
		if (!mark.Run.Merged)
		{
			(double from, double to, StatePiece piece) = TrueSpan(view, mark.Run.First);
			StateInfo state = _states[piece.State];
			string took = Took(to - from);
			rows.Add(TipRow(state, took));
			points.Add(Point(view, state, piece.State, from, to - from, took));
			notes.Add(Words.Between(When(from), When(to)));
			if (piece.Segment >= 0 && view.Data.Segments[piece.Segment].Detail is { Length: > 0 } detail)
			{
				notes.Add(detail);
			}
		}
		else
		{
			double from = view.Pieces[mark.Run.First].From;
			double to = view.Pieces[mark.Run.First + mark.Run.Count - 1].To;
			foreach ((int index, double milliseconds) in StateTimelineLayout.Durations(view.Pieces, mark.Run)
				         .OrderByDescending(total => _severity[total.State]))
			{
				StateInfo state = _states[index];
				string took = Took(milliseconds);
				rows.Add(TipRow(state, took));
				points.Add(Point(view, state, index, from, milliseconds, took));
			}

			notes.Add(Words.Between(When(from), When(to)));
		}

		if (view.Data.Row.Detail is { Length: > 0 } rowDetail)
		{
			notes.Add(rowDetail);
		}

		if (uptime && UptimeNote(view) is { } note)
		{
			notes.Add(note);
		}

		return ChartRows.Describe(drawn, view.Data.Name, rows, notes, points);
	}

	/// <summary>The uptime text for a share, as the row's end writes it.</summary>
	/// <param name="share">The share, 0 to 1.</param>
	internal string UptimeText(double share)
	{
		if (UptimeFormatter is { } formatter)
		{
			return formatter(share);
		}

		double percent = Math.Floor((Math.Clamp(share, 0, 1) * 10000) + 1e-6) / 100;
		return (percent >= 100 ? "100" : percent.ToString("0.00", CultureInfo.CurrentCulture)) + "%";
	}

	private bool DataChanged()
	{
		if (Now != _resolvedNowParameter || !ChartSnapshot.SameItems(States, _resolvedStates))
		{
			return true;
		}

		return DataVersion is { } version
			? version != _resolvedVersion || !ReferenceEquals(Rows, _resolvedRows)
			: !SameRows(Rows);
	}

	private bool SameRows(IReadOnlyList<MokaStateRow>? rows)
	{
		if (rows is null || _rowsSnapshot is null || _spansSnapshot is null)
		{
			return rows is null && _rowsSnapshot is null;
		}

		if (rows.Count != _rowsSnapshot.Length)
		{
			return false;
		}

		for (int i = 0; i < rows.Count; i++)
		{
			MokaStateRow? row = rows[i];
			MokaStateRow? was = _rowsSnapshot[i];
			if (!ReferenceEquals(row, was) && (row is null || was is null || row.Name != was.Name || row.Key != was.Key
			                                   || row.Detail != was.Detail))
			{
				return false;
			}

			if (!ChartSnapshot.SameItems(row?.Spans, _spansSnapshot[i]))
			{
				return false;
			}
		}

		return true;
	}

	private void Resolve()
	{
		IReadOnlyList<MokaStateRow> rows = Rows ?? [];
		_resolved = true;
		_resolvedRows = Rows;
		_resolvedStates = ChartSnapshot.Copy(States);
		_resolvedNowParameter = Now;
		_resolvedVersion = DataVersion;
		if (DataVersion is null)
		{
			_rowsSnapshot = [.. rows];
			_spansSnapshot = [.. rows.Select(row => row?.Spans is { } spans ? (MokaStateSpan[]?)[.. spans] : null)];
		}
		else
		{
			_rowsSnapshot = null;
			_spansSnapshot = null;
		}

		DateTimeOffset now = Now ?? TimelineServices?.GetService<TimeProvider>()?.GetUtcNow() ?? DateTimeOffset.UtcNow;
		_nowMs = MokaChartX.FromDateTimeOffset(now).Value;
		BuildStates();

		List<RowData> data = [];
		for (int i = 0; i < rows.Count; i++)
		{
			if (rows[i] is not { } row)
			{
				continue;
			}

			StateSegment[] segments = StateTimelineLayout.Resolve(row.Spans ?? [], StateOf, _severity, _unknown, _nowMs);
			data.Add(new RowData(i, row, row.Name ?? string.Empty, row.Key ?? row.Name ?? string.Empty, segments));
		}

		_data = [.. data];
	}

	private void BuildStates()
	{
		_states.Clear();
		_stateIndex.Clear();
		_severity.Clear();
		foreach (MokaTimelineState state in States ?? [])
		{
			if (state is { Key.Length: > 0 } && !_stateIndex.ContainsKey(state.Key))
			{
				AddState(StateInfo.From(state));
			}
		}

		if (States is null)
		{
			foreach (string key in (string[])[UpKey, DegradedKey, DownKey, MaintenanceKey, UnknownKey])
			{
				AddState(StateInfo.BuiltIn(key));
			}
		}

		int unknown = _states.FindIndex(state => state.Tone == MokaStateTone.Unknown);
		_unknown = unknown >= 0 ? unknown : AddState(StateInfo.BuiltIn(UnknownKey));
	}

	private int AddState(StateInfo state)
	{
		_states.Add(state);
		_severity.Add(state.Severity);
		_stateIndex[state.Key] = _states.Count - 1;
		return _states.Count - 1;
	}

	private int StateOf(string key)
	{
		if (_stateIndex.TryGetValue(key, out int index))
		{
			return index;
		}

		return AddState(StateInfo.IsBuiltIn(key) ? StateInfo.BuiltIn(key) : StateInfo.Unlisted(key));
	}

	private void LayOut(double from, double to)
	{
		double measured = PlotWidthBucket is { } width && width > 0 ? width : DefaultWidth;
		if (_layoutWidth is not { } kept || measured < kept / SettleRatio || measured > kept * SettleRatio)
		{
			_layoutWidth = measured;
		}

		_quantum = StateTimelineLayout.Quantum(to - from, _layoutWidth.Value / PixelsPerMark);
		double block = _quantum * OriginBlock;
		_origin = Math.Floor(from / block) * block;
		_axisOffset = (from - _origin) / _quantum;
		_axisScale = (100 * _quantum / (to - from)).ToString("0.##########", CultureInfo.InvariantCulture) + "%";
		double known = Math.Min(to, _nowMs);
		bool[] present = new bool[_states.Count];
		List<RowView> views = [];
		List<Mark> marks = [];
		_widest = 0;
		foreach (RowData data in _data)
		{
			MarkPresent(data, from, to, known, present);
			List<StatePiece> pieces = StateTimelineLayout.Pieces(data.Segments, from, to, _nowMs, _unknown,
				state => IsHidden(_states[state].Key));
			_widest = Math.Max(_widest, pieces.Count);
			List<StateRun> runs = StateTimelineLayout.Runs(pieces, _quantum, _origin, _severity);
			int first = marks.Count;
			for (int i = 0; i < runs.Count; i++)
			{
				StateRun run = runs[i];
				StateRun? before = i > 0 ? runs[i - 1] : null;
				marks.Add(BuildMark(views.Count, marks.Count, data, run, before, i == 0, i == runs.Count - 1,
					Identity(data, pieces[run.First])));
			}

			(double knownTime, double good) = ShowUptime
				? StateTimelineLayout.Uptime(data.Segments, from, known, state => _states[state].Tone != MokaStateTone.Unknown,
					state => _states[state].Tone == MokaStateTone.Good)
				: (0, 0);
			views.Add(new RowView(data, pieces, first, runs.Count, knownTime > 0 ? UptimeText(good / knownTime) : null,
				Math.Max(0, known - from - knownTime)));
		}

		_views = [.. views];
		_marks = [.. marks];
		_table = null;
		_series =
		[
			.. _states.Select((state, index) => (state, index)).Where(pair => present[pair.index]).Select(pair =>
				new ChartSeriesInfo(pair.state.Key, NameOf(pair.state), pair.index + 1, pair.state.ColorClass, pair.state.ColorStyle,
					ChartMark.Box, IsHidden(pair.state.Key), false, null) { Icon = pair.state.Icon })
		];
	}

	// Which states show on the clock, whether hidden or not, so a hidden state stays in the legend to be shown again.
	private void MarkPresent(RowData data, double from, double to, double known, bool[] present)
	{
		StateSegment[] segments = data.Segments;
		foreach (StateSegment segment in segments)
		{
			if (segment.To > from && segment.From < to)
			{
				present[segment.State] = true;
			}
		}

		double first = segments.Length > 0 ? segments[0].From : double.PositiveInfinity;
		double last = segments.Length > 0 ? segments[^1].To : double.NegativeInfinity;
		if (Math.Min(first, known) > from || (segments.Length > 0 && known > Math.Max(last, from)))
		{
			present[_unknown] = true;
		}
	}

	// A mark is known by where the stretch it starts with starts, before the axis cut it, so the first mark of a row keeps
	// its element while a window slides across it. Time before the first span has nothing before it.
	private static double Identity(RowData data, StatePiece piece) => piece.Segment >= 0
		? data.Segments[piece.Segment].From
		: data.Segments.Length > 0 && piece.From >= data.Segments[^1].To
			? piece.From
			: double.NegativeInfinity;

	private Mark BuildMark(int row, int drawn, RowData data, StateRun run, StateRun? before, bool first, bool last,
		double identity)
	{
		StateInfo state = _states[run.State];
		bool muted = IsMuted(data.Key) && IsMuted(state.Key);
		bool reverse = XAxis?.Reverse == true;
		double length = run.To - run.From;
		bool gap = before is { } previous && previous.State != run.State && run.From - previous.To < 1
		           && length >= _quantum * GapQuanta;
		string classes = new CssBuilder("moka-chart-hatch")
			.AddClass(muted ? "moka-chart-muted" : state.MarkClass)
			.AddClass("moka-stl-hatched", state.Hatched)
			.AddClass("moka-stl-thin", length < _quantum)
			.AddClass("moka-stl-gap", gap)
			.AddClass("moka-stl-l", reverse ? last : first)
			.AddClass("moka-stl-r", reverse ? first : last)
			.Build();
		string style = new StyleBuilder()
			.AddStyle("--_s", Number((run.From - _origin) / _quantum))
			.AddStyle("--_w", Number(length / _quantum))
			.AddStyle("--moka-chart-color", muted ? null : state.ColorValue)
			.Build()!;
		return new Mark(row, run, drawn, drawn.ToString(CultureInfo.InvariantCulture), state.Key, classes, style, identity);
	}

	private (DateTimeOffset From, DateTimeOffset To) Span()
	{
		DateTimeOffset now = DateTimeOffset.FromUnixTimeMilliseconds((long)_nowMs);
		DateTimeOffset? first = null;
		DateTimeOffset? last = null;
		foreach (RowData data in _data)
		{
			if (data.Segments.Length == 0)
			{
				continue;
			}

			DateTimeOffset start = Time(data.Segments[0].From);
			DateTimeOffset end = Time(data.Segments[^1].To);
			first = first is null || start < first ? start : first;
			last = last is null || end > last ? end : last;
		}

		DateTimeOffset from = From ?? XAxis?.MinTime ?? first ?? now;
		DateTimeOffset to = To ?? XAxis?.MaxTime ?? last ?? now;
		if (to <= from)
		{
			(from, to) = (from - TimeSpan.FromMinutes(30), from + TimeSpan.FromMinutes(30));
		}

		return (from, to);
	}

	// A mark of one piece names the whole stretch it shows, not the part inside the clock.
	private static (double From, double To, StatePiece Piece) TrueSpan(RowView view, int pieceIndex)
	{
		StatePiece piece = view.Pieces[pieceIndex];
		return piece.Segment >= 0
			? (view.Data.Segments[piece.Segment].From, view.Data.Segments[piece.Segment].To, piece)
			: (piece.From, piece.To, piece);
	}

	private ChartActiveRow TipRow(StateInfo state, string took) =>
		new(NameOf(state), state.ColorClass, state.ColorStyle, took, null, false) { Icon = state.Icon };

	private MokaChartPoint Point(RowView view, StateInfo state, int stateIndex, double from, double milliseconds,
		string took) => new(state.Key, NameOf(state), stateIndex, view.Data.Source, milliseconds, took)
	{
		Category = view.Data.Name,
		Time = Time(from),
		X = from,
		RowKey = view.Data.Key
	};

	private string? UptimeNote(RowView view)
	{
		if (view.UptimeText is not { } figure)
		{
			return null;
		}

		string note = $"{StateWords.Uptime} {figure}";
		return view.UnknownMilliseconds >= 1000 ? $"{note}, {StateWords.UnknownTime(Took(view.UnknownMilliseconds))}" : note;
	}

	// Marks are written here rather than in markup, so they carry no scope attribute: a chart can draw thousands, and
	// every attribute of every mark crosses to the browser. The stylesheet reaches them through the lane. Each is keyed by
	// when its stretch starts, so when the oldest mark of a row slides out of a live window the rest keep their elements
	// rather than each taking its neighbour's attributes (element-component-model-relationships.md, "When to use @key").
	private RenderFragment MarksOf(RowView view) => builder =>
	{
		for (int i = view.FirstMark; i < view.FirstMark + view.MarkCount; i++)
		{
			Mark mark = _marks[i];
			builder.OpenElement(0, "span");
			builder.SetKey(mark.Identity);
			builder.AddAttribute(1, "class", mark.Class);
			builder.AddAttribute(2, "style", mark.Style);
			builder.AddAttribute(3, "data-i", mark.DrawnText);
			builder.AddAttribute(4, "data-moka-key", mark.Key);
			builder.AddAttribute(5, "title", MarkTitle(mark));
			builder.CloseElement();
		}
	};

	private string? MarkTitle(Mark mark)
	{
		if (!NativeTooltips || _views[mark.Row].MarkCount > TitledMarksPerRow)
		{
			return null;
		}

		return mark.Title ??= MarkTitleText(mark);
	}

	private string? LaneTitle(RowView view)
	{
		if (!NativeTooltips || view.MarkCount <= TitledMarksPerRow)
		{
			return null;
		}

		return view.Title ??=
			$"{view.Data.Name}: {string.Join(", ", Totals(view).Select(total => $"{NameOf(_states[total.State])} {Took(total.Milliseconds)}"))}";
	}

	private string MarkTitleText(Mark mark)
	{
		ChartActiveInfo? info = Describe(mark.Drawn, false);
		return info?.CellText ?? string.Empty;
	}

	private IEnumerable<(int State, double Milliseconds)> Totals(RowView view) =>
		StateTimelineLayout.Durations(view.Pieces, new StateRun(0, view.Pieces.Count, 0, 1, _unknown))
			.OrderByDescending(total => _severity[total.State]);

	private (int Row, int Piece)[] TableIndex()
	{
		if (_table is { } cached && cached.Version == GeometryVersion)
		{
			return cached.Index;
		}

		List<(int Row, int Piece)> index = [];
		for (int row = 0; row < _views.Length; row++)
		{
			for (int piece = 0; piece < _views[row].Pieces.Count; piece++)
			{
				index.Add((row, piece));
			}
		}

		_table = (GeometryVersion, [.. index]);
		return _table.Value.Index;
	}

	private string TableCell((int Row, int Piece) at, int column, bool raw)
	{
		RowView view = _views[at.Row];
		(double from, double to, StatePiece piece) = TrueSpan(view, at.Piece);
		StateInfo state = _states[piece.State];
		return column switch
		{
			0 => view.Data.Name,
			1 => raw ? state.Key : NameOf(state),
			2 => raw ? Iso(from) : When(from),
			3 => raw ? Iso(to) : When(to),
			_ => raw ? ((to - from) / 1000).ToString(CultureInfo.InvariantCulture) : Took(to - from)
		};
	}

	private double Flip(double share) => XAxis?.Reverse == true ? 1 - share : share;

	private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

	private string When(double milliseconds) => TimeText(milliseconds, _to - _from);

	private string Took(double milliseconds)
	{
		TimeSpan span = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
		return DurationText?.Invoke(span) ?? ChartRows.Duration(span);
	}

	private string NameOf(StateInfo state) => state.Name ?? state.Key switch
	{
		UpKey => StateWords.Up,
		DegradedKey => StateWords.Degraded,
		DownKey => StateWords.Down,
		MaintenanceKey => StateWords.Maintenance,
		_ => StateWords.Unknown
	};

	private static DateTimeOffset Time(double milliseconds) => new MokaChartX(milliseconds, true).Time;

	private static string Iso(double milliseconds) => Time(milliseconds).ToString("o", CultureInfo.InvariantCulture);

	/// <summary>A row once its spans are resolved, which lasts until the data changes.</summary>
	private sealed record RowData(int Source, MokaStateRow Row, string Name, string Key, StateSegment[] Segments);

	/// <summary>A row as the clock shows it now: its pieces, its marks and its uptime.</summary>
	private sealed record RowView(
		RowData Data,
		List<StatePiece> Pieces,
		int FirstMark,
		int MarkCount,
		string? UptimeText,
		double UnknownMilliseconds)
	{
		public string? Title { get; set; }
	}

	/// <summary>A drawn mark, with its classes and style worked out once per geometry change.</summary>
	private sealed record Mark(int Row, StateRun Run, int Drawn, string DrawnText, string Key, string Class, string Style,
		object Identity)
	{
		public string? Title { get; set; }
	}

	/// <summary>A state as the chart draws it. A null name is a built-in state's, read from the words when written.</summary>
	private sealed record StateInfo(
		string Key,
		string? Name,
		MokaStateTone Tone,
		int Severity,
		bool Hatched,
		string? ColorValue,
		MokaIconDefinition Icon)
	{
		/// <summary>The class that colors its legend entry and tooltip key.</summary>
		public string ColorClass => Tone switch
		{
			MokaStateTone.Good => "moka-chart-tone-success",
			MokaStateTone.Warn => "moka-chart-tone-warning",
			MokaStateTone.Bad => "moka-chart-tone-error",
			MokaStateTone.Info => "moka-chart-tone-info",
			_ => "moka-chart-muted"
		};

		/// <summary>An inline color for its key, its own or the unknown tone's.</summary>
		public string? ColorStyle => ColorValue is not null
			? new StyleBuilder().AddStyle("--moka-chart-color", ColorValue).Build()
			: Tone == MokaStateTone.Unknown
				? new StyleBuilder().AddStyle("--moka-chart-color", "var(--moka-color-on-surface-tertiary)").Build()
				: null;

		/// <summary>The class that colors its marks and sets their hatch angle.</summary>
		public string MarkClass => Tone switch
		{
			MokaStateTone.Good => "moka-stl-good",
			MokaStateTone.Warn => "moka-stl-warn",
			MokaStateTone.Bad => "moka-stl-bad",
			MokaStateTone.Info => "moka-stl-info",
			MokaStateTone.Off => "moka-stl-off",
			_ => "moka-stl-unknown"
		};

		public static StateInfo From(MokaTimelineState state) => new(state.Key, state.Name, state.Tone,
			state.Severity ?? SeverityOf(state.Tone), state.Hatched ?? state.Tone == MokaStateTone.Unknown,
			CssValues.IsColor(state.ColorValue) ? state.ColorValue.Trim() : null, state.Icon ?? IconOf(state.Tone));

		public static bool IsBuiltIn(string key) => key is UpKey or DegradedKey or DownKey or MaintenanceKey or UnknownKey;

		public static StateInfo BuiltIn(string key)
		{
			MokaStateTone tone = key switch
			{
				UpKey => MokaStateTone.Good,
				DegradedKey => MokaStateTone.Warn,
				DownKey => MokaStateTone.Bad,
				MaintenanceKey => MokaStateTone.Info,
				_ => MokaStateTone.Unknown
			};
			return new StateInfo(key, null, tone, SeverityOf(tone), tone == MokaStateTone.Unknown, null, IconOf(tone));
		}

		public static StateInfo Unlisted(string key) =>
			new(key, key, MokaStateTone.Off, SeverityOf(MokaStateTone.Off), false, null, IconOf(MokaStateTone.Off));

		private static int SeverityOf(MokaStateTone tone) => tone switch
		{
			MokaStateTone.Unknown => 0,
			MokaStateTone.Good => 10,
			MokaStateTone.Info => 20,
			MokaStateTone.Off => 30,
			MokaStateTone.Warn => 40,
			_ => 50
		};

		private static MokaIconDefinition IconOf(MokaStateTone tone) => tone switch
		{
			MokaStateTone.Good => MokaIcons.Status.CheckCircle,
			MokaStateTone.Warn => MokaIcons.Status.Warning,
			MokaStateTone.Bad => MokaIcons.Status.Error,
			MokaStateTone.Info => MokaIcons.Content.Wrench,
			MokaStateTone.Off => MokaIcons.Action.Power,
			_ => MokaIcons.Status.HelpCircle
		};
	}
}
