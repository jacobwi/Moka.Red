using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.DatePicker;

/// <summary>
///     A date input component with an inline calendar dropdown.
///     Pure C# implementation — no JavaScript interop required.
/// </summary>
public partial class MokaDatePicker
{
	private readonly string _inputId = $"moka-datepicker-{Guid.NewGuid():N}";
	private DateTime _displayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
	private bool _isOpen;

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Placeholder text displayed when no date is selected.</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Select date...";

	/// <summary>Display format for the date. Default is "yyyy-MM-dd".</summary>
	[Parameter]
	public string Format { get; set; } = "yyyy-MM-dd";

	/// <summary>Minimum selectable date.</summary>
	[Parameter]
	public DateTime? Min { get; set; }

	/// <summary>Maximum selectable date.</summary>
	[Parameter]
	public DateTime? Max { get; set; }

	/// <summary>First day of the week. Default is Monday.</summary>
	[Parameter]
	public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;

	/// <summary>Whether to show the Today button. Default is true.</summary>
	[Parameter]
	public bool ShowTodayButton { get; set; } = true;

	/// <summary>Whether to show the Clear button. Default is true.</summary>
	[Parameter]
	public bool ShowClearButton { get; set; } = true;

	/// <summary>Whether the input is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-datepicker";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string InputCssClass => new CssBuilder("moka-datepicker-input")
		.AddClass($"moka-datepicker-input--{SizeToKebab(Size)}")
		.Build();

	private string DisplayValue => CurrentValue?.ToString(Format, CultureInfo.InvariantCulture) ?? "";

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

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private IEnumerable<DateTime> GetCalendarDays()
	{
		var firstOfMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
		int firstDayOffset = ((int)firstOfMonth.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;
		DateTime startDate = firstOfMonth.AddDays(-firstDayOffset);

		for (int i = 0; i < 42; i++)
		{
			yield return startDate.AddDays(i);
		}
	}

	private bool IsDayDisabled(DateTime day) =>
		(Min.HasValue && day.Date < Min.Value.Date) ||
		(Max.HasValue && day.Date > Max.Value.Date);

	private string DayCssClass(DateTime day) => new CssBuilder("moka-datepicker-day")
		.AddClass("moka-datepicker-day--other-month", day.Month != _displayMonth.Month)
		.AddClass("moka-datepicker-day--today", day.Date == DateTime.Today)
		.AddClass("moka-datepicker-day--selected", CurrentValue.HasValue && day.Date == CurrentValue.Value.Date)
		.AddClass("moka-datepicker-day--disabled", IsDayDisabled(day))
		.Build();

	private async Task SelectDate(DateTime day)
	{
		if (IsDayDisabled(day))
		{
			return;
		}

		CurrentValue = day.Date;
		_isOpen = false;
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	private void ToggleCalendar()
	{
		if (ReadOnly || Disabled)
		{
			return;
		}

		_isOpen = !_isOpen;
		if (_isOpen && CurrentValue.HasValue)
		{
			_displayMonth = new DateTime(CurrentValue.Value.Year, CurrentValue.Value.Month, 1);
		}
		else if (_isOpen)
		{
			_displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
		}
	}

	private void CloseCalendar() => _isOpen = false;

	private void PrevMonth() => _displayMonth = _displayMonth.AddMonths(-1);

	private void NextMonth() => _displayMonth = _displayMonth.AddMonths(1);

	private async Task SelectToday()
	{
		CurrentValue = DateTime.Today;
		_isOpen = false;
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	private async Task ClearDate()
	{
		CurrentValue = null;
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	private static void HandleFocusOut(FocusEventArgs e)
	{
		// Focus out is handled by the backdrop click
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out DateTime? result,
		out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = null;
			validationErrorMessage = "";
			return true;
		}

		if (DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
		{
			result = date;
			validationErrorMessage = "";
			return true;
		}

		result = null;
		validationErrorMessage = $"'{value}' is not a valid date.";
		return false;
	}
}
