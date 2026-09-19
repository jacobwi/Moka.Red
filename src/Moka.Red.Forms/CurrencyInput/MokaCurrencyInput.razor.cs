using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.CurrencyInput;

/// <summary>
///     Formatted currency input with symbol and decimal handling.
/// </summary>
public partial class MokaCurrencyInput
{
	private readonly string _generatedId = $"moka-currency-{Guid.NewGuid():N}";
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

	/// <summary>Currency symbol displayed as prefix. Default "$".</summary>
	[Parameter]
	public string CurrencySymbol { get; set; } = "$";

	/// <summary>ISO currency code shown as suffix (e.g., "USD", "EUR").</summary>
	[Parameter]
	public string? CurrencyCode { get; set; }

	/// <summary>Number of decimal places. Default 2.</summary>
	[Parameter]
	public int DecimalPlaces { get; set; } = 2;

	/// <summary>Minimum allowed value. A smaller amount is raised to it when the field loses focus.</summary>
	[Parameter]
	public decimal? Min { get; set; }

	/// <summary>Maximum allowed value. A larger amount is lowered to it when the field loses focus.</summary>
	[Parameter]
	public decimal? Max { get; set; }


	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-currency";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-currency-wrapper")
		.AddClass("moka-currency-wrapper--error", HasError)
		.AddClass("moka-currency-wrapper--focused", _isFocused)
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

	/// <summary>Placeholder to render: the caller's value, or the currency default.</summary>
	private string ResolvedPlaceholder => string.IsNullOrEmpty(Placeholder) ? "0.00" : Placeholder;

	private string InputCssClass => new CssBuilder("moka-currency-input")
		.AddClass($"moka-currency-input--{SizeToKebab(Size)}")
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (!_isFocused)
		{
			_displayValue = FormatAmount(Value);
		}
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out decimal? result,
		out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = null;
			validationErrorMessage = "";
			return true;
		}

		// Strip currency symbol, commas, whitespace
		string cleaned = value
			.Replace(CurrencySymbol, "", StringComparison.Ordinal)
			.Replace(",", "", StringComparison.Ordinal)
			.Trim();

		if (decimal.TryParse(cleaned, CultureInfo.InvariantCulture, out decimal parsed))
		{
			// Clamped while parsing, so the base reports the amount once, already in range.
			result = Clamp(Math.Round(parsed, DecimalPlaces));
			validationErrorMessage = "";
			return true;
		}

		result = null;
		validationErrorMessage = $"'{value}' is not a valid currency amount.";
		return false;
	}

	private Task HandleInput(ChangeEventArgs e)
	{
		string raw = e.Value?.ToString() ?? "";
		// Strip non-numeric except decimal point and minus
		string cleaned = new(raw.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
		_displayValue = cleaned;
		return Task.CompletedTask;
	}

	private Task HandleFocus()
	{
		_isFocused = true;
		// Show raw number for editing
		if (Value.HasValue)
		{
			_displayValue = Value.Value.ToString(CultureInfo.InvariantCulture);
		}

		return Task.CompletedTask;
	}

	private Task HandleBlur()
	{
		_isFocused = false;

		// The base parses (and clamps) through TryParseValueFromString, then sets CurrentValue, which
		// raises ValueChanged and tells the EditContext. Writing Value here instead, as the clamp used
		// to, told nobody: the parent kept the unclamped amount and its next render put it back.
		CurrentValueAsString = _displayValue;
		_displayValue = FormatAmount(CurrentValue);

		return Task.CompletedTask;
	}

	private decimal Clamp(decimal amount)
	{
		if (Min.HasValue && amount < Min.Value)
		{
			amount = Min.Value;
		}

		if (Max.HasValue && amount > Max.Value)
		{
			amount = Max.Value;
		}

		return amount;
	}

	private string FormatAmount(decimal? amount) =>
		amount.HasValue ? amount.Value.ToString($"N{DecimalPlaces}", CultureInfo.InvariantCulture) : "";
}
