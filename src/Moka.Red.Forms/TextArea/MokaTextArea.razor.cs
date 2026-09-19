using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.TextArea;

/// <summary>
///     A multi-line text input (textarea) with optional auto-resize support.
/// </summary>
public partial class MokaTextArea
{
	private readonly string _generatedId = $"moka-textarea-{Guid.NewGuid():N}";

	// Id goes on the textarea, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

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

	/// <summary>Number of visible text lines. Default 3.</summary>
	[Parameter]
	public int Rows { get; set; } = 3;

	/// <summary>Whether the textarea grows with content. Default false.</summary>
	[Parameter]
	public bool AutoResize { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-textarea";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-textarea--error", HasError)
		.AddClass("moka-textarea--auto-resize", AutoResize)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The textarea draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? TextAreaStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

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
