using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.DataList;

/// <summary>
///     A definition list component for displaying structured key-value data.
///     Renders as a semantic &lt;dl&gt; element with &lt;dt&gt; and &lt;dd&gt; pairs.
///     Supports both data-driven (via <see cref="Items" />) and template-driven
///     (via <see cref="ChildContent" />) usage.
/// </summary>
public partial class MokaDataList : MokaVisualComponentBase
{
	/// <summary>The data items to display. Each item renders as a dt/dd pair.</summary>
	[Parameter]
	public IReadOnlyList<MokaDataListItem>? Items { get; set; }

	/// <summary>Manual child content as an alternative to <see cref="Items" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Layout orientation. Column (default) stacks terms vertically; Row displays them inline.
	/// </summary>
	[Parameter]
	public MokaDirection Orientation { get; set; } = MokaDirection.Column;

	/// <summary>Whether to apply alternating row backgrounds.</summary>
	[Parameter]
	public bool Striped { get; set; }

	/// <summary>Whether to show borders between items.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>Whether to use compact spacing.</summary>
	[Parameter]
	public bool Dense { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-data-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-data-list--{SizeToKebab(Size)}")
		.AddClass("moka-data-list--row", Orientation == MokaDirection.Row)
		.AddClass("moka-data-list--striped", Striped)
		.AddClass("moka-data-list--bordered", Bordered)
		.AddClass("moka-data-list--dense", Dense)
		.AddClass("moka-data-list--disabled", Disabled)
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
