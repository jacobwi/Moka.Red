using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Plugins;
using Moka.Red.Navigation.Tabs.Theming;

namespace Moka.Red.Navigation.Tabs;

/// <summary>
///     Renders the horizontal tab strip with drag/drop, context menus, and group support.
/// </summary>
/// <typeparam name="TValue">The type of value stored by tabs.</typeparam>
public partial class MokaTabStrip<TValue>
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

	private TabInfo<TValue>? _draggedTab;
	private bool _showContextMenu;
	private double _contextMenuX;
	private double _contextMenuY;
	private TabInfo<TValue>? _contextMenuTab;

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
		var otherIds = Tabs.Where(t => t.Id != tabId && t.IsClosable && !t.IsPinned).Select(t => t.Id).ToList();
		foreach (string id in otherIds)
		{
			await OnTabClosed.InvokeAsync(id);
		}
	}

	private async Task HandleContextMenuCloseToRight(string tabId)
	{
		int index = FindTabIndex(tabId);
		var rightIds = Tabs.Skip(index + 1).Where(t => t.IsClosable && !t.IsPinned).Select(t => t.Id).ToList();
		foreach (string id in rightIds)
		{
			await OnTabClosed.InvokeAsync(id);
		}
	}

	private async Task HandleContextMenuCloseAll()
	{
		var closableIds = Tabs.Where(t => t.IsClosable && !t.IsPinned).Select(t => t.Id).ToList();
		foreach (string id in closableIds)
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
		BorderPosition position = group.BorderPosition != BorderPosition.Left
			? group.BorderPosition
			: Theme?.DefaultGroupBorderPosition ?? BorderPosition.Left;
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

	private List<TabGroup> GetGroupedTabs()
	{
		var result = new List<TabGroup>();
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

		if (ungrouped.Count > 0)
		{
			result.Add(new TabGroup(null, ungrouped));
		}

		foreach (TabGroupInfo group in Groups.OrderBy(g => g.Order))
		{
			if (tabsByGroup.TryGetValue(group.Name, out List<TabInfo<TValue>>? tabs))
			{
				result.Add(new TabGroup(group, tabs));
			}
		}

		return result;
	}

	#endregion
}
