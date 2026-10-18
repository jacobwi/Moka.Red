using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.MenuBar;

/// <summary>
///     The row of menus along the top of an app: File, Edit, View. One tab stop for the whole bar, the arrow
///     keys between and inside the menus, Escape out, and type-ahead, as a desktop menu bar has.
///     Put <see cref="MokaMenuBarMenu" /> children in it, and <see cref="MokaMenuBarItem" /> children in those.
/// </summary>
public partial class MokaMenuBar : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	// The keys the bar acts on. Left to the page they scroll it, and Home and End jump the document.
	private static readonly string[] CancelledKeys =
		["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End", "Enter", " "];

	private readonly List<MokaMenuBarMenu> _menus = [];

	private ElementReference _barRef;
	private bool _keysBound;
	private int _openIndex = -1;

	// Which menu Tab lands on. It follows the arrow keys, so coming back to the bar returns to where you were.
	private int _tabStopIndex;

	private string _typeAhead = string.Empty;
	private DateTime _typeAheadAt = DateTime.MinValue;

	/// <summary>The menus, as <see cref="MokaMenuBarMenu" /> children.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Name of the menu bar, for screen readers. Defaults to "Main menu"; set it on a second bar so the two
	///     are told apart.
	/// </summary>
	[Parameter]
	public string AriaLabel { get; set; } = "Main menu";

	/// <summary>
	///     Whether moving the pointer onto another menu while one is open switches to it, the way a desktop menu
	///     bar does. Default true. Hovering opens nothing while the bar is closed.
	/// </summary>
	[Parameter]
	public bool OpenOnHover { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-menubar";

	/// <summary>The open and focused menus are internal state, so the bar always re-renders.</summary>
	protected override bool ShouldRender() => true;

	internal IReadOnlyList<MokaMenuBarMenu> Menus => _menus;

	internal bool AnyOpen => _openIndex >= 0;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	internal void Register(MokaMenuBarMenu menu)
	{
		if (!_menus.Contains(menu))
		{
			_menus.Add(menu);
		}
	}

	internal void Unregister(MokaMenuBarMenu menu)
	{
		int index = _menus.IndexOf(menu);
		if (index < 0)
		{
			return;
		}

		_menus.RemoveAt(index);

		// A menu removed while open, or before the open one, would otherwise leave the index pointing at a
		// different menu than the one that is actually showing.
		if (_openIndex == index)
		{
			_openIndex = -1;
		}
		else if (_openIndex > index)
		{
			_openIndex--;
		}

		if (_tabStopIndex >= _menus.Count)
		{
			_tabStopIndex = Math.Max(0, _menus.Count - 1);
		}
	}

	internal bool IsOpen(MokaMenuBarMenu menu) => _openIndex >= 0 && _openIndex < _menus.Count
		&& ReferenceEquals(_menus[_openIndex], menu);

	internal bool IsTabStop(MokaMenuBarMenu menu)
	{
		int index = _menus.IndexOf(menu);

		// Before anything is registered every menu would claim the tab stop, so fall back to the first.
		return index >= 0 && (index == _tabStopIndex || (_tabStopIndex >= _menus.Count && index == 0));
	}

	/// <summary>Closes whatever is open. A click outside the bar, or an item being chosen, ends up here.</summary>
	public void CloseAll()
	{
		if (_openIndex < 0)
		{
			return;
		}

		_openIndex = -1;
		StateHasChanged();
	}

	internal async Task ToggleAsync(MokaMenuBarMenu menu)
	{
		int index = _menus.IndexOf(menu);
		if (index < 0)
		{
			return;
		}

		_tabStopIndex = index;
		if (_openIndex == index)
		{
			_openIndex = -1;
			StateHasChanged();
			await menu.FocusTriggerAsync();
			return;
		}

		_openIndex = index;
		menu.ResetHighlight();
		StateHasChanged();
	}

	// A pointer moving along an open menu bar switches menus without a click, as a desktop one does.
	internal void HoverOpen(MokaMenuBarMenu menu)
	{
		if (!OpenOnHover || _openIndex < 0)
		{
			return;
		}

		int index = _menus.IndexOf(menu);
		if (index < 0 || index == _openIndex)
		{
			return;
		}

		_openIndex = index;
		_tabStopIndex = index;
		menu.ResetHighlight();
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && !_keysBound)
		{
			_keysBound = true;

			// Blazor's preventDefault is all or nothing on an element, and the bar also carries typing for
			// type-ahead, so the keys are cancelled one by one in JS.
			await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _barRef, CancelledKeys);
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (_menus.Count == 0)
		{
			return;
		}

		switch (e.Key)
		{
			case "ArrowRight":
				await MoveAsync(1);
				return;
			case "ArrowLeft":
				await MoveAsync(-1);
				return;
			case "Home":
				await MoveToAsync(0);
				return;
			case "End":
				await MoveToAsync(_menus.Count - 1);
				return;
			case "Escape":
				await CloseAndFocusAsync();
				return;
		}

		// Everything else belongs to the open menu: its own items, and the typing that jumps between them.
		if (_openIndex >= 0)
		{
			await _menus[_openIndex].HandleMenuKeyAsync(e);
			return;
		}

		switch (e.Key)
		{
			case "ArrowDown":
			case "Enter":
			case " ":
				await OpenAtAsync(_tabStopIndex, fromEnd: false);
				return;
			case "ArrowUp":
				await OpenAtAsync(_tabStopIndex, fromEnd: true);
				return;
		}

		await TypeAheadAsync(e.Key);
	}

	// Left and Right wrap, and carry an open menu with them: that is what makes a menu bar feel like one bar
	// rather than a row of separate dropdowns.
	private async Task MoveAsync(int step) =>
		await MoveToAsync((_tabStopIndex + step + _menus.Count) % _menus.Count);

	private async Task MoveToAsync(int index)
	{
		bool wasOpen = _openIndex >= 0;
		_tabStopIndex = index;

		if (wasOpen)
		{
			_openIndex = index;
			_menus[index].ResetHighlight();
			_menus[index].FocusFirstItemAsync();
			StateHasChanged();
			return;
		}

		StateHasChanged();
		await _menus[index].FocusTriggerAsync();
	}

	private async Task OpenAtAsync(int index, bool fromEnd)
	{
		_openIndex = index;
		_tabStopIndex = index;
		_menus[index].ResetHighlight();
		StateHasChanged();

		if (fromEnd)
		{
			_menus[index].FocusLastItemAsync();
		}
		else
		{
			_menus[index].FocusFirstItemAsync();
		}
	}

	internal async Task CloseAndFocusAsync()
	{
		if (_openIndex < 0)
		{
			return;
		}

		MokaMenuBarMenu menu = _menus[_openIndex];
		_openIndex = -1;
		StateHasChanged();
		await menu.FocusTriggerAsync();
	}

	// Typing a letter on a closed bar jumps to the menu that starts with it, as the arrow keys would.
	private async Task TypeAheadAsync(string key)
	{
		if (key.Length != 1 || char.IsControl(key[0]))
		{
			return;
		}

		DateTime now = DateTime.UtcNow;
		_typeAhead = now - _typeAheadAt > TypeAheadWindow ? key : _typeAhead + key;
		_typeAheadAt = now;

		int match = _menus.FindIndex(m =>
			m.Label?.StartsWith(_typeAhead, StringComparison.CurrentCultureIgnoreCase) == true);

		if (match >= 0)
		{
			await MoveToAsync(match);
		}
	}

	private static readonly TimeSpan TypeAheadWindow = TimeSpan.FromSeconds(1);
}
