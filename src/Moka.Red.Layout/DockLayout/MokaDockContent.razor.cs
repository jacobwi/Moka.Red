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

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-content";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("grid-area", "content")
		.AddStyle(Style)
		.Build();
}
