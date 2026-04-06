using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Menu;

/// <summary>
///     A single item in a <see cref="MokaMenu" />. Supports nesting, icons, badges, and expand/collapse.
/// </summary>
public partial class MokaMenuItem
{
	/// <summary>Nested child menu items (sub-menu).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Display text for the menu item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Navigation link. When set, renders as an anchor element.</summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>Whether this item is highlighted as the active route.</summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>Whether the sub-menu is expanded. Two-way bindable.</summary>
	[Parameter]
	public bool Expanded { get; set; }

	/// <summary>Callback when <see cref="Expanded" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> ExpandedChanged { get; set; }

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>Badge text displayed to the right (e.g., "3", "New").</summary>
	[Parameter]
	public string? Badge { get; set; }

	/// <summary>Color for the badge. Default Primary.</summary>
	[Parameter]
	public MokaColor? BadgeColor { get; set; } = MokaColor.Primary;

	/// <summary>Nesting depth. Auto-incremented for nested items.</summary>
	[Parameter]
	public int Indent { get; set; }

	[CascadingParameter] private MokaMenu? ParentMenu { get; set; }

	[CascadingParameter(Name = "MenuCollapsed")]
	private bool MenuCollapsed { get; set; }

	[CascadingParameter(Name = "MenuIndent")]
	private int CascadedIndent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-menu-item";

	private bool HasChildren => ChildContent is not null;
	private bool IsLink => !string.IsNullOrEmpty(Href);
	private int ResolvedIndent => Indent > 0 ? Indent : CascadedIndent;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-menu-item--active", Active)
		.AddClass("moka-menu-item--expanded", Expanded && HasChildren)
		.AddClass("moka-menu-item--has-children", HasChildren)
		.AddClass("moka-menu-item--collapsed", MenuCollapsed)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("padding-left", $"{12 * ResolvedIndent}px", ResolvedIndent > 0 && !MenuCollapsed)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleClick(MouseEventArgs args)
	{
		if (HasChildren)
		{
			Expanded = !Expanded;
			await ExpandedChanged.InvokeAsync(Expanded);
		}

		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
