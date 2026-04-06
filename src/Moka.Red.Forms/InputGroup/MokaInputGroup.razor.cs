using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.InputGroup;

/// <summary>
///     Groups multiple inputs into a single visual row with connected borders.
///     Removes internal border-radius so child elements appear as one unified control.
/// </summary>
public partial class MokaInputGroup : MokaVisualComponentBase
{
	/// <summary>The grouped input elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-input-group";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-input-group--{SizeToKebab(Size)}")
		.AddClass("moka-input-group--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();
}
