using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     Panels docked around a main area, as in an IDE: groups of tabbed <see cref="MokaDockPanel" />s in rows and
///     columns at any depth, dragged to any edge or into another group, floated in windows, pinned to side bars,
///     maximized, closed and opened from code, saved and restored. The markup declares where things start; a
///     <see cref="MokaDockState" /> holds where they are, bindable through <see cref="State" />. Each panel's content
///     renders once, in the same place in the render tree wherever the state puts it, and is drawn over its group's
///     slot with CSS anchor positioning, so it keeps its component state across every move.
/// </summary>
public partial class MokaDockLayout : MokaComponentBase
{
	internal const string ModulePath = "./_content/Moka.Red.Layout/DockLayout/MokaDockLayout.razor.js";
	internal const string ContentGroupId = "content";

	private readonly List<MokaDockPart> _parts = [];
	private readonly Dictionary<string, MokaDockPanel> _panels = new(StringComparer.Ordinal);
	private readonly Dictionary<MokaDockPanel, string> _panelIds = [];
	private readonly Dictionary<string, MokaDockPanelOptions> _opened = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> _anchors = new(StringComparer.Ordinal);

	private MokaDockState _defaults = MokaDockState.Empty;
	private MokaDockState _state = MokaDockState.Empty;
	private MokaDockState? _lastStateParameter;
	private string? _signature;
	private bool _structureDirty = true;
	private bool _stateFromOutside;
	private bool _stateChanged;
	private bool _loaded;
	private bool _bound;
	private int _autoIds;
	private DotNetObjectReference<MokaDockLayout>? _dotNetRef;
	private ElementReference _root;
	private MokaDockFrame? _frame;
	private MokaDockAnnouncer? _announcer;
	private MokaDockOpenedPanels? _openedHost;
	private object? _lastViewKey;
	private MokaDockNarrowMode _lastNarrowMode;
	private object? _boundOptions;

	/// <summary>The layout's parts: <see cref="MokaDockRow" />, <see cref="MokaDockColumn" />, <see cref="MokaDockGroup" />, <see cref="MokaDockPanel" /> and <see cref="MokaDockContent" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Where everything is. Two-way bindable: the layout raises <see cref="StateChanged" /> after every change the
	///     person makes. A state passed in is fitted to the declared panels: panels it doesn't know join it where the
	///     markup has them, and panels the markup doesn't have are dropped.
	/// </summary>
	[Parameter]
	public MokaDockState? State { get; set; }

	/// <summary>Raised with the new state after the person docks, moves, resizes, floats, pins or closes something.</summary>
	[Parameter]
	public EventCallback<MokaDockState> StateChanged { get; set; }

	/// <summary>
	///     Saves the state under this key after every change and restores it on the first interactive render, through
	///     the registered <see cref="IMokaDockStorage" />, else the browser's local storage. Every panel needs an
	///     <c>Id</c> for this to find it again.
	/// </summary>
	[Parameter]
	public string? StorageKey { get; set; }

	/// <summary>The app's own version of its layout, written into the saved state as <see cref="MokaDockState.AppVersion" />.</summary>
	[Parameter]
	public int StateVersion { get; set; }

	/// <summary>
	///     Brings a saved state from an older <see cref="StateVersion" /> up to date, such as after a panel was renamed.
	///     Gets the saved state and its version; null drops the saved state and starts from the markup.
	/// </summary>
	[Parameter]
	public Func<MokaDockState, int, MokaDockState?>? MigrateState { get; set; }

	/// <summary>
	///     How the layout is drawn. Unset follows the theme's <see cref="MokaTheme.Docks" />. The original design
	///     (<see cref="MokaLook.Matrix" />) always draws <see cref="MokaDockLook.Classic" />.
	/// </summary>
	[Parameter]
	public MokaDockLook? Look { get; set; }

	/// <summary>How groups show their tabs, unless a <see cref="MokaDockGroup" /> says otherwise.</summary>
	[Parameter]
	public MokaDockTabStyle TabStyle { get; set; } = MokaDockTabStyle.Underline;

	/// <summary>What shows where a dragged panel will land.</summary>
	[Parameter]
	public MokaDockDropIndicator DropIndicator { get; set; } = MokaDockDropIndicator.Zones;

	/// <summary>The buttons group headers show.</summary>
	[Parameter]
	public MokaDockControls Controls { get; set; } = MokaDockControls.Default;

	/// <summary>Whether tabs and groups can be dragged to dock them elsewhere. Default true.</summary>
	[Parameter]
	public bool AllowDragging { get; set; } = true;

	/// <summary>Whether a pinned group opens when its tab in the side bar is hovered, as well as when it is clicked. Default true.</summary>
	[Parameter]
	public bool AutoHideOnHover { get; set; } = true;

	/// <summary>How the layout shows its panels while it is narrower than <see cref="NarrowBelow" />.</summary>
	[Parameter]
	public MokaDockNarrowMode NarrowMode { get; set; } = MokaDockNarrowMode.Sheets;

	/// <summary>
	///     The layout's own width, as a CSS length, below which it switches to <see cref="NarrowMode" />. Default
	///     <c>40rem</c>. Null keeps the wide layout at every width.
	/// </summary>
	[Parameter]
	public string? NarrowBelow { get; set; } = "40rem";

	/// <summary>Named layouts the person can switch between with <see cref="ApplyPresetAsync" />.</summary>
	[Parameter]
	public IReadOnlyList<MokaDockPreset>? Presets { get; set; }

	/// <summary>Every word the layout writes.</summary>
	[Parameter]
	public MokaDockText? Text { get; set; }

	/// <summary>Raised when a panel is opened from code or shown again after it was closed.</summary>
	[Parameter]
	public EventCallback<MokaDockPanelChange> PanelOpened { get; set; }

	/// <summary>Raised when a panel becomes the one showing in its group.</summary>
	[Parameter]
	public EventCallback<MokaDockPanelChange> PanelActivated { get; set; }

	/// <summary>Raised when a panel is closed.</summary>
	[Parameter]
	public EventCallback<MokaDockPanelChange> PanelClosed { get; set; }

	/// <summary>Raised when a panel moves: to another group, a new group, a floating window, a side bar or another window.</summary>
	[Parameter]
	public EventCallback<MokaDockPanelChange> PanelMoved { get; set; }

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <summary>Where everything is now.</summary>
	public MokaDockState CurrentState => _state;

	/// <summary>Where everything starts, as the markup declares it. <see cref="ResetAsync" /> goes back to it.</summary>
	public MokaDockState DefaultState => _defaults;

	internal MokaDockText Words => Text ?? MokaDockText.Default;

	internal MokaDockLook ResolvedLook =>
		CascadedTheme is { Look: MokaLook.Matrix } ? MokaDockLook.Classic : Look ?? CascadedTheme?.Docks ?? MokaDockLook.Classic;

	internal int ViewVersion { get; private set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-layout";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(LookClass)
		.AddClass(Class)
		.Build();

	private string? LookClass => ResolvedLook switch
	{
		MokaDockLook.Ide => "moka-dock-layout--ide",
		MokaDockLook.Cards => "moka-dock-layout--cards",
		MokaDockLook.Minimal => "moka-dock-layout--minimal",
		MokaDockLook.Terminal => "moka-dock-layout--terminal",
		_ => null
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!ReferenceEquals(State, _lastStateParameter))
		{
			_lastStateParameter = State;
			if (State is not null && !ReferenceEquals(State, _state))
			{
				_stateFromOutside = true;
				_structureDirty = true;
			}
		}

		if (NarrowMode != _lastNarrowMode)
		{
			_lastNarrowMode = NarrowMode;
			_sheetPanel = null;
		}

		// Where each host is drawn depends on the narrow mode and on whether its group shows tabs (the look and the tab
		// style), so the placements are worked out again before the frame draws the new slots.
		object viewKey = (ResolvedLook, TabStyle, Controls, AllowDragging, Text, NarrowMode, DropIndicator);
		if (!viewKey.Equals(_lastViewKey))
		{
			_lastViewKey = viewKey;
			ViewVersion++;
			Recompute();
			_frame?.Refresh();
		}
		else if (_stateFromOutside)
		{
			_frame?.Refresh();
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_dotNetRef = DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(ModulePath, "bind", _root, _dotNetRef, ModuleOptions());
			_bound = true;
			await LoadStoredStateAsync();
			return;
		}

		if (_bound && !Equals(_boundOptions, OptionsKey()))
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "update", _root, ModuleOptions());
		}
	}

	private object OptionsKey() => (NarrowBelow, DropIndicator, AllowDragging, AutoHideOnHover);

	private Dictionary<string, object?> ModuleOptions()
	{
		_boundOptions = OptionsKey();
		return new Dictionary<string, object?>
		{
			["narrowBelow"] = NarrowBelow,
			["indicator"] = DropIndicator == MokaDockDropIndicator.Compass ? "compass" : "zones",
			["drag"] = AllowDragging,
			["hover"] = AutoHideOnHover
		};
	}

	internal void AttachFrame(MokaDockFrame frame) => _frame = frame;

	internal void AttachAnnouncer(MokaDockAnnouncer announcer) => _announcer = announcer;

	internal void AttachOpened(MokaDockOpenedPanels host) => _openedHost = host;

	internal void Register(MokaDockPart part)
	{
		if (part.Container is { } container && !ReferenceEquals(container, part))
		{
			container.Parts.Add(part);
		}
		else
		{
			_parts.Add(part);
		}

		if (part is MokaDockPanel panel)
		{
			string id = panel.Id ?? string.Create(CultureInfo.InvariantCulture, $"panel-{++_autoIds}");
			_panelIds[panel] = id;
			_panels[id] = panel;
		}

		_structureDirty = true;
		_frame?.Refresh();
	}

	internal void Unregister(MokaDockPart part)
	{
		if (part.Container is { } container)
		{
			container.Parts.Remove(part);
		}

		_parts.Remove(part);
		if (part is MokaDockPanel panel && _panelIds.Remove(panel, out string? id)
		    && _panels.TryGetValue(id, out MokaDockPanel? registered) && ReferenceEquals(registered, panel))
		{
			_panels.Remove(id);
		}

		_structureDirty = true;
		_frame?.Refresh();
	}

	internal void DeclarationChanged(MokaDockPart part)
	{
		if (part is MokaDockPanel panel && panel.Id is { } id && _panelIds.TryGetValue(panel, out string? known) && known != id)
		{
			_panels.Remove(known);
			_panelIds[panel] = id;
			_panels[id] = panel;
		}

		_structureDirty = true;
		_frame?.Refresh();
	}

	internal string IdOf(MokaDockPanel panel) => _panelIds.TryGetValue(panel, out string? id) ? id : panel.Id ?? "";

	internal MokaDockPanel? PanelFor(string id) => _panels.GetValueOrDefault(id);

	internal IEnumerable<MokaDockPanelOptions> OpenedPanels => _opened.Values;

	// Called at the start of every frame render, after the parts that rendered before it have registered. Rebuilds the
	// starting state when the declared structure changed, and fits the live state to it.
	internal void EnsureCurrent()
	{
		if (!_structureDirty)
		{
			return;
		}

		_structureDirty = false;
		string signature = Signature();
		bool structureChanged = signature != _signature;
		_signature = signature;
		if (structureChanged)
		{
			_defaults = BuildDefaults();
		}

		MokaDockState next = _state;
		if (_stateFromOutside && State is not null)
		{
			_stateFromOutside = false;
			_stateChanged = true;
			next = Fit(State);
		}
		else if (structureChanged)
		{
			next = Fit(_stateChanged ? _state : _defaults);
		}

		SetState(next, notify: false);
	}

	private IEnumerable<string> KnownPanels() => _panels.Keys.Union(_opened.Keys);

	// A state from elsewhere (the markup, a preset, a saved or bound state) fitted to the panels there are now, with
	// what is open now carried over: the markup has no place for a panel opened from code, and only the live state
	// knows which panels another window shows.
	private MokaDockState Fit(MokaDockState state) => state.Reconcile(_defaults, [.. KnownPanels()], _state);

	private string Signature()
	{
		StringBuilder sb = new();
		foreach (MokaDockPart part in _parts)
		{
			Describe(sb, part);
		}

		foreach (string id in _opened.Keys)
		{
			sb.Append("o:").Append(id).Append(';');
		}

		return sb.ToString();
	}

	private void Describe(StringBuilder sb, MokaDockPart part)
	{
		switch (part)
		{
			case MokaDockPanel panel:
				sb.Append("p:").Append(IdOf(panel)).Append(':').Append(panel.Dock).Append(':').Append(panel.Visible)
					.Append(':').Append(panel.AutoHide).Append(';');
				break;
			case MokaDockContent:
				sb.Append("c;");
				break;
			case MokaDockGroup group:
				sb.Append("g(").Append(group.Id).Append(':').Append(group.Dock).Append(':').Append(group.AutoHide).Append(':');
				foreach (MokaDockPart child in group.Parts)
				{
					Describe(sb, child);
				}

				sb.Append(')');
				break;
			case MokaDockContainer container:
				sb.Append(container is MokaDockRow ? "r(" : "k(").Append(container.Id).Append(':');
				foreach (MokaDockPart child in container.Parts)
				{
					Describe(sb, child);
				}

				sb.Append(')');
				break;
		}
	}

	private async Task LoadStoredStateAsync()
	{
		if (StorageKey is not { } key || _loaded)
		{
			return;
		}

		_loaded = true;
		string? json = await LoadAsync(key);
		if (MokaDockState.FromJson(json) is not { } saved)
		{
			return;
		}

		MokaDockState? current = saved;
		if (saved.AppVersion != StateVersion)
		{
			current = MigrateState is not null
				? MigrateState(saved, saved.AppVersion)
				: saved.AppVersion < StateVersion ? saved : null;
		}

		if (current is null)
		{
			return;
		}

		await CommitAsync(Fit(current with { AppVersion = StateVersion }), save: false);
	}

	private async Task<string?> LoadAsync(string key)
	{
		if (Services.GetService<IMokaDockStorage>() is { } storage)
		{
			return await storage.LoadAsync(key);
		}

		return await SafeModuleInvokeAsync<string?>(ModulePath, "readStorage", key);
	}

	private async Task SaveAsync(MokaDockState state)
	{
		if (StorageKey is not { } key)
		{
			return;
		}

		string json = (state with { AppVersion = StateVersion }).ToJson();
		if (Services.GetService<IMokaDockStorage>() is { } storage)
		{
			await storage.SaveAsync(key, json);
			return;
		}

		await SafeModuleInvokeVoidAsync(ModulePath, "writeStorage", key, json);
	}

	private async Task ForgetSavedAsync()
	{
		if (StorageKey is not { } key)
		{
			return;
		}

		if (Services.GetService<IMokaDockStorage>() is { } storage)
		{
			await storage.RemoveAsync(key);
			return;
		}

		await SafeModuleInvokeVoidAsync(ModulePath, "removeStorage", key);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		foreach (MokaDockWindowSession session in _windows.Values)
		{
			session.ReturnRequested -= OnWindowReturnRequested;
			session.End();
		}

		_windows.Clear();
		if (_bound)
		{
			_bound = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbind", _root);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
