using System.Diagnostics.CodeAnalysis;
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

	/// <summary>Opens the menu at the position of a mouse event (typically an <c>@oncontextmenu</c> handler).</summary>
	void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items);

	/// <summary>Closes the menu if it is open.</summary>
	void Close();
}
