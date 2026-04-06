using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.TextField;

/// <summary>
///     A single-line text input field with optional icons, clear button, and validation support.
/// </summary>
public partial class MokaTextField
{
	private readonly string _inputId = $"moka-textfield-{Guid.NewGuid():N}";

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

	/// <summary>Icon displayed before the input text.</summary>
	[Parameter]
	public MokaIconDefinition? StartIcon { get; set; }

	/// <summary>Icon displayed after the input text.</summary>
	[Parameter]
	public MokaIconDefinition? EndIcon { get; set; }

	/// <summary>Whether to show a clear button when the input has a value.</summary>
	[Parameter]
	public bool Clearable { get; set; }

	/// <summary>Callback invoked on every keystroke.</summary>
	[Parameter]
	public EventCallback<string> OnInput { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-textfield";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ComputedCssClass { get; set; } = "";

	private string? ComputedStyle => Style;

	private string InputCssClass { get; set; } = "";

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		ComputedCssClass = new CssBuilder(RootClass)
			.AddClass("moka-textfield--error", HasError)
			.AddClass(Class)
			.Build();
		InputCssClass = new CssBuilder("moka-textfield-input")
			.AddClass($"moka-textfield-input--{SizeToKebab(Size)}")
			.AddClass("moka-textfield-input--has-start-icon", StartIcon is not null)
			.AddClass("moka-textfield-input--has-end-icon", EndIcon is not null || Clearable)
			.Build();
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	private async Task HandleInput(ChangeEventArgs e)
	{
		string? value = e.Value?.ToString();
		CurrentValueAsString = value;

		if (OnInput.HasDelegate)
		{
			await OnInput.InvokeAsync(value ?? string.Empty);
		}
	}

	private async Task HandleClear()
	{
		CurrentValueAsString = string.Empty;

		if (OnInput.HasDelegate)
		{
			await OnInput.InvokeAsync(string.Empty);
		}
	}
}
