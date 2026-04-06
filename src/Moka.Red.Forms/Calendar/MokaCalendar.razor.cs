using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Calendar;

/// <summary>
///     A month-view calendar grid for date selection.
///     Displays a full month with day headers and interactive date cells.
///     Unlike <c>MokaDatePicker</c>, this renders the calendar inline rather than as a popup.
/// </summary>
public partial class MokaCalendar : MokaVisualComponentBase
{
	private DateOnly _resolvedMonth;

	/// <summary>The currently selected date. Supports two-way binding.</summary>
	[Parameter]
	public DateOnly? Value { get; set; }

	/// <summary>Callback invoked when the selected date changes.</summary>
	[Parameter]
	public EventCallback<DateOnly?> ValueChanged { get; set; }

	/// <summary>The earliest selectable date.</summary>
	[Parameter]
	public DateOnly? MinDate { get; set; }

	/// <summary>The latest selectable date.</summary>
	[Parameter]
	public DateOnly? MaxDate { get; set; }

	/// <summary>Controls which month is displayed. Defaults to the current month.</summary>
	[Parameter]
	public DateOnly DisplayMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today);

	/// <summary>Callback invoked when the displayed month changes via navigation.</summary>
	[Parameter]
	public EventCallback<DateOnly> DisplayMonthChanged { get; set; }

	/// <summary>The first day of the week. Defaults to <see cref="DayOfWeek.Sunday" />.</summary>
	[Parameter]
	public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;

	/// <summary>Whether to show days from adjacent months in the grid. Defaults to true.</summary>
	[Parameter]
	public bool ShowAdjacentMonthDays { get; set; } = true;

	/// <summary>Whether today's date is visually highlighted. Defaults to true.</summary>
	[Parameter]
	public bool HighlightToday { get; set; } = true;

	/// <summary>Predicate that returns true for dates that should be disabled.</summary>
	[Parameter]
	public Func<DateOnly, bool>? DisabledDates { get; set; }

	/// <summary>Callback invoked when a date cell is clicked.</summary>
	[Parameter]
	public EventCallback<DateOnly> OnDateClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-calendar";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-calendar--{SizeToKebab(Size)}")
		.AddClass($"moka-calendar--{ColorToKebab(ResolvedColor)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	/// <summary>Gets the abbreviated day names starting from <see cref="FirstDayOfWeek" />.</summary>
	private string[] DayHeaders
	{
		get
		{
			string[] names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
			int start = (int)FirstDayOfWeek;
			string[] result = new string[7];
			for (int i = 0; i < 7; i++)
			{
				result[i] = names[(start + i) % 7];
			}

			return result;
		}
	}

	/// <summary>Gets all dates to display in the calendar grid (42 cells = 6 weeks).</summary>
	private List<DateOnly> CalendarDays
	{
		get
		{
			var firstOfMonth = new DateOnly(_resolvedMonth.Year, _resolvedMonth.Month, 1);
			int dayOfWeek = (int)firstOfMonth.DayOfWeek;
			int offset = (dayOfWeek - (int)FirstDayOfWeek + 7) % 7;
			DateOnly startDate = firstOfMonth.AddDays(-offset);

			var days = new List<DateOnly>(42);
			for (int i = 0; i < 42; i++)
			{
				days.Add(startDate.AddDays(i));
			}

			return days;
		}
	}

	/// <summary>Gets the display string for the current month header.</summary>
	private string MonthYearLabel =>
		_resolvedMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

	/// <inheritdoc />
	protected override void OnParametersSet() =>
		_resolvedMonth = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, 1);

	private bool IsCurrentMonth(DateOnly date) =>
		date.Year == _resolvedMonth.Year && date.Month == _resolvedMonth.Month;

	private static bool IsToday(DateOnly date) =>
		date == DateOnly.FromDateTime(DateTime.Today);

	private bool IsSelected(DateOnly date) =>
		Value.HasValue && Value.Value == date;

	private bool IsDateDisabled(DateOnly date)
	{
		if (MinDate.HasValue && date < MinDate.Value)
		{
			return true;
		}

		if (MaxDate.HasValue && date > MaxDate.Value)
		{
			return true;
		}

		return DisabledDates?.Invoke(date) == true;
	}

	private string GetDayCssClass(DateOnly date) => new CssBuilder("moka-calendar__day")
		.AddClass("moka-calendar__day--adjacent", !IsCurrentMonth(date))
		.AddClass("moka-calendar__day--today", HighlightToday && IsToday(date))
		.AddClass("moka-calendar__day--selected", IsSelected(date))
		.AddClass("moka-calendar__day--disabled", IsDateDisabled(date))
		.Build();

	private async Task HandleDateClick(DateOnly date)
	{
		if (IsDateDisabled(date))
		{
			return;
		}

		Value = date;
		await ValueChanged.InvokeAsync(date);
		await OnDateClick.InvokeAsync(date);
	}

	private async Task NavigateMonth(int offset)
	{
		_resolvedMonth = _resolvedMonth.AddMonths(offset);
		DisplayMonth = _resolvedMonth;
		await DisplayMonthChanged.InvokeAsync(_resolvedMonth);
	}
}
