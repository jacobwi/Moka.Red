using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.CurrencyInput;

/// <summary>
///     Formatted currency input with symbol and decimal handling.
/// </summary>
public partial class MokaCurrencyInput
{
	private readonly string _inputId = $"moka-currency-{Guid.NewGuid():N}";
	private string _displayValue = "";
	private bool _isFocused;

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

	/// <summary>Minimum allowed value.</summary>
	[Parameter]
	public decimal? Min { get; set; }

	/// <summary>Maximum allowed value.</summary>
	[Parameter]
	public decimal? Max { get; set; }


	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-currency";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ComputedCssClass => new CssBuilder("moka-currency-wrapper")
		.AddClass("moka-currency-wrapper--error", HasError)
		.AddClass("moka-currency-wrapper--focused", _isFocused)
		.Build();

	private string InputCssClass => new CssBuilder("moka-currency-input")
		.AddClass($"moka-currency-input--{SizeToKebab(Size)}")
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Placeholder ??= "0.00";
		if (!_isFocused)
		{
			_displayValue = Value.HasValue
				? Value.Value.ToString($"N{DecimalPlaces}", CultureInfo.InvariantCulture)
				: "";
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
			result = Math.Round(parsed, DecimalPlaces);
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
		// Parse and format
		CurrentValueAsString = _displayValue;

		if (Value.HasValue)
		{
			decimal clamped = Value.Value;
			if (Min.HasValue && clamped < Min.Value)
			{
				clamped = Min.Value;
			}

			if (Max.HasValue && clamped > Max.Value)
			{
				clamped = Max.Value;
			}

			if (clamped != Value.Value)
			{
				Value = clamped;
			}

			_displayValue = clamped.ToString($"N{DecimalPlaces}", CultureInfo.InvariantCulture);
		}
		else
		{
			_displayValue = "";
		}

		return Task.CompletedTask;
	}
}
