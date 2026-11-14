using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Navigation.AppShell;

namespace Moka.Red.Navigation.Menu;

/// <summary>
///     A single item in a <see cref="MokaMenu" />. Supports nesting, icons, badges, and expand/collapse.
///     An item with <see cref="Href" /> renders as a link. An item with children renders as a button
///     that expands and collapses them, or with <see cref="Href" /> as well, as a link with a button beside it that does.
///     An item with <see cref="OnClick" /> or <see cref="OnContextMenu" /> renders as a button, so the keyboard reaches
///     all of them. On a menu showing only icons, a group opens beside its icon as a flyout. A group opens by itself when
///     the current page is inside it.
/// </summary>
public partial class MokaMenuItem
{
	private const string MenuModule = "./_content/Moka.Red.Navigation/Menu/MokaMenu.razor.js";

	private readonly string _generatedId = $"moka-menu-item-{Guid.NewGuid():N}";

	// Told to the parent after the render that opened the group for the current page, since that happens in the middle
	// of a render, where an EventCallback can't be awaited.
	private bool _announceExpanded;
	private ElementReference _button;

	// The children whose page, or a page inside them, is the current one. A link holding a group has to know as it
	// renders, since a click there that only closes the group is cancelled in the browser, not from .NET.
	private HashSet<MokaMenuItem>? _currentChildren;

	// The rendered state. The Expanded parameter only seeds it when the parent passes a new value,
	// so a parent re-render with the same one-way value does not undo the user's toggle.
	private bool _expanded;
	private string? _failedImage;
	private bool _flyoutOpen;
	private DotNetObjectReference<MokaMenuItem>? _flyoutReporter;
	private bool _flyoutShown;
	private bool _lastExpandedParameter;
	private bool _listening;
	private bool _lit;
	private bool _matched;
	private bool _railTracked;
	private bool _repeatClickTracked;
	private bool _returnFocus;
	private ElementReference _submenu;
	private bool _toldParentCurrent;

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	/// <summary>Nested child menu items (sub-menu).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Display text for the menu item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     A picture in place of <see cref="Icon" />, such as a plugin's own icon: a URL or a <c>data:image/...</c> URI. It
	///     draws as an <c>img</c> in the icon's box, so pictures and icons line up, and the item's text names it. An address
	///     a picture can't come from (see <see cref="UrlValues.SafeImageSource" />), or a picture that fails to load, shows
	///     <see cref="Icon" /> instead.
	/// </summary>
	[Parameter]
	public string? ImageSource { get; set; }

	/// <summary>
	///     Navigation link. When set, renders as an anchor element. On an item with children the name is the link and an
	///     arrow button beside it opens and closes the group; a click on the name follows the link and opens the group,
	///     unless <see cref="CollapseOnRepeatClick" /> keeps it here. On a menu showing only icons the icon opens the
	///     flyout, which starts with this link.
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     Whether this item is highlighted as the active route. A link says so to screen readers
	///     with <c>aria-current="page"</c>. When it turns on, the groups around the item open.
	/// </summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>
	///     Lets a link work out <see cref="Active" /> from the address, as <c>NavLink</c> does, and follow it as the person
	///     moves around. <see cref="NavLinkMatch.All" /> is the page itself, <see cref="NavLinkMatch.Prefix" /> the page and
	///     anything under it. The query and fragment never decide, as with <c>NavLink</c> on .NET 10. Left out,
	///     <see cref="Active" /> alone decides. With it, <see cref="Active" /> can still light the item. When the address
	///     lights it, the groups around the item open.
	/// </summary>
	[Parameter]
	public NavLinkMatch? Match { get; set; }

	/// <summary>
	///     Whether the sub-menu is expanded. Two-way bindable. A one-way value sets the state only
	///     when it changes, so the user's own toggle survives a parent re-render. A group that opens by itself for the
	///     current page raises <see cref="ExpandedChanged" />.
	/// </summary>
	[Parameter]
	public bool Expanded { get; set; }

	/// <summary>Callback when the sub-menu expands or collapses.</summary>
	[Parameter]
	public EventCallback<bool> ExpandedChanged { get; set; }

	/// <summary>
	///     For an item with <see cref="Href" /> and children: what a click on its name does when the person is already
	///     there. True: on the item's own page the click opens or closes the group without going anywhere, and on a page
	///     inside an open group it closes the group. False: the click always follows the link and opens the group. Anywhere
	///     else it follows the link and opens the group either way. Null, the default, takes
	///     <see cref="MokaMenu.CollapseOnRepeatClick" />, which is true. Enter on the link does the same as a click, and the
	///     arrow beside the name only ever opens and closes the group.
	/// </summary>
	[Parameter]
	public bool? CollapseOnRepeatClick { get; set; }

	/// <summary>
	///     The name of the arrow that opens the group of an item with <see cref="Href" /> and children, given the item's
	///     <see cref="Text" />. Null says "Show" and the text. It's the button's <c>aria-label</c> and its tooltip.
	/// </summary>
	[Parameter]
	public Func<string, string>? ShowLabel { get; set; }

	/// <summary>
	///     The name of the arrow that closes the group of an item with <see cref="Href" /> and children, given the item's
	///     <see cref="Text" />. Null says "Hide" and the text. It's the button's <c>aria-label</c> and its tooltip.
	/// </summary>
	[Parameter]
	public Func<string, string>? HideLabel { get; set; }

	/// <summary>
	///     Click event callback. On an item with <see cref="Href" /> and children it's raised when the link is followed,
	///     from the name or from the top of its flyout, and not for a click that only opens or closes the group, so a
	///     drawer the app closes on a pick stays open.
	/// </summary>
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

	/// <summary>
	///     A count with a meaning, drawn as a <c>MokaBadge</c> in its tone beside the text and as a corner badge on the
	///     icon when the menu shows only icons. Screen readers and the tag beside the rail get its <c>Why</c>.
	/// </summary>
	[Parameter]
	public MokaNavPill? Pill { get; set; }

	/// <summary>Nesting depth. Auto-incremented for nested items.</summary>
	[Parameter]
	public int Indent { get; set; }

	[CascadingParameter] private MokaMenu? ParentMenu { get; set; }

	[CascadingParameter] private MokaMenuItem? ParentItem { get; set; }

	[CascadingParameter(Name = "MenuCollapsed")]
	private bool MenuCollapsed { get; set; }

	[CascadingParameter(Name = "MenuIndent")]
	private int CascadedIndent { get; set; }

	[CascadingParameter(Name = "MenuAdaptive")]
	private bool MenuAdaptive { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-menu-item";

	private bool HasChildren => ChildContent is not null;
	private string? LinkHref => UrlValues.SafeHref(Href);
	private bool IsActive => Active || _matched;

	private static readonly string? ImageStyle = new StyleBuilder()
		.AddStyle("width", MapSizeToPx(MokaSize.Sm))
		.AddStyle("height", MapSizeToPx(MokaSize.Sm))
		.Build();

	private string? ShownImage
	{
		get
		{
			string? image = UrlValues.SafeImageSource(ImageSource);
			return image is null || string.Equals(image, _failedImage, StringComparison.Ordinal) ? null : image;
		}
	}

	private void HandleImageError() => _failedImage = UrlValues.SafeImageSource(ImageSource);

	private bool IsLink => !string.IsNullOrEmpty(LinkHref);
	private bool IsLinkGroup => HasChildren && IsLink;

	// Wherever its group opens in place, a link holding a group is the link with the arrow beside it. On the rail its
	// icon is the button that opens the flyout.
	private bool ShowsLinkRow => IsLinkGroup && !OnRail;

	private bool CollapsesOnRepeat => CollapseOnRepeatClick ?? ParentMenu?.CollapseOnRepeatClick ?? true;
	private bool HoldsCurrentChild => _currentChildren is { Count: > 0 };

	// Whether a click on the link opens or closes the group in place of following the link. Rendered as the click's
	// preventDefault, which the browser applies to the click Enter makes on a link too, and which Blazor's own link
	// handling respects, so no script is needed.
	private bool ClickStaysHere => ShowsLinkRow && !Disabled && CollapsesOnRepeat && (IsActive || (_expanded && HoldsCurrentChild));

	private string ToggleLabel => _expanded
		? HideLabel?.Invoke(Text ?? string.Empty) ?? $"Hide {Text}"
		: ShowLabel?.Invoke(Text ?? string.Empty) ?? $"Show {Text}";

	private bool IsInteractive => OnClick.HasDelegate || OnContextMenu.HasDelegate;
	private bool IsStatic => !HasChildren && !IsLink && !IsInteractive;
	private int ResolvedIndent => Indent > 0 ? Indent : CascadedIndent;
	private string SubmenuId => $"{Id ?? _generatedId}-submenu";
	private string FlyoutKey => $"{_generatedId}-flyout";

	// A group at the top of a menu that can show only icons. Groups inside it are rows in its flyout.
	private bool CanFlyout => HasChildren && ParentItem is null && (MenuCollapsed || MenuAdaptive);

	// Only an adaptive menu's script knows whether it's the rail, since that follows the window's width.
	private bool OnRail => CanFlyout && (MenuCollapsed || ParentMenu?.ShowsRail == true);

	// What the parent button says and the group's inert follow: the flyout on the rail, the group in place in rows.
	private bool GroupShown => OnRail ? _flyoutOpen : _expanded;

	// A flyout lists the group from its own edge.
	private int ChildIndent => OnRail ? 0 : ResolvedIndent + 1;

	// The text is not rendered while the menu shows only icons, so it names the item instead, and shows in a
	// tooltip beside it.
	private string? CollapsedLabel => !MenuCollapsed || string.IsNullOrEmpty(Text)
		? null
		: Pill is null ? Text : $"{Text}, {Pill.Why}";

	// An adaptive menu keeps its text for screen readers, so it needs the tag but no aria-label. It sits in the tag from
	// the start because the rail can come and go without .NET hearing of it.
	private string? TagLabel => (MenuCollapsed || MenuAdaptive) && !string.IsNullOrEmpty(Text) ? Text : null;

	// A rail has nothing but icons, so its names show at once.
	private int TagDelay => MenuAdaptive && !MenuCollapsed ? 0 : 300;

	private string? PillCount => Pill?.Count.ToString(CultureInfo.InvariantCulture);

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-menu-item--active", IsActive)
		.AddClass("moka-menu-item--expanded", _expanded && HasChildren)
		.AddClass("moka-menu-item--has-children", HasChildren && !ShowsLinkRow)
		.AddClass("moka-menu-item--group-link", ShowsLinkRow)
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
		.AddClass("moka-menu-item__flyout", CanFlyout)
		.AddClass("moka-thin-scrollbar", CanFlyout)
		.Build();

	private string ChevronClass => new CssBuilder("moka-menu-item__chevron")
		.AddClass("moka-menu-item__chevron--rotated", _expanded)
		.Build();

	/// <summary>
	///     Called by the menu's script when the flyout closes from the browser: Escape, the arrow back to the rail, a press
	///     outside it, or focus leaving it. Focus is already where it belongs.
	/// </summary>
	/// <returns>A task that completes once the flyout has been asked to close.</returns>
	[JSInvokable]
	public Task CloseFlyoutAsync() => InvokeAsync(() =>
	{
		if (SetFlyout(false, false))
		{
			StateHasChanged();
		}
	});

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

		bool matching = Match is not null && IsLink;
		if (matching && !_listening)
		{
			Navigation.LocationChanged += HandleLocationChanged;
			_listening = true;
		}
		else if (!matching && _listening)
		{
			Navigation.LocationChanged -= HandleLocationChanged;
			_listening = false;
		}

		_matched = matching && Matches(Navigation.Uri);
		TrackRail();
		TrackRepeatClick();

		// The menu stopped showing only icons, so the group is back in place.
		if (_flyoutOpen && !OnRail)
		{
			SetFlyout(false, true);
		}

		UpdateCurrent();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (CanFlyout && ParentMenu is not null)
		{
			await ParentMenu.BindRailAsync();
		}

		// The group goes to the top layer after the render that opened it, and leaves after the one that closed it.
		if (_flyoutOpen != _flyoutShown)
		{
			_flyoutShown = _flyoutOpen;
			if (_flyoutOpen)
			{
				_flyoutReporter ??= DotNetObjectReference.Create(this);
				await SafeModuleInvokeVoidAsync(MenuModule, "openFlyout", FlyoutKey, _submenu, _button, _flyoutReporter);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(MenuModule, "closeFlyout", FlyoutKey, _returnFocus);
				_flyoutReporter?.Dispose();
				_flyoutReporter = null;
			}
		}

		if (_announceExpanded)
		{
			_announceExpanded = false;
			await ExpandedChanged.InvokeAsync(true);
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_listening)
		{
			Navigation.LocationChanged -= HandleLocationChanged;
			_listening = false;
		}

		SetFlyout(false, false);
		if (_flyoutShown)
		{
			_flyoutShown = false;
			await SafeModuleInvokeVoidAsync(MenuModule, "closeFlyout", FlyoutKey, false);
		}

		_flyoutReporter?.Dispose();
		_flyoutReporter = null;

		if (_railTracked)
		{
			_railTracked = false;
			ParentMenu?.ForgetRail(this);
		}

		if (_repeatClickTracked)
		{
			_repeatClickTracked = false;
			ParentMenu?.ForgetRepeatClick(this);
		}

		if (_toldParentCurrent)
		{
			_toldParentCurrent = false;
			ParentItem?.ChildCurrentChanged(this, false);
		}

		await base.DisposeAsyncCore();
	}

	internal void MenuDefaultChanged() => StateHasChanged();

	// The parent hears whether this item's page, or a page inside it, is the current one. Only an item that can light
	// ever tells it anything, and only a link holding an open group redraws for it.
	internal void ChildCurrentChanged(MokaMenuItem child, bool current)
	{
		bool before = HoldsCurrentChild;
		if (current)
		{
			(_currentChildren ??= []).Add(child);
		}
		else
		{
			_currentChildren?.Remove(child);
		}

		if (HoldsCurrentChild != before && ShowsLinkRow && _expanded && CollapsesOnRepeat)
		{
			StateHasChanged();
		}

		TellParentCurrent();
	}

	internal void RailChanged()
	{
		if (_flyoutOpen && !OnRail)
		{
			SetFlyout(false, true);
		}

		StateHasChanged();
	}

	// A group with the current page inside opens, and so do the groups around it. It never closes one, and a group the
	// person closed stays closed until the page inside lights again.
	internal void RevealCurrentPage()
	{
		ParentItem?.RevealCurrentPage();
		if (_expanded)
		{
			return;
		}

		_expanded = true;
		_announceExpanded = ExpandedChanged.HasDelegate;
		StateHasChanged();
	}

	// Something was picked inside a group, so the flyout holding it closes. Focus goes back to the parent first, so a
	// dialog the app opens from the pick remembers the parent, not a row about to be hidden.
	internal async Task ChildPickedAsync()
	{
		if (SetFlyout(false, true))
		{
			StateHasChanged();
			await SafeFocusAsync(_button);
		}
		else if (ParentItem is not null)
		{
			await ParentItem.ChildPickedAsync();
		}
	}

	// Only a group on an adaptive menu has to hear the rail come and go: a collapsed menu's always is.
	private void TrackRail()
	{
		bool track = CanFlyout && MenuAdaptive && !MenuCollapsed && ParentMenu is not null;
		if (track == _railTracked)
		{
			return;
		}

		_railTracked = track;
		if (track)
		{
			ParentMenu!.TrackRail(this);
		}
		else
		{
			ParentMenu?.ForgetRail(this);
		}
	}

	// Only a link holding a group can be told when the menu's CollapseOnRepeatClick changes, and only while it takes
	// the menu's.
	private void TrackRepeatClick()
	{
		bool track = IsLinkGroup && CollapseOnRepeatClick is null && ParentMenu is not null;
		if (track == _repeatClickTracked)
		{
			return;
		}

		_repeatClickTracked = track;
		if (track)
		{
			ParentMenu!.TrackRepeatClick(this);
		}
		else
		{
			ParentMenu?.ForgetRepeatClick(this);
		}
	}

	// An item that lights opens the groups around it. A link holding a group opens its own too, since its page is the
	// group's first page.
	private void UpdateCurrent()
	{
		bool lit = IsActive;
		if (lit && !_lit)
		{
			if (IsLinkGroup)
			{
				RevealCurrentPage();
			}
			else
			{
				ParentItem?.RevealCurrentPage();
			}
		}

		_lit = lit;
		TellParentCurrent();
	}

	private void TellParentCurrent()
	{
		bool current = IsActive || HoldsCurrentChild;
		if (ParentItem is null || current == _toldParentCurrent)
		{
			return;
		}

		_toldParentCurrent = current;
		ParentItem.ChildCurrentChanged(this, current);
	}

	// Pages are listened for only while the flyout is open, since any of them closes it.
	private bool SetFlyout(bool open, bool returnFocus)
	{
		if (open == _flyoutOpen)
		{
			return false;
		}

		_flyoutOpen = open;
		_returnFocus = returnFocus;
		if (open)
		{
			Navigation.LocationChanged += HandleFlyoutLocationChanged;
		}
		else
		{
			Navigation.LocationChanged -= HandleFlyoutLocationChanged;
		}

		return true;
	}

	private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
	{
		bool matched = Matches(e.Location);
		if (matched != _matched)
		{
			RunDetached(() =>
			{
				_matched = matched;
				UpdateCurrent();
				StateHasChanged();
			});
		}
	}

	private void HandleFlyoutLocationChanged(object? sender, LocationChangedEventArgs e) => RunDetached(() =>
	{
		if (SetFlyout(false, true))
		{
			StateHasChanged();
		}
	});

	// NavLink's rules: a trailing slash doesn't count, and a prefix ends at a path separator, so "theme" doesn't light
	// for "theme-editor".
	private bool Matches(string location)
	{
		string target = PathOf(Navigation.ToAbsoluteUri(LinkHref).AbsoluteUri);
		string current = PathOf(Navigation.ToAbsoluteUri(location).AbsoluteUri);
		if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return Match == NavLinkMatch.Prefix
		       && current.Length > target.Length
		       && current[target.Length] == '/'
		       && current.StartsWith(target, StringComparison.OrdinalIgnoreCase);
	}

	private static string PathOf(string absoluteUri) =>
		absoluteUri.Split('?', '#')[0].TrimEnd('/');

	private async Task HandleClick(MouseEventArgs args)
	{
		if (Disabled)
		{
			return;
		}

		if (!HasChildren)
		{
			if (ParentItem is not null)
			{
				await ParentItem.ChildPickedAsync();
			}
		}
		else if (OnRail)
		{
			SetFlyout(!_flyoutOpen, true);
		}
		else
		{
			_expanded = !_expanded;
			await ExpandedChanged.InvokeAsync(_expanded);
		}

		// A link holding a group was picked only once its link is followed.
		if (OnClick.HasDelegate && !IsLinkGroup)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleGroupLinkClick(MouseEventArgs args)
	{
		// Ctrl, Shift, Alt or Cmd ask the browser for a new tab, a window or a download, which leaves this page as it is.
		if (Disabled || HasModifier(args))
		{
			return;
		}

		if (ClickStaysHere)
		{
			_expanded = !_expanded;
			await ExpandedChanged.InvokeAsync(_expanded);
			return;
		}

		if (!_expanded)
		{
			_expanded = true;
			await ExpandedChanged.InvokeAsync(true);
		}

		if (ParentItem is not null)
		{
			await ParentItem.ChildPickedAsync();
		}

		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleToggleClick()
	{
		if (Disabled)
		{
			return;
		}

		_expanded = !_expanded;
		await ExpandedChanged.InvokeAsync(_expanded);
	}

	// The page at the top of the flyout is followed like a link inside it: the flyout closes and focus goes back to the
	// parent's icon first.
	private async Task HandleFlyoutLinkClick(MouseEventArgs args)
	{
		if (HasModifier(args))
		{
			return;
		}

		if (SetFlyout(false, true))
		{
			StateHasChanged();
			await SafeFocusAsync(_button);
		}

		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private static bool HasModifier(MouseEventArgs args) => args.CtrlKey || args.ShiftKey || args.AltKey || args.MetaKey;

	private async Task HandleContextMenu(MouseEventArgs args)
	{
		if (!Disabled && OnContextMenu.HasDelegate)
		{
			await OnContextMenu.InvokeAsync(args);
		}
	}
}
