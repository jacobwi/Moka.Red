using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Utilities;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Plugins;
using Moka.Red.Navigation.Tabs.Theming;

namespace Moka.Red.Navigation.Tabs;

/// <summary>
///     Renders the horizontal tab strip with drag/drop, context menus, and group support.
/// </summary>
/// <typeparam name="TValue">The type of value stored by tabs.</typeparam>
public partial class MokaTabStrip<TValue> : IAsyncDisposable
{
	#region Parameters

	/// <summary>
	///     The list of tabs to display.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<TabInfo<TValue>> Tabs { get; set; } = [];

	/// <summary>
	///     The list of tab groups.
	/// </summary>
	[Parameter]
	public IReadOnlyList<TabGroupInfo> Groups { get; set; } = [];

	/// <summary>
	///     The currently active tab identifier.
	/// </summary>
	[Parameter]
	public string? ActiveTabId { get; set; }

	/// <summary>
	///     Callback invoked when a tab header is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<string> OnTabActivated { get; set; }

	/// <summary>
	///     Callback invoked when a tab is closed from the strip: its close button, a middle click
	///     (see <see cref="CloseOnMiddleClick" />), the Delete key on a focused tab, or the built-in
	///     context menu.
	/// </summary>
	[Parameter]
	public EventCallback<string> OnTabClosed { get; set; }

	/// <summary>
	///     Callback invoked for the context menu's "Close Others". When no delegate is attached the
	///     strip falls back to raising <see cref="OnTabClosed" /> once per tab.
	/// </summary>
	[Parameter]
	public EventCallback<string> OnCloseOtherTabs { get; set; }

	/// <summary>
	///     Callback invoked for the context menu's "Close to the Right". When no delegate is attached
	///     the strip falls back to raising <see cref="OnTabClosed" /> once per tab.
	/// </summary>
	[Parameter]
	public EventCallback<string> OnCloseTabsToTheRight { get; set; }

	/// <summary>
	///     Callback invoked for the context menu's "Close All". When no delegate is attached the strip
	///     falls back to raising <see cref="OnTabClosed" /> once per tab.
	/// </summary>
	[Parameter]
	public EventCallback OnCloseAllTabs { get; set; }

	/// <summary>
	///     Callback invoked when a tab is reordered via drag and drop.
	/// </summary>
	[Parameter]
	public EventCallback<(string TabId, int NewIndex)> OnTabReordered { get; set; }

	/// <summary>
	///     Callback invoked when a tab's pin state is toggled.
	/// </summary>
	[Parameter]
	public EventCallback<string> OnTabPinToggled { get; set; }

	/// <summary>
	///     Callback invoked when a tab's group membership changes.
	/// </summary>
	[Parameter]
	public EventCallback<(string TabId, string? GroupName)> OnTabGroupChanged { get; set; }

	/// <summary>
	///     Callback invoked when a group's collapse state is toggled.
	/// </summary>
	[Parameter]
	public EventCallback<string> OnGroupCollapseToggled { get; set; }

	/// <summary>
	///     Whether to show close buttons on tabs.
	/// </summary>
	[Parameter]
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>
	///     Whether to show pin buttons on tabs.
	/// </summary>
	[Parameter]
	public bool ShowPinButton { get; set; } = true;

	/// <summary>
	///     Whether drag-and-drop reordering is enabled.
	/// </summary>
	[Parameter]
	public bool AllowDragReorder { get; set; } = true;

	/// <summary>
	///     Whether a right click on a tab opens the built-in context menu. Ignored while
	///     <see cref="OnTabContextMenu" /> has a delegate.
	/// </summary>
	[Parameter]
	public bool AllowContextMenu { get; set; } = true;

	/// <summary>
	///     Raised when a tab is right-clicked, or gets the context-menu key while focused, so an app can
	///     show its own menu (for example through <c>IMokaContextMenuService</c>). While it has a
	///     delegate the built-in menu stays closed and the browser's own menu is suppressed.
	/// </summary>
	[Parameter]
	public EventCallback<MokaItemContextMenuArgs<TabInfo<TValue>>> OnTabContextMenu { get; set; }

	/// <summary>
	///     Whether a middle click closes a tab. Pinned tabs and tabs that are not closable stay open.
	///     Default true.
	/// </summary>
	[Parameter]
	public bool CloseOnMiddleClick { get; set; } = true;

	/// <summary>
	///     Custom context menu items appended to the built-in items, after any items from
	///     <see cref="PluginRegistry" />.
	/// </summary>
	[Parameter]
	public IReadOnlyList<ContextMenuItem>? CustomContextMenuItems { get; set; }

	/// <summary>
	///     Additional CSS class(es) for the tab strip container.
	/// </summary>
	[Parameter]
	public string? TabStripCssClass { get; set; }

	/// <summary>
	///     The plugin registry for collecting plugin-contributed context menu items.
	/// </summary>
	[Parameter]
	public MokaTabPluginRegistry? PluginRegistry { get; set; }

	/// <summary>
	///     The icon provider for customizable tab icons.
	/// </summary>
	[Parameter]
	public MokaTabIconProvider? IconProvider { get; set; }

	/// <summary>
	///     The theme for customizing colors and styles. The strip applies it itself, so it also works
	///     outside a <see cref="MokaTabContainer{TValue}" />.
	/// </summary>
	[Parameter]
	public TabTheme? Theme { get; set; }

	#endregion

	#region Private State

	/// <summary>Sort key for the bucket holding tabs that belong to no group.</summary>
	private const int UngroupedOrder = 0;

	private const string ModulePath = "./_content/Moka.Red.Navigation/moka-tabs.js";

	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

	private TabInfo<TValue>? _draggedTab;
	private bool _showContextMenu;
	private double _contextMenuX;
	private double _contextMenuY;
	private TabInfo<TValue>? _contextMenuTab;
	private IReadOnlyList<ContextMenuItem>? _contextMenuItems;
	private ElementReference _stripRef;
	private IJSObjectReference? _module;
	private DotNetObjectReference<MokaTabStrip<TValue>>? _dotNetRef;
	private string? _lastActiveTabId;
	private string? _pendingScrollTabId;
	private string? _pendingFocusTabId;
	private bool _disposed;

	#endregion

	#region Lifecycle

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (ActiveTabId == _lastActiveTabId)
		{
			return;
		}

		_lastActiveTabId = ActiveTabId;
		_pendingScrollTabId = ActiveTabId;
	}

	/// <summary>
	///     Binds the arrow keys on the first render, scrolls a newly activated tab into view (the strip
	///     scrolls horizontally once the tabs overflow, so the active header can otherwise sit
	///     off-screen), and hands focus back to a tab after the built-in menu closes.
	/// </summary>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_disposed)
		{
			return;
		}

		if (firstRender)
		{
			_dotNetRef = DotNetObjectReference.Create(this);
			await InvokeModuleAsync("MokaTabs.bindTabStrip", _stripRef, _dotNetRef);
		}

		if (_pendingScrollTabId is { } scrollTabId)
		{
			_pendingScrollTabId = null;
			await InvokeModuleAsync("MokaTabs.scrollTabIntoView", scrollTabId, _stripRef);
		}

		if (_pendingFocusTabId is { } focusTabId)
		{
			_pendingFocusTabId = null;
			await InvokeModuleAsync("MokaTabs.restoreTabFocus", _stripRef, focusTabId);
		}
	}

	private async Task InvokeModuleAsync(string identifier, params object?[] args)
	{
		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			await _module.InvokeVoidAsync(identifier, args);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected.
		}
		catch (ObjectDisposedException)
		{
			// JS runtime torn down mid-call.
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException - the circuit went away while awaiting.
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering.
		}
	}

	/// <summary>
	///     Closes a tab for the Delete key. Called by moka-tabs.js, which only calls it for a key
	///     pressed on the tab itself, not on content nested inside it.
	/// </summary>
	/// <param name="tabId">The focused tab.</param>
	[JSInvokable]
	public Task CloseTabFromKeyboard(string tabId)
	{
		TabInfo<TValue>? tab = Tabs.FirstOrDefault(t => t.Id == tabId);
		return tab is not null && CanClose(tab) ? OnTabClosed.InvokeAsync(tab.Id) : Task.CompletedTask;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (_module is not null)
		{
			try
			{
				await _module.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
				// Circuit already gone - nothing to release.
			}

			_module = null;
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		GC.SuppressFinalize(this);
	}

	#endregion

	#region Drag and Drop

	private void HandleDragStart(TabInfo<TValue> tab)
	{
		if (AllowDragReorder && tab.IsDraggable)
		{
			_draggedTab = tab;
		}
	}

	private async Task HandleDrop(TabInfo<TValue> targetTab)
	{
		if (_draggedTab is null || _draggedTab.Id == targetTab.Id)
		{
			_draggedTab = null;
			return;
		}

		int newIndex = IndexOfTab(targetTab);
		await OnTabReordered.InvokeAsync((_draggedTab.Id, newIndex));
		_draggedTab = null;
	}

	#endregion

	#region Middle Click

	private Task HandleMouseUp(MouseEventArgs e, TabInfo<TValue> tab) =>
		e.Button == 1 && CloseOnMiddleClick && CanClose(tab)
			? OnTabClosed.InvokeAsync(tab.Id)
			: Task.CompletedTask;

	#endregion

	#region Context Menu

	private bool ContextMenuEnabled => AllowContextMenu || OnTabContextMenu.HasDelegate;

	private async Task HandleContextMenu(MouseEventArgs e, TabInfo<TValue> tab)
	{
		if (OnTabContextMenu.HasDelegate)
		{
			await OnTabContextMenu.InvokeAsync(new MokaItemContextMenuArgs<TabInfo<TValue>>(tab, e));
			return;
		}

		if (!AllowContextMenu)
		{
			return;
		}

		_contextMenuTab = tab;
		_contextMenuItems = CollectContextMenuItems(tab);
		_contextMenuX = e.ClientX;
		_contextMenuY = e.ClientY;
		_showContextMenu = true;
	}

	private IReadOnlyList<ContextMenuItem>? CollectContextMenuItems(TabInfo<TValue> tab)
	{
		IReadOnlyList<ContextMenuItem> pluginItems = PluginRegistry?.GetContextMenuItems(tab) ?? [];
		if (pluginItems.Count == 0)
		{
			return CustomContextMenuItems;
		}

		return CustomContextMenuItems is { Count: > 0 } custom ? [.. pluginItems, .. custom] : pluginItems;
	}

	private void CloseContextMenu()
	{
		// The menu held focus, and it is about to leave the DOM with it.
		_pendingFocusTabId = _contextMenuTab?.Id;
		_showContextMenu = false;
		_contextMenuTab = null;
		_contextMenuItems = null;
	}

	private async Task HandleContextMenuClose(string tabId) => await OnTabClosed.InvokeAsync(tabId);

	private async Task HandleContextMenuCloseOthers(string tabId)
	{
		if (OnCloseOtherTabs.HasDelegate)
		{
			await OnCloseOtherTabs.InvokeAsync(tabId);
			return;
		}

		await CloseEachAsync(Tabs.Where(t => t.Id != tabId));
	}

	private async Task HandleContextMenuCloseToRight(string tabId)
	{
		if (OnCloseTabsToTheRight.HasDelegate)
		{
			await OnCloseTabsToTheRight.InvokeAsync(tabId);
			return;
		}

		int index = FindTabIndex(tabId);
		if (index < 0)
		{
			return;
		}

		await CloseEachAsync(Tabs.Skip(index + 1));
	}

	private async Task HandleContextMenuCloseAll()
	{
		if (OnCloseAllTabs.HasDelegate)
		{
			await OnCloseAllTabs.InvokeAsync();
			return;
		}

		await CloseEachAsync(Tabs);
	}

	/// <summary>
	///     Fallback for consumers that host the strip directly and only wire <see cref="OnTabClosed" />.
	/// </summary>
	private async Task CloseEachAsync(IEnumerable<TabInfo<TValue>> tabs)
	{
		var ids = tabs.Where(t => t.IsClosable && !t.IsPinned).Select(t => t.Id).ToList();
		foreach (string id in ids)
		{
			await OnTabClosed.InvokeAsync(id);
		}
	}

	private async Task HandleContextMenuTogglePin(string tabId) => await OnTabPinToggled.InvokeAsync(tabId);

	private async Task HandleContextMenuMoveToGroup((string TabId, string? GroupName) args) =>
		await OnTabGroupChanged.InvokeAsync(args);

	#endregion

	#region Styling

	private string StripCssClass => new CssBuilder("moka-tab-strip")
		.AddClass("moka-thin-scrollbar")
		.AddClass(TabStripCssClass)
		.Build();

	private bool IsActive(TabInfo<TValue> tab) => tab.Id == ActiveTabId;

	private bool IsDraggable(TabInfo<TValue> tab) => AllowDragReorder && tab.IsDraggable;

	private static bool CanClose(TabInfo<TValue> tab) => tab.IsClosable && !tab.IsPinned;

	private string GetGroupBorderStyle(TabGroupInfo group)
	{
		string color = group.Color ?? ColorHelper.GetDeterministicColor(group.Name);
		BorderPosition position = group.BorderPosition
		                          ?? Theme?.DefaultGroupBorderPosition
		                          ?? BorderPosition.Left;
		string width = Theme?.GroupBorderWidth ?? "var(--moka-tab-group-border-width, 3px)";
		return $"{ColorHelper.ToCssProperty(position)}: {width} solid {color}";
	}

	private string TabHeaderCssClass(TabInfo<TValue> tab) => new CssBuilder("moka-tab-header")
		.AddClass("moka-tab-header--active", IsActive(tab))
		.AddClass("moka-tab-header--pinned", tab.IsPinned)
		.AddClass(tab.CssClass)
		.Build();

	private static string PinCssClass(TabInfo<TValue> tab) => new CssBuilder("moka-tab-pin")
		.AddClass("moka-tab-pin--active", tab.IsPinned)
		.Build();

	private string? GetActiveTabStyle(TabInfo<TValue> tab)
	{
		if (!IsActive(tab) || string.IsNullOrEmpty(tab.ActiveColor))
		{
			return null;
		}

		return $"--moka-tab-active-color: {tab.ActiveColor}; --moka-tab-active-border-color: {tab.ActiveColor}";
	}

	private int IndexOfTab(TabInfo<TValue> target)
	{
		for (int i = 0; i < Tabs.Count; i++)
		{
			if (Tabs[i] == target)
			{
				return i;
			}
		}

		return -1;
	}

	private int FindTabIndex(string tabId)
	{
		for (int i = 0; i < Tabs.Count; i++)
		{
			if (Tabs[i].Id == tabId)
			{
				return i;
			}
		}

		return -1;
	}

	#endregion

	#region Grouping

	private record TabGroup(TabGroupInfo? GroupInfo, List<TabInfo<TValue>> Tabs);

	/// <summary>
	///     Buckets tabs by group and orders the buckets by <see cref="TabGroupInfo.Order" />. Tabs with
	///     no group form their own bucket sorted at order <see cref="UngroupedOrder" />, so a group with
	///     a negative order renders before it. Ties keep declaration order, with the ungrouped bucket first.
	/// </summary>
	private List<TabGroup> GetGroupedTabs()
	{
		var ungrouped = new List<TabInfo<TValue>>();
		var groupLookup = Groups.ToDictionary(g => g.Name);
		var tabsByGroup = new Dictionary<string, List<TabInfo<TValue>>>();

		foreach (TabInfo<TValue> tab in Tabs)
		{
			if (tab.GroupName is not null && groupLookup.ContainsKey(tab.GroupName))
			{
				if (!tabsByGroup.TryGetValue(tab.GroupName, out List<TabInfo<TValue>>? list))
				{
					list = [];
					tabsByGroup[tab.GroupName] = list;
				}

				list.Add(tab);
			}
			else
			{
				ungrouped.Add(tab);
			}
		}

		var buckets = new List<(int Order, int Sequence, TabGroup Group)>();

		if (ungrouped.Count > 0)
		{
			buckets.Add((UngroupedOrder, -1, new TabGroup(null, ungrouped)));
		}

		for (int i = 0; i < Groups.Count; i++)
		{
			TabGroupInfo group = Groups[i];
			if (tabsByGroup.TryGetValue(group.Name, out List<TabInfo<TValue>>? tabs))
			{
				buckets.Add((group.Order, i, new TabGroup(group, tabs)));
			}
		}

		return buckets
			.OrderBy(b => b.Order)
			.ThenBy(b => b.Sequence)
			.Select(b => b.Group)
			.ToList();
	}

	#endregion
}
