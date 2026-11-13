using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Checkbox;

/// <summary>
///     A checkbox input with custom styling, indeterminate state support, and label positioning. <c>Color</c> fills
///     the checked or indeterminate box and tints the focus ring; unset, both are the theme's primary color.
/// </summary>
public partial class MokaCheckbox
{
	/// <summary>Helper text displayed below the checkbox.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Whether the checkbox is in an indeterminate state (shows a dash instead of check).</summary>
	[Parameter]
	public bool Indeterminate { get; set; }

	/// <summary>
	///     Draws the checkbox without making it a control: no label, no input and no tab stop, and
	///     hidden from screen readers. Use it inside a row that is clickable itself, where a real
	///     checkbox takes the click too and toggles twice. The row should expose its own checked or
	///     selected state.
	/// </summary>
	[Parameter]
	public bool DisplayOnly { get; set; }

	/// <inheritdoc />
	/// <summary>Error text displayed below the control. Overrides any EditContext message.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>True when an explicit <see cref="ErrorText" /> is set or the EditContext reports one.</summary>
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	/// <inheritdoc />
	protected override string RootClass => "moka-checkbox";

	// No class without a Color, so the default checkbox keeps its markup and reads the accent through the fallback.
	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass(Color is { } color ? $"moka-checkbox--{ColorToKebab(color)}" : null)
		.AddClass("moka-checkbox--disabled", Disabled)
		.AddClass("moka-checkbox--indeterminate", Indeterminate)
		.AddClass("moka-checkbox--checked", CurrentValue && !Indeterminate)
		.AddClass("moka-checkbox--display-only", DisplayOnly)
		.AddClass("moka-checkbox--loading", Loading)
		.AddClass(CssClass) // InputBase's field classes: modified, valid, invalid
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. Padding widens the
	// clickable row. The radius shapes the box that draws the check, since the row has no outline.

	/// <inheritdoc />
	protected override string? ComponentStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? BoxStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private void HandleChange(ChangeEventArgs e) => Toggle();
}
