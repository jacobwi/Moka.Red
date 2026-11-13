using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Default implementation of <see cref="IMokaContextMenuService" />. Holds the state of a
///     single shared menu and notifies <see cref="MokaContextMenuHost" /> to re-render on change.
/// </summary>
public sealed class MokaContextMenuService : IMokaContextMenuService
{
	/// <summary>The value <see cref="MouseEventArgs.Button" /> has for the secondary (right) mouse button.</summary>
	private const long SecondaryButton = 2;

	private int _hostCount;

	/// <inheritdoc />
	public event Action? OnChanged;

	/// <inheritdoc />
	public bool Visible { get; private set; }

	/// <inheritdoc />
	public double X { get; private set; }

	/// <inheritdoc />
	public double Y { get; private set; }

	/// <inheritdoc />
	public IReadOnlyList<MokaContextMenuItem> Items { get; private set; } = [];

	/// <inheritdoc />
	public bool OpenedFromKeyboard { get; private set; }

	/// <inheritdoc />
	public double? AnchorTop { get; private set; }

	/// <inheritdoc />
	public bool AnchorToFocus { get; private set; }

	/// <inheritdoc />
	public IHandleEvent? Owner { get; private set; }

	/// <inheritdoc />
	public MokaRadialMenuRequest? Radial { get; private set; }

	/// <inheritdoc />
	public bool HasHost => _hostCount > 0;

	/// <inheritdoc />
	public void RegisterHost() => _hostCount++;

	/// <inheritdoc />
	public void UnregisterHost()
	{
		if (_hostCount > 0)
		{
			_hostCount--;
		}

		if (_hostCount == 0)
		{
			Close();
		}
	}

	/// <inheritdoc />
	public void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items) =>
		Show(x, y, items, openedFromKeyboard: false);

	/// <inheritdoc />
	public void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items, bool openedFromKeyboard,
		double? anchorTop = null) =>
		ShowCore(x, y, items, openedFromKeyboard, anchorTop, anchorToFocus: false, owner: null);

	/// <inheritdoc />
	public void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items, IHandleEvent? owner,
		bool openedFromKeyboard = false, double? anchorTop = null) =>
		ShowCore(x, y, items, openedFromKeyboard, anchorTop, anchorToFocus: false, owner);

	/// <inheritdoc />
	public void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items) => Show(mouseEvent, items, owner: null);

	/// <inheritdoc />
	public void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items, IHandleEvent? owner)
	{
		ArgumentNullException.ThrowIfNull(mouseEvent);

		// Enter or Space on a button reports 0,0, so the menu had nothing to open under and landed in
		// the corner of the viewport. Let it find the focused element instead, which is still the one
		// the user acted on: the menu takes focus only after it has positioned itself. The context-menu
		// key is left alone, because the browser positions that event on the focused element already.
		bool fromKeyboard = IsFromKeyboard(mouseEvent);
		bool noPosition = fromKeyboard && mouseEvent.ClientX == 0 && mouseEvent.ClientY == 0;

		ShowCore(mouseEvent.ClientX, mouseEvent.ClientY, items, fromKeyboard, anchorTop: null, noPosition, owner);
	}

	// One place where a menu opens, so every open leaves coherent state and raises OnChanged once.
	private void ShowCore(double x, double y, IReadOnlyList<MokaContextMenuItem> items, bool openedFromKeyboard,
		double? anchorTop, bool anchorToFocus, IHandleEvent? owner)
	{
		ArgumentNullException.ThrowIfNull(items);

		X = x;
		Y = y;
		Items = items;
		OpenedFromKeyboard = openedFromKeyboard;
		AnchorTop = anchorTop;
		AnchorToFocus = anchorToFocus;
		Owner = owner;
		Visible = true;
		Radial = null;
		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void ShowRadial(double x, double y, MokaRadialMenuSpec spec, bool openedFromKeyboard = false) =>
		ShowRadial(x, y, spec, owner: null, openedFromKeyboard);

	/// <inheritdoc />
	public void ShowRadial(double x, double y, MokaRadialMenuSpec spec, IHandleEvent? owner, bool openedFromKeyboard = false)
	{
		ArgumentNullException.ThrowIfNull(spec);
		ShowRadialCore(new MokaRadialMenuRequest(spec, x, y, openedFromKeyboard) { Owner = owner });
	}

	/// <inheritdoc />
	public void ShowRadial(MouseEventArgs mouseEvent, MokaRadialMenuSpec spec) => ShowRadial(mouseEvent, spec, owner: null);

	/// <inheritdoc />
	public void ShowRadial(MouseEventArgs mouseEvent, MokaRadialMenuSpec spec, IHandleEvent? owner)
	{
		ArgumentNullException.ThrowIfNull(mouseEvent);
		ArgumentNullException.ThrowIfNull(spec);

		// A keyboard event has no point of its own worth keeping, and the ring belongs around the thing it
		// acts on, so it centres on the focused element. Buttons tells a press still held from a released one.
		bool fromKeyboard = IsFromKeyboard(mouseEvent);
		ShowRadialCore(new MokaRadialMenuRequest(spec, mouseEvent.ClientX, mouseEvent.ClientY, fromKeyboard,
			AnchorToFocus: fromKeyboard, Flick: !fromKeyboard && mouseEvent.Buttons != 0) { Owner = owner });
	}

	private void ShowRadialCore(MokaRadialMenuRequest request)
	{
		ResetListMenu();
		Radial = request;
		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void Close()
	{
		if (!Visible && Radial is null)
		{
			return;
		}

		ResetListMenu();
		Radial = null;
		OnChanged?.Invoke();
	}

	private void ResetListMenu()
	{
		Visible = false;
		Items = [];
		OpenedFromKeyboard = false;
		AnchorTop = null;
		AnchorToFocus = false;
		Owner = null;
	}

	/// <summary>
	///     Whether the keyboard caused a <c>click</c> or <c>contextmenu</c> event. Enter or Space on a button
	///     clicks with no click count (<c>Detail</c> 0). The context-menu key and Shift+F10 fire
	///     <c>contextmenu</c> with no button pressed (<c>Button</c> 0), where a right click reports 2.
	///     <c>Detail</c> alone does not tell them apart: Chromium reports 0 for a right click too.
	/// </summary>
	internal static bool IsFromKeyboard(MouseEventArgs mouseEvent) =>
		mouseEvent.Detail == 0 && mouseEvent.Button != SecondaryButton;
}
