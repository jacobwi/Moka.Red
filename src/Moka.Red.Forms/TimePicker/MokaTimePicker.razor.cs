using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.TimePicker;

/// <summary>
///     A time input component with hour/minute selector columns.
///     Pure C# implementation — no JavaScript interop required.
/// </summary>
public partial class MokaTimePicker
{
	private readonly string _inputId = $"moka-timepicker-{Guid.NewGuid():N}";
	private bool _isOpen;
	private bool _isPm;
	private int _selectedHour;
	private int _selectedMinute;

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

	/// <summary>Placeholder text displayed when no time is selected.</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Select time...";

	/// <summary>Whether to use 24-hour format. Default is true.</summary>
	[Parameter]
	public bool Format24 { get; set; } = true;

	/// <summary>Step for minute selection (1, 5, 15, 30). Default is 1.</summary>
	[Parameter]
	public int MinuteStep { get; set; } = 1;

	/// <summary>Minimum selectable time.</summary>
	[Parameter]
	public TimeSpan? Min { get; set; }

	/// <summary>Maximum selectable time.</summary>
	[Parameter]
	public TimeSpan? Max { get; set; }

	/// <summary>Whether to show the Clear button. Default is true.</summary>
	[Parameter]
	public bool ShowClearButton { get; set; } = true;

	/// <summary>Whether the input is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-timepicker";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string InputCssClass => new CssBuilder("moka-timepicker-input")
		.AddClass($"moka-timepicker-input--{SizeToKebab(Size)}")
		.Build();

	private string DisplayValue
	{
		get
		{
			if (!CurrentValue.HasValue)
			{
				return "";
			}

			TimeSpan ts = CurrentValue.Value;
			if (Format24)
			{
				return $"{ts.Hours:D2}:{ts.Minutes:D2}";
			}

			int hour12 = ts.Hours % 12;
			if (hour12 == 0)
			{
				hour12 = 12;
			}

			string amPm = ts.Hours >= 12 ? "PM" : "AM";
			return $"{hour12:D2}:{ts.Minutes:D2} {amPm}";
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private IEnumerable<int> GetHours()
	{
		if (Format24)
		{
			for (int i = 0; i < 24; i++)
			{
				yield return i;
			}
		}
		else
		{
			for (int i = 1; i <= 12; i++)
			{
				yield return i;
			}
		}
	}

	private IEnumerable<int> GetMinutes()
	{
		int step = Math.Max(1, MinuteStep);
		for (int i = 0; i < 60; i += step)
		{
			yield return i;
		}
	}

	private int GetEffectiveHour()
	{
		if (Format24)
		{
			return _selectedHour;
		}

		int h = _selectedHour % 12;
		if (_isPm)
		{
			h += 12;
		}

		return h;
	}

	private bool IsTimeDisabled(int hour24, int minute)
	{
		var ts = new TimeSpan(hour24, minute, 0);
		return (Min.HasValue && ts < Min.Value) || (Max.HasValue && ts > Max.Value);
	}

	private string HourCssClass(int hour) => new CssBuilder("moka-timepicker-item")
		.AddClass("moka-timepicker-item--selected", IsHourSelected(hour))
		.Build();

	private string MinuteCssClass(int minute) => new CssBuilder("moka-timepicker-item")
		.AddClass("moka-timepicker-item--selected", _selectedMinute == minute && CurrentValue.HasValue)
		.Build();

	private string AmPmCssClass(bool isPm) => new CssBuilder("moka-timepicker-item")
		.AddClass("moka-timepicker-item--selected", _isPm == isPm && CurrentValue.HasValue)
		.Build();

	private bool IsHourSelected(int hour)
	{
		if (!CurrentValue.HasValue)
		{
			return false;
		}

		if (Format24)
		{
			return _selectedHour == hour;
		}

		return _selectedHour == hour;
	}

	private void ToggleDropdown()
	{
		if (ReadOnly || Disabled)
		{
			return;
		}

		_isOpen = !_isOpen;
		if (_isOpen && CurrentValue.HasValue)
		{
			TimeSpan ts = CurrentValue.Value;
			if (Format24)
			{
				_selectedHour = ts.Hours;
			}
			else
			{
				_isPm = ts.Hours >= 12;
				_selectedHour = ts.Hours % 12;
				if (_selectedHour == 0)
				{
					_selectedHour = 12;
				}
			}

			_selectedMinute = ts.Minutes;
		}
		else if (_isOpen)
		{
			TimeSpan now = DateTime.Now.TimeOfDay;
			if (Format24)
			{
				_selectedHour = now.Hours;
			}
			else
			{
				_isPm = now.Hours >= 12;
				_selectedHour = now.Hours % 12;
				if (_selectedHour == 0)
				{
					_selectedHour = 12;
				}
			}

			_selectedMinute = now.Minutes;
		}
	}

	private void CloseDropdown() => _isOpen = false;

	private async Task SelectHour(int hour)
	{
		_selectedHour = hour;
		await UpdateValue();
	}

	private async Task SelectMinute(int minute)
	{
		_selectedMinute = minute;
		await UpdateValue();
	}

	private async Task SelectAmPm(bool isPm)
	{
		_isPm = isPm;
		await UpdateValue();
	}

	private async Task UpdateValue()
	{
		int hour24 = GetEffectiveHour();
		var ts = new TimeSpan(hour24, _selectedMinute, 0);
		if (!IsTimeDisabled(hour24, _selectedMinute))
		{
			CurrentValue = ts;
			await ValueChanged.InvokeAsync(CurrentValue);
		}
	}

	private async Task SelectNow()
	{
		TimeSpan now = DateTime.Now.TimeOfDay;
		CurrentValue = new TimeSpan(now.Hours, now.Minutes, 0);
		_isOpen = false;
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	private async Task ClearTime()
	{
		CurrentValue = null;
		await ValueChanged.InvokeAsync(CurrentValue);
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TimeSpan? result,
		out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = null;
			validationErrorMessage = "";
			return true;
		}

		if (TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan ts))
		{
			result = ts;
			validationErrorMessage = "";
			return true;
		}

		if (DateTime.TryParseExact(value, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None,
			    out DateTime dt))
		{
			result = dt.TimeOfDay;
			validationErrorMessage = "";
			return true;
		}

		result = null;
		validationErrorMessage = $"'{value}' is not a valid time.";
		return false;
	}
}
