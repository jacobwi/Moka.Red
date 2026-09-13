using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Default implementation of <see cref="IMokaContextMenuService" />. Holds the state of a
///     single shared menu and notifies <see cref="MokaContextMenuHost" /> to re-render on change.
/// </summary>
public sealed class MokaContextMenuService : IMokaContextMenuService
{
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
	public void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items)
	{
		ArgumentNullException.ThrowIfNull(items);

		X = x;
		Y = y;
		Items = items;
		Visible = true;
		OnChanged?.Invoke();
	}

	/// <inheritdoc />
	public void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items)
	{
		ArgumentNullException.ThrowIfNull(mouseEvent);

		Show(mouseEvent.ClientX, mouseEvent.ClientY, items);
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
		OnChanged?.Invoke();
	}
}
