using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
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
	///     Callback invoked when a tab's close button is clicked.
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
	///     Whether right-click context menus are enabled.
	/// </summary>
	[Parameter]
	public bool AllowContextMenu { get; set; } = true;

	/// <summary>
	///     Custom context menu items appended to the built-in items.
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
	///     The theme for customizing colors and styles.
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
	private IJSObjectReference? _module;
	private string? _lastActiveTabId;
	private string? _pendingScrollTabId;
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
	///     Scrolls a newly activated tab into view; the strip scrolls horizontally once the tabs
	///     overflow, so the active header can otherwise sit off-screen.
	/// </summary>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_pendingScrollTabId is null || _disposed)
		{
			return;
		}

		string tabId = _pendingScrollTabId;
		_pendingScrollTabId = null;

		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			await _module.InvokeVoidAsync("MokaTabs.scrollTabIntoView", tabId);
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

	#region Context Menu

	private void HandleContextMenu(MouseEventArgs e, TabInfo<TValue> tab)
	{
		if (!AllowContextMenu)
		{
			return;
		}

		_contextMenuTab = tab;
		_contextMenuX = e.ClientX;
		_contextMenuY = e.ClientY;
		_showContextMenu = true;
	}

	private void CloseContextMenu()
	{
		_showContextMenu = false;
		_contextMenuTab = null;
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

	private string GetGroupBorderStyle(TabGroupInfo group)
	{
		string color = group.Color ?? ColorHelper.GetDeterministicColor(group.Name);
		BorderPosition position = group.BorderPosition
		                          ?? Theme?.DefaultGroupBorderPosition
		                          ?? BorderPosition.Left;
		string width = Theme?.GroupBorderWidth ?? "var(--moka-tab-group-border-width, 3px)";
		return $"{ColorHelper.ToCssProperty(position)}: {width} solid {color}";
	}

	private static string GetTabHeaderClass(TabInfo<TValue> tab, bool isActive)
	{
		string cls = "moka-tab-header";
		if (isActive)
		{
			cls += " moka-tab-header--active";
		}

		if (tab.IsPinned)
		{
			cls += " moka-tab-header--pinned";
		}

		if (!string.IsNullOrEmpty(tab.CssClass))
		{
			cls += " " + tab.CssClass;
		}

		return cls;
	}

	private static string? GetActiveTabStyle(TabInfo<TValue> tab, bool isActive)
	{
		if (!isActive || string.IsNullOrEmpty(tab.ActiveColor))
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
