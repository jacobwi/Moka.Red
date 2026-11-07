using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Menu;

/// <summary>
///     A vertical navigation menu with support for nested items, icons, badges, and collapsible groups.
/// </summary>
public partial class MokaMenu
{
	/// <summary>Menu item children (MokaMenuItem / MokaMenuDivider).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>When true, shows only icons (sidebar collapsed mode).</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Whether to use dense (compact) spacing. Default true.</summary>
	[Parameter]
	public bool Dense { get; set; } = true;

	/// <summary>Whether to show a border around the menu.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>
	///     Draws every item for rows and the icon rail alike and leaves the choice to CSS: it's the rail inside an element
	///     with a <c>data-moka-rail</c> attribute, and taller rows for fingers on a phone. For a menu that has to change
	///     before .NET can tell it to, as <c>MokaAppShell</c>'s does. Ignored while <see cref="Collapsed" />.
	/// </summary>
	[Parameter]
	public bool Adaptive { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-menu";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-menu--collapsed", Collapsed)
		.AddClass("moka-menu--dense", Dense)
		.AddClass("moka-menu--bordered", Bordered)
		.AddClass("moka-menu--adaptive", Adaptive && !Collapsed)
		.AddClass(Class)
		.Build();
}
