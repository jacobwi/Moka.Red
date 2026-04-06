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
	///     Raised when a tab is added.
	/// </summary>
	[Parameter]
	public EventCallback<TabEventArgs> TabAdded { get; set; }

	/// <summary>
	///     Raised when a tab is removed.
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
	private bool _disposed;

	#endregion

	#region Lifecycle

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_sessionState = SessionState;
		_pluginRegistry = PluginRegistry;
		_sessionState.StateChanged += OnSessionStateChanged;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_sessionState.StateChanged -= OnSessionStateChanged;
		await Task.CompletedTask;
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Event Handlers

	private void OnSessionStateChanged(object? sender, EventArgs e)
	{
		if (!_disposed)
		{
			InvokeAsync(StateHasChanged);
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

	private async Task HandleTabClosed(string tabId)
	{
		int index = FindTabIndex(tabId);
		if (await _sessionState.RemoveTabAsync(tabId))
		{
			await TabRemoved.InvokeAsync(new TabEventArgs { TabId = tabId, Index = index });
		}
	}

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
