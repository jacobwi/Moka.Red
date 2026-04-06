using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.ColorInput;

/// <summary>
///     A simple color input field with a color swatch preview and optional native color picker.
///     Lighter than <c>MokaColorPicker</c> — just a text input for hex values with a preview swatch.
/// </summary>
public partial class MokaColorInput
{
	private readonly string _inputId = $"moka-colorinput-{Guid.NewGuid():N}";

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

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ComputedCssClass { get; set; } = "";

	private string? ComputedStyle => Style;

	private string InputCssClass { get; set; } = "";

	private string SwatchColor =>
		string.IsNullOrWhiteSpace(CurrentValueAsString) ? "transparent" : CurrentValueAsString;

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
