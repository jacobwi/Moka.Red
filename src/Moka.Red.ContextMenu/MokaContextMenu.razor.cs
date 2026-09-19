using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Renders a context menu at a fixed position with support for icons, shortcuts,
///     dividers, checked items, disabled items, nested sub-menus, and keyboard navigation.
///     The menu measures itself after render and clamps into the viewport. A menu that does not fit
///     below its anchor opens above it; sub-menus open flush to the parent's right edge and flip to
///     the left when they would overflow.
/// </summary>
public partial class MokaContextMenu : ComponentBase, IAsyncDisposable
{
	private const string ModulePath = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";

	/// <summary>Hover-intent delay before a sub-menu opens, in milliseconds.</summary>
	private const int SubmenuOpenDelayMs = 140;

	/// <summary>Grace period before a sub-menu closes after the pointer leaves the menu, in milliseconds.</summary>
	private const int SubmenuCloseDelayMs = 250;

	/// <summary>Vertical nudge so a sub-menu's first row lines up with its parent item.</summary>
	private const double SubmenuTopOffset = 6;

	/// <summary>Horizontal offset used only when the menu cannot be measured (matches the CSS min-width).</summary>
	private const double UnmeasuredSubmenuOffset = 180;

	/// <summary>Gap kept between the menu and the viewport edge, in pixels.</summary>
	private const double ViewportMargin = 8;

	// The .NET key handler moves the highlight and opens sub-menus, so these keys must not also
	// scroll the page behind the menu. A null selector means the menu element itself.
	private static readonly Dictionary<string, object?> NavigationKeys = new()
	{
		["selector"] = null,
		["keys"] = new[] { " ", "ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight", "Home", "End" }
	};

	// Tab closes the whole menu, and closing puts focus back where it was before the menu opened.
	// Left alone, Tab would first move focus to whatever follows the menu in the page, which for a
	// shared menu at the end of the layout is somewhere unrelated, or out of the page.
	private static readonly Dictionary<string, object?> TabKey = new()
	{
		["selector"] = null,
		["keys"] = new[] { "Tab" }
	};

	private static readonly Dictionary<string, object?>[] KeyRules = [NavigationKeys];

	// Only for a menu that can close itself. One without OnClose leaves Tab alone, so Tab still
	// moves focus on instead of trapping it in a menu that stays open.
	private static readonly Dictionary<string, object?>[] ClosingKeyRules = [NavigationKeys, TabKey];

	private readonly string _idPrefix = $"moka-ctx-{Guid.NewGuid():N}";
	private bool _disposed;
	private int _focusedIndex = -1;
	private string? _focusToken;
	private bool _isOpen;
	private double _menuLeft;
	private ElementReference _menuRef;
	private IJSObjectReference? _module;
	private int _openSubmenuIndex = -1;
	private MokaContextMenuItem? _openSubmenuParent;
	private double _positionedForX;
	private double _positionedForY;
	private bool _positionResolved;
	private bool _reclaimFocus;
	private double _renderX;
	private double _renderY;
	private CancellationTokenSource? _submenuCts;
	private bool _submenuFocusRequested;
	private double _submenuX;
	private double _submenuY;
	private bool _takeFocus;
	private bool _wasVisible;

	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

	/// <summary>The menu that renders this one as its sub-menu. Null for a root menu.</summary>
	[CascadingParameter]
	private MokaContextMenu? ParentMenu { get; set; }

	/// <summary>The menu items to display.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaContextMenuItem> Items { get; set; } = [];

	/// <summary>Whether the menu is visible.</summary>
	[Parameter]
	public bool Visible { get; set; }

	/// <summary>Horizontal position in pixels from the left edge of the viewport.</summary>
	[Parameter]
	public double X { get; set; }

	/// <summary>Vertical position in pixels from the top edge of the viewport.</summary>
	[Parameter]
	public double Y { get; set; }

	/// <summary>
	///     Whether the keyboard opened the menu. It then opens with its first enabled item highlighted, so
	///     Enter chooses it straight away. Read when the menu opens. <see cref="MokaContextMenuTrigger" /> and
	///     <see cref="MokaContextMenuHost" /> set it.
	/// </summary>
	[Parameter]
	public bool OpenedFromKeyboard { get; set; }

	/// <summary>
	///     Top edge of what the menu opens under, in viewport pixels, such as the button a menu hangs below.
	///     A menu that does not fit below <see cref="Y" /> opens above this line instead of covering it.
	///     Leave it null for a menu at a pointer, which then opens above <see cref="Y" />.
	/// </summary>
	[Parameter]
	public double? AnchorTop { get; set; }

	/// <summary>
	///     Whether this instance is a nested sub-menu. Set automatically when a parent menu renders
	///     its children: sub-menus skip the backdrop, and take focus only when the keyboard opens them.
	/// </summary>
	[Parameter]
	public bool IsSubmenu { get; set; }

	/// <summary>
	///     Left edge of the parent menu in viewport pixels. Set automatically for sub-menus and used
	///     as the flip anchor when the sub-menu does not fit to the right of its parent.
	/// </summary>
	[Parameter]
	public double? ParentLeft { get; set; }

	/// <summary>
	///     Fires when the menu should close: a backdrop click, Escape in the top-level menu, Tab in any
	///     menu, or item activation. Escape in a sub-menu closes only that sub-menu.
	/// </summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <summary>Id of the item whose sub-menu is open. The sub-menu takes its accessible name from it.</summary>
	internal string? OpenSubmenuItemId => _openSubmenuParent is null ? null : ItemId(_openSubmenuIndex);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		CancelSubmenuTimer();

		if (_focusToken is not null)
		{
			string token = _focusToken;
			_focusToken = null;
			await TryInvokeVoidAsync("restoreFocus", token);
		}

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

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (!Visible)
		{
			_isOpen = false;
			_focusedIndex = -1;
			_openSubmenuParent = null;
			_openSubmenuIndex = -1;
			_submenuFocusRequested = false;
			_takeFocus = false;
			_reclaimFocus = false;
			CancelSubmenuTimer();
			_positionResolved = false;
			_renderX = X;
			_renderY = Y;
			return;
		}

		if (!_isOpen)
		{
			_isOpen = true;

			// Enter should choose something right after a keyboard open, as in a native menu.
			if (OpenedFromKeyboard)
			{
				MoveHighlight(-1, 1, GetActionItems());
			}
		}

		// Asked on every parameter set, not just the first, so a sub-menu already open from a
		// hover also hears it when the keyboard opens it.
		if (ParentMenu?.TakeSubmenuFocusRequest() == true)
		{
			_takeFocus = true;
			MoveHighlight(-1, 1, GetActionItems());
		}

		if (_positionResolved && _positionedForX == X && _positionedForY == Y)
		{
			return;
		}

		_positionResolved = false;
		_renderX = X;
		_renderY = Y;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Position first: the menu is visibility:hidden until it is clamped, and a hidden
		// element cannot take focus. Resolving triggers the re-render that runs the open hook.
		if (Visible && !_positionResolved)
		{
			await ResolvePositionAsync();
			return;
		}

		if (Visible != _wasVisible)
		{
			_wasVisible = Visible;
			if (!Visible)
			{
				await HandleClosedAsync();
				return;
			}

			await HandleOpenedAsync();
		}

		if (!Visible)
		{
			return;
		}

		if (_takeFocus)
		{
			_takeFocus = false;
			await FocusMenuAsync();
		}

		if (_reclaimFocus)
		{
			_reclaimFocus = false;
			await TryInvokeVoidAsync("reclaimFocus", _menuRef);
		}
	}

	#region Rendering helpers

	private string MenuClass => new CssBuilder("moka-ctx-menu")
		.AddClass("moka-ctx-menu--submenu", IsSubmenu)
		.AddClass("moka-ctx-menu--measuring", !_positionResolved)
		.Build();

	private string MenuStyle => new StyleBuilder()
		.AddStyle("left", FormatPx(_renderX))
		.AddStyle("top", FormatPx(_renderY))
		.Build() ?? string.Empty;

	private static string FormatPx(double value) =>
		value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

	// Focus stays on the menu while the arrows move the highlight, so aria-activedescendant
	// points at the highlighted item by this id.
	private string ItemId(int actionIndex) => $"{_idPrefix}-item-{actionIndex}";

	private string ItemClass(MokaContextMenuItem item, int actionIndex) => new CssBuilder("moka-ctx-item")
		.AddClass("moka-ctx-item--disabled", item.Disabled)
		.AddClass("moka-ctx-item--focused", actionIndex == _focusedIndex)
		.AddClass(item.CssClass)
		.Build();

	// A string, not a bool: Blazor drops a false boolean attribute, and a missing aria-expanded
	// does not read as collapsed.
	private string? AriaExpanded(MokaContextMenuItem item)
	{
		if (!item.HasChildren)
		{
			return null;
		}

		return ReferenceEquals(_openSubmenuParent, item) ? "true" : "false";
	}

	/// <summary>
	///     Flattens <see cref="Items" /> into the rows actually rendered. An item with no text and no
	///     children is a divider regardless of <see cref="MokaContextMenuItem.DividerBefore" />, so it
	///     never becomes an empty clickable row. Leading, trailing, and repeated dividers are dropped.
	///     <c>ActionIndex</c> matches the index used for keyboard focus.
	/// </summary>
	private List<MenuEntry> BuildEntries()
	{
		var entries = new List<MenuEntry>(Items.Count);
		int actionIndex = 0;
		bool pendingDivider = false;

		foreach (MokaContextMenuItem item in Items)
		{
			if (string.IsNullOrEmpty(item.Text) && !item.HasChildren)
			{
				pendingDivider = entries.Count > 0;
				continue;
			}

			if ((pendingDivider || item.DividerBefore) && entries.Count > 0)
			{
				entries.Add(new MenuEntry(true, null, -1));
			}

			pendingDivider = false;
			entries.Add(new MenuEntry(false, item, actionIndex));
			actionIndex++;
		}

		return entries;
	}

	private List<MokaContextMenuItem> GetActionItems()
	{
		var items = new List<MokaContextMenuItem>(Items.Count);
		foreach (MenuEntry entry in BuildEntries())
		{
			if (!entry.IsDivider && entry.Item is not null)
			{
				items.Add(entry.Item);
			}
		}

		return items;
	}

	#endregion

	#region Interaction

	private async Task HandleItemClick(MokaContextMenuItem item)
	{
		if (item.Disabled || item.HasChildren)
		{
			return;
		}

		// Close first. The menu used to wait for the action, so an action that opened a dialog left
		// the menu open behind it until the dialog was done.
		if (OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}

		if (item.OnClick is not null)
		{
			await item.OnClick();
		}
		else
		{
			item.OnClickSync?.Invoke();
		}
	}

	private void HandleMouseEnterItem(MokaContextMenuItem item, int actionIndex)
	{
		CancelSubmenuTimer();

		if (!item.HasChildren)
		{
			// Leaving a sub-menu parent for a sibling closes the sub-menu immediately.
			CloseSubmenu();
			return;
		}

		if (ReferenceEquals(_openSubmenuParent, item))
		{
			return;
		}

		_submenuCts = new CancellationTokenSource();
		_ = OpenSubmenuAfterDelayAsync(item, actionIndex, _submenuCts.Token);
	}

	private void HandleMenuMouseEnter() => CancelSubmenuTimer();

	/// <summary>
	///     The sub-menu is a DOM descendant of this menu, so <c>mouseleave</c> fires only once the
	///     pointer has left both. A short grace period keeps the sub-menu open across that transition.
	/// </summary>
	private void HandleMenuMouseLeave()
	{
		if (_openSubmenuParent is null)
		{
			return;
		}

		CancelSubmenuTimer();
		_submenuCts = new CancellationTokenSource();
		_ = CloseSubmenuAfterDelayAsync(_submenuCts.Token);
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		List<MokaContextMenuItem> actionItems = GetActionItems();

		switch (e.Key)
		{
			case "Escape":
				CancelSubmenuTimer();

				// Escape closes only the menu that has focus. In a sub-menu that hands focus back.
				if (ParentMenu is not null)
				{
					await ParentMenu.CloseSubmenuFromKeyboardAsync();
				}
				else if (OnClose.HasDelegate)
				{
					await OnClose.InvokeAsync();
				}

				break;

			case "Tab":
				// Tab leaves the menu, so the whole menu closes: a sub-menu gets the root's OnClose.
				// The browser does not move focus (ClosingKeyRules), and the root puts it back on close.
				CancelSubmenuTimer();
				if (OnClose.HasDelegate)
				{
					await OnClose.InvokeAsync();
				}

				break;

			case "ArrowDown":
				MoveHighlight(_focusedIndex, 1, actionItems);
				break;

			case "ArrowUp":
				MoveHighlight(_focusedIndex, -1, actionItems);
				break;

			case "Home":
				MoveHighlight(-1, 1, actionItems);
				break;

			case "End":
				MoveHighlight(actionItems.Count, -1, actionItems);
				break;

			// A held key repeats. Enter that opened the menu from a button can still be down when the menu
			// takes focus, and its repeats would choose the item a keyboard open highlights.
			case "Enter" or " " when !e.Repeat:
				if (TryGetFocusedItem(actionItems, out MokaContextMenuItem activated))
				{
					if (activated.HasChildren)
					{
						await OpenSubmenuFromKeyboardAsync(activated, _focusedIndex);
					}
					else
					{
						await HandleItemClick(activated);
					}
				}

				break;

			case "ArrowRight":
				if (TryGetFocusedItem(actionItems, out MokaContextMenuItem expanded) && expanded.HasChildren)
				{
					await OpenSubmenuFromKeyboardAsync(expanded, _focusedIndex);
				}

				break;

			case "ArrowLeft":
				CancelSubmenuTimer();
				if (ParentMenu is not null)
				{
					await ParentMenu.CloseSubmenuFromKeyboardAsync();
				}
				else
				{
					CloseSubmenu();
				}

				break;
		}
	}

	private bool TryGetFocusedItem(List<MokaContextMenuItem> actionItems, out MokaContextMenuItem item)
	{
		if (_focusedIndex >= 0 && _focusedIndex < actionItems.Count)
		{
			item = actionItems[_focusedIndex];
			return true;
		}

		item = null!;
		return false;
	}

	/// <summary>
	///     Moves the highlight from <paramref name="start" /> to the next enabled item in the direction
	///     of <paramref name="delta" />, wrapping at the ends. -1 and <c>Count</c> sit just outside the
	///     list, for Home and End. With every item disabled the highlight stays where it was.
	/// </summary>
	private void MoveHighlight(int start, int delta, List<MokaContextMenuItem> actionItems)
	{
		int index = start;
		for (int step = 0; step < actionItems.Count; step++)
		{
			index += delta;
			if (index < 0)
			{
				index = actionItems.Count - 1;
			}
			else if (index >= actionItems.Count)
			{
				index = 0;
			}

			if (!actionItems[index].Disabled)
			{
				_focusedIndex = index;
				return;
			}
		}
	}

	private async Task HandleBackdropClick()
	{
		CancelSubmenuTimer();
		if (OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}

	#endregion

	#region Sub-menus

	/// <summary>Right, Enter or Space on an item with children: open its sub-menu with focus in it.</summary>
	private async Task OpenSubmenuFromKeyboardAsync(MokaContextMenuItem item, int actionIndex)
	{
		CancelSubmenuTimer();

		if (ReferenceEquals(_openSubmenuParent, item))
		{
			// Already open from a hover, so it only needs focus. It picks the request up when
			// this menu renders.
			_submenuFocusRequested = true;
			StateHasChanged();
			return;
		}

		await OpenSubmenuAsync(item, actionIndex, takeFocus: true);
	}

	/// <summary>
	///     Called by the open sub-menu each time it gets parameters: whether the keyboard opened it,
	///     in which case it takes focus and highlights its first enabled item. Answers true once.
	/// </summary>
	internal bool TakeSubmenuFocusRequest()
	{
		bool requested = _submenuFocusRequested;
		_submenuFocusRequested = false;
		return requested;
	}

	/// <summary>Left or Escape in the open sub-menu: close it and put focus back on its item here.</summary>
	internal async Task CloseSubmenuFromKeyboardAsync()
	{
		CancelSubmenuTimer();
		if (_openSubmenuParent is null)
		{
			return;
		}

		_focusedIndex = _openSubmenuIndex;
		CloseSubmenu();
		StateHasChanged();

		// This call goes out before the render that removes the sub-menu, so focus moves straight
		// here instead of falling to the body first.
		await FocusMenuAsync();
	}

	/// <summary>
	///     Closes the open sub-menu. If focus was in it, removing it leaves focus on the body, so this
	///     menu takes focus back after the render.
	/// </summary>
	private void CloseSubmenu()
	{
		if (_openSubmenuParent is null)
		{
			return;
		}

		_openSubmenuParent = null;
		_openSubmenuIndex = -1;
		_submenuFocusRequested = false;
		_reclaimFocus = true;
	}

	private void CancelSubmenuTimer()
	{
		if (_submenuCts is null)
		{
			return;
		}

		_submenuCts.Cancel();
		_submenuCts.Dispose();
		_submenuCts = null;
	}

	private async Task OpenSubmenuAfterDelayAsync(MokaContextMenuItem item, int actionIndex, CancellationToken token)
	{
		try
		{
			await Task.Delay(SubmenuOpenDelayMs, token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (_disposed || token.IsCancellationRequested)
		{
			return;
		}

		await InvokeAsync(() => OpenSubmenuAsync(item, actionIndex, takeFocus: false));
	}

	private async Task CloseSubmenuAfterDelayAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(SubmenuCloseDelayMs, token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (_disposed || token.IsCancellationRequested)
		{
			return;
		}

		await InvokeAsync(() =>
		{
			CloseSubmenu();
			StateHasChanged();
		});
	}

	#endregion

	#region Positioning

	/// <summary>
	///     Anchors a sub-menu to the parent's measured right edge and the hovered row's top edge.
	///     The sub-menu clamps and, if needed, flips itself once it knows its own size.
	/// </summary>
	private async Task OpenSubmenuAsync(MokaContextMenuItem item, int actionIndex, bool takeFocus)
	{
		if (item.Disabled)
		{
			return;
		}

		ItemAnchor? anchor = await TryInvokeAsync<ItemAnchor?>("measureItemAnchor", _menuRef, actionIndex);

		if (anchor is not null)
		{
			_menuLeft = anchor.MenuLeft;
			_submenuX = anchor.MenuRight;
			_submenuY = anchor.ItemTop - SubmenuTopOffset;
		}
		else
		{
			_menuLeft = _renderX;
			_submenuX = _renderX + UnmeasuredSubmenuOffset;
			_submenuY = _renderY;
		}

		// Swapping one sub-menu for another drops focus to the body if the old one had it.
		if (_openSubmenuParent is not null && !ReferenceEquals(_openSubmenuParent, item))
		{
			_reclaimFocus = true;
		}

		_openSubmenuParent = item;
		_openSubmenuIndex = actionIndex;

		// Set after the measurement, not before: this menu renders while that call is out, and the
		// sub-menu open at that moment (from a hover) would take the request meant for this one.
		_submenuFocusRequested = takeFocus;
		StateHasChanged();
	}

	/// <summary>
	///     Measures the rendered menu and moves it so it sits inside the viewport. A sub-menu that
	///     would overflow the right edge flips to the left of its parent first. A root menu that would
	///     overflow the bottom opens above its anchor, and is only pushed up when that does not fit either.
	/// </summary>
	private async Task ResolvePositionAsync()
	{
		try
		{
			MenuMetrics? metrics = await TryInvokeAsync<MenuMetrics?>("measureMenu", _menuRef);
			if (metrics is null)
			{
				return;
			}

			double x = _renderX;
			if (IsSubmenu && ParentLeft.HasValue &&
			    x + metrics.Width > metrics.ViewportWidth - ViewportMargin)
			{
				x = ParentLeft.Value - metrics.Width;
			}

			// Clamping alone would push the menu up over the pointer or the button it opened from.
			// A sub-menu keeps the clamp: it sits beside its item, so moving up covers nothing.
			double y = _renderY;
			if (!IsSubmenu && y + metrics.Height > metrics.ViewportHeight - ViewportMargin)
			{
				double above = (AnchorTop ?? y) - metrics.Height;
				if (above >= ViewportMargin)
				{
					y = above;
				}
			}

			Point? clamped = await TryInvokeAsync<Point?>(
				"constrainToViewport", x, y, metrics.Width, metrics.Height, ViewportMargin);

			_renderX = clamped?.X ?? x;
			_renderY = clamped?.Y ?? y;
		}
		finally
		{
			// Whatever happened, stop hiding the menu and do not measure this position again.
			_positionedForX = X;
			_positionedForY = Y;
			_positionResolved = true;
			StateHasChanged();
		}
	}

	#endregion

	#region Focus

	private async Task HandleOpenedAsync()
	{
		// The menu element is new on every open, so it is bound every time.
		await TryInvokeVoidAsync("preventKeys", _menuRef, OnClose.HasDelegate ? ClosingKeyRules : KeyRules);

		// A sub-menu takes focus only when the keyboard opened it, through the parent's request.
		if (IsSubmenu)
		{
			return;
		}

		_focusToken = await TryInvokeAsync<string?>("captureFocus");
		_takeFocus = true;
	}

	private async Task HandleClosedAsync()
	{
		CancelSubmenuTimer();

		if (_focusToken is null)
		{
			return;
		}

		string token = _focusToken;
		_focusToken = null;
		await TryInvokeVoidAsync("restoreFocus", token);
	}

	private async Task FocusMenuAsync()
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			await _menuRef.FocusAsync();
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected before the menu could take focus.
		}
		catch (JSException)
		{
			// The menu left the DOM before the call reached the browser.
		}
		catch (InvalidOperationException)
		{
			// No JS runtime (prerender) or the element is already gone.
		}
	}

	#endregion

	#region JS interop

	/// <summary>
	///     Invokes a module function, returning <c>default</c> when JS is unavailable
	///     (prerendering, a disconnected circuit, or a disposed component).
	/// </summary>
	private async Task<T?> TryInvokeAsync<T>(string identifier, params object?[] args)
	{
		if (_disposed)
		{
			return default;
		}

		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			return await _module.InvokeAsync<T>(identifier, args);
		}
		catch (JSDisconnectedException)
		{
			return default;
		}
		catch (ObjectDisposedException)
		{
			return default;
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException - the circuit went away mid-call.
			return default;
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering.
			return default;
		}
	}

	/// <summary>Invokes a module function that returns nothing, with the same guards as <see cref="TryInvokeAsync{T}" />.</summary>
	private async Task TryInvokeVoidAsync(string identifier, params object?[] args)
	{
		if (_disposed && _module is null)
		{
			return;
		}

		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			await _module.InvokeVoidAsync(identifier, args);
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected - nothing to do.
		}
		catch (ObjectDisposedException)
		{
			// JS runtime torn down mid-call.
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException - the circuit went away mid-call.
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering.
		}
	}

	#endregion

	#region Interop models

	/// <summary>One rendered row: either a divider or an item with its keyboard-focus index.</summary>
	private sealed record MenuEntry(bool IsDivider, MokaContextMenuItem? Item, int ActionIndex);

	/// <summary>Menu rectangle plus viewport size, as returned by <c>measureMenu</c>.</summary>
	internal sealed class MenuMetrics
	{
		/// <summary>Distance from the left of the viewport.</summary>
		public double Left { get; set; }

		/// <summary>Distance from the top of the viewport.</summary>
		public double Top { get; set; }

		/// <summary>Measured menu width.</summary>
		public double Width { get; set; }

		/// <summary>Measured menu height.</summary>
		public double Height { get; set; }

		/// <summary>Viewport width.</summary>
		public double ViewportWidth { get; set; }

		/// <summary>Viewport height.</summary>
		public double ViewportHeight { get; set; }
	}

	/// <summary>Parent menu edges and the anchored row's top, as returned by <c>measureItemAnchor</c>.</summary>
	internal sealed class ItemAnchor
	{
		/// <summary>Left edge of the parent menu.</summary>
		public double MenuLeft { get; set; }

		/// <summary>Right edge of the parent menu.</summary>
		public double MenuRight { get; set; }

		/// <summary>Top edge of the anchored item row.</summary>
		public double ItemTop { get; set; }
	}

	/// <summary>A clamped viewport coordinate, as returned by <c>constrainToViewport</c>.</summary>
	internal sealed class Point
	{
		/// <summary>Clamped horizontal position.</summary>
		public double X { get; set; }

		/// <summary>Clamped vertical position.</summary>
		public double Y { get; set; }
	}

	#endregion
}
