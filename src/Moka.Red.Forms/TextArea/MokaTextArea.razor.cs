using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.TextArea;

/// <summary>
///     A multi-line text input (textarea) with optional auto-resize support.
/// </summary>
public partial class MokaTextArea
{
	private readonly string _inputId = $"moka-textarea-{Guid.NewGuid():N}";

	/// <summary>Label text displayed above the textarea.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the textarea.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the textarea when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Placeholder text displayed when the textarea is empty.</summary>
	[Parameter]
	public string? Placeholder { get; set; }

	/// <summary>Number of visible text lines. Default 3.</summary>
	[Parameter]
	public int Rows { get; set; } = 3;

	/// <summary>Maximum number of characters allowed.</summary>
	[Parameter]
	public int? MaxLength { get; set; }

	/// <summary>Whether the textarea grows with content. Default false.</summary>
	[Parameter]
	public bool AutoResize { get; set; }

	/// <summary>Whether the textarea is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-textarea";

	private bool HasError => !string.IsNullOrEmpty(ErrorText);

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-textarea--error", HasError)
		.AddClass("moka-textarea--auto-resize", AutoResize)
		.AddClass(Class)
		.Build();

	private string? ComputedStyle => Style;

	private string TextAreaCssClass => new CssBuilder("moka-textarea-input")
		.AddClass($"moka-textarea-input--{SizeToKebab(Size)}")
		.Build();

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
}
