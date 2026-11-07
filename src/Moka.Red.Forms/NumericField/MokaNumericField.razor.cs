using System.Globalization;
using System.Numerics;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.NumericField;

/// <summary>
///     A numeric input field that supports any numeric type via generic math.
///     Uses text input with numeric inputmode for better mobile UX.
/// </summary>
/// <typeparam name="TValue">A numeric type that implements <see cref="INumber{TSelf}" />.</typeparam>
public partial class MokaNumericField<TValue> where TValue : struct, INumber<TValue>
{
	private readonly string _generatedId = $"moka-numericfield-{Guid.NewGuid():N}";

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

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether the field can be left empty. Shows a quiet word beside the label. <see cref="Required" /> wins.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>Writes the value in the monospace font.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>A unit inside the box at the right, such as "Mbit/s". The input's <c>aria-describedby</c> points at it.</summary>
	[Parameter]
	public string? Suffix { get; set; }

	/// <summary>Icon displayed before the input text.</summary>
	[Parameter]
	public MokaIconDefinition? StartIcon { get; set; }

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
		.AddStyle("--moka-field-suffix-ch", SuffixWidth)
		.Build();

	private string InputCssClass => new CssBuilder("moka-numericfield-input")
		.AddClass($"moka-numericfield-input--{SizeToKebab(Size)}")
		.AddClass("moka-field-input--has-start-icon", StartIcon is not null)
		.AddClass("moka-field-input--has-suffix", HasSuffix)
		.AddClass("moka-field-input--mono", Mono)
		.Build();

	private bool HasSuffix => !string.IsNullOrEmpty(Suffix);

	private string SuffixId => $"{InputId}-suffix";

	private string? SuffixWidth => HasSuffix ? FormattableString.Invariant($"{Suffix!.Length}ch") : null;

	private string? SuffixDescribedBy => HasSuffix ? MokaAttributes.DescribedBy(AdditionalAttributes, SuffixId) : null;

	private IReadOnlyDictionary<string, object>? InputAttributes =>
		HasSuffix ? MokaAttributes.Without(AdditionalAttributes, "aria-describedby") : AdditionalAttributes;

	/// <inheritdoc />
	/// <remarks>
	///     Written in the invariant culture, the one the text is read back in. InputBase would write the
	///     current culture's "1,5" on a German page, and the next edit would read it back as 15.
	/// </remarks>
	protected override string? FormatValueAsString(TValue value) => value.ToString(null, CultureInfo.InvariantCulture);

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

	private void HandleInput(ChangeEventArgs e) => HandleInputWithDebounce(e.Value?.ToString());
}
