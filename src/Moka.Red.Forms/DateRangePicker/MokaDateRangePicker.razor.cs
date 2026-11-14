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
/// </summary>
public partial class MokaDateRangePicker : MokaVisualComponentBase
{
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	private readonly string _generatedId = $"moka-daterange-{Guid.NewGuid():N}";
	private ElementReference _backdrop;
	private DateOnly? _hoverDate;
	private bool _isOpen;
	private DateTime _leftMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
	private ElementReference _panel;
	private bool _panelShown;
	private ElementReference _root;
	private bool _selectingEnd;

	// The range the picker shows, apart from StartDate and EndDate, each owned by a parent that passes it and listens:
	// a pick shows before the parent answers, and a re-render that passes the old dates cannot undo it (gotcha #9).
	private readonly MokaInputValue<DateOnly?> _startDate;
	private readonly MokaInputValue<DateOnly?> _endDate;

	// The input is the control, so it takes the consumer's Id and the unmatched attributes, and the
	// label follows the Id.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Creates the picker. It shows its own range until the parent passes one.</summary>
	public MokaDateRangePicker()
	{
		_startDate = new MokaInputValue<DateOnly?>(StateHasChanged, DispatchExceptionAsync);
		_endDate = new MokaInputValue<DateOnly?>(StateHasChanged, DispatchExceptionAsync);
	}

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

	private DateOnly? ShownStart => _startDate.Shown;

	private DateOnly? ShownEnd => _endDate.Shown;

	private string DisplayValue
	{
		get
		{
			if (ShownStart.HasValue && ShownEnd.HasValue)
			{
				return
					$"{ShownStart.Value.ToString(Format, CultureInfo.InvariantCulture)} - {ShownEnd.Value.ToString(Format, CultureInfo.InvariantCulture)}";
			}

			if (ShownStart.HasValue)
			{
				return $"{ShownStart.Value.ToString(Format, CultureInfo.InvariantCulture)} - ...";
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
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The calendars go to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		if (_panelShown != _isOpen)
		{
			_panelShown = _isOpen;
			if (_isOpen)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _generatedId, _panel, _root, _backdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-start" });
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_panelShown)
		{
			_panelShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _generatedId);
		}

		_startDate.Stop();
		_endDate.Stop();
		await base.DisposeAsyncCore();
	}

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_startDate.Take(parameters, nameof(StartDate));
		_endDate.Take(parameters, nameof(EndDate));
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_startDate.AfterRender();
		_endDate.AfterRender();
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
			if (ShownStart.HasValue)
			{
				_leftMonth = new DateTime(ShownStart.Value.Year, ShownStart.Value.Month, 1);
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
		if (ShownStart.HasValue && ShownEnd.HasValue)
		{
			return day >= ShownStart.Value && day <= ShownEnd.Value;
		}

		if (ShownStart.HasValue && _selectingEnd && _hoverDate.HasValue)
		{
			DateOnly rangeEnd = _hoverDate.Value;
			DateOnly rangeStart = ShownStart.Value;
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
		.AddClass("moka-daterange__day--start", ShownStart.HasValue && day == ShownStart.Value)
		.AddClass("moka-daterange__day--end", ShownEnd.HasValue && day == ShownEnd.Value)
		.AddClass("moka-daterange__day--in-range", IsInRange(day))
		.AddClass("moka-daterange__day--disabled", IsDayDisabled(day))
		.Build();

	private void SelectDay(DateOnly day)
	{
		if (IsDayDisabled(day))
		{
			return;
		}

		if (!_selectingEnd || ShownStart is not { } start)
		{
			_selectingEnd = true;
			Commit(day, null);
			return;
		}

		// A day before the start becomes the start.
		_selectingEnd = false;
		_isOpen = false;
		_hoverDate = null;
		Commit(day < start ? day : start, day < start ? start : day);
	}

	// Only an end that moved is reported, the start first.
	private void Commit(DateOnly? start, DateOnly? end)
	{
		if (start != ShownStart)
		{
			_startDate.Pick(start, StartDateChanged);
		}

		if (end != ShownEnd)
		{
			_endDate.Pick(end, EndDateChanged);
		}
	}

	// Hover previews the range, so it only matters between picking the start and the end. A default
	// EventCallback renders no listener, and the rest of the time every day would be a round trip for nothing.
	private EventCallback DayEnterHandler(DateOnly day) => _selectingEnd
		? EventCallback.Factory.Create(this, () => _hoverDate = day)
		: default;

	private void ClearRange()
	{
		_selectingEnd = false;
		Commit(null, null);
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
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (InvalidOperationException)
		{
			// Not interactive, or the element is gone.
		}
	}
}
