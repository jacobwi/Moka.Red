using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Switch;

/// <summary>
///     A toggle switch input with a track and thumb slider design.
/// </summary>
public partial class MokaSwitch
{
	/// <summary>Helper text displayed below the switch.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <inheritdoc />
	/// <summary>Error text displayed below the control. Overrides any EditContext message.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>True when an explicit <see cref="ErrorText" /> is set or the EditContext reports one.</summary>
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	protected override string RootClass => "moka-switch";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-switch--disabled", Disabled)
		.AddClass(Class)
		.Build();

	private string? ComputedStyle => Style;

	private void HandleChange(ChangeEventArgs e) => Toggle();
}
