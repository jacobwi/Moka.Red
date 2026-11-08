using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Hotkeys;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.AppShell;

/// <summary>
///     The frame around every page of an app: a side menu and a main area, and on a phone a top bar with the menu in a
///     drawer. Rows or rail is decided in the browser, before first paint, so a prerendered page doesn't jump. Needs an
///     interactive layout, and one <c>MokaHotkeyHost</c> in it for the <c>[</c> key.
/// </summary>
public partial class MokaAppShell : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Navigation/AppShell/MokaAppShell.razor.js";
	private const string SpaceModule = "./_content/Moka.Red.Core/moka-space.js";

	// The first thing inside the root of a static render, so it runs as the parser reaches it, with the root open and
	// nothing in it drawn. The module has the same few lines: this can't import anything and has to be done before .NET
	// has started. It writes to <html>, which Blazor never replaces, so the state outlives the prerendered page when the
	// interactive render takes its place, and that render needs no script. It must not have one: Blazor recreates a
	// script in markup by setting its text, which a Trusted Types policy blocks, and it would run after the paint anyway.
	private const string BootScript =
		"(function(){var s=document.currentScript.parentElement,k=null;" +
		"try{k=sessionStorage.getItem('moka-app-shell-rail')}catch(e){}" +
		"var c=k||s.getAttribute('data-moka-rail-choice');" +
		"document.documentElement.toggleAttribute('data-moka-rail',!matchMedia('(max-width: 720px)').matches&&" +
		"(c==='collapsed'||(c!=='expanded'&&matchMedia('(max-width: 1279.98px)').matches)))})()";

	private readonly string _key = $"moka-app-shell-{Guid.NewGuid():N}";
	private readonly string _navId;

	private bool _drawerOpen;
	private bool _drawerTold;
	private IDisposable? _hotkey;
	private MokaRailChoice? _lastRailParameter;
	private MokaRailChoice _rail;
	private bool _railChosen;
	private ElementReference _root;
	private DotNetObjectReference<MokaAppShell>? _self;
	private IJSObjectReference? _space;
	private ElementReference _topBar;

	/// <summary>Creates the shell.</summary>
	public MokaAppShell() => _navId = $"{_key}-nav";

	/// <summary>The page.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     The screens, in order. Groups show in the order they first appear. The entry the address belongs to is marked
	///     as the current page, and names the top bar on a phone.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaNavEntry> Entries { get; set; } = [];

	/// <summary>
	///     The logo and name over the menu. Give the text the class <c>moka-app-shell__brand-text</c> and it hides on the
	///     rail, leaving the logo. Inside it, <c>moka-app-shell__brand-name</c> and <c>moka-app-shell__brand-line</c> draw
	///     the name and the mono line under it.
	/// </summary>
	[Parameter]
	public RenderFragment? Brand { get; set; }

	/// <summary>A word on how things are, under the name in the phone's drawer, where it replaces the mono line.</summary>
	[Parameter]
	public RenderFragment? Status { get; set; }

	/// <summary>What's pinned under the list, usually a <see cref="MokaAccountCard" />.</summary>
	[Parameter]
	public RenderFragment? Account { get; set; }

	/// <summary>The right end of the phone's top bar, such as a search button.</summary>
	[Parameter]
	public RenderFragment? TopBarEnd { get; set; }

	/// <summary>The name in the phone's top bar. Null takes it from the entry the address belongs to.</summary>
	[Parameter]
	public string? TopBarTitle { get; set; }

	/// <summary>
	///     How the side menu is drawn, on anything wider than a phone. A new value also forgets a flip made with the
	///     <c>[</c> key in this tab.
	/// </summary>
	[Parameter]
	public MokaRailChoice Rail { get; set; } = MokaRailChoice.Automatic;

	/// <summary>
	///     Raised when the person flips the rail, and on first render when an earlier flip in this tab is still kept.
	/// </summary>
	[Parameter]
	public EventCallback<MokaRailChoice> RailChanged { get; set; }

	/// <summary>The name of the navigation, for screen readers.</summary>
	[Parameter]
	public string NavLabel { get; set; } = "Main";

	/// <summary>The name of the top bar's menu button.</summary>
	[Parameter]
	public string OpenMenuLabel { get; set; } = "Open the menu";

	/// <summary>The name of the drawer's close button.</summary>
	[Parameter]
	public string CloseMenuLabel { get; set; } = "Close the menu";

	/// <summary>
	///     The nonce of a strict Content-Security-Policy, for the small script that sets the rail before first paint. Only
	///     a static render (prerendering) writes that script; an interactive render writes none, so it needs no nonce.
	///     Without it such a policy blocks the script, and the rail settles a moment later, when the module loads.
	/// </summary>
	[Parameter]
	public string? Nonce { get; set; }

	/// <summary>
	///     The rail shows each screen's name under its icon, and widens to fit (80 px). A name that doesn't fit is cut
	///     with an ellipsis, and the tag beside an item then shows only for a pill or a cut name. Off by default.
	/// </summary>
	[Parameter]
	public bool RailLabels { get; set; }

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-app-shell";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-app-shell--labels", RailLabels)
		.AddClass(Class)
		.Build();

	private string HeadClass => new CssBuilder("moka-app-shell__head")
		.AddClass("moka-app-shell__head--status", Status is not null)
		.Build();

	private string RailChoiceValue => _rail switch
	{
		MokaRailChoice.Expanded => "expanded",
		MokaRailChoice.Collapsed => "collapsed",
		_ => "automatic"
	};

	private string BootMarkup => string.IsNullOrEmpty(Nonce)
		? $"<script>{BootScript}</script>"
		: $"<script nonce=\"{CssValues.EscapeXml(Nonce)}\">{BootScript}</script>";

	private MokaNavEntry? CurrentEntry => MokaNavEntry.Find(Entries, Navigation.ToBaseRelativePath(Navigation.Uri));

	// Only while the drawer is out, so a click on a wider screen doesn't redraw the whole shell.
	private EventCallback<MouseEventArgs> PickCallback => _drawerOpen
		? EventCallback.Factory.Create<MouseEventArgs>(this, CloseDrawerAsync)
		: default;

	/// <summary>Flips between the rail and the full menu for this tab, as the <c>[</c> key does. Does nothing on a phone.</summary>
	/// <returns>A task that completes when the rail has flipped.</returns>
	public async Task FlipRailAsync()
	{
		string? flipped = await SafeModuleInvokeAsync<string>(ModulePath, "flipRail", _key);
		if (ParseRail(flipped) is { } choice)
		{
			await SettleRailAsync(choice);
		}
	}

	// Only the browser knows: the rail follows the window's width, and a flip kept for the tab.
	internal ValueTask<bool> ShowsRailAsync() => SafeModuleInvokeAsync<bool>(ModulePath, "showsRail", _root);

	/// <summary>Closes the phone's drawer, if it's out.</summary>
	/// <returns>A task that completes when the shell has been told.</returns>
	[JSInvokable]
	public Task CloseDrawerAsync() => InvokeAsync(() =>
	{
		if (_drawerOpen)
		{
			_drawerOpen = false;
			StateHasChanged();
		}
	});

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		Navigation.LocationChanged += HandleLocationChanged;

		// Looked up rather than injected: [Inject] throws in an app that never registered the service.
		_hotkey = Services.GetService<IMokaHotkeyService>()?.Register(new MokaHotkey
		{
			Id = "moka-app-shell-rail",
			Keys = "[",
			Description = "Flip the side menu between names and icons",
			Group = "Navigation",
			OnInvoke = () => InvokeAsync(FlipRailAsync)
		});
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_lastRailParameter == Rail)
		{
			return;
		}

		bool first = _lastRailParameter is null;
		_lastRailParameter = Rail;

		// The same value is @bind-Rail echoing a flip, which has to stay kept for this tab.
		if (Rail != _rail)
		{
			_rail = Rail;
			_railChosen = !first;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await BindAsync();
		}
		else if (_railChosen)
		{
			_railChosen = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "chooseRail", _key);
		}

		if (_drawerOpen != _drawerTold)
		{
			_drawerTold = _drawerOpen;
			await SafeModuleInvokeVoidAsync(ModulePath, "setDrawerOpen", _key, _drawerOpen);
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Navigation.LocationChanged -= HandleLocationChanged;
		_hotkey?.Dispose();

		if (_self is not null)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindShell", _key);
			_self.Dispose();
		}

		IJSObjectReference? space = _space;
		_space = null;
		await space.SafeInvokeVoidAsync("dispose");
		await space.SafeDisposeAsync();

		await base.DisposeAsyncCore();
	}

	private static MokaRailChoice? ParseRail(string? value) => value switch
	{
		"collapsed" => MokaRailChoice.Collapsed,
		"expanded" => MokaRailChoice.Expanded,
		_ => null
	};

	// The phone's top bar is sticky, so it reports how much of the window it covers, as a fixed MokaAppBar does. It is
	// hidden above 720 px, which reports nothing.
	private async Task BindAsync()
	{
		_space = await SafeModuleInvokeAsync<IJSObjectReference>(SpaceModule, "watchSpace", _topBar, "--moka-appbar-space", false);
		_self = DotNetObjectReference.Create(this);
		string? kept = await SafeModuleInvokeAsync<string>(ModulePath, "bindShell", _key, _root, _self);
		if (ParseRail(kept) is { } choice && choice != _rail)
		{
			await SettleRailAsync(choice);
		}
	}

	private async Task SettleRailAsync(MokaRailChoice choice)
	{
		_rail = choice;
		StateHasChanged();
		await RailChanged.InvokeAsync(choice);
	}

	private void OpenDrawer() => _drawerOpen = true;

	private void HandleDrawerChanged(bool open) => _drawerOpen = open;

	// Any way of reaching a page closes the drawer, and moves the highlight and the top bar's name.
	private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) => RunDetached(() =>
	{
		_drawerOpen = false;
		StateHasChanged();
	});
}
