using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Wraps child content and attaches a context menu triggered by right-click, left-click, or both.
///     Uses <c>display: contents</c> for zero layout impact.
///     When <see cref="IMokaContextMenuService" /> is registered and a <see cref="MokaContextMenuHost" />
///     is mounted, the trigger opens the shared menu through the service so only one menu is open at a
///     time. Without a host it falls back to rendering its own menu.
///     A left click that comes from the keyboard (Enter or Space on a button inside) opens the menu
///     under the trigger's content, since such a click has no pointer position, or above it when there
///     is no room below. A menu the keyboard opened starts with its first enabled item highlighted.
/// </summary>
public partial class MokaContextMenuTrigger : ComponentBase, IAsyncDisposable
{
	private const string ModulePath = "./_content/Moka.Red.ContextMenu/MokaContextMenuTrigger.razor.js";

	/// <summary>Space between the trigger's content and a menu opened under it, in pixels.</summary>
	private const double MenuGap = 4;

	private double? _anchorTop;
	private bool _disposed;
	private bool _isOpen;
	private IJSObjectReference? _module;
	private bool _openedFromKeyboard;
	private IMokaContextMenuService? _service;
	private ElementReference _wrapper;
	private double _x;
	private double _y;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	[Inject]
	private IJSRuntime JsRuntime { get; set; } = default!;

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
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (_module is not null)
		{
			try
			{
				await _module.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
				// Circuit already gone - nothing to release.
			}

			_module = null;
		}

		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized() => _service = Services.GetService<IMokaContextMenuService>();

	// The context-menu key and Shift+F10 land here too, at a position the browser picks on the
	// focused element.
	private void HandleRightClick(MouseEventArgs e)
	{
		if (Disabled || Trigger == MokaContextMenuTriggerType.LeftClick)
		{
			return;
		}

		Open(e.ClientX, e.ClientY, MokaContextMenuService.IsFromKeyboard(e), anchorTop: null);
	}

	private async Task HandleLeftClick(MouseEventArgs e)
	{
		if (Disabled || Trigger == MokaContextMenuTriggerType.RightClick)
		{
			return;
		}

		// A click from the keyboard reports no clicks (Detail 0) and a position of 0,0, which
		// would put the menu in the top-left corner of the viewport.
		if (e.Detail == 0 && await MeasureContentAsync() is { } content)
		{
			Open(content.Left, content.Bottom + MenuGap, fromKeyboard: true, anchorTop: content.Top - MenuGap);
			return;
		}

		Open(e.ClientX, e.ClientY, MokaContextMenuService.IsFromKeyboard(e), anchorTop: null);
	}

	private void Open(double x, double y, bool fromKeyboard, double? anchorTop)
	{
		if (UsesSharedMenu)
		{
			_service!.Show(x, y, Items, fromKeyboard, anchorTop);
			return;
		}

		_x = x;
		_y = y;
		_openedFromKeyboard = fromKeyboard;
		_anchorTop = anchorTop;
		_isOpen = true;
	}

	private void Close() => _isOpen = false;

	/// <summary>
	///     Measures the first element inside the wrapper that has a box. The wrapper itself is
	///     <c>display: contents</c> and has none. Returns null when JS is unavailable.
	/// </summary>
	private async Task<ContentBox?> MeasureContentAsync()
	{
		if (_disposed)
		{
			return null;
		}

		try
		{
			_module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
			return await _module.InvokeAsync<ContentBox?>("measureContent", _wrapper);
		}
		catch (JSDisconnectedException)
		{
			return null;
		}
		catch (ObjectDisposedException)
		{
			return null;
		}
		catch (OperationCanceledException)
		{
			// Covers TaskCanceledException - the circuit went away mid-call.
			return null;
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering.
			return null;
		}
	}

	/// <summary>Viewport edges of the trigger's content, as returned by <c>measureContent</c>.</summary>
	internal sealed class ContentBox
	{
		/// <summary>Left edge of the content.</summary>
		public double Left { get; set; }

		/// <summary>Top edge of the content. A menu that does not fit below the content opens above this.</summary>
		public double Top { get; set; }

		/// <summary>Bottom edge of the content.</summary>
		public double Bottom { get; set; }
	}
}
