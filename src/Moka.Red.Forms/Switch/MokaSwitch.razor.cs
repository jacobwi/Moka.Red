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
	protected override string RootClass => "moka-switch";

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-switch--disabled", Disabled)
		.AddClass(Class)
		.Build();

	private string? ComputedStyle => Style;

	private void HandleChange(ChangeEventArgs e) => Toggle();
}
