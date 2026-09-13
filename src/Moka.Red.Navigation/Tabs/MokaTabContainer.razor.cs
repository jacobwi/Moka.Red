using Microsoft.AspNetCore.Components;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Plugins;
using Moka.Red.Navigation.Tabs.Services;

namespace Moka.Red.Navigation.Tabs;

/// <summary>
///     The main container component for the tab system. Manages tab state, rendering, and lifecycle.
/// </summary>
/// <typeparam name="TValue">The type of value stored by each tab.</typeparam>
public partial class MokaTabContainer<TValue> : IAsyncDisposable
{
	#region Rendering

	private RenderFragment RenderTabContent(TabInfo<TValue> tab)
	{
		return builder =>
		{
			if (tab.ContentComponentType is not null)
			{
				builder.OpenComponent(0, typeof(DynamicComponent));
				builder.AddAttribute(1, nameof(DynamicComponent.Type), tab.ContentComponentType);
				if (tab.ContentParameters is not null)
				{
					builder.AddAttribute(2, nameof(DynamicComponent.Parameters), tab.ContentParameters);
				}

				builder.CloseComponent();
			}
			else if (DefaultTabContent is not null)
			{
				builder.AddContent(0, DefaultTabContent(tab));
			}
		};
	}

	#endregion

	#region Injected Services

	[Inject] private IMokaTabSessionState<TValue> SessionState { get; set; } = default!;
	[Inject] private MokaTabPluginRegistry PluginRegistry { get; set; } = default!;
	[Inject] private MokaTabIconProvider IconProvider { get; set; } = default!;

	#endregion

	#region Parameters

	/// <summary>
	///     Gets or sets whether tab content is lazily rendered (only active + keep-alive tabs render their content).
	///     Default is <c>true</c>.
	/// </summary>
	[Parameter]
	public bool LazyRendering { get; set; } = true;

	/// <summary>
	///     Gets or sets whether close buttons are shown on tab headers.
	/// </summary>
	[Parameter]
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>
	///     Gets or sets whether pin buttons are shown on tab headers.
	/// </summary>
	[Parameter]
	public bool ShowPinButton { get; set; } = true;

	/// <summary>
	///     Gets or sets whether tabs can be reordered via drag and drop.
	/// </summary>
	[Parameter]
	public bool AllowDragReorder { get; set; } = true;

	/// <summary>
	///     Gets or sets whether right-click context menus are enabled on tabs.
	/// </summary>
	[Parameter]
	public bool AllowContextMenu { get; set; } = true;

	/// <summary>
	///     Gets or sets custom context menu items added to all tabs.
	/// </summary>
	[Parameter]
	public IReadOnlyList<ContextMenuItem>? CustomContextMenuItems { get; set; }

	/// <summary>
	///     Gets or sets the default content template for tabs that do not specify a ContentComponentType.
	///     Receives the tab info as context.
	/// </summary>
	[Parameter]
	public RenderFragment<TabInfo<TValue>>? DefaultTabContent { get; set; }

	/// <summary>
	///     Gets or sets additional CSS class(es) for the container element.
	/// </summary>
	[Parameter]
	public string? CssClass { get; set; }

	/// <summary>
	///     Gets or sets additional CSS class(es) for the tab strip.
	/// </summary>
	[Parameter]
	public string? TabStripCssClass { get; set; }

	/// <summary>
	///     Gets or sets a theme for customizing colors and styles of the tab system.
	/// </summary>
	[Parameter]
	public TabTheme? Theme { get; set; }

	/// <summary>
	///     Gets or sets additional HTML attributes rendered on the container element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object>? AdditionalAttributes { get; set; }

	#endregion

	#region Event Callbacks

	/// <summary>
	///     Raised when a tab appears in the session state, whoever added it. <c>Index</c> is the tab's
	///     position in the new list.
	/// </summary>
	[Parameter]
	public EventCallback<TabEventArgs> TabAdded { get; set; }

	/// <summary>
	///     Raised when a tab disappears from the session state, including bulk closes. <c>Index</c> is
	///     the tab's position before the removal.
	/// </summary>
	[Parameter]
	public EventCallback<TabEventArgs> TabRemoved { get; set; }

	/// <summary>
	///     Raised when a tab is activated.
	/// </summary>
	[Parameter]
	public EventCallback<TabActivatedEventArgs> TabActivated { get; set; }

	/// <summary>
	///     Raised when a tab is reordered.
	/// </summary>
	[Parameter]
	public EventCallback<TabReorderedEventArgs> TabReordered { get; set; }

	/// <summary>
	///     Raised when a tab's group membership changes.
	/// </summary>
	[Parameter]
	public EventCallback<TabGroupChangedEventArgs> TabGroupChanged { get; set; }

	#endregion

	#region Private State

	private IMokaTabSessionState<TValue> _sessionState = default!;
	private MokaTabPluginRegistry _pluginRegistry = default!;
	private List<string> _knownTabIds = [];
	private bool _disposed;

	#endregion

	#region Lifecycle

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_sessionState = SessionState;
		_pluginRegistry = PluginRegistry;
		_sessionState.StateChanged += OnSessionStateChanged;

		// Tabs that already exist when the container mounts are not "added" by it.
		_knownTabIds = _sessionState.Tabs.Select(t => t.Id).ToList();
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return ValueTask.CompletedTask;
		}

		_disposed = true;
		_sessionState.StateChanged -= OnSessionStateChanged;
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}

	#endregion

	#region Event Handlers

	private void OnSessionStateChanged(object? sender, EventArgs e)
	{
		if (_disposed)
		{
			return;
		}

		InvokeAsync(async () =>
		{
			await RaiseTabDiffEventsAsync();
			StateHasChanged();
		});
	}

	/// <summary>
	///     Tabs are added and removed straight through <see cref="IMokaTabSessionState{TValue}" />, which
	///     only reports a generic state change. Diffing the id list is what lets the container raise
	///     <see cref="TabAdded" /> and <see cref="TabRemoved" /> for every path, bulk closes included.
	/// </summary>
	private async Task RaiseTabDiffEventsAsync()
	{
		List<string> current = _sessionState.Tabs.Select(t => t.Id).ToList();
		List<string> previous = _knownTabIds;
		_knownTabIds = current;

		if (!TabAdded.HasDelegate && !TabRemoved.HasDelegate)
		{
			return;
		}

		var currentSet = new HashSet<string>(current, StringComparer.Ordinal);
		var previousSet = new HashSet<string>(previous, StringComparer.Ordinal);

		if (TabRemoved.HasDelegate)
		{
			for (int i = 0; i < previous.Count; i++)
			{
				if (!currentSet.Contains(previous[i]))
				{
					await TabRemoved.InvokeAsync(new TabEventArgs { TabId = previous[i], Index = i });
				}
			}
		}

		if (TabAdded.HasDelegate)
		{
			for (int i = 0; i < current.Count; i++)
			{
				if (!previousSet.Contains(current[i]))
				{
					await TabAdded.InvokeAsync(new TabEventArgs { TabId = current[i], Index = i });
				}
			}
		}
	}

	private async Task HandleTabActivated(string tabId)
	{
		string? previousTabId = _sessionState.ActiveTabId;
		await _sessionState.ActivateTabAsync(tabId);
		await TabActivated.InvokeAsync(new TabActivatedEventArgs
		{
			TabId = tabId,
			Index = FindTabIndex(tabId),
			PreviousTabId = previousTabId
		});
	}

	// TabRemoved is raised by the state diff in RaiseTabDiffEventsAsync, so these only drive state.
	// EventCallback<string> needs a plain Task, and the bool result (whether a plugin
	// vetoed the close) is not actionable here, so await and discard it.
	private async Task HandleTabClosed(string tabId) => await _sessionState.RemoveTabAsync(tabId);

	private Task HandleCloseOtherTabs(string tabId) => _sessionState.CloseOtherTabsAsync(tabId);

	private Task HandleCloseTabsToTheRight(string tabId) => _sessionState.CloseTabsToTheRightAsync(tabId);

	private Task HandleCloseAllTabs() => _sessionState.CloseAllTabsAsync();

	private async Task HandleTabReordered((string TabId, int NewIndex) args)
	{
		int oldIndex = FindTabIndex(args.TabId);
		await _sessionState.ReorderTabAsync(args.TabId, args.NewIndex);
		await TabReordered.InvokeAsync(new TabReorderedEventArgs
		{
			TabId = args.TabId,
			Index = args.NewIndex,
			OldIndex = oldIndex,
			NewIndex = args.NewIndex
		});
	}

	private void HandleTabPinToggled(string tabId) => _sessionState.TogglePin(tabId);

	private async Task HandleTabGroupChanged((string TabId, string? GroupName) args)
	{
		TabInfo<TValue>? tab = _sessionState.GetTab(args.TabId);
		string? oldGroup = tab?.GroupName;
		await _sessionState.SetTabGroupAsync(args.TabId, args.GroupName);
		await TabGroupChanged.InvokeAsync(new TabGroupChangedEventArgs
		{
			TabId = args.TabId,
			Index = FindTabIndex(args.TabId),
			OldGroup = oldGroup,
			NewGroup = args.GroupName
		});
	}

	private int FindTabIndex(string tabId)
	{
		IReadOnlyList<TabInfo<TValue>> tabs = _sessionState.Tabs;
		for (int i = 0; i < tabs.Count; i++)
		{
			if (tabs[i].Id == tabId)
			{
				return i;
			}
		}

		return -1;
	}

	private void HandleGroupCollapseToggled(string groupName) => _sessionState.ToggleGroupCollapse(groupName);

	#endregion
}
