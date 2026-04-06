using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Sticky;

/// <summary>
///     A wrapper that makes content stick to the viewport on scroll using CSS <c>position: sticky</c>.
///     Supports configurable top/bottom offsets and z-index.
/// </summary>
public partial class MokaSticky : MokaVisualComponentBase
{
	/// <summary>The child content to render inside the sticky wrapper.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>CSS top value when stuck. Default "0".</summary>
	[Parameter]
	public string? Top { get; set; } = "0";

	/// <summary>CSS bottom value. When set, sticks to the bottom instead of top.</summary>
	[Parameter]
	public string? Bottom { get; set; }

	/// <summary>Z-index when stuck. Default 1020 (from --moka-z-sticky).</summary>
	[Parameter]
	public int? ZIndex { get; set; }

	/// <summary>Custom offset value as an alternative to <see cref="Top" /> or <see cref="Bottom" />.</summary>
	[Parameter]
	public string? OffsetValue { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-sticky";

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("position", "sticky")
		.AddStyle("top", Top, Bottom is null && OffsetValue is null)
		.AddStyle("bottom", Bottom, Bottom is not null)
		.AddStyle("top", OffsetValue, OffsetValue is not null && Bottom is null)
		.AddStyle("z-index", ZIndex?.ToString(CultureInfo.InvariantCulture) ?? "var(--moka-z-sticky)")
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();
}
