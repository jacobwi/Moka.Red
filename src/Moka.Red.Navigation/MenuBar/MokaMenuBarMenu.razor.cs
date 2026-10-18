using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.MenuBar;

/// <summary>
///     One menu in a <see cref="MokaMenuBar" />: a label in the bar, and the items it drops down. Its items are
///     <see cref="MokaMenuBarItem" /> and <see cref="MokaMenuBarDivider" /> children.
/// </summary>
public partial class MokaMenuBarMenu : MokaVisualComponentBase
{
	private readonly string _menuId = $"moka-menubar-menu-{Guid.NewGuid():N}";
	private readonly List<MokaMenuBarItem> _items = [];

	private int _highlighted = -1;
	private Pending _pending = Pending.None;
	private ElementReference _triggerRef;

	private string _typeAhead = string.Empty;
	private DateTime _typeAheadAt = DateTime.MinValue;

	private static readonly TimeSpan TypeAheadWindow = TimeSpan.FromSeconds(1);

	/// <summary>The menu bar this belongs to. Supplied by <see cref="MokaMenuBar" />.</summary>
	[CascadingParameter]
	public MokaMenuBar? Bar { get; set; }

	/// <summary>The items, as <see cref="MokaMenuBarItem" /> and <see cref="MokaMenuBarDivider" /> children.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The menu's name in the bar, such as "File".</summary>
	[Parameter]
	[EditorRequired]
	public string? Label { get; set; }

	/// <summary>Icon before the label.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-menubar-menu";

	/// <summary>Open state and the highlight live here, outside the parameter flow.</summary>
	protected override bool ShouldRender() => true;

	internal bool IsOpen => Bar?.IsOpen(this) == true;

	private bool IsTabStop => Bar?.IsTabStop(this) != false;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-menubar-menu--open", IsOpen)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		Bar?.Register(this);
	}

	internal void Register(MokaMenuBarItem item)
	{
		if (!_items.Contains(item))
		{
			_items.Add(item);
		}
	}

	internal void Unregister(MokaMenuBarItem item)
	{
		int index = _items.IndexOf(item);
		if (index < 0)
		{
			return;
		}

		_items.RemoveAt(index);
		if (_highlighted >= _items.Count)
		{
			_highlighted = _items.Count - 1;
		}
	}

	internal bool IsHighlighted(MokaMenuBarItem item) =>
		_highlighted >= 0 && _highlighted < _items.Count && ReferenceEquals(_items[_highlighted], item);

	internal void ResetHighlight()
	{
		_highlighted = -1;
		_typeAhead = string.Empty;
	}

	/// <summary>Closes this menu and runs the item's action, in that order, as the context menu does.</summary>
	/// <param name="item">The item that was chosen.</param>
	/// <returns>A task that completes when the action has run.</returns>
	internal async Task ChooseAsync(MokaMenuBarItem item)
	{
		Bar?.CloseAll();
		await item.InvokeAsync();
	}

	internal ValueTask FocusTriggerAsync() => SafeFocusAsync(_triggerRef);

	// The items do not exist yet when the bar opens the menu: StateHasChanged only queues the render that
	// creates them, so highlighting here would read an empty list. The ask is remembered and applied once
	// they have registered, in OnAfterRenderAsync.
	internal void FocusFirstItemAsync() => RequestHighlight(Pending.First);

	internal void FocusLastItemAsync() => RequestHighlight(Pending.Last);

	private void RequestHighlight(Pending pending)
	{
		_pending = pending;
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_pending != Pending.None)
		{
			Pending pending = _pending;

			// Cleared whatever happens: a menu closed before it rendered its items must not grab focus when
			// it next opens.
			_pending = Pending.None;

			if (IsOpen && _items.Count > 0)
			{
				await HighlightAsync(pending == Pending.First ? NextEnabled(-1, 1) : NextEnabled(_items.Count, -1));
			}
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	private enum Pending
	{
		None,
		First,
		Last
	}

	// The bar hands on whatever it did not use itself, so one keydown handler serves the bar and its menus.
	internal async Task HandleMenuKeyAsync(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "ArrowDown":
				await HighlightAsync(NextEnabled(_highlighted, 1));
				return;
			case "ArrowUp":
				await HighlightAsync(NextEnabled(_highlighted, -1));
				return;
			case "Home":
				await HighlightAsync(NextEnabled(-1, 1));
				return;
			case "End":
				await HighlightAsync(NextEnabled(_items.Count, -1));
				return;
			case "Enter":
			case " ":
				if (_highlighted >= 0 && _highlighted < _items.Count)
				{
					await ChooseAsync(_items[_highlighted]);
				}

				return;
			case "Tab":
				Bar?.CloseAll();
				return;
			default:
				await TypeAheadAsync(e.Key);
				return;
		}
	}

	private async Task HighlightAsync(int index)
	{
		if (index < 0 || index >= _items.Count)
		{
			return;
		}

		_highlighted = index;
		StateHasChanged();
		await _items[index].FocusAsync();
	}

	// Wraps, and steps over dividers and disabled items, which cannot be chosen anyway.
	private int NextEnabled(int from, int step)
	{
		if (_items.Count == 0)
		{
			return -1;
		}

		for (int i = 0; i < _items.Count; i++)
		{
			int index = (from + (step * (i + 1)) + (_items.Count * (i + 2))) % _items.Count;
			if (!_items[index].Disabled)
			{
				return index;
			}
		}

		return -1;
	}

	private async Task TypeAheadAsync(string key)
	{
		if (key.Length != 1 || char.IsControl(key[0]) || _items.Count == 0)
		{
			return;
		}

		DateTime now = DateTime.UtcNow;
		_typeAhead = now - _typeAheadAt > TypeAheadWindow ? key : _typeAhead + key;
		_typeAheadAt = now;

		int match = _items.FindIndex(i =>
			!i.Disabled && i.Text?.StartsWith(_typeAhead, StringComparison.CurrentCultureIgnoreCase) == true);

		if (match >= 0)
		{
			await HighlightAsync(match);
		}
	}

	private async Task HandleTriggerClick()
	{
		if (Bar is not null)
		{
			await Bar.ToggleAsync(this);
		}
	}

	private void HandleMouseEnter() => Bar?.HoverOpen(this);
}
