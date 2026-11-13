using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Service for showing a single, app-wide context menu at an arbitrary screen position.
///     Register with <c>AddMokaContextMenu()</c> and place one <see cref="MokaContextMenuHost" />
///     in your layout. Any component can then inject this service and call
///     <see cref="Show(MouseEventArgs, IReadOnlyList{MokaContextMenuItem})" /> from a right-click
///     handler.
/// </summary>
[SuppressMessage("Design", "CA1003:Use generic event handler instances",
	Justification = "A parameterless Action is simpler for this lightweight state-changed event.")]
public interface IMokaContextMenuService
{
	/// <summary>Raised whenever the menu state changes so the host can re-render.</summary>
	event Action OnChanged;

	/// <summary>Whether the menu is currently open.</summary>
	bool Visible { get; }

	/// <summary>Horizontal position in pixels from the left edge of the viewport.</summary>
	double X { get; }

	/// <summary>Vertical position in pixels from the top edge of the viewport.</summary>
	double Y { get; }

	/// <summary>The items shown by the currently open menu.</summary>
	IReadOnlyList<MokaContextMenuItem> Items { get; }

	/// <summary>
	///     Whether the keyboard opened the current menu. The menu then starts with its first enabled item
	///     highlighted, so Enter chooses it straight away.
	/// </summary>
	bool OpenedFromKeyboard => false;

	/// <summary>
	///     Top edge of what the current menu opens under, in viewport pixels, or null for a menu at a pointer.
	///     A menu that does not fit below <see cref="Y" /> opens above this line instead of covering it.
	/// </summary>
	double? AnchorTop => null;

	/// <summary>
	///     Whether the current menu should open under whatever has focus rather than at <see cref="X" /> and
	///     <see cref="Y" />. Set for a menu opened from the keyboard, which reports no position of its own.
	/// </summary>
	bool AnchorToFocus => false;

	/// <summary>
	///     The component that redraws after an item of the current menu runs, or null for a menu opened without one,
	///     whose items redraw only the menu. Set by the overloads that take an owner, and passed to the menu by
	///     <see cref="MokaContextMenuHost" />.
	/// </summary>
	IHandleEvent? Owner => null;

	/// <summary>
	///     Whether a <see cref="MokaContextMenuHost" /> is mounted and able to render the shared menu.
	///     Components that can also render their own menu (such as <see cref="MokaContextMenuTrigger" />)
	///     route through this service when it is <c>true</c> so only one menu is ever open.
	/// </summary>
	bool HasHost => false;

	/// <summary>Registers the mounted host. Called by <see cref="MokaContextMenuHost" />.</summary>
	void RegisterHost()
	{
		// Implementations that do not track a host keep HasHost false.
	}

	/// <summary>Unregisters the host when it is disposed. Called by <see cref="MokaContextMenuHost" />.</summary>
	void UnregisterHost()
	{
		// Implementations that do not track a host keep HasHost false.
	}

	/// <summary>Opens the menu at the given viewport coordinates.</summary>
	void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items);

	/// <summary>
	///     Opens the menu at the given viewport coordinates and says how it was opened. Use it from a keyboard
	///     handler, or to open a menu under an element such as a button.
	/// </summary>
	/// <param name="x">Left edge of the menu in viewport pixels.</param>
	/// <param name="y">Top edge of the menu in viewport pixels.</param>
	/// <param name="items">The menu items.</param>
	/// <param name="openedFromKeyboard">
	///     Whether the keyboard opened the menu. It then starts with its first enabled item highlighted.
	/// </param>
	/// <param name="anchorTop">
	///     Top edge of the element the menu opens under. A menu that does not fit below <paramref name="y" />
	///     opens above this line instead. Leave it null for a menu at a pointer, which then opens above
	///     <paramref name="y" />.
	/// </param>
	void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items, bool openedFromKeyboard, double? anchorTop = null) =>
		Show(x, y, items);

	/// <summary>
	///     Opens the menu at the position of a mouse event (typically an <c>@oncontextmenu</c> handler). An event
	///     that the keyboard caused, such as the context-menu key or Enter on a button, opens it the way
	///     <see cref="Show(double, double, IReadOnlyList{MokaContextMenuItem}, bool, double?)" /> does with
	///     <c>openedFromKeyboard</c> set.
	/// </summary>
	void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items);

	/// <summary>
	///     Opens the menu at the position of a mouse event, as
	///     <see cref="Show(MouseEventArgs, IReadOnlyList{MokaContextMenuItem})" /> does, and runs each item through
	///     <paramref name="owner" />, the way an <see cref="EventCallback" /> runs through its receiver: the owner redraws
	///     after the item's <see cref="MokaContextMenuItem.OnClick" /> starts and again when it finishes, so whatever the
	///     item changed shows without a <c>StateHasChanged</c>. From a component, pass <c>this</c>.
	/// </summary>
	/// <param name="mouseEvent">The event that opens the menu.</param>
	/// <param name="items">The menu items.</param>
	/// <param name="owner">The component that redraws after an item runs. Null runs items from the menu, as the other overloads do.</param>
	void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items, IHandleEvent? owner) =>
		Show(mouseEvent, items);

	/// <summary>
	///     Opens the menu at the given viewport coordinates, as
	///     <see cref="Show(double, double, IReadOnlyList{MokaContextMenuItem}, bool, double?)" /> does, and runs each item
	///     through <paramref name="owner" />, which redraws afterwards as an <see cref="EventCallback" />'s receiver does.
	/// </summary>
	/// <param name="x">Left edge of the menu in viewport pixels.</param>
	/// <param name="y">Top edge of the menu in viewport pixels.</param>
	/// <param name="items">The menu items.</param>
	/// <param name="owner">The component that redraws after an item runs. Null runs items from the menu.</param>
	/// <param name="openedFromKeyboard">Whether the keyboard opened the menu.</param>
	/// <param name="anchorTop">Top edge of the element the menu opens under, or null for a menu at a pointer.</param>
	void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items, IHandleEvent? owner, bool openedFromKeyboard = false,
		double? anchorTop = null) =>
		Show(x, y, items, openedFromKeyboard, anchorTop);

	/// <summary>
	///     The radial menu that is open, or null. A list menu and a radial menu never show at the same time:
	///     opening one closes the other, and <see cref="Visible" /> stays about the list menu.
	/// </summary>
	MokaRadialMenuRequest? Radial => null;

	/// <summary>Opens a <see cref="MokaRadialMenu" /> centred on a point in the viewport, from code.</summary>
	/// <param name="x">The centre's distance from the left of the viewport, in pixels.</param>
	/// <param name="y">The centre's distance from the top of the viewport, in pixels.</param>
	/// <param name="spec">What the menu holds.</param>
	/// <param name="openedFromKeyboard">Whether the keyboard opened it. It then starts on its first button.</param>
	void ShowRadial(double x, double y, MokaRadialMenuSpec spec, bool openedFromKeyboard = false)
	{
		// Implementations without radial menus show nothing.
	}

	/// <summary>
	///     Opens a <see cref="MokaRadialMenu" /> around where a mouse event happened, typically in an
	///     <c>@oncontextmenu</c> handler. The context-menu key or Shift+F10 centres it on the focused element and
	///     starts on its first button. A button still held when it opens (a right press on macOS and Linux, a long
	///     press) picks whatever it is let go over.
	/// </summary>
	/// <param name="mouseEvent">The event that opens it.</param>
	/// <param name="spec">What the menu holds.</param>
	void ShowRadial(MouseEventArgs mouseEvent, MokaRadialMenuSpec spec)
	{
		// Implementations without radial menus show nothing.
	}

	/// <summary>
	///     Opens a <see cref="MokaRadialMenu" /> around where a mouse event happened, as
	///     <see cref="ShowRadial(MouseEventArgs, MokaRadialMenuSpec)" /> does, and runs the spec's
	///     <see cref="MokaRadialMenuSpec.OnPick" /> through <paramref name="owner" />, which redraws afterwards as an
	///     <see cref="EventCallback" />'s receiver does. From a component, pass <c>this</c>.
	/// </summary>
	/// <param name="mouseEvent">The event that opens it.</param>
	/// <param name="spec">What the menu holds.</param>
	/// <param name="owner">The component that redraws after a pick. Null runs the pick from the menu.</param>
	void ShowRadial(MouseEventArgs mouseEvent, MokaRadialMenuSpec spec, IHandleEvent? owner) =>
		ShowRadial(mouseEvent, spec);

	/// <summary>
	///     Opens a <see cref="MokaRadialMenu" /> centred on a point, as
	///     <see cref="ShowRadial(double, double, MokaRadialMenuSpec, bool)" /> does, and runs the spec's
	///     <see cref="MokaRadialMenuSpec.OnPick" /> through <paramref name="owner" />.
	/// </summary>
	/// <param name="x">The centre's distance from the left of the viewport, in pixels.</param>
	/// <param name="y">The centre's distance from the top of the viewport, in pixels.</param>
	/// <param name="spec">What the menu holds.</param>
	/// <param name="owner">The component that redraws after a pick. Null runs the pick from the menu.</param>
	/// <param name="openedFromKeyboard">Whether the keyboard opened it. It then starts on its first button.</param>
	void ShowRadial(double x, double y, MokaRadialMenuSpec spec, IHandleEvent? owner, bool openedFromKeyboard = false) =>
		ShowRadial(x, y, spec, openedFromKeyboard);

	/// <summary>Closes the menu if it is open.</summary>
	void Close();
}
