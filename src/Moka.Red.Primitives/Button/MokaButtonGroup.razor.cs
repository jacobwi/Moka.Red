using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Button;

/// <summary>
///     Groups buttons with connected styling — shared border-radius on ends only, no gaps.
/// </summary>
public partial class MokaButtonGroup
{
	/// <summary>Child content containing <see cref="MokaButton" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Layout direction. Default <see cref="MokaDirection.Row" />.</summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Row;

	/// <inheritdoc />
	protected override string RootClass => "moka-btn-group";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-btn-group--column", Orientation is MokaDirection.Column or MokaDirection.ColumnReverse)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();
}
