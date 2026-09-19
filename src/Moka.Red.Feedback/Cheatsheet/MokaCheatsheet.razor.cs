using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.Cheatsheet;

/// <summary>
///     A keyboard-shortcut overlay: a centered modal listing shortcut groups in a
///     two-column grid, with descriptions on the left and key chips on the right.
///     Typically toggled by a "?" key handler in the host app. Renders nothing while closed.
/// </summary>
public partial class MokaCheatsheet : MokaVisualComponentBase
{
	private readonly MokaOverlayFocusTrap _focusTrap;
	private ElementReference _dialogRef;
	private bool _disposed;
	private bool? _lastOpen;
	private bool _open;
	private bool _scrollLocked;

	/// <summary>Creates the cheatsheet.</summary>
	public MokaCheatsheet() => _focusTrap = new MokaOverlayFocusTrap(
		element => SafeModuleInvokeAsync<int>(MokaOverlayFocusTrap.Module, "trapFocus", element),
		handle => SafeModuleInvokeVoidAsync(MokaOverlayFocusTrap.Module, "releaseFocus", handle));

	/// <summary>Whether the cheatsheet is currently visible. Two-way bindable. Renders nothing when false.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Heading shown in the header row. Defaults to "Keyboard Shortcuts".</summary>
	[Parameter]
	public string Title { get; set; } = "Keyboard Shortcuts";

	/// <summary>The shortcut groups rendered in the grid.</summary>
	[Parameter]
	public IReadOnlyList<MokaCheatsheetGroup>? Groups { get; set; }

	/// <summary>Whether clicking the backdrop closes the overlay. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnBackdropClick { get; set; } = true;

	/// <summary>Whether pressing Escape closes the overlay. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Maximum modal width. Accepts any CSS width value. Defaults to "720px".</summary>
	[Parameter]
	public string MaxWidth { get; set; } = "720px";

	/// <summary>Optional hint line rendered under the shortcut grid.</summary>
	[Parameter]
	public RenderFragment? FooterContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-cheatsheet";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("max-width", MaxWidth)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Open only seeds the state when the parent passes a new value. Copying it on every parent
		// render reopened an unbound cheatsheet the user had just closed.
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
		await _focusTrap.SyncAsync(() => _open, _dialogRef);
		await SyncScrollLockAsync();
	}

	// The page behind the open cheatsheet stays still, like behind a dialog. The lock in
	// moka-dialog.js is counted and shared with dialogs, drawers and bottom sheets, so the
	// cheatsheet holds at most one and gives back only that one. The flag flips before the call,
	// so a render that finishes while a call is still out cannot send a second lock. Every close
	// renders or disposes, and both end here.
	private async Task SyncScrollLockAsync()
	{
		bool wanted = _open && !_disposed;
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
}
