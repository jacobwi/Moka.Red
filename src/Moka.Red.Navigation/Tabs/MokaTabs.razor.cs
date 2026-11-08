using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Tabs;

/// <summary>
///     The plain tabs on a page, with a line under the one showing or as a segmented switch. For a closable,
///     pinnable session tab bar use <see cref="MokaTabStrip{TValue}" />.
/// </summary>
public partial class MokaTabs : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string FitModule = "./_content/Moka.Red.Core/moka-fit.js";

	// Left to the browser these scroll the list or jump the page, and Space scrolls it from a link.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new()
		{
			["selector"] = "[role=\"tab\"]",
			["keys"] = new[] { "ArrowLeft", "ArrowRight", "Home", "End", " " },
			["unlessModified"] = true
		}
	];

	private static readonly Dictionary<string, object?> FitLayout = new()
	{
		["sides"] = new[] { ":scope > .moka-tabs__list" },
		["more"] = ":scope > .moka-tabs__more [data-moka-more]",
		["keep"] = "[aria-selected=\"true\"], [tabindex=\"0\"]",
		["watch"] = new[] { "aria-selected", "tabindex" },
		["positions"] = true,
		["flexible"] = true
	};

	private readonly string _idPrefix = $"moka-tabs-{Guid.NewGuid():N}";
	private readonly string _panelId;
	private readonly Dictionary<string, ElementReference> _tabRefs = [];

	private string? _active;
	private DotNetObjectReference<MokaTabs>? _dotNetRef;
	private int[] _dropped = [];
	private string? _fittedLook;
	private string? _focusRequest;
	private bool _keysBound;
	private string? _lastActiveParameter;
	private ElementReference _listRef;
	private bool _menuOpen;
	private ElementReference _root;

	/// <summary>Creates the tabs.</summary>
	public MokaTabs() => _panelId = $"{_idPrefix}-panel";

	/// <summary>The tabs, in order.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaTabItem> Items { get; set; } = [];

	/// <summary>The <see cref="MokaTabItem.Id" /> of the tab showing.</summary>
	[Parameter]
	public string? Active { get; set; }

	/// <summary>Raised when the person picks another tab.</summary>
	[Parameter]
	public EventCallback<string> ActiveChanged { get; set; }

	/// <summary>What the tabs choose between, for screen readers: "Firewall views".</summary>
	[Parameter]
	[EditorRequired]
	public string Label { get; set; } = string.Empty;

	/// <summary>A segmented switch, drawn like <c>MokaSegmentedControl</c>, instead of a line under the tab showing.</summary>
	[Parameter]
	public bool Segmented { get; set; }

	/// <summary>The tabs share the full width.</summary>
	[Parameter]
	public bool Stretch { get; set; }

	/// <summary>
	///     What happens to tabs that don't fit. <see cref="MokaTabsOverflow.Scroll" /> by default.
	///     <see cref="MokaTabsOverflow.Menu" /> needs an interactive render; without one the list scrolls.
	/// </summary>
	[Parameter]
	public MokaTabsOverflow Overflow { get; set; }

	/// <summary>Names the menu button in <see cref="MokaTabsOverflow.Menu" />, from how many tabs are in it. Default "2 more tabs".</summary>
	[Parameter]
	public Func<int, string>? MoreLabel { get; set; }

	/// <summary>The showing tab's content, wrapped as its tab panel. Leave it out to put the content anywhere on the page.</summary>
	[Parameter]
	public RenderFragment? Panel { get; set; }

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-tabs";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Segmented ? "moka-tabs--segmented" : "moka-tabs--underline")
		.AddClass("moka-tabs--stretch", Stretch)
		.AddClass("moka-tabs--menu", Overflow == MokaTabsOverflow.Menu)
		.AddClass(Class)
		.Build();

	private string? ControlledPanel => Panel is null ? null : _panelId;

	// With nothing selected the first tab that can take focus holds the tab stop, or Tab could not reach the list.
	private MokaTabItem? TabStop =>
		Items.FirstOrDefault(item => !item.Disabled && item.Id == _active) ?? Items.FirstOrDefault(item => !item.Disabled);

	private string? ActiveTabId
	{
		get
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].Id == _active)
				{
					return TabId(i);
				}
			}

			return null;
		}
	}

	private IEnumerable<MokaTabItem> DroppedItems => _dropped.Where(position => position < Items.Count).Select(position => Items[position]);

	private int DroppedCount => DroppedItems.Count();

	private string MoreText => DroppedCount > 0 ? $"+{CountText(DroppedCount)}" : "+";

	private string MoreName =>
		MoreLabel?.Invoke(DroppedCount) ?? (DroppedCount == 1 ? "1 more tab" : $"{CountText(DroppedCount)} more tabs");

	/// <summary>Called by the list's script when which tabs didn't fit changes.</summary>
	/// <param name="positions">Where those tabs are in <see cref="Items" />.</param>
	/// <returns>A task that completes once the tabs have been asked to redraw.</returns>
	[JSInvokable]
	public Task OnDroppedChanged(int[] positions) =>
		InvokeAsync(() =>
		{
			_dropped = positions;
			_menuOpen = _menuOpen && positions.Length > 0;
			StateHasChanged();
		});

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Active != _lastActiveParameter)
		{
			_lastActiveParameter = Active;
			_active = Active;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && !_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _listRef, KeyRules);
		}

		await FitAsync();

		if (_focusRequest is { } id)
		{
			_focusRequest = null;
			if (_tabRefs.TryGetValue(id, out ElementReference tab))
			{
				await SafeFocusAsync(tab);
			}
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_fittedLook is not null)
		{
			await SafeModuleInvokeVoidAsync(FitModule, "unbindFit", _root);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private static string CountText(int count) => count.ToString(CultureInfo.InvariantCulture);

	// The look decides how much room the tabs take, and the script can't hear a class change on the list's parent.
	private async Task FitAsync()
	{
		string? look = Overflow == MokaTabsOverflow.Menu ? CssClass : null;
		if (look == _fittedLook)
		{
			return;
		}

		bool wasFitted = _fittedLook is not null;
		_fittedLook = look;
		if (look is not null)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(FitModule, "bindFit", _root, _dotNetRef, FitLayout);
		}
		else if (wasFitted)
		{
			_dropped = [];
			_menuOpen = false;
			await SafeModuleInvokeVoidAsync(FitModule, "unbindFit", _root);
		}
	}

	private string TabId(int index) => $"{_idPrefix}-{index.ToString(CultureInfo.InvariantCulture)}";

	private string AriaSelected(MokaTabItem item) => item.Id == _active ? "true" : "false";

	private string TabClass(MokaTabItem item) => new CssBuilder("moka-tabs__tab")
		.AddClass("moka-tabs__tab--selected", item.Id == _active)
		.AddClass("moka-tabs__tab--disabled", item.Disabled)
		.Build();

	private async Task PickAsync(MokaTabItem item)
	{
		if (item.Id == _active)
		{
			return;
		}

		_active = item.Id;
		await ActiveChanged.InvokeAsync(item.Id);
	}

	// A mouse click on a link's row lands on its real link, so it opens in a new tab with a modifier or a middle click
	// and needs no help going there. Enter and Space click the row itself, which reports no click count.
	private async Task PickFromMenuAsync(MokaTabItem item, MouseEventArgs e)
	{
		if (e.CtrlKey || e.ShiftKey || e.MetaKey)
		{
			return;
		}

		if (e.Detail == 0 && UrlValues.SafeHref(item.Href) is { } href)
		{
			Navigation.NavigateTo(href);
		}

		await PickAsync(item);
	}

	// The WAI-ARIA tabs pattern with automatic activation: moving to a tab shows it.
	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.AltKey || e.CtrlKey || e.MetaKey)
		{
			return;
		}

		List<MokaTabItem> enabled = [.. Items.Where((item, position) => !item.Disabled && !_dropped.Contains(position))];
		if (enabled.Count == 0)
		{
			return;
		}

		int current = Math.Max(0, enabled.FindIndex(item => item.Id == _active));
		int next = e.Key switch
		{
			"ArrowRight" => (current + 1) % enabled.Count,
			"ArrowLeft" => (current - 1 + enabled.Count) % enabled.Count,
			"Home" => 0,
			"End" => enabled.Count - 1,
			" " => current,
			_ => -1
		};
		if (next < 0)
		{
			return;
		}

		MokaTabItem target = enabled[next];
		if (target.Id == _active)
		{
			return;
		}

		_focusRequest = target.Id;
		if (UrlValues.SafeHref(target.Href) is { } href)
		{
			Navigation.NavigateTo(href);
		}

		await PickAsync(target);
	}
}
