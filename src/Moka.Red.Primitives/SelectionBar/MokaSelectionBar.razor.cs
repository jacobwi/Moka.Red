using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SelectionBar;

/// <summary>
///     A floating bulk-actions bar that appears when a selection set is non-empty.
///     Shows the selected count, a slot for action buttons, and an optional deselect button.
///     While <see cref="Count" /> is zero only a hidden live region is rendered, which announces
///     the count to screen readers when a selection starts or changes.
///     By default the bar sits sticky at the bottom of its scroll container;
///     set <see cref="Fixed" /> to pin it bottom-centered on the viewport instead.
/// </summary>
public partial class MokaSelectionBar : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/SelectionBar/MokaSelectionBar.razor.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// The bar usually sits at the bottom of a view, so the menu opens above its button and flips below without room.
	private static readonly Dictionary<string, object?> MenuPlacement = new() { ["placement"] = "top-end" };

	private readonly Dictionary<string, MokaSelectionAction> _actions = new(StringComparer.Ordinal);
	private readonly string _idPrefix = $"moka-selection-bar-{Guid.NewGuid():N}";
	private bool? _boundFixed;
	private DotNetObjectReference<MokaSelectionBar>? _dotNetRef;
	private string[] _dropped = [];
	private ElementReference _menu;
	private bool _menuOpen;
	private bool _menuShown;
	private ElementReference _moreButton;
	private int _nextAction;
	private ElementReference _root;

	/// <summary>Number of selected items. The bar shows only when greater than zero.</summary>
	[Parameter]
	public int Count { get; set; }

	/// <summary>
	///     Text shown after the count, also used as the toolbar's accessible label and read out with
	///     the count, as in "3 files selected". Defaults to "selected".
	/// </summary>
	[Parameter]
	public string Label { get; set; } = "selected";

	/// <summary>
	///     The action buttons area, rendered between the count and the clear button. Write the actions as
	///     <see cref="MokaSelectionAction" />s for <see cref="MokaSelectionBarOverflow.Menu" /> to move them.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether to show the trailing deselect button. Defaults to true.</summary>
	[Parameter]
	public bool ShowClear { get; set; } = true;

	/// <summary>Text and accessible label of the clear button. Defaults to "Deselect".</summary>
	[Parameter]
	public string ClearLabel { get; set; } = "Deselect";

	/// <summary>Raised when the clear button is clicked.</summary>
	[Parameter]
	public EventCallback OnClear { get; set; }

	/// <summary>
	///     When false (default) the bar is position: sticky at the bottom of its container.
	///     When true it is position: fixed, bottom-centered on the viewport.
	/// </summary>
	[Parameter]
	public bool Fixed { get; set; }

	/// <summary>
	///     What happens to actions that don't fit on one row. <see cref="MokaSelectionBarOverflow.Wrap" /> by default;
	///     <see cref="MokaSelectionBarOverflow.Menu" /> keeps one row and moves the <see cref="MokaSelectionAction" />s
	///     that don't fit into a More menu.
	/// </summary>
	[Parameter]
	public MokaSelectionBarOverflow Overflow { get; set; }

	/// <summary>
	///     Names the More button under <see cref="MokaSelectionBarOverflow.Menu" />, in its tooltip and for screen readers,
	///     from how many actions are in the menu. Default "2 more actions".
	/// </summary>
	[Parameter]
	public Func<int, string>? MoreLabel { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-selection-bar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-selection-bar--fixed", Fixed)
		.AddClass("moka-selection-bar--menu", InMenuMode)
		.AddClass(Class)
		.Build();

	private bool InMenuMode => Overflow == MokaSelectionBarOverflow.Menu;

	private bool ShouldFit => InMenuMode && Count > 0;

	private string CountText => Count.ToString(CultureInfo.InvariantCulture);

	// Empty while nothing is selected, so clearing the selection is not read out as "0 selected".
	private string Announcement => Count > 0 ? $"{CountText} {Label}" : string.Empty;

	private string MoreId => $"{_idPrefix}-more";

	private string MenuId => $"{_idPrefix}-menu";

	private List<MokaSelectionAction> MenuActions =>
		[.. _dropped.Select(id => _actions.GetValueOrDefault(id)).OfType<MokaSelectionAction>()];

	private string MoreName
	{
		get
		{
			int count = MenuActions.Count;
			return MoreLabel?.Invoke(count) ?? (count == 1
				? "1 more action"
				: $"{count.ToString(CultureInfo.InvariantCulture)} more actions");
		}
	}

	/// <summary>Called by the bar's script when which actions have no room in the row changes.</summary>
	/// <param name="ids">The ids of those actions, in the row's order.</param>
	/// <returns>A task that completes once the bar has been asked to redraw.</returns>
	[JSInvokable]
	public Task OnDroppedChanged(string[] ids) =>
		InvokeAsync(() =>
		{
			_dropped = ids;
			_menuOpen = _menuOpen && ids.Length > 0;
			StateHasChanged();
		});

	/// <inheritdoc />
	protected override bool ShouldRender() => InMenuMode || base.ShouldRender();

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		// Hiding the bar, or moving it in or out of the fixed dock, removes the element the script holds. The script lets
		// go of it first: this call reaches the browser before the render that removes it.
		if (_boundFixed is { } boundFixed && (!ShouldFit || boundFixed != Fixed))
		{
			await UnbindAsync();
		}

		await base.OnParametersSetAsync();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (ShouldFit && _boundFixed is null)
		{
			_boundFixed = Fixed;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(ModulePath, "bindSelectionBar", _root, _dotNetRef);
		}

		// The menu goes to the top layer, out of reach of an ancestor's overflow and of later stacking contexts.
		if (_menuShown != _menuOpen)
		{
			_menuShown = _menuOpen;
			if (_menuOpen)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", MenuId, _menu, _moreButton, null, MenuPlacement);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", MenuId);
			}

			if (_boundFixed is not null)
			{
				await SafeModuleInvokeVoidAsync(ModulePath, "menuShown", _root, _menuOpen);
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_menuShown)
		{
			_menuShown = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", MenuId);
		}

		if (_boundFixed is not null)
		{
			_boundFixed = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindSelectionBar", _root);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	internal string Register(MokaSelectionAction action)
	{
		_nextAction++;
		string id = $"{_idPrefix}-{_nextAction.ToString(CultureInfo.InvariantCulture)}";
		_actions[id] = action;
		return id;
	}

	internal void Unregister(string id) => _actions.Remove(id);

	internal void ActionChanged()
	{
		if (_menuOpen)
		{
			StateHasChanged();
		}
	}

	private async Task UnbindAsync()
	{
		_boundFixed = null;
		_dropped = [];
		_menuOpen = false;
		await SafeModuleInvokeVoidAsync(ModulePath, "unbindSelectionBar", _root);
	}

	private void ToggleMenu() => _menuOpen = !_menuOpen && MenuActions.Count > 0;

	private static string MenuItemClass(MokaSelectionAction action) => new CssBuilder("moka-selection-bar-menu-item")
		.AddClass("moka-selection-bar-menu-item--danger", action.IsDanger)
		.AddClass("moka-selection-bar-menu-item--disabled", action.Disabled)
		.Build();

	// Closed first, so an action that opens a dialog doesn't leave the menu open behind it.
	private async Task PickAsync(MokaSelectionAction action, MouseEventArgs args)
	{
		if (action.Disabled)
		{
			return;
		}

		_menuOpen = false;
		await action.ClickAsync(args);
	}

	private async Task HandleClearAsync()
	{
		if (OnClear.HasDelegate)
		{
			await OnClear.InvokeAsync();
		}
	}
}
