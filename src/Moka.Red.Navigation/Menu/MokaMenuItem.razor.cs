using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Menu;

/// <summary>
///     A single item in a <see cref="MokaMenu" />. Supports nesting, icons, badges, and expand/collapse.
///     An item with <see cref="Href" /> renders as a link. An item with children renders as a button
///     that expands and collapses them, and an item with <see cref="OnClick" /> or
///     <see cref="OnContextMenu" /> renders as a button, so the keyboard reaches all of them.
/// </summary>
public partial class MokaMenuItem
{
	private readonly string _generatedId = $"moka-menu-item-{Guid.NewGuid():N}";

	// The rendered state. The Expanded parameter only seeds it when the parent passes a new value,
	// so a parent re-render with the same one-way value does not undo the user's toggle.
	private bool _expanded;
	private bool _lastExpandedParameter;

	/// <summary>Nested child menu items (sub-menu).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Display text for the menu item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Navigation link. When set, renders as an anchor element. Ignored on an item with children.</summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     Whether this item is highlighted as the active route. A link says so to screen readers
	///     with <c>aria-current="page"</c>.
	/// </summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>
	///     Whether the sub-menu is expanded. Two-way bindable. A one-way value sets the state only
	///     when it changes, so the user's own toggle survives a parent re-render.
	/// </summary>
	[Parameter]
	public bool Expanded { get; set; }

	/// <summary>Callback when the sub-menu expands or collapses.</summary>
	[Parameter]
	public EventCallback<bool> ExpandedChanged { get; set; }

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Right-click (context menu) callback. When attached, the browser's default context menu
	///     is suppressed. Pair with a context-menu service. The item is focusable when this is set,
	///     so the context-menu key reaches it too.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnContextMenu { get; set; }

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
	private string? LinkHref => UrlValues.SafeHref(Href);

	private bool IsLink => !string.IsNullOrEmpty(LinkHref);
	private bool IsInteractive => OnClick.HasDelegate || OnContextMenu.HasDelegate;
	private int ResolvedIndent => Indent > 0 ? Indent : CascadedIndent;
	private string SubmenuId => $"{Id ?? _generatedId}-submenu";

	// The text is not rendered while the menu shows only icons, so it names the item instead.
	private string? CollapsedLabel => MenuCollapsed && !string.IsNullOrEmpty(Text) ? Text : null;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-menu-item--active", Active)
		.AddClass("moka-menu-item--expanded", _expanded && HasChildren)
		.AddClass("moka-menu-item--has-children", HasChildren)
		.AddClass("moka-menu-item--collapsed", MenuCollapsed)
		.AddClass("moka-menu-item--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		// Padding, when set, replaces the indent: a padding-left after it would override its left side.
		.AddStyle("padding-left", $"{12 * ResolvedIndent}px", ResolvedIndent > 0 && !MenuCollapsed && ResolvedPadding is null)
		.AddStyle(Style)
		.Build();

	private string SubmenuClass => new CssBuilder("moka-menu-item__submenu")
		.AddClass("moka-menu-item__submenu--open", _expanded)
		.Build();

	private string ChevronClass => new CssBuilder("moka-menu-item__chevron")
		.AddClass("moka-menu-item__chevron--rotated", _expanded)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Expanded != _lastExpandedParameter)
		{
			_lastExpandedParameter = Expanded;
			_expanded = Expanded;
		}
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (Disabled)
		{
			return;
		}

		if (HasChildren)
		{
			_expanded = !_expanded;
			await ExpandedChanged.InvokeAsync(_expanded);
		}

		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleContextMenu(MouseEventArgs args)
	{
		if (!Disabled && OnContextMenu.HasDelegate)
		{
			await OnContextMenu.InvokeAsync(args);
		}
	}
}
