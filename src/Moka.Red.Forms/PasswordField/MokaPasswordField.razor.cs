using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.PasswordField;

/// <summary>
///     A password input field with an optional visibility toggle button.
///     Shows eye/eye-off icon to reveal or hide the password.
/// </summary>
public partial class MokaPasswordField
{
	private readonly string _inputId = $"moka-passwordfield-{Guid.NewGuid():N}";

	private bool _showPassword;

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

	/// <summary>Whether to show the visibility toggle button. Default true.</summary>
	[Parameter]
	public bool ShowToggle { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-passwordfield";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ResolvedInputType => _showPassword ? "text" : "password";

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
			.AddClass("moka-textfield-input--has-end-icon", ShowToggle)
			.Build();
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	private void ToggleVisibility() => _showPassword = !_showPassword;

	private Task HandleInput(ChangeEventArgs e)
	{
		CurrentValueAsString = e.Value?.ToString();
		return Task.CompletedTask;
	}
}
