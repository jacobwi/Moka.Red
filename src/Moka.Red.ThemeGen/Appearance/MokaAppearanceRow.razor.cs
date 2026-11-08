using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Appearance;

/// <summary>
///     One row of <see cref="MokaAppearancePanel" />: a label with its control beside it, the control moving under it when
///     the panel is narrow. An app puts its own rows in the panel with this.
/// </summary>
public partial class MokaAppearanceRow
{
	/// <summary>The row's label. A screen reader doesn't hear it from the control, so name the control too.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>A line under the label.</summary>
	[Parameter]
	public string? Hint { get; set; }

	/// <summary>The control.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-appearance-row";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();
}
