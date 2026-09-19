using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.PhoneInput;

/// <summary>
///     Phone number input with country code prefix and auto-formatting.
/// </summary>
public partial class MokaPhoneInput
{
	private readonly string _generatedId = $"moka-phone-{Guid.NewGuid():N}";
	private string _displayValue = "";
	private bool _isFocused;

	// Id goes on the input, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Dialing code prefix. Default "+1".</summary>
	[Parameter]
	public string CountryCode { get; set; } = "+1";


	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether to auto-format as (XXX) XXX-XXXX for US numbers. Default true.</summary>
	[Parameter]
	public bool AutoFormat { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-phone";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-phone-wrapper")
		.AddClass("moka-phone-wrapper--error", HasError)
		.AddClass("moka-phone-wrapper--focused", _isFocused)
		.AddClass(CssClass) // InputBase's field classes: modified, valid, invalid
		.AddClass(Class)
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

	private string InputCssClass => new CssBuilder("moka-phone-input")
		.AddClass($"moka-phone-input--{SizeToKebab(Size)}")
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Placeholder ??= "(555) 123-4567";
		if (!_isFocused && !string.IsNullOrEmpty(CurrentValueAsString))
		{
			string digits = StripNonDigits(CurrentValueAsString);
			// Remove country code prefix if present
			string codeDigits = StripNonDigits(CountryCode);
			if (digits.StartsWith(codeDigits, StringComparison.Ordinal))
			{
				digits = digits[codeDigits.Length..];
			}

			_displayValue = AutoFormat ? FormatUS(digits) : digits;
		}
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = "";
			validationErrorMessage = "";
			return true;
		}

		string digits = StripNonDigits(value);
		string codeDigits = StripNonDigits(CountryCode);
		result = digits.Length > 0 ? $"{CountryCode}{digits}" : "";
		validationErrorMessage = "";
		return true;
	}

	private static string StripNonDigits(string? input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "";
		}

		char[] chars = new char[input.Length];
		int index = 0;
		foreach (char c in input)
		{
			if (char.IsDigit(c))
			{
				chars[index++] = c;
			}
		}

		return new string(chars, 0, index);
	}

	private static string FormatUS(string digits)
	{
		if (digits.Length == 0)
		{
			return "";
		}

		if (digits.Length <= 3)
		{
			return $"({digits}";
		}

		if (digits.Length <= 6)
		{
			return $"({digits[..3]}) {digits[3..]}";
		}

		string limited = digits.Length > 10 ? digits[..10] : digits;
		return $"({limited[..3]}) {limited[3..6]}-{limited[6..]}";
	}

	private Task HandleInput(ChangeEventArgs e)
	{
		string raw = e.Value?.ToString() ?? "";
		string digits = StripNonDigits(raw);
		if (digits.Length > 10)
		{
			digits = digits[..10];
		}

		_displayValue = AutoFormat ? FormatUS(digits) : digits;
		return Task.CompletedTask;
	}

	private Task HandleFocus()
	{
		_isFocused = true;
		return Task.CompletedTask;
	}

	private Task HandleBlur()
	{
		_isFocused = false;
		// Commit value
		string digits = StripNonDigits(_displayValue);
		CurrentValueAsString = digits;
		if (AutoFormat)
		{
			_displayValue = FormatUS(digits);
		}

		return Task.CompletedTask;
	}
}
