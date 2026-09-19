using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Renders a context menu at a fixed position with support for icons, shortcuts,
///     dividers, checked items, disabled items, nested sub-menus, and keyboard navigation.
///     The menu measures itself after render and clamps into the viewport; sub-menus open
///     flush to the parent's right edge and flip to the left when they would overflow.
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
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new() { ["selector"] = null, ["keys"] = new[] { " ", "ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight", "Home", "End" } }
	];

	private readonly string _idPrefix = $"moka-ctx-{Guid.NewGuid():N}";
	private bool _disposed;
	private int _focusedIndex = -1;
	private string? _focusToken;
	private double _menuLeft;
	private ElementReference _menuRef;
	private IJSObjectReference? _module;
	private MokaContextMenuItem? _openSubmenuParent;
	private double _positionedForX;
	private double _positionedForY;
	private bool _positionResolved;
	private double _renderX;
	private double _renderY;
	private CancellationTokenSource? _submenuCts;
	private double _submenuX;
	private double _submenuY;
	private bool _wasVisible;

	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

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
	///     Whether this instance is a nested sub-menu. Set automatically when a parent menu renders
	///     its children: sub-menus skip the backdrop and the focus handling that belong to the root menu.
	/// </summary>
	[Parameter]
	public bool IsSubmenu { get; set; }

	/// <summary>
	///     Left edge of the parent menu in viewport pixels. Set automatically for sub-menus and used
	///     as the flip anchor when the sub-menu does not fit to the right of its parent.
	/// </summary>
	[Parameter]
	public double? ParentLeft { get; set; }

	/// <summary>Fires when the menu should close (backdrop click, Escape, or item activation).</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

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
			_focusedIndex = -1;
			_openSubmenuParent = null;
			CancelSubmenuTimer();
			_positionResolved = false;
			_renderX = X;
			_renderY = Y;
			return;
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

		if (Visible == _wasVisible)
		{
			return;
		}

		_wasVisible = Visible;
		if (Visible)
		{
			await HandleOpenedAsync();
		}
		else
		{
			await HandleClosedAsync();
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
			_openSubmenuParent = null;
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
				if (OnClose.HasDelegate)
				{
					await OnClose.InvokeAsync();
				}

				break;

			case "ArrowDown":
				MoveFocus(1, actionItems);
				break;

			case "ArrowUp":
				MoveFocus(-1, actionItems);
				break;

			case "Home":
				_focusedIndex = -1;
				MoveFocus(1, actionItems);
				break;

			case "End":
				_focusedIndex = actionItems.Count;
				MoveFocus(-1, actionItems);
				break;

			case "Enter" or " ":
				if (TryGetFocusedItem(actionItems, out MokaContextMenuItem activated))
				{
					if (activated.HasChildren)
					{
						await OpenSubmenuAsync(activated, _focusedIndex);
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
					await OpenSubmenuAsync(expanded, _focusedIndex);
				}

				break;

			case "ArrowLeft":
				CancelSubmenuTimer();
				_openSubmenuParent = null;
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

	private void MoveFocus(int delta, List<MokaContextMenuItem> actionItems)
	{
		if (actionItems.Count == 0)
		{
			return;
		}

		int index = _focusedIndex;
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

	#region Sub-menu timing

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

		await InvokeAsync(() => OpenSubmenuAsync(item, actionIndex));
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
			_openSubmenuParent = null;
			StateHasChanged();
		});
	}

	#endregion

	#region Positioning

	/// <summary>
	///     Anchors a sub-menu to the parent's measured right edge and the hovered row's top edge.
	///     The sub-menu clamps and, if needed, flips itself once it knows its own size.
	/// </summary>
	private async Task OpenSubmenuAsync(MokaContextMenuItem item, int actionIndex)
	{
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

		_openSubmenuParent = item;
		StateHasChanged();
	}

	/// <summary>
	///     Measures the rendered menu and moves it so it sits inside the viewport. A sub-menu that
	///     would overflow the right edge flips to the left of its parent first.
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

			Point? clamped = await TryInvokeAsync<Point?>(
				"constrainToViewport", x, _renderY, metrics.Width, metrics.Height, ViewportMargin);

			_renderX = clamped?.X ?? x;
			_renderY = clamped?.Y ?? _renderY;
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
		await TryInvokeVoidAsync("preventKeys", _menuRef, KeyRules);

		if (IsSubmenu)
		{
			return;
		}

		_focusToken = await TryInvokeAsync<string?>("captureFocus");

		try
		{
			await _menuRef.FocusAsync();
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected before the menu could take focus.
		}
		catch (InvalidOperationException)
		{
			// No JS runtime (prerender) or the element is already gone.
		}
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
