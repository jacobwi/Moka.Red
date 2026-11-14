using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Moka.Red.Forms.TimePicker;

/// <summary>
///     A time input component with hour/minute selector columns.
/// </summary>
public partial class MokaTimePicker
{
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";
	private readonly string _generatedId = $"moka-timepicker-{Guid.NewGuid():N}";
	private const string DefaultPlaceholder = "Select time...";

	private ElementReference _backdrop;
	private bool _isOpen;
	private ElementReference _panel;
	private bool _panelShown;
	private ElementReference _root;
	private bool _isPm;
	private int _selectedHour;
	private int _selectedMinute;
	private string? _typedText;
	private string? _typedError;
	private TimeSpan? _valueWhenRejected;

	// Id goes on the input, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>
	///     Content beside the field's box, in its row, such as a button that acts on the value. The label stays above
	///     and the helper text below, and on the new design a button here is as tall as the box.
	/// </summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Placeholder text displayed when no time is selected.</summary>
	[Parameter]
	public string Placeholder { get; set; } = DefaultPlaceholder;

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

	/// <summary>
	///     Whether the time can be typed as well as picked. The input stops being read-only and reads what it's given
	///     when it loses focus or on Enter: 21:30, 9:30 pm, 9.30p, 21h30, 2130, 930pm, 9 pm or 21. A time it reads is
	///     shown back in the field's own format. On a 12-hour field a time with no AM or PM keeps the half the field is
	///     in. Text it can't read, or a time outside <see cref="Min" /> and <see cref="Max" />, keeps the last good value
	///     and shows an error. Emptying the field clears the value.
	/// </summary>
	[Parameter]
	public bool Typed { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-timepicker";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || _typedError is not null || HasValidationError;

	/// <summary>
	///     Explicit <see cref="ErrorText" /> when set, then what was wrong with the typed text, otherwise the EditContext
	///     validation message.
	/// </summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : _typedError ?? ValidationErrorText;

	private string InputCssClass => new CssBuilder("moka-timepicker-input")
		.AddClass($"moka-timepicker-input--{SizeToKebab(Size)}")
		.AddClass("moka-timepicker-input--typed", Typed)
		.AddClass("moka-timepicker-input--invalid", Typed && HasError)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The input draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? InputStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string DisplayValue => CurrentValue.HasValue ? TimeText(CurrentValue.Value) : "";

	private string ShownText => Typed ? _typedText ?? DisplayValue : DisplayValue;

	private string TypedExample => Format24 ? "21:30" : "9:30 PM";

	private string ShownPlaceholder => Typed && Placeholder == DefaultPlaceholder ? TypedExample : Placeholder;

	// A click only opens the popup while the field can't be typed in. Once it can, a click puts the caret there.
	private EventCallback FieldClick => Typed ? default : EventCallback.Factory.Create(this, ToggleDropdown);

	private EventCallback<ChangeEventArgs> TypedChange => Typed
		? EventCallback.Factory.Create<ChangeEventArgs>(this, CommitTypedAsync)
		: default;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The columns go to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
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

		await base.DisposeAsyncCore();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// A new value from the parent replaces the text that was turned down.
		if (!Typed || CurrentValue != _valueWhenRejected)
		{
			DropRejectedText();
		}
	}

	private string TimeText(TimeSpan ts)
	{
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
		if (_isOpen)
		{
			ShowInColumns(CurrentValue ?? DateTime.Now.TimeOfDay);
		}
	}

	private void ShowInColumns(TimeSpan time)
	{
		if (Format24)
		{
			_selectedHour = time.Hours;
		}
		else
		{
			_isPm = time.Hours >= 12;
			_selectedHour = time.Hours % 12;
			if (_selectedHour == 0)
			{
				_selectedHour = 12;
			}
		}

		_selectedMinute = time.Minutes;
	}

	private void CloseDropdown() => _isOpen = false;

	private void SelectHour(int hour)
	{
		_selectedHour = hour;
		UpdateValue();
	}

	private void SelectMinute(int minute)
	{
		_selectedMinute = minute;
		UpdateValue();
	}

	private void SelectAmPm(bool isPm)
	{
		_isPm = isPm;
		UpdateValue();
	}

	private void UpdateValue()
	{
		DropRejectedText();
		int hour24 = GetEffectiveHour();
		var ts = new TimeSpan(hour24, _selectedMinute, 0);
		if (!IsTimeDisabled(hour24, _selectedMinute))
		{
			CurrentValue = ts;
		}
	}

	private void SelectNow()
	{
		DropRejectedText();
		TimeSpan now = DateTime.Now.TimeOfDay;
		CurrentValue = new TimeSpan(now.Hours, now.Minutes, 0);
		_isOpen = false;
	}

	private void ClearTime()
	{
		DropRejectedText();
		CurrentValue = null;
	}

	private async Task CommitTypedAsync(ChangeEventArgs e)
	{
		string text = e.Value?.ToString() ?? string.Empty;
		_typedError = TryParseValueFromString(text, out _, out string error) ? null : error;
		_valueWhenRejected = CurrentValue;
		_typedText = text;
		CurrentValueAsString = text;
		if (_typedError is not null)
		{
			return;
		}

		if (_isOpen && CurrentValue is { } time)
		{
			ShowInColumns(time);
		}

		// The last render may already hold the time this text reads as, such as "2130" for 21:30. Blazor would see
		// nothing to change and leave "2130" on screen, so one render with the text gives the next a value to replace.
		StateHasChanged();
		await Task.Yield();
		if (_typedError is null && string.Equals(_typedText, text, StringComparison.Ordinal))
		{
			_typedText = null;
		}
	}

	private void DropRejectedText()
	{
		if (_typedError is null)
		{
			return;
		}

		_typedError = null;
		_typedText = null;

		// Only this setter takes the rejected text's message back out of a form.
		CurrentValueAsString = CurrentValue is { } time ? TimeText(time) : string.Empty;
	}

	private bool TryReadTyped(string text, out TimeSpan? result, out string validationErrorMessage)
	{
		bool? afternoon = CurrentValue is { } shown ? shown.Hours >= 12 : null;
		result = MokaTimeParser.Parse(text, Format24, afternoon);
		validationErrorMessage = "";
		if (result is not { } time)
		{
			validationErrorMessage = $"Write a time like {TypedExample}.";
			return false;
		}

		if (time != CurrentValue && IsTimeDisabled(time.Hours, time.Minutes))
		{
			result = null;
			validationErrorMessage = OutOfRangeMessage;
			return false;
		}

		return true;
	}

	private string OutOfRangeMessage => (Min, Max) switch
	{
		({ } min, { } max) => $"From {TimeText(min)} to {TimeText(max)}.",
		({ } min, null) => $"At least {TimeText(min)}.",
		(null, { } max) => $"At most {TimeText(max)}.",
		_ => ""
	};

	// The post carries the value in the hidden input, not this input's formatted text, so a name
	// the consumer set goes there alone.
	private IReadOnlyDictionary<string, object>? DisplayAttributes =>
		MokaAttributes.WithoutDescribedBy(MokaAttributes.Without(AdditionalAttributes, "name"));

	/// <inheritdoc />
	protected override string? PostedValue => CurrentValue?.ToString("c", CultureInfo.InvariantCulture);

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

		if (Typed)
		{
			return TryReadTyped(value, out result, out validationErrorMessage);
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

		CloseDropdown();
		await FocusTriggerAsync();
	}

	// Down, Alt+Down and Space open the popup from the field, as on a native date input. Enter is
	// left alone: in a text input it submits the surrounding form. A field that takes typing keeps
	// Space, since "9:30 pm" has one in it.
	private void HandleTriggerKeyDown(KeyboardEventArgs e)
	{
		if (!_isOpen && !Disabled && (e.Key == "ArrowDown" || (e.Key == " " && !Typed)))
		{
			ToggleDropdown();
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
	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		MokaFieldWrapper.HelperIdIfShown(InputId, HelperText, ResolvedErrorText));
}
