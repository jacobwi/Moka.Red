using System.Globalization;
using System.Numerics;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.NumericField;

/// <summary>
///     A numeric input field that supports any numeric type via generic math.
///     Uses text input with numeric inputmode for better mobile UX.
/// </summary>
/// <typeparam name="TValue">A numeric type that implements <see cref="INumber{TSelf}" />.</typeparam>
public partial class MokaNumericField<TValue> where TValue : struct, INumber<TValue>
{
	private readonly string _inputId = $"moka-numericfield-{Guid.NewGuid():N}";

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

	/// <summary>Minimum allowed value.</summary>
	[Parameter]
	public TValue? Min { get; set; }

	/// <summary>Maximum allowed value.</summary>
	[Parameter]
	public TValue? Max { get; set; }

	/// <summary>Increment step value.</summary>
	[Parameter]
	public TValue? Step { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-numericfield";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-numericfield--error", HasError)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private string? ComputedStyle => Style;

	private string InputCssClass => new CssBuilder("moka-numericfield-input")
		.AddClass($"moka-numericfield-input--{SizeToKebab(Size)}")
		.Build();

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = default;
			validationErrorMessage = string.Empty;
			return true;
		}

		if (TValue.TryParse(value, CultureInfo.InvariantCulture, out TValue parsed))
		{
			result = parsed;
			validationErrorMessage = string.Empty;
			return true;
		}

		result = default;
		validationErrorMessage = $"'{value}' is not a valid number.";
		return false;
	}

	private Task HandleInput(ChangeEventArgs e)
	{
		CurrentValueAsString = e.Value?.ToString();
		return Task.CompletedTask;
	}
}
