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
		double? anchorTop = null)
	{
		ArgumentNullException.ThrowIfNull(items);

		X = x;
		Y = y;
		Items = items;
		OpenedFromKeyboard = openedFromKeyboard;
		AnchorTop = anchorTop;
		Visible = true;
		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items)
	{
		ArgumentNullException.ThrowIfNull(mouseEvent);

		Show(mouseEvent.ClientX, mouseEvent.ClientY, items, IsFromKeyboard(mouseEvent));
	}

	/// <inheritdoc />
	public void Close()
	{
		if (!Visible)
		{
			return;
		}

		Visible = false;
		Items = [];
		OpenedFromKeyboard = false;
		AnchorTop = null;
		OnChanged?.Invoke();
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
