using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Common;

/// <summary>
///     Shared wrapper component for form fields. Provides label, helper text,
///     error text, and consistent layout for all input components.
/// </summary>
public partial class MokaFieldWrapper
{
	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether the field is required. Shows an asterisk next to the label.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>
	///     Whether the field can be left empty. Shows <see cref="OptionalText" /> beside the label, inside it,
	///     so it is read with the label. <see cref="Required" /> wins when both are set.
	/// </summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>The word shown beside the label when <see cref="Optional" /> is set.</summary>
	[Parameter]
	public string OptionalText { get; set; } = "Optional";

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is in an error state.</summary>
	[Parameter]
	public bool HasError { get; set; }

	/// <summary>The HTML id of the input element, used for label association.</summary>
	[Parameter]
	public string? InputId { get; set; }

	/// <summary>
	///     Set when the control is not a native form control, such as a <c>role="combobox"</c> or
	///     <c>role="slider"</c> div. <c>&lt;label for&gt;</c> may only point at a native control, so the label
	///     then keeps its id but renders no <c>for</c>, and the control points <c>aria-labelledby</c> at
	///     <see cref="LabelIdFor" />.
	/// </summary>
	[Parameter]
	public bool CustomControl { get; set; }

	private string? LabelId => InputId is null ? null : LabelIdFor(InputId);

	private string? LabelFor => CustomControl ? null : InputId;

	private string? HelperId => InputId is null ? null : HelperIdFor(InputId);

	private string HelperClass => new CssBuilder("moka-field-helper")
		.AddClass("moka-field-helper--error", HasError)
		.Build();

	/// <summary>
	///     Id of the label rendered for <paramref name="inputId" />. <c>&lt;label for&gt;</c> only names native
	///     form controls, so a custom control such as a <c>role="combobox"</c> div points
	///     <c>aria-labelledby</c> at this id instead.
	/// </summary>
	public static string LabelIdFor(string inputId) => $"{inputId}-label";

	/// <summary>
	///     Id of the helper or error line rendered under the control for <paramref name="inputId" />. The field's
	///     control points <c>aria-describedby</c> at it, so a screen reader reads the line with the control. A
	///     control drawn apart from the field can point at it too.
	/// </summary>
	public static string HelperIdFor(string inputId) => $"{inputId}-help";

	/// <summary>Whether the wrapper renders its helper or error line for these texts.</summary>
	internal static bool ShowsHelper(string? helperText, string? errorText) => helperText is not null || errorText is not null;

	/// <summary>
	///     <see cref="HelperIdFor" /> for a field whose wrapper renders a helper or error line, else
	///     <see langword="null" />, so <c>aria-describedby</c> never names an element that isn't there.
	/// </summary>
	internal static string? HelperIdIfShown(string? inputId, string? helperText, string? errorText) =>
		inputId is not null && ShowsHelper(helperText, errorText) ? HelperIdFor(inputId) : null;

	/// <summary>Whether the field is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Size of the field for styling purposes.</summary>
	[Parameter]
	public MokaSize Size { get; set; } = MokaSize.Md;

	/// <summary>The input content to render inside the wrapper.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Content drawn beside the field's box, in its row, such as a button that acts on the value. The label
	///     stays above and the helper text below, and on the new design a button here is as tall as the box.
	/// </summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>
	///     Inline style for the wrapper. The wrapper is the field's outermost element, so an input passes
	///     its margin here.
	/// </summary>
	[Parameter]
	public string? Style { get; set; }

	/// <summary>
	///     CSS classes for the wrapper, added after its own. For an input with no single element of its
	///     own around its markup, which passes its <c>Class</c> here along with its <c>Style</c>.
	/// </summary>
	[Parameter]
	public string? Class { get; set; }

	private string WrapperClass => new CssBuilder("moka-field")
		.AddClass($"moka-field--{MokaEnumHelpers.ToCssClass(Size)}")
		.AddClass("moka-field--disabled", Disabled)
		.AddClass("moka-field--error", HasError)
		.AddClass(Class)
		.Build();
}
