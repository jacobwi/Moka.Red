using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.ColorInput;

/// <summary>
///     A simple color input field with a color swatch preview and optional native color picker.
///     Lighter than <c>MokaColorPicker</c> - just a text input for hex values with a preview swatch.
/// </summary>
public partial class MokaColorInput
{
	private readonly string _generatedId = $"moka-colorinput-{Guid.NewGuid():N}";

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

	/// <summary>Whether to show a color swatch preview next to the input. Defaults to true.</summary>
	[Parameter]
	public bool ShowPreview { get; set; } = true;

	/// <summary>Whether to show the browser's native color picker button. Defaults to true.</summary>
	[Parameter]
	public bool ShowNativeInput { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-color-input";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass { get; set; } = "";

	// The field wrapper is the outermost element, so the margin goes there. This component's own
	// element draws the field's border, so it keeps the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string InputCssClass { get; set; } = "";

	// The swatch shows the value only while it is a hex colour: typed text such as
	// "red; background-image: url(...)" must not add declarations of its own.
	private string? SwatchStyle => new StyleBuilder()
		.AddStyle("background-color", MokaHexColor.IsValid(CurrentValueAsString) ? CurrentValueAsString : "transparent")
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (string.IsNullOrEmpty(Placeholder))
		{
			Placeholder = "#000000";
		}

		ComputedCssClass = new CssBuilder(RootClass)
			.AddClass("moka-color-input--error", HasError)
			.AddClass("moka-color-input--disabled", Disabled)
			.AddClass($"moka-color-input--{SizeToKebab(Size)}")
			.AddClass(CssClass)
			.AddClass(Class)
			.Build();
		InputCssClass = new CssBuilder("moka-color-input__text")
			.AddClass($"moka-color-input__text--{SizeToKebab(Size)}")
			.Build();
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	private Task HandleInput(ChangeEventArgs e)
	{
		CurrentValueAsString = e.Value?.ToString();
		return Task.CompletedTask;
	}

	private Task HandleNativeInput(ChangeEventArgs e)
	{
		CurrentValueAsString = e.Value?.ToString();
		return Task.CompletedTask;
	}
}
