using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Heatmap;

/// <summary>
///     A calendar heatmap, the contribution-graph view: one small square per day, colored by how
///     much happened that day, laid out weeks across and days of the week down.
/// </summary>
public partial class MokaHeatmap : MokaVisualComponentBase
{
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

	/// <summary>What the heatmap shows, for assistive technology.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Days nobody knows about, such as days before tracking began or while a collector was down, which aren't the
	///     same as days with nothing in them. They are drawn hollow and hatched rather than as an empty day. Matched by
	///     day; time is ignored. A date that is also in <see cref="Data" /> counts as known.
	/// </summary>
	[Parameter]
	public IReadOnlyCollection<DateTime>? Unknown { get; set; }

	/// <summary>What an unknown day's tooltip says before its date. Defaults to "No data".</summary>
	[Parameter]
	public string UnknownText { get; set; } = "No data";

	/// <inheritdoc />
	protected override string RootClass => "moka-heatmap";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private sealed record MonthLabel(string Text);
	private sealed record Cell(DateTime Date, int Count, int Level, string CssClass, string Title);
	private sealed record WeekdayLabel(string Text);

	private DateTime End => EndDate?.Date ?? DateTime.Today;

	// The grid runs Monday to Sunday and always includes the whole week the end day sits in, so
	// the last column holds it and the days after it. GridEnd is exclusive: the Monday after.
	private DateTime GridEnd => End.AddDays(7 - ((int)End.DayOfWeek + 6) % 7);

	// Counting back Weeks whole weeks from that Monday lands on a Monday again, so every column is
	// a full week.
	private DateTime Start => GridEnd.AddDays(-Math.Max(1, Weeks) * 7);

	private Dictionary<DateTime, int> Counts
	{
		get
		{
			Dictionary<DateTime, int> counts = [];
			if (Data is not null)
			{
				foreach (KeyValuePair<DateTime, int> pair in Data)
				{
					counts[pair.Key.Date] = pair.Value;
				}
			}

			return counts;
		}
	}

	private int Max
	{
		get
		{
			if (MaxValue.HasValue)
			{
				return Math.Max(1, MaxValue.Value);
			}

			int max = 0;
			foreach (int count in Counts.Values)
			{
				if (count > max)
				{
					max = count;
				}
			}

			return Math.Max(1, max);
		}
	}

	private IReadOnlyList<MonthLabel> Months
	{
		get
		{
			List<MonthLabel> labels = [];
			DateTime gridEnd = GridEnd;
			DateTime previousMonth = Start.Month == 1 ? new DateTime(Start.Year - 1, 12, 1) : new DateTime(Start.Year, Start.Month - 1, 1);

			for (DateTime week = Start; week < gridEnd; week = week.AddDays(7))
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
	}

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

	private IReadOnlyList<Cell> Cells
	{
		get
		{
			List<Cell> cells = [];
			Dictionary<DateTime, int> counts = Counts;
			HashSet<DateTime> unknown = [.. (Unknown ?? []).Select(day => day.Date)];
			int max = Max;
			DateTime gridEnd = GridEnd;
			string pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

			for (DateTime day = Start; day < gridEnd; day = day.AddDays(1))
			{
				bool future = day > End;
				if (!future && unknown.Contains(day) && !counts.ContainsKey(day))
				{
					cells.Add(new Cell(day, 0, 0, "moka-heatmap-cell moka-heatmap-cell--unknown",
						$"{UnknownText} on {day.ToString(pattern, CultureInfo.CurrentCulture)}"));
					continue;
				}

				int count = future ? 0 : counts.GetValueOrDefault(day);
				int level = count <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(count * 4d / max), 1, 4);

				cells.Add(new Cell(day, count, level,
					$"moka-heatmap-cell moka-heatmap-cell--l{level.ToString(CultureInfo.InvariantCulture)}{(future ? " moka-heatmap-cell--future" : "")}",
					$"{count.ToString(CultureInfo.CurrentCulture)} on {day.ToString(pattern, CultureInfo.CurrentCulture)}"));
			}

			return cells;
		}
	}
}
