using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Menu;

/// <summary>
///     A vertical navigation menu with support for nested items, icons, badges, and collapsible groups. While it shows
///     only icons, a group opens beside its icon as a flyout.
/// </summary>
public partial class MokaMenu
{
	private const string ModulePath = "./_content/Moka.Red.Navigation/Menu/MokaMenu.razor.js";

	private readonly HashSet<MokaMenuItem> _railGroups = [];

	// Links holding a group that take CollapseOnRepeatClick from the menu. The menu is cascaded as a fixed value, so
	// they hear a change from here rather than through a subscription on every item.
	private readonly HashSet<MokaMenuItem> _repeatClickReaders = [];

	private bool? _lastCollapseOnRepeatClick;
	private ElementReference _nav;

	// Null until bound. False listens for the key that opens a group, true also watches the rail come and go.
	private bool? _railBinding;
	private DotNetObjectReference<MokaMenu>? _self;

	/// <summary>Menu item children (MokaMenuItem / MokaMenuDivider).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     When true, shows only icons (sidebar collapsed mode). A group then opens beside its icon as a flyout that lists
	///     its items with their names.
	/// </summary>
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
	///     before .NET can tell it to, as <c>MokaAppShell</c>'s does. On the rail a group opens beside its icon as a
	///     flyout, so a menu with groups watches the attribute in the browser. Ignored while <see cref="Collapsed" />.
	/// </summary>
	[Parameter]
	public bool Adaptive { get; set; }

	/// <summary>
	///     With <see cref="Adaptive" />, the rail shows each item's name under its icon, cut to one line, and an item's tag
	///     shows only for a pill or a name that was cut. <c>MokaAppShell</c> marks the cut names
	///     (<c>data-moka-cut</c> on the item's tooltip).
	/// </summary>
	[Parameter]
	public bool RailLabels { get; set; }

	/// <summary>
	///     What a click on the name of an item that is a link and a group at once does when the person is already there. On,
	///     the default: on the item's own page the click opens or closes the group without going anywhere, and on a page
	///     inside an open group it closes the group. Off, the click always follows the link and opens the group. Anywhere
	///     else the click follows the link and opens the group either way. Each item can say otherwise through
	///     <see cref="MokaMenuItem.CollapseOnRepeatClick" />.
	/// </summary>
	[Parameter]
	public bool CollapseOnRepeatClick { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-menu";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-menu--collapsed", Collapsed)
		.AddClass("moka-menu--dense", Dense)
		.AddClass("moka-menu--bordered", Bordered)
		.AddClass("moka-menu--adaptive", Adaptive && !Collapsed)
		.AddClass("moka-menu--rail-labels", Adaptive && !Collapsed && RailLabels)
		.AddClass(Class)
		.Build();

	// What the script last reported for an adaptive menu: whether it sits under data-moka-rail.
	internal bool ShowsRail { get; private set; }

	/// <summary>Called by the menu's script when an adaptive menu's rail comes or goes.</summary>
	/// <param name="rail">Whether the menu now shows the rail.</param>
	/// <returns>A task that completes once its groups have been told.</returns>
	[JSInvokable]
	public Task OnRailChanged(bool rail) => InvokeAsync(() => ApplyRail(rail));

	internal void TrackRail(MokaMenuItem group) => _railGroups.Add(group);

	internal void ForgetRail(MokaMenuItem group) => _railGroups.Remove(group);

	internal void TrackRepeatClick(MokaMenuItem item) => _repeatClickReaders.Add(item);

	internal void ForgetRepeatClick(MokaMenuItem item) => _repeatClickReaders.Remove(item);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		bool changed = _lastCollapseOnRepeatClick is { } last && last != CollapseOnRepeatClick;
		_lastCollapseOnRepeatClick = CollapseOnRepeatClick;
		if (!changed)
		{
			return;
		}

		foreach (MokaMenuItem item in _repeatClickReaders.ToArray())
		{
			item.MenuDefaultChanged();
		}
	}

	// Asked by a group after it renders, since only a menu with a group at the top needs anything from the browser.
	internal async Task BindRailAsync()
	{
		bool watch = Adaptive && !Collapsed;
		if (_railBinding == watch)
		{
			return;
		}

		_railBinding = watch;
		if (watch)
		{
			_self ??= DotNetObjectReference.Create(this);
		}

		bool rail = await SafeModuleInvokeAsync<bool>(ModulePath, "bindRail", _nav, watch ? _self : null);
		if (watch)
		{
			ApplyRail(rail);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_railBinding is not null)
		{
			_railBinding = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindRail", _nav);
		}

		_self?.Dispose();
		_self = null;
		await base.DisposeAsyncCore();
	}

	// Only the groups at the top redraw. Every other item looks the same on the rail and off it.
	private void ApplyRail(bool rail)
	{
		if (rail == ShowsRail)
		{
			return;
		}

		ShowsRail = rail;
		foreach (MokaMenuItem group in _railGroups.ToArray())
		{
			group.RailChanged();
		}
	}
}
