using System.Collections.Frozen;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;
using Moka.Red.Primitives.Flag;

namespace Moka.Red.Forms.PhoneInput;

/// <summary>
///     Phone number input with country code prefix and auto-formatting.
/// </summary>
public partial class MokaPhoneInput
{
	// Numbers there carry their leading 0 after the dialing code, so it is no trunk prefix to drop.
	private static readonly FrozenSet<string> KeepsLeadingZero =
		new[] { "IT", "SM", "VA", "CI", "CG", "BJ", "GA" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	private readonly string _generatedId = $"moka-phone-{Guid.NewGuid():N}";
	private string _displayValue = "";
	private bool _isFocused;
	private string? _country;
	private string? _lastCountry;
	private string? _committed;

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

	/// <summary>Dialing code prefix. Default "+1".</summary>
	[Parameter]
	public string CountryCode { get; set; } = "+1";


	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether to auto-format as (XXX) XXX-XXXX for US numbers. Default true.</summary>
	[Parameter]
	public bool AutoFormat { get; set; } = true;

	/// <summary>
	///     Turns the fixed prefix into a country picker: a button with the flag and the dialing code that opens a
	///     searchable list of countries (by name, code or dialing code, so "44" finds the United Kingdom). The value
	///     stays one string, the country's dialing code and the number, such as "+46701234567". Numbers in the North
	///     American plan (+1) are formatted as (555) 123-4567 when <see cref="AutoFormat" /> is on; others keep the
	///     grouping typed. Null keeps the fixed <see cref="CountryCode" />.
	/// </summary>
	[Parameter]
	public string? Country { get; set; }

	/// <summary>Raised with the new code when the person picks another country.</summary>
	[Parameter]
	public EventCallback<string?> CountryChanged { get; set; }

	/// <summary>Limits the picker to these country codes. Null lists every country.</summary>
	[Parameter]
	public IReadOnlyList<string>? Countries { get; set; }

	/// <summary>Country codes listed first in the picker, after the browser's own country.</summary>
	[Parameter]
	public IReadOnlyList<string>? SuggestedCountries { get; set; }

	/// <summary>The picker button's name and tooltip. Null is "Country code".</summary>
	[Parameter]
	public string? CountryLabel { get; set; }

	/// <summary>
	///     With <see cref="Country" /> set, whether a 0 typed before the number is taken as the trunk prefix and left
	///     out of the value, so 070 123 45 67 in Sweden is saved as +46701234567. Countries whose numbers keep the 0
	///     after the dialing code (Italy, San Marino, Vatican City, Côte d’Ivoire, the Republic of the Congo, Benin
	///     and Gabon) keep it either way. Default true.
	/// </summary>
	[Parameter]
	public bool DropTrunkZero { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-phone";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-phone-wrapper")
		.AddClass("moka-phone-wrapper--picker", UsesPicker)
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

	private bool UsesPicker => _country is not null;

	// The dialing code the number goes after: the picked country's, else the fixed one.
	private string DialingCode => UsesPicker && MokaCountries.Find(_country)?.DialingCode is { } dial ? dial : CountryCode;

	private bool NorthAmerican => !UsesPicker || DialingCode == "+1";

	private string? ShownPlaceholder => Placeholder ?? (NorthAmerican ? "(555) 123-4567" : null);

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!string.Equals(Country, _lastCountry, StringComparison.Ordinal))
		{
			_lastCountry = Country;
			_country = Country;
		}

		if (UsesPicker)
		{
			// Only a value from outside replaces what's on screen, so the grouping typed survives a commit.
			if (!_isFocused && !string.Equals(CurrentValueAsString, _committed, StringComparison.Ordinal))
			{
				_committed = CurrentValueAsString;
				string national = NationalDigits(CurrentValueAsString);
				_displayValue = NorthAmerican && AutoFormat ? FormatUS(national) : national;
			}

			return;
		}

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
		if (UsesPicker)
		{
			if (DropTrunkZero && digits.StartsWith('0') && !KeepsLeadingZero.Contains(_country!))
			{
				digits = digits[1..];
			}

			result = digits.Length > 0 ? $"{DialingCode}{digits}" : "";
			validationErrorMessage = "";
			return true;
		}

		string codeDigits = StripNonDigits(CountryCode);
		result = digits.Length > 0 ? $"{CountryCode}{digits}" : "";
		validationErrorMessage = "";
		return true;
	}

	// The number without the picked country's dialing code in front.
	private string NationalDigits(string? value)
	{
		string digits = StripNonDigits(value);
		string codeDigits = StripNonDigits(DialingCode);
		return codeDigits.Length > 0 && digits.StartsWith(codeDigits, StringComparison.Ordinal) ? digits[codeDigits.Length..] : digits;
	}

	// Outside the North American plan the grouping typed stays, up to E.164's 15 digits with the dialing code.
	private string KeepTyped(string typed)
	{
		int room = 15 - StripNonDigits(DialingCode).Length;
		StringBuilder kept = new(typed.Length);
		int digits = 0;
		foreach (char c in typed)
		{
			if (char.IsAsciiDigit(c))
			{
				if (digits++ < room)
				{
					kept.Append(c);
				}
			}
			else if (c is ' ' or '-' or '(' or ')' or '.' && kept.Length > 0)
			{
				kept.Append(c);
			}
		}

		return kept.ToString();
	}

	private async Task HandleCountryChanged(string? code)
	{
		if (code is null || string.Equals(code, _country, StringComparison.Ordinal))
		{
			return;
		}

		_country = code;
		string digits = StripNonDigits(_displayValue);
		if (NorthAmerican && AutoFormat)
		{
			_displayValue = FormatUS(digits.Length > 10 ? digits[..10] : digits);
		}

		if (digits.Length > 0)
		{
			CurrentValueAsString = _displayValue;
			_committed = CurrentValueAsString;
		}

		await CountryChanged.InvokeAsync(code);
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

	private Task HandleInput(string? typed)
	{
		string raw = typed ?? "";
		if (!NorthAmerican)
		{
			_displayValue = KeepTyped(raw);
			return Task.CompletedTask;
		}

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
		CurrentValueAsString = UsesPicker ? _displayValue : digits;
		if (UsesPicker)
		{
			_committed = CurrentValueAsString;
		}

		if (AutoFormat && NorthAmerican)
		{
			_displayValue = FormatUS(digits);
		}

		return Task.CompletedTask;
	}
	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes,
		MokaFieldWrapper.HelperIdIfShown(InputId, HelperText, ResolvedErrorText));

	private IReadOnlyDictionary<string, object>? ControlAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);
}
