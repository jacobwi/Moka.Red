using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.DateRangePicker;

/// <summary>
///     A dual-date picker for selecting a date range (start and end).
///     Displays two side-by-side month calendars in a popup.
///     Pure C# implementation — no JavaScript interop required.
/// </summary>
public partial class MokaDateRangePicker : MokaVisualComponentBase
{
	private readonly string _inputId = $"moka-daterange-{Guid.NewGuid():N}";
	private DateOnly? _hoverDate;
	private bool _isOpen;
	private DateTime _leftMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
	private bool _selectingEnd;

	/// <summary>The start date of the range. Two-way bindable.</summary>
	[Parameter]
	public DateOnly? StartDate { get; set; }

	/// <summary>Callback for when <see cref="StartDate" /> changes.</summary>
	[Parameter]
	public EventCallback<DateOnly?> StartDateChanged { get; set; }

	/// <summary>The end date of the range. Two-way bindable.</summary>
	[Parameter]
	public DateOnly? EndDate { get; set; }

	/// <summary>Callback for when <see cref="EndDate" /> changes.</summary>
	[Parameter]
	public EventCallback<DateOnly?> EndDateChanged { get; set; }

	/// <summary>Minimum selectable date.</summary>
	[Parameter]
	public DateOnly? MinDate { get; set; }

	/// <summary>Maximum selectable date.</summary>
	[Parameter]
	public DateOnly? MaxDate { get; set; }

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Placeholder text when no range is selected.</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Select date range";

	/// <summary>Display format for dates. Default is "yyyy-MM-dd".</summary>
	[Parameter]
	public string Format { get; set; } = "yyyy-MM-dd";

	/// <summary>First day of the week. Default is Monday.</summary>
	[Parameter]
	public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;

	/// <inheritdoc />
	protected override string RootClass => "moka-daterange";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-daterange--{SizeToKebab(Size)}")
		.AddClass("moka-daterange--disabled", Disabled)
		.AddClass("moka-daterange--open", _isOpen)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private DateTime RightMonth => _leftMonth.AddMonths(1);

	private string DisplayValue
	{
		get
		{
			if (StartDate.HasValue && EndDate.HasValue)
			{
				return
					$"{StartDate.Value.ToString(Format, CultureInfo.InvariantCulture)} — {EndDate.Value.ToString(Format, CultureInfo.InvariantCulture)}";
			}

			if (StartDate.HasValue)
			{
				return $"{StartDate.Value.ToString(Format, CultureInfo.InvariantCulture)} — ...";
			}

			return "";
		}
	}

	private string InputCssClass => new CssBuilder("moka-daterange__input")
		.AddClass($"moka-daterange__input--{SizeToKebab(Size)}")
		.Build();

	private IEnumerable<string> WeekdayHeaders
	{
		get
		{
			CultureInfo culture = CultureInfo.CurrentCulture;
			int firstDay = (int)FirstDayOfWeek;
			for (int i = 0; i < 7; i++)
			{
				string dayName = culture.DateTimeFormat.AbbreviatedDayNames[(firstDay + i) % 7];
				yield return dayName[..2];
			}
		}
	}

	/// <summary>Has internal open/closed and selection state.</summary>
	protected override bool ShouldRender() => true;

	private void ToggleCalendar()
	{
		if (Disabled)
		{
			return;
		}

		_isOpen = !_isOpen;
		if (_isOpen)
		{
			_selectingEnd = false;
			if (StartDate.HasValue)
			{
				_leftMonth = new DateTime(StartDate.Value.Year, StartDate.Value.Month, 1);
			}
			else
			{
				_leftMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			}
		}
	}

	private void CloseCalendar()
	{
		_isOpen = false;
		_hoverDate = null;
		_selectingEnd = false;
	}

	private void PrevMonth() => _leftMonth = _leftMonth.AddMonths(-1);

	private void NextMonth() => _leftMonth = _leftMonth.AddMonths(1);

	private IEnumerable<DateOnly> GetCalendarDays(DateTime monthStart)
	{
		var firstOfMonth = new DateTime(monthStart.Year, monthStart.Month, 1);
		int firstDayOffset = ((int)firstOfMonth.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;
		var startDate = DateOnly.FromDateTime(firstOfMonth.AddDays(-firstDayOffset));

		for (int i = 0; i < 42; i++)
		{
			yield return startDate.AddDays(i);
		}
	}

	private bool IsDayDisabled(DateOnly day) =>
		(MinDate.HasValue && day < MinDate.Value) ||
		(MaxDate.HasValue && day > MaxDate.Value);

	private bool IsInRange(DateOnly day)
	{
		if (StartDate.HasValue && EndDate.HasValue)
		{
			return day >= StartDate.Value && day <= EndDate.Value;
		}

		if (StartDate.HasValue && _selectingEnd && _hoverDate.HasValue)
		{
			DateOnly rangeEnd = _hoverDate.Value;
			DateOnly rangeStart = StartDate.Value;
			if (rangeEnd < rangeStart)
			{
				(rangeStart, rangeEnd) = (rangeEnd, rangeStart);
			}

			return day >= rangeStart && day <= rangeEnd;
		}

		return false;
	}

	private string DayCssClass(DateOnly day, DateTime monthContext) => new CssBuilder("moka-daterange__day")
		.AddClass("moka-daterange__day--other-month", day.Month != monthContext.Month)
		.AddClass("moka-daterange__day--today", day == DateOnly.FromDateTime(DateTime.Today))
		.AddClass("moka-daterange__day--start", StartDate.HasValue && day == StartDate.Value)
		.AddClass("moka-daterange__day--end", EndDate.HasValue && day == EndDate.Value)
		.AddClass("moka-daterange__day--in-range", IsInRange(day))
		.AddClass("moka-daterange__day--disabled", IsDayDisabled(day))
		.Build();

	private async Task SelectDay(DateOnly day)
	{
		if (IsDayDisabled(day))
		{
			return;
		}

		if (!_selectingEnd || !StartDate.HasValue)
		{
			// Selecting start date
			StartDate = day;
			EndDate = null;
			_selectingEnd = true;
			await StartDateChanged.InvokeAsync(StartDate);
			await EndDateChanged.InvokeAsync(EndDate);
		}
		else
		{
			// Selecting end date
			if (day < StartDate.Value)
			{
				// Swap: user clicked before start
				EndDate = StartDate;
				StartDate = day;
			}
			else
			{
				EndDate = day;
			}

			_selectingEnd = false;
			_isOpen = false;
			_hoverDate = null;
			await StartDateChanged.InvokeAsync(StartDate);
			await EndDateChanged.InvokeAsync(EndDate);
		}
	}

	private void HandleDayHover(DateOnly day)
	{
		if (_selectingEnd)
		{
			_hoverDate = day;
		}
	}

	private async Task ClearRange()
	{
		StartDate = null;
		EndDate = null;
		_selectingEnd = false;
		await StartDateChanged.InvokeAsync(null);
		await EndDateChanged.InvokeAsync(null);
	}
}
