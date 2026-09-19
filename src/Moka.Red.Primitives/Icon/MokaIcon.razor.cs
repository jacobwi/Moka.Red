using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
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

	// An icon is foreground: Surface means the text colour on a surface. The surface colour itself
	// would draw the icon in the colour of the background behind it.
	private string? IconColor => Color switch
	{
		null => null,
		MokaColor.Surface => "var(--moka-color-on-surface)",
		MokaColor c => $"var(--moka-color-{ColorToKebab(c)})"
	};

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("width", ResolvedSize)
		.AddStyle("height", ResolvedSize)
		.AddStyle("color", IconColor)
		.AddStyle(Style)
		.Build();
}
