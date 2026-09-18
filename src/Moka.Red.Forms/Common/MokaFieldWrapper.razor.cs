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

	private string? LabelId => InputId is null ? null : LabelIdFor(InputId);

	/// <summary>
	///     Id of the label rendered for <paramref name="inputId" />. <c>&lt;label for&gt;</c> only names native
	///     form controls, so a custom control such as a <c>role="combobox"</c> div points
	///     <c>aria-labelledby</c> at this id instead.
	/// </summary>
	internal static string LabelIdFor(string inputId) => $"{inputId}-label";

	/// <summary>Whether the field is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Size of the field for styling purposes.</summary>
	[Parameter]
	public MokaSize Size { get; set; } = MokaSize.Md;

	/// <summary>The input content to render inside the wrapper.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	private string WrapperClass => new CssBuilder("moka-field")
		.AddClass($"moka-field--{MokaEnumHelpers.ToCssClass(Size)}")
		.AddClass("moka-field--disabled", Disabled)
		.AddClass("moka-field--error", HasError)
		.Build();
}
