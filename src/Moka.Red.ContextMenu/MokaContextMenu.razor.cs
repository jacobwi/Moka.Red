using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Renders a context menu at a fixed position with support for icons, shortcuts,
///     dividers, checked items, disabled items, nested sub-menus, and keyboard navigation.
/// </summary>
public partial class MokaContextMenu : ComponentBase
{
	private int _focusedIndex = -1;
	private MokaContextMenuItem? _hoveredSubmenuParent;
	private ElementReference _menuRef;
	private double _submenuX;
	private double _submenuY;

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

	/// <summary>Fires when the menu should close (backdrop click, Escape, or item activation).</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private string ItemClass(MokaContextMenuItem item, int index) => new CssBuilder("moka-ctx-item")
		.AddClass("moka-ctx-item--disabled", item.Disabled)
		.AddClass("moka-ctx-item--focused", index == _focusedIndex)
		.AddClass(item.CssClass)
		.Build();

	private async Task HandleItemClick(MokaContextMenuItem item)
	{
		if (item.Disabled || item.HasChildren)
		{
			return;
		}

		if (item.OnClick is not null)
		{
			await item.OnClick();
		}
		else
		{
			item.OnClickSync?.Invoke();
		}

		if (OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}

	private void HandleMouseEnterItem(MokaContextMenuItem item, MouseEventArgs e)
	{
		if (item.HasChildren)
		{
			_hoveredSubmenuParent = item;
			// Position sub-menu to the right of the parent item
			_submenuX = X + 200; // approximate menu width
			_submenuY = e.ClientY - 8;
		}
		else
		{
			_hoveredSubmenuParent = null;
		}
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		var actionItems = Items.Where(i => !i.DividerBefore || !string.IsNullOrEmpty(i.Text)).ToList();

		switch (e.Key)
		{
			case "Escape":
				if (OnClose.HasDelegate)
				{
					await OnClose.InvokeAsync();
				}

				break;
			case "ArrowDown":
				_focusedIndex = Math.Min(_focusedIndex + 1, actionItems.Count - 1);
				while (_focusedIndex < actionItems.Count && actionItems[_focusedIndex].Disabled)
				{
					_focusedIndex++;
				}

				break;
			case "ArrowUp":
				_focusedIndex = Math.Max(_focusedIndex - 1, 0);
				while (_focusedIndex >= 0 && actionItems[_focusedIndex].Disabled)
				{
					_focusedIndex--;
				}

				break;
			case "Enter" or " ":
				if (_focusedIndex >= 0 && _focusedIndex < actionItems.Count)
				{
					await HandleItemClick(actionItems[_focusedIndex]);
				}

				break;
			case "ArrowRight":
				if (_focusedIndex >= 0 && _focusedIndex < actionItems.Count && actionItems[_focusedIndex].HasChildren)
				{
					_hoveredSubmenuParent = actionItems[_focusedIndex];
				}

				break;
			case "ArrowLeft":
				_hoveredSubmenuParent = null;
				break;
		}
	}

	private async Task HandleBackdropClick()
	{
		if (OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}
}
