using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Card;

/// <summary>
///     A versatile content container with optional header, body, footer, media, and actions slots.
///     Supports elevation shadows, outlined variant, click interaction, loading state,
///     collapsible body, and accent color.
/// </summary>
public partial class MokaCard : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private readonly string _regionId = $"moka-card-{Guid.NewGuid():N}";
	private ElementReference _element;
	private ElementReference _headerContent;
	private string? _boundRootId;
	private string? _boundHeaderId;

	// The card shows _collapsed, not Collapsed: the header changes it before the parent answers,
	// and a parent that does not bind Collapsed must not undo it by re-rendering.
	private bool _collapsed;
	private bool? _lastCollapsedParameter;

	/// <summary>Body content rendered in the card body section.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Custom header content. Overrides <see cref="Title" /> and <see cref="Subtitle" />.</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>Footer content rendered at the bottom of the card.</summary>
	[Parameter]
	public RenderFragment? Footer { get; set; }

	/// <summary>Media content (e.g., image) rendered above the header.</summary>
	[Parameter]
	public RenderFragment? Media { get; set; }

	/// <summary>Actions rendered in the header area, right-aligned. Use for icon buttons, menus, etc.</summary>
	[Parameter]
	public RenderFragment? HeaderActions { get; set; }

	/// <summary>Simple text title for the card header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Secondary text below the title.</summary>
	[Parameter]
	public string? Subtitle { get; set; }

	/// <summary>Box shadow depth. Range 0-4. Default 1.</summary>
	[Parameter]
	public int Elevation { get; set; } = 1;

	/// <summary>When true, renders a border instead of a box shadow.</summary>
	[Parameter]
	public bool Outlined { get; set; }

	/// <summary>
	///     When true, the card is a button: hover effect, pointer cursor, a tab stop, and Enter or
	///     Space click it.
	/// </summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>When true, the card stretches to fill its container width.</summary>
	[Parameter]
	public bool FullWidth { get; set; }

	/// <summary>Shows a loading skeleton over the card body. Default false.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>
	///     When true, the card body can be collapsed/expanded by clicking the header. Default false.
	///     The title area is then a button with <c>aria-expanded</c> that Enter or Space toggle.
	/// </summary>
	[Parameter]
	public bool Collapsible { get; set; }

	/// <summary>Whether the body is currently collapsed. Two-way bindable.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Callback when collapsed state changes.</summary>
	[Parameter]
	public EventCallback<bool> CollapsedChanged { get; set; }

	/// <summary>Accent color bar on the left edge. Null = no accent.</summary>
	[Parameter]
	public MokaColor? AccentColor { get; set; }

	/// <summary>Accent bar width. Default "3px".</summary>
	[Parameter]
	public string AccentWidth { get; set; } = "3px";

	/// <summary>When true, removes all internal padding (header, body, footer). Useful for custom layouts.</summary>
	[Parameter]
	public bool NoPadding { get; set; }

	/// <summary>
	///     Optional href - makes the entire card a clickable link. A <c>javascript:</c>, <c>vbscript:</c> or
	///     <c>data:</c> URL is not rendered: the card stays a plain card.
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-card";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-card--elevation-{ClampElevation}", !Outlined)
		.AddClass("moka-card--outlined", Outlined)
		.AddClass("moka-card--clickable", Clickable || LinkHref is not null)
		.AddClass("moka-card--full-width", FullWidth)
		.AddClass("moka-card--loading", Loading)
		.AddClass("moka-card--no-padding", NoPadding)
		.AddClass("moka-card--has-accent", AccentColor.HasValue)
		.AddClass("moka-card--collapsed", IsCollapsed)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("border-left-width", AccentWidth, AccentColor.HasValue)
		.AddStyle("border-left-style", "solid", AccentColor.HasValue)
		.AddStyle("border-left-color",
			AccentColor.HasValue ? $"var(--moka-color-{ColorToKebab(AccentColor.Value)})" : null)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private int ClampElevation => Math.Clamp(Elevation, 0, 4);
	private bool HasHeader => Header is not null || Title is not null || HeaderActions is not null;
	private bool IsCollapsed => Collapsible && _collapsed;

	private string? LinkHref => UrlValues.SafeHref(Href);

	// A link card is already focusable, and the browser activates it.
	private bool IsButton => Clickable && LinkHref is null;

	private int? RootTabIndex => IsButton ? 0 : null;

	private bool IsToggleable => Collapsible && HasHeader;

	private int? HeaderTabIndex => Collapsible ? 0 : null;

	private string? HeaderExpanded => Collapsible ? (IsCollapsed ? "false" : "true") : null;

	// The toggle takes its name from the title. With only HeaderActions there is no title to use.
	private string? HeaderToggleLabel =>
		Collapsible && Header is null && Title is null ? "Toggle section" : null;

	private string? BodyId => Collapsible ? $"{_regionId}-body" : null;

	private string? FooterId => Collapsible ? $"{_regionId}-footer" : null;

	// Only regions on the page: a collapsed card renders neither the body nor the footer.
	private string? HeaderControls
	{
		get
		{
			if (!Collapsible || IsCollapsed)
			{
				return null;
			}

			bool hasBody = Loading || ChildContent is not null;
			return (hasBody, Footer is not null) switch
			{
				(true, true) => $"{BodyId} {FooterId}",
				(true, false) => BodyId,
				(false, true) => FooterId,
				_ => null
			};
		}
	}

	private string HeaderCssClass => new CssBuilder("moka-card-header")
		.AddClass("moka-card-header--collapsible", Collapsible)
		.Build();

	private string CollapseIconCssClass => new CssBuilder("moka-card-collapse-icon")
		.AddClass("moka-card-collapse-icon--expanded", !IsCollapsed)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_lastCollapsedParameter != Collapsed)
		{
			_lastCollapsedParameter = Collapsed;
			_collapsed = Collapsed;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Enter and Space are handled in the browser, each element for itself only: keys pressed in
		// a button inside the card stay with that button, and Space does not scroll the page. Each
		// element is bound once, and a new one again (Href or the header can come and go).
		if (IsButton && _element.Id != _boundRootId)
		{
			_boundRootId = _element.Id;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _element);
		}

		if (IsToggleable && _headerContent.Id != _boundHeaderId)
		{
			_boundHeaderId = _headerContent.Id;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _headerContent);
		}
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (Clickable || LinkHref is not null)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleHeaderClick()
	{
		if (Collapsible)
		{
			_collapsed = !_collapsed;
			await CollapsedChanged.InvokeAsync(_collapsed);
		}
	}
}
