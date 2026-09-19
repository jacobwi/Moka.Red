using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     The main content area within a <see cref="MokaDockLayout" />.
///     Fills the remaining space not occupied by docked panels.
/// </summary>
public partial class MokaDockContent : MokaComponentBase
{
	/// <summary>The content to render in the main area.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Whether the area scrolls when its content overflows. Default true. Set false for content that
	///     scrolls itself, such as a terminal or an editor: the area then clips instead, and a single child
	///     fills it, so the child gets a definite size and shows the only scrollbar.
	/// </summary>
	[Parameter]
	public bool Scrollable { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-content";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-dock-content--no-scroll", !Scrollable)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("grid-area", "content")
		.AddStyle(Style)
		.Build();
}
