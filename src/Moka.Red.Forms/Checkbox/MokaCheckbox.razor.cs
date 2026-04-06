using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Checkbox;

/// <summary>
///     A checkbox input with custom styling, indeterminate state support, and label positioning.
/// </summary>
public partial class MokaCheckbox
{
	/// <summary>Helper text displayed below the checkbox.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Whether the checkbox is in an indeterminate state (shows a dash instead of check).</summary>
	[Parameter]
	public bool Indeterminate { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-checkbox";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-checkbox--disabled", Disabled)
		.AddClass("moka-checkbox--indeterminate", Indeterminate)
		.AddClass(Class)
		.Build();

	private string? ComputedStyle => Style;

	private void HandleChange(ChangeEventArgs e) => Toggle();
}
