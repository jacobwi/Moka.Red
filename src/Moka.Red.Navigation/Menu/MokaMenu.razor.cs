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

	/// <inheritdoc />
	protected override string RootClass => "moka-menu";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-menu--collapsed", Collapsed)
		.AddClass("moka-menu--dense", Dense)
		.AddClass("moka-menu--bordered", Bordered)
		.AddClass(Class)
		.Build();
}
