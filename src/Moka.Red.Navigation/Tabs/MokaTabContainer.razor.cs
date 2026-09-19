using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Interactions;
using Moka.Red.Core.Utilities;
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

	private string ContainerCssClass => new CssBuilder("moka-tab-container")
		.AddClass(CssClass)
		.Build();

	private static string ContentCssClass(bool isActive) => new CssBuilder("moka-tab-content")
		.AddClass("moka-tab-content--active", isActive)
		.AddClass("moka-tab-content--hidden", !isActive)
		.Build();

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

	// Resolved only when StorageKey is set, so apps that never persist need no provider registered.
	[Inject] private IServiceProvider Services { get; set; } = default!;

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
	///     Gets or sets whether a right click on a tab opens the built-in context menu. Ignored while
	///     <see cref="OnTabContextMenu" /> has a delegate.
	/// </summary>
	[Parameter]
	public bool AllowContextMenu { get; set; } = true;

	/// <summary>
	///     Raised when a tab is right-clicked, or gets the context-menu key while focused, so an app can
	///     show its own menu. While it has a delegate the built-in menu stays closed. See
	///     <see cref="MokaTabStrip{TValue}.OnTabContextMenu" />.
	/// </summary>
	[Parameter]
	public EventCallback<MokaItemContextMenuArgs<TabInfo<TValue>>> OnTabContextMenu { get; set; }

	/// <summary>
	///     Gets or sets whether a middle click closes a tab. Pinned tabs and tabs that are not closable
	///     stay open. Default true.
	/// </summary>
	[Parameter]
	public bool CloseOnMiddleClick { get; set; } = true;

	/// <summary>
	///     Saves the open tabs under this key after every change, and restores them the first time a
	///     container renders for the session. Uses the registered <see cref="ITabStorageProvider" />;
	///     <c>AddMokaTabs</c> registers one backed by the browser's sessionStorage. A tab's
	///     <c>Value</c> is saved only when <see cref="IMokaTabSessionState{TValue}.ValueSerializer" />
	///     is set, and <see cref="TabInfo{TValue}.ContentParameters" /> are never saved. Read on the
	///     first render. Default <c>null</c>: nothing is saved.
	/// </summary>
	[Parameter]
	public string? StorageKey { get; set; }

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

	// Session states that a container has already restored. A container mounted again in the same
	// circuit, after navigating away and back, must not swap the live tabs for saved copies, which
	// have lost their ContentParameters.
	private static readonly ConditionalWeakTable<IMokaTabSessionState<TValue>, string> RestoredStates = new();

	private ITabStorageProvider? _storage;
	private string? _storageKey;

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
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || string.IsNullOrEmpty(StorageKey) ||
		    Services.GetService<ITabStorageProvider>() is not { } storage)
		{
			return;
		}

		string key = StorageKey;
		bool restored = false;
		if (RestoredStates.TryAdd(_sessionState, key))
		{
			string? json = null;
			if (!await TryStorageAsync(async () => json = await storage.LoadAsync(key)))
			{
				// Storage is unreachable. Saving over it now could replace a session that is still there.
				return;
			}

			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					await _sessionState.RestoreStateAsync(json);
					restored = true;
				}
				catch (JsonException)
				{
					// Unreadable, so the save below replaces it.
				}
			}
		}

		_storage = storage;
		_storageKey = key;

		// Restored tabs are already what storage holds. Otherwise store what is open now, which may
		// include changes made while no container was listening.
		if (!restored)
		{
			await SaveStateAsync();
		}
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
			await SaveStateAsync();
		});
	}

	private async Task SaveStateAsync()
	{
		if (_storage is null || _storageKey is null)
		{
			return;
		}

		string json = _sessionState.SerializeState();
		ITabStorageProvider storage = _storage;
		string key = _storageKey;
		await TryStorageAsync(() => storage.SaveAsync(key, json));
	}

	// Storage lives in the browser: the circuit can drop, the page can block storage, and the
	// provider is disposed with its scope. None of that should break the tabs.
	private static async Task<bool> TryStorageAsync(Func<Task> action)
	{
		try
		{
			await action();
			return true;
		}
		catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException
			                           or ObjectDisposedException or InvalidOperationException)
		{
			return false;
		}
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
