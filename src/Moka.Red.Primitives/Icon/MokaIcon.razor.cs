using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Icon;

/// <summary>
///     Renders an inline SVG icon from a <see cref="MokaIconDefinition" />.
///     Supports size and color variants via CSS classes.
/// </summary>
public partial class MokaIcon
{
	/// <summary>The icon definition to render.</summary>
	[Parameter]
	[EditorRequired]
	public MokaIconDefinition Icon { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-icon";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-icon--{SizeToKebab(Size)}")
		.AddClass(Color.HasValue ? $"moka-icon--{ColorToKebab(Color.Value)}" : null)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", ResolvedSize)
		.AddStyle("height", ResolvedSize)
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null)
		.AddStyle(Style)
		.Build();
}
