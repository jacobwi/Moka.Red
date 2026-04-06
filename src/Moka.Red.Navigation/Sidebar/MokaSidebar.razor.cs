using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Sidebar;

/// <summary>
///     A vertical sidebar navigation panel with collapsible, overlay, and mini modes.
///     Provides header, body (scrollable), and footer sections.
/// </summary>
public partial class MokaSidebar
{
	/// <summary>Main body content (menu items, links, custom content).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Top area content (logo, app name).</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>Bottom area content (user info, settings).</summary>
	[Parameter]
	public RenderFragment? Footer { get; set; }

	/// <summary>Whether the sidebar is open. Two-way bindable. Default true.</summary>
	[Parameter]
	public bool Open { get; set; } = true;

	/// <summary>Callback when <see cref="Open" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Mini-sidebar mode (icons only). Two-way bindable. Default false.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Callback when <see cref="Collapsed" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> CollapsedChanged { get; set; }

	/// <summary>Full width when expanded. Default "240px".</summary>
	[Parameter]
	public string Width { get; set; } = "240px";

	/// <summary>Width when collapsed (mini mode). Default "56px".</summary>
	[Parameter]
	public string CollapsedWidth { get; set; } = "56px";

	/// <summary>When true, sidebar overlays content (mobile mode). Default false.</summary>
	[Parameter]
	public bool Overlay { get; set; }

	/// <summary>Whether to show a right border. Default true.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>Whether to apply elevation shadow. Default false.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <summary>Position of the sidebar. Default Left.</summary>
	[Parameter]
	public MokaSidebarPosition Position { get; set; } = MokaSidebarPosition.Left;

	/// <inheritdoc />
	protected override string RootClass => "moka-sidebar";

	private string ResolvedWidth => Collapsed ? CollapsedWidth : Width;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-sidebar--open", Open)
		.AddClass("moka-sidebar--closed", !Open)
		.AddClass("moka-sidebar--collapsed", Collapsed)
		.AddClass("moka-sidebar--overlay", Overlay)
		.AddClass("moka-sidebar--bordered", Bordered)
		.AddClass("moka-sidebar--elevated", Elevated)
		.AddClass($"moka-sidebar--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", ResolvedWidth)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleBackdropClick()
	{
		if (Overlay)
		{
			Open = false;
			await OpenChanged.InvokeAsync(Open);
		}
	}
}
