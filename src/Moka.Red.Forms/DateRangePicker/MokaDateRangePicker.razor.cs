using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Moka.Red.Forms.DateRangePicker;

/// <summary>
///     A dual-date picker for selecting a date range (start and end).
///     Displays two side-by-side month calendars in a popup.
///     Pure C# implementation, no JavaScript interop required.
/// </summary>
public partial class MokaDateRangePicker : MokaVisualComponentBase
{
	private readonly string _generatedId = $"moka-daterange-{Guid.NewGuid():N}";
	private DateOnly? _hoverDate;
	private bool _isOpen;
	private DateTime _leftMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
	private bool _selectingEnd;

	// The range the picker shows. StartDate and EndDate only replace it when the parent passes new
	// values, so a re-render that passes the old ones cannot undo a pick (gotcha #9).
	private DateOnly? _start;
	private DateOnly? _end;
	private DateOnly? _lastStartParameter;
	private DateOnly? _lastEndParameter;

	// The input is the control, so it takes the consumer's Id and the unmatched attributes, and the
	// label follows the Id.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

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
		.AddStyle(Style)
		.Build();

	// The root also holds the label and the calendars. The input draws the field's border, so it
	// takes the padding and the radius.
	private string? InputStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private DateTime RightMonth => _leftMonth.AddMonths(1);

	private string DisplayValue
	{
		get
		{
			if (_start.HasValue && _end.HasValue)
			{
				return
					$"{_start.Value.ToString(Format, CultureInfo.InvariantCulture)} - {_end.Value.ToString(Format, CultureInfo.InvariantCulture)}";
			}

			if (_start.HasValue)
			{
				return $"{_start.Value.ToString(Format, CultureInfo.InvariantCulture)} - ...";
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

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (StartDate != _lastStartParameter)
		{
			_lastStartParameter = StartDate;
			_start = StartDate;
		}

		if (EndDate != _lastEndParameter)
		{
			_lastEndParameter = EndDate;
			_end = EndDate;
		}
	}

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
			if (_start.HasValue)
			{
				_leftMonth = new DateTime(_start.Value.Year, _start.Value.Month, 1);
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
		if (_start.HasValue && _end.HasValue)
		{
			return day >= _start.Value && day <= _end.Value;
		}

		if (_start.HasValue && _selectingEnd && _hoverDate.HasValue)
		{
			DateOnly rangeEnd = _hoverDate.Value;
			DateOnly rangeStart = _start.Value;
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
		.AddClass("moka-daterange__day--start", _start.HasValue && day == _start.Value)
		.AddClass("moka-daterange__day--end", _end.HasValue && day == _end.Value)
		.AddClass("moka-daterange__day--in-range", IsInRange(day))
		.AddClass("moka-daterange__day--disabled", IsDayDisabled(day))
		.Build();

	private async Task SelectDay(DateOnly day)
	{
		if (IsDayDisabled(day))
		{
			return;
		}

		if (!_selectingEnd || !_start.HasValue)
		{
			// Selecting start date
			_start = day;
			_end = null;
			_selectingEnd = true;
			await StartDateChanged.InvokeAsync(_start);
			await EndDateChanged.InvokeAsync(_end);
		}
		else
		{
			// Selecting end date
			if (day < _start.Value)
			{
				// Swap: user clicked before start
				_end = _start;
				_start = day;
			}
			else
			{
				_end = day;
			}

			_selectingEnd = false;
			_isOpen = false;
			_hoverDate = null;
			await StartDateChanged.InvokeAsync(_start);
			await EndDateChanged.InvokeAsync(_end);
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
		_start = null;
		_end = null;
		_selectingEnd = false;
		await StartDateChanged.InvokeAsync(null);
		await EndDateChanged.InvokeAsync(null);
	}

	private ElementReference _triggerRef;

	// Escape closes the popup and puts focus back on the field, since the control that had it may
	// have gone with the popup. While open, keys stop at the picker (see the markup), so a MokaDialog
	// around it does not close on the same Escape.
	private async Task HandlePickerKeyDown(KeyboardEventArgs e)
	{
		if (e.Key != "Escape" || !_isOpen)
		{
			return;
		}

		CloseCalendar();
		await FocusTriggerAsync();
	}

	// Down, Alt+Down and Space open the popup from the field, as on a native date input. Enter is
	// left alone: in a text input it submits the surrounding form.
	private void HandleTriggerKeyDown(KeyboardEventArgs e)
	{
		if (!_isOpen && !Disabled && e.Key is "ArrowDown" or " ")
		{
			ToggleCalendar();
		}
	}

	private async Task FocusTriggerAsync()
	{
		try
		{
			await _triggerRef.FocusAsync();
		}
		catch (JSDisconnectedException)
		{
			// Circuit gone.
		}
		catch (InvalidOperationException)
		{
			// Not interactive, or the element is gone.
		}
	}
}
