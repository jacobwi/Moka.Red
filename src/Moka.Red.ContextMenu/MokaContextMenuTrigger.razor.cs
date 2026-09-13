using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Wraps child content and attaches a context menu triggered by right-click, left-click, or both.
///     Uses <c>display: contents</c> for zero layout impact.
///     When <see cref="IMokaContextMenuService" /> is registered and a <see cref="MokaContextMenuHost" />
///     is mounted, the trigger opens the shared menu through the service so only one menu is open at a
///     time. Without a host it falls back to rendering its own menu.
/// </summary>
public partial class MokaContextMenuTrigger : ComponentBase
{
	private bool _isOpen;
	private IMokaContextMenuService? _service;
	private double _x;
	private double _y;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

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

	/// <summary>Whether right-clicks are routed to the shared, service-driven menu.</summary>
	private bool UsesSharedMenu => _service is { HasHost: true };

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized() => _service = Services.GetService<IMokaContextMenuService>();

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
		if (UsesSharedMenu)
		{
			_service!.Show(x, y, Items);
			return;
		}

		_x = x;
		_y = y;
		_isOpen = true;
	}

	private void Close() => _isOpen = false;
}
