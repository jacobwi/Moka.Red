using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.Drawer;

/// <summary>
///     A slide-in overlay panel from any screen edge.
///     Use for temporary content such as filters, settings, or navigation.
///     Unlike <c>MokaSidebar</c>, the drawer floats above page content with an optional backdrop.
/// </summary>
public partial class MokaDrawer : MokaComponentBase
{
	private readonly MokaOverlayFocusTrap _focusTrap;
	private bool _disposed;
	private ElementReference _drawerRef;
	private bool? _lastOpen;
	private bool _open;
	private bool _scrollLocked;

	/// <summary>Creates the drawer.</summary>
	public MokaDrawer() => _focusTrap = new MokaOverlayFocusTrap(
		element => SafeModuleInvokeAsync<int>(MokaOverlayFocusTrap.Module, "trapFocus", element),
		handle => SafeModuleInvokeVoidAsync(MokaOverlayFocusTrap.Module, "releaseFocus", handle));

	/// <summary>The drawer body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether the drawer is currently visible. Two-way bindable.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>The edge from which the drawer slides in. Defaults to <see cref="MokaDrawerPosition.Left" />.</summary>
	[Parameter]
	public MokaDrawerPosition Position { get; set; } = MokaDrawerPosition.Left;

	/// <summary>
	///     Drawer width for <see cref="MokaDrawerPosition.Left" /> and <see cref="MokaDrawerPosition.Right" /> positions.
	///     Accepts any CSS width value. Defaults to "320px".
	/// </summary>
	[Parameter]
	public string Width { get; set; } = "320px";

	/// <summary>
	///     Drawer height for <see cref="MokaDrawerPosition.Top" /> and <see cref="MokaDrawerPosition.Bottom" /> positions.
	///     Accepts any CSS height value. Defaults to "40vh".
	/// </summary>
	[Parameter]
	public string Height { get; set; } = "40vh";

	/// <summary>Optional title displayed in the drawer header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Whether to show a close button in the header. Defaults to true.</summary>
	[Parameter]
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>Whether clicking the backdrop closes the drawer. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnBackdropClick { get; set; } = true;

	/// <summary>Whether pressing Escape closes the drawer. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>
	///     Whether to show a backdrop overlay behind the drawer. Defaults to true. With it the drawer is
	///     modal: it takes focus, keeps Tab inside and stops the page behind it from scrolling.
	/// </summary>
	[Parameter]
	public bool Overlay { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-drawer";

	private bool IsHorizontal => Position is MokaDrawerPosition.Left or MokaDrawerPosition.Right;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-drawer--{PositionToKebab(Position)}")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", Width, IsHorizontal)
		.AddStyle("height", Height, !IsHorizontal)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Open only seeds the state when the parent passes a new value. Copying it on every parent
		// render reopened an unbound drawer the user had just closed.
		if (_lastOpen != Open)
		{
			_lastOpen = Open;
			_open = Open;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		// Only a drawer with Overlay is modal and takes focus. Without it the page stays usable,
		// so focus is left where it is.
		await _focusTrap.SyncAsync(() => _open && Overlay, _drawerRef);
		await SyncScrollLockAsync();
	}

	// A modal drawer keeps the page behind it still, like a dialog. The lock in moka-dialog.js is
	// counted and shared with dialogs and bottom sheets, so the drawer holds at most one and gives
	// back only that one. The flag flips before the call, so a render that finishes while a call
	// is still out cannot send a second lock. Every close renders or disposes, and both end here.
	private async Task SyncScrollLockAsync()
	{
		bool wanted = _open && Overlay && !_disposed;
		if (wanted == _scrollLocked)
		{
			return;
		}

		_scrollLocked = wanted;
		await SafeModuleInvokeVoidAsync(MokaOverlayFocusTrap.Module, wanted ? "lockBodyScroll" : "unlockBodyScroll");
	}

	private async Task HandleBackdropClick()
	{
		if (CloseOnBackdropClick)
		{
			await CloseAsync();
		}
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (CloseOnEscape && e.Key == "Escape")
		{
			await CloseAsync();
		}
	}

	private async Task CloseAsync()
	{
		_open = false;

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// Set first: a render still finishing must not take the lock again after it is given back.
		_disposed = true;
		await _focusTrap.ReleaseAsync();
		await SyncScrollLockAsync();
		await base.DisposeAsyncCore();
	}

	private static string PositionToKebab(MokaDrawerPosition position) => position switch
	{
		MokaDrawerPosition.Left => "left",
		MokaDrawerPosition.Right => "right",
		MokaDrawerPosition.Top => "top",
		MokaDrawerPosition.Bottom => "bottom",
		_ => "left"
	};
}
