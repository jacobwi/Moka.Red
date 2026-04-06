using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Wraps child content and attaches a context menu triggered by right-click, left-click, or both.
///     Uses <c>display: contents</c> for zero layout impact.
/// </summary>
public partial class MokaContextMenuTrigger : ComponentBase
{
	private bool _isOpen;
	private double _x;
	private double _y;

	/// <summary>The content that triggers the context menu.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The menu items to display.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaContextMenuItem> Items { get; set; } = [];

	/// <summary>How the context menu is triggered. Default is right-click.</summary>
	[Parameter]
	public MokaContextMenuTriggerType Trigger { get; set; } = MokaContextMenuTriggerType.RightClick;

	/// <summary>Whether the trigger is disabled (no menu will open).</summary>
	[Parameter]
	public bool Disabled { get; set; }

	private bool PreventDefault =>
		!Disabled && Trigger is MokaContextMenuTriggerType.RightClick or MokaContextMenuTriggerType.Both;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private void HandleRightClick(MouseEventArgs e)
	{
		if (Disabled || Trigger == MokaContextMenuTriggerType.LeftClick)
		{
			return;
		}

		Open(e.ClientX, e.ClientY);
	}

	private void HandleLeftClick(MouseEventArgs e)
	{
		if (Disabled || Trigger == MokaContextMenuTriggerType.RightClick)
		{
			return;
		}

		Open(e.ClientX, e.ClientY);
	}

	private void Open(double x, double y)
	{
		_x = x;
		_y = y;
		_isOpen = true;
	}

	private void Close() => _isOpen = false;
}
