using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Data.Chart;

/// <summary>
///     One row per job with a bar from its start to its end on a shared clock, for runs, backups and schedules, and a
///     line at now. Done and failed mean good and bad, so bars take status colors, each with an icon and a word, and
///     the legend lists the statuses.
/// </summary>
/// <remarks>
///     A job without an end runs until <see cref="Now" />, which the chart reads when its data changes; pass a new
///     <see cref="Now" /> to move it. Past 200 rows the rows are drawn only as they scroll into view.
/// </remarks>
public partial class MokaGanttChart : MokaCartesianChartBase
{
	// A side of the track with less room than this holds no word, so the word goes to the other side or inside.
	private const double WordRoom = 0.2;

	private static readonly MokaGanttStatus[] Statuses = Enum.GetValues<MokaGanttStatus>();

	private MokaGanttRow[]? _snapshot;
	private Job[] _jobs = [];
	private List<ChartSeriesInfo> _series = [];
	private DateTimeOffset _from;
	private DateTimeOffset _to;
	private DateTimeOffset _now;
	private double? _nowShare;

	[Inject]
	private IServiceProvider GanttServices { get; set; } = default!;

	/// <summary>The jobs, one row each, in order.</summary>
	[Parameter]
	public IReadOnlyList<MokaGanttRow>? Rows { get; set; }

	/// <summary>Where the shared clock starts. Null starts at the earliest job.</summary>
	[Parameter]
	public DateTimeOffset? From { get; set; }

	/// <summary>Where the shared clock ends. Null ends at the latest job's end, or now for a job still running.</summary>
	[Parameter]
	public DateTimeOffset? To { get; set; }

	/// <summary>The time now, where running jobs end and the line is drawn. Null reads a registered <see cref="TimeProvider" />, else the clock.</summary>
	[Parameter]
	public DateTimeOffset? Now { get; set; }

	/// <summary>Whether a line marks now when it falls on the clock. Defaults to true.</summary>
	[Parameter]
	public bool ShowNow { get; set; } = true;

	/// <summary>The word for a status, on each bar, in the legend and the tooltip. Null is English: "Done", "Failed" and so on.</summary>
	[Parameter]
	public Func<MokaGanttStatus, string>? StatusText { get; set; }

	/// <summary>What the line at now is called, before its time. Defaults to "Now".</summary>
	[Parameter]
	public string NowText { get; set; } = "Now";

	/// <summary>Writes how long a job took, such as "41 min". Null writes minutes, hours and days in English.</summary>
	[Parameter]
	public Func<TimeSpan, string>? DurationText { get; set; }

	/// <summary>The table's heading for how long a job took. Defaults to "Took".</summary>
	[Parameter]
	public string TookLabel { get; set; } = "Took";

	/// <summary>The table's heading for a job's status. Defaults to "Status".</summary>
	[Parameter]
	public string StatusLabel { get; set; } = "Status";

	/// <inheritdoc />
	protected override string RootClass => "moka-gantt";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(ModernRootClasses)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => _jobs.Length == 0
		? "Gantt chart"
		: $"Gantt chart: {string.Join(", ", _series.Where(s => !s.Hidden).Select(s => $"{Count(s.Key)} {s.Name}"))}";

	/// <inheritdoc />
	internal override bool HasData => _jobs.Length > 0;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _series;

	/// <inheritdoc />
	internal override bool DrawsRows => true;

	/// <inheritdoc />
	internal override bool SurfaceRows => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int RowCount => _jobs.Length;

	/// <inheritdoc />
	internal override int CategoryCount => _jobs.Length;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _jobs.Length;

	/// <inheritdoc />
	internal override int LatestDrawn(int panel) => _jobs.Length > 0 ? 0 : -1;

	/// <inheritdoc />
	internal override (DateTimeOffset From, DateTimeOffset To)? RowTimeRange => _jobs.Length > 0 ? (_from, _to) : null;

	/// <inheritdoc />
	internal override string RowLabel(int index) => index >= 0 && index < _jobs.Length ? _jobs[index].Row.Name : string.Empty;

	/// <summary>
	///     The time now, over the names, where it stays while the rows scroll: the line down the rows is unlabeled, so
	///     it never covers the first job.
	/// </summary>
	internal override string? DefaultValueTitle => _nowShare is null ? null : NowLabel;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => new([Words.Name, Words.From, Words.To, TookLabel, StatusLabel], _jobs.Length,
		(row, column) => column switch
		{
			0 => _jobs[row].Row.Name,
			1 => When(_jobs[row].Row.Start),
			2 => _jobs[row].Row.End is { } end ? When(end) : string.Empty,
			3 => Took(_jobs[row]),
			_ => _jobs[row].Word
		},
		(row, column) => column switch
		{
			0 => _jobs[row].Row.Name,
			1 => _jobs[row].Row.Start.ToString("o", CultureInfo.InvariantCulture),
			2 => _jobs[row].Row.End?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
			3 => (_jobs[row].End - _jobs[row].Row.Start).TotalSeconds.ToString(CultureInfo.InvariantCulture),
			_ => _jobs[row].Row.Status.ToString()
		});

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	/// <summary>A drawn job: its record, where it came from, where its bar ends, and how its status is drawn.</summary>
	private sealed record Job(int Source, MokaGanttRow Row, DateTimeOffset End, string Key, string ToneClass, bool Muted,
		MokaIconDefinition Icon, string Word);

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Rows)
		? ChartSnapshot.SameItems(after as IReadOnlyList<MokaGanttRow>, _snapshot)
		: base.SameValue(name, before, after);

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_snapshot = DataVersion is null ? ChartSnapshot.Copy(Rows) : null;
		IReadOnlyList<MokaGanttRow> rows = Rows ?? [];
		_now = Now ?? GanttServices?.GetService<TimeProvider>()?.GetUtcNow() ?? DateTimeOffset.UtcNow;

		bool[] present = new bool[Statuses.Length];
		foreach (MokaGanttRow row in rows)
		{
			present[(int)row.Status] = true;
		}

		_series =
		[
			.. Statuses.Where(status => present[(int)status]).Select(status => new ChartSeriesInfo(KeyOf(status), Word(status),
				(int)status + 1, ToneOf(status), null, ChartMark.Box, IsHidden(KeyOf(status)), false, null))
		];

		List<Job> jobs = [];
		for (int i = 0; i < rows.Count; i++)
		{
			MokaGanttRow row = rows[i];
			string status = KeyOf(row.Status);
			if (IsHidden(status))
			{
				continue;
			}

			DateTimeOffset end = row.End ?? (_now > row.Start ? _now : row.Start);
			bool muted = IsMuted(row.Key ?? row.Name) && IsMuted(status);
			jobs.Add(new Job(i, row, end, status, muted ? "moka-chart-muted" : ToneOf(row.Status), muted, IconOf(row.Status),
				Word(row.Status)));
		}

		_jobs = [.. jobs];
		(_from, _to) = Span(_jobs);
		_nowShare = ShowNow && _jobs.Length > 0 && _now >= _from && _now <= _to ? Share(_now) : null;
	}

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _jobs.Length)
		{
			return null;
		}

		Job job = _jobs[drawn];
		string span = Words.Between(When(job.Row.Start), When(job.End));
		string took = Took(job);
		List<string> notes = [string.IsNullOrEmpty(job.Row.Detail) ? job.Word : $"{job.Word}: {job.Row.Detail}"];
		MokaChartPoint point = new(job.Key, job.Word, (int)job.Row.Status, job.Source, (job.End - job.Row.Start).TotalMilliseconds, took)
		{
			Category = job.Row.Name,
			Time = job.Row.Start,
			X = MokaChartX.FromDateTimeOffset(job.Row.Start).Value
		};
		return ChartRows.Describe(drawn, job.Row.Name, [new ChartActiveRow(took, job.ToneClass, null, span, null, false)], notes,
			[point]);
	}

	private string LaneClass(Job job) => new CssBuilder("moka-gantt__lane")
		.AddClass(job.ToneClass)
		.AddClass(WordSide(job))
		.Build();

	private string? LaneStyle(Job job)
	{
		double start = Share(job.Row.Start);
		double end = Share(job.End);
		return new StyleBuilder()
			.AddStyle("--_ga-from", ChartRows.Css(Math.Min(start, end)))
			.AddStyle("--_ga-to", ChartRows.Css(Math.Max(start, end)))
			.AddStyle("--_ga-len", ChartRows.Css(Math.Abs(end - start)))
			.Build();
	}

	private static string BarClass(Job job) => new CssBuilder("moka-gantt__bar moka-chart-hatch")
		.AddClass($"moka-gantt__bar--{job.Key}")
		.Build();

	// The word goes after the bar, else before it, else inside it at its start.
	private string WordSide(Job job)
	{
		double start = Math.Clamp(Share(job.Row.Start), 0, 1);
		double end = Math.Clamp(Share(job.End), 0, 1);
		(double low, double high) = (Math.Min(start, end), Math.Max(start, end));
		return 1 - high >= WordRoom ? "moka-gantt__lane--after" : low >= WordRoom ? "moka-gantt__lane--before" : "moka-gantt__lane--inside";
	}

	private string? NativeTitle(Job job) => NativeTooltips
		? $"{job.Row.Name}: {job.Word}, {Words.Between(When(job.Row.Start), When(job.End))}, {Took(job)}"
		: null;

	private string NowLabel => $"{NowText} {When(_now)}";

	private string? NowStyle => _nowShare is { } share ? new StyleBuilder().AddStyle("--_ga-now", ChartRows.Css(share)).Build() : null;

	private double Share(DateTimeOffset time)
	{
		double span = (_to - _from).TotalMilliseconds;
		double share = span > 0 ? (time - _from).TotalMilliseconds / span : 0.5;
		return XAxis?.Reverse == true ? 1 - share : share;
	}

	private (DateTimeOffset From, DateTimeOffset To) Span(Job[] jobs)
	{
		DateTimeOffset from = From ?? (jobs.Length > 0 ? jobs.Min(job => job.Row.Start < job.End ? job.Row.Start : job.End) : _now);
		DateTimeOffset to = To ?? (jobs.Length > 0 ? jobs.Max(job => job.Row.Start > job.End ? job.Row.Start : job.End) : _now);
		if (to <= from)
		{
			(from, to) = (from - TimeSpan.FromMinutes(30), from + TimeSpan.FromMinutes(30));
		}

		return (from, to);
	}

	private string When(DateTimeOffset time) => MokaTimeTicks.TooltipText(time, _to - _from, new MokaTimeTickOptions
	{
		Zone = ResolvedTimeZone,
		Culture = CultureInfo.CurrentCulture,
		Format = XAxis?.TimeFormat
	});

	private string Took(Job job) =>
		DurationText?.Invoke(job.End - job.Row.Start) ?? ChartRows.Duration(job.End - job.Row.Start);

	private int Count(string key) => _jobs.Count(job => job.Key == key);

	private string Word(MokaGanttStatus status) => StatusText?.Invoke(status) ?? status switch
	{
		MokaGanttStatus.Planned => "Planned",
		MokaGanttStatus.Running => "Running",
		MokaGanttStatus.Failed => "Failed",
		MokaGanttStatus.Skipped => "Skipped",
		_ => "Done"
	};

	private static string KeyOf(MokaGanttStatus status) => status switch
	{
		MokaGanttStatus.Planned => "planned",
		MokaGanttStatus.Running => "running",
		MokaGanttStatus.Failed => "failed",
		MokaGanttStatus.Skipped => "skipped",
		_ => "done"
	};

	private static string ToneOf(MokaGanttStatus status) => status switch
	{
		MokaGanttStatus.Running => ChartRows.ToneClass(MokaColor.Info),
		MokaGanttStatus.Done => ChartRows.ToneClass(MokaColor.Success),
		MokaGanttStatus.Failed => ChartRows.ToneClass(MokaColor.Error),
		_ => "moka-chart-muted"
	};

	private static MokaIconDefinition IconOf(MokaGanttStatus status) => status switch
	{
		MokaGanttStatus.Planned => MokaIcons.Status.Clock,
		MokaGanttStatus.Running => MokaIcons.Action.PlayCircle,
		MokaGanttStatus.Failed => MokaIcons.Status.AlertCircle,
		MokaGanttStatus.Skipped => MokaIcons.Action.Remove,
		_ => MokaIcons.Status.CheckCircle
	};
}
