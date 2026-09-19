using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.BottomSheet;

/// <summary>
///     A slide-up panel from the bottom of the screen.
///     Common on mobile, useful for actions and forms.
///     Supports backdrop close, escape close, dragging the handle down to close, and body scroll lock.
///     It is modal: focus moves into the sheet when it opens, Tab stays inside, and focus goes back to
///     where it was when it closes.
/// </summary>
public partial class MokaBottomSheet : MokaVisualComponentBase
{
	private const string DismissDragModule = "./_content/Moka.Red.Feedback/BottomSheet/MokaBottomSheet.razor.js";

	// Nothing used to move focus into the sheet, and its Escape handler only hears keys from inside,
	// so Escape did nothing until the user clicked into the sheet.
	private readonly MokaOverlayFocusTrap _focusTrap;
	private readonly string _headerId = $"moka-bottom-sheet-header-{Guid.NewGuid():N}";
	private DotNetObjectReference<MokaBottomSheet>? _dotNetRef;

	// The handle the drag-to-close listener sits on. Each open renders a new handle.
	private string? _dragHandleId;
	private ElementReference _handleRef;
	private IJSObjectReference? _jsModule;
	private Task<IJSObjectReference?>? _jsModuleImport;

	// What is rendered. The sheet closes itself (Escape, the backdrop, the handle), and Open only
	// replaces this when the parent passes a new value, so a parent render that repeats the old
	// value cannot reopen a sheet the user closed.
	private bool _open;
	private bool _openParameter;
	private bool _scrollLocked;
	private ElementReference _sheetRef;

	// The trap goes through the module this sheet already imports for the scroll lock.
	/// <summary>Creates the bottom sheet.</summary>
	public MokaBottomSheet() => _focusTrap = new MokaOverlayFocusTrap(TrapFocusAsync, ReleaseFocusAsync);

	/// <summary>The sheet body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Optional header content displayed at the top of the sheet.</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>
	///     Whether the bottom sheet is currently visible. Two-way bindable. The sheet can also close
	///     itself and reports that through <see cref="OpenChanged" />; a new value from the parent
	///     replaces that state.
	/// </summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>
	///     Whether a click on the backdrop, the area above the sheet, closes it. Defaults to true. The
	///     press and the release must both land on the backdrop, so a drag that starts in the sheet
	///     and ends outside it does not close it.
	/// </summary>
	[Parameter]
	public bool CloseOnBackdrop { get; set; } = true;

	/// <summary>Whether pressing Escape closes the sheet. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Maximum height of the sheet. Defaults to "70vh".</summary>
	[Parameter]
	public string MaxHeight { get; set; } = "70vh";

	/// <summary>
	///     Whether to show a drag handle at the top. Defaults to true. Dragging the handle down past 30%
	///     of the sheet's height, or 120px on a tall sheet, closes the sheet; a shorter drag lets it
	///     slide back. Without the handle the sheet cannot be dragged.
	/// </summary>
	[Parameter]
	public bool ShowHandle { get; set; } = true;

	/// <summary>Whether to expand to full screen height. Defaults to false.</summary>
	[Parameter]
	public bool FullScreen { get; set; }

	/// <summary>Whether to prevent body scrolling when open. Defaults to true.</summary>
	[Parameter]
	public bool PreventScroll { get; set; } = true;

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-bottom-sheet";

	private string SheetCss => new CssBuilder(RootClass)
		.AddClass("moka-bottom-sheet--fullscreen", FullScreen)
		.AddClass(Class)
		.Build();

	// The sheet is the box the user sees, so it takes the margin, padding and radius. A full-screen
	// sheet stretches to the wrapper in CSS instead of taking 100vh, which a margin would push off
	// the top of the screen.
	private string? SheetStyle => SpacingStyle()
		.AddStyle("max-height", MaxHeight, !FullScreen)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();

		if (Open == _openParameter)
		{
			return;
		}

		_openParameter = Open;
		if (Open == _open)
		{
			return;
		}

		_open = Open;
		if (_open)
		{
			await OnOpenedAsync();
		}
		else
		{
			await OnClosedAsync();
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Every close path (Escape, the backdrop, the handle, the parent) ends in a render without the
		// sheet, and the trap hands focus back after it.
		await _focusTrap.SyncAsync(() => _open, _sheetRef);

		if (firstRender && _open)
		{
			await OnOpenedAsync();
		}

		await SyncDismissDragAsync();
	}

	/// <summary>Called from JS when the handle was dragged down far enough to close the sheet.</summary>
	[JSInvokable]
	public async Task OnHandleDraggedDown()
	{
		await CloseAsync();

		// A call from JS is not a UI event, so nothing renders on its own, and an unbound
		// OpenChanged renders nobody.
		StateHasChanged();
	}

	// The listener goes away with the handle element, when the sheet closes or the handle is hidden,
	// so only a new handle needs setting up.
	private async Task SyncDismissDragAsync()
	{
		string? handle = _open && ShowHandle ? _handleRef.Id : null;
		if (string.Equals(handle, _dragHandleId, StringComparison.Ordinal))
		{
			return;
		}

		_dragHandleId = handle;
		if (handle is null)
		{
			return;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);
		await SafeModuleInvokeVoidAsync(DismissDragModule, "bindDismissDrag", _dotNetRef, _sheetRef, _handleRef);
	}

	private async ValueTask<int> TrapFocusAsync(ElementReference sheet)
	{
		await EnsureJsModuleAsync();

		if (_jsModule is null)
		{
			return 0;
		}

		try
		{
			return await _jsModule.InvokeAsync<int>("trapFocus", sheet);
		}
		catch (JSDisconnectedException)
		{
			return 0;
		}
		catch (ObjectDisposedException)
		{
			return 0;
		}
		catch (OperationCanceledException)
		{
			return 0;
		}
	}

	private async ValueTask ReleaseFocusAsync(int handle)
	{
		if (_jsModule is null)
		{
			return;
		}

		try
		{
			await _jsModule.InvokeVoidAsync("releaseFocus", handle);
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
		catch (OperationCanceledException)
		{
		}
	}

	// lockBodyScroll is reference counted on the JS side, so every lock this component
	// takes must be released exactly once - hence the _scrollLocked guard on both sides.
	private async Task OnOpenedAsync()
	{
		if (!PreventScroll || _scrollLocked)
		{
			return;
		}

		// Claimed before the await: a sheet created open gets here from OnParametersSetAsync and
		// again from its first render while the import is still running, and a second lock would
		// keep the page locked after the sheet closes.
		_scrollLocked = true;

		await EnsureJsModuleAsync();

		if (_jsModule is null)
		{
			_scrollLocked = false;
			return;
		}

		try
		{
			await _jsModule.InvokeVoidAsync("lockBodyScroll");
		}
		catch (JSDisconnectedException)
		{
			_scrollLocked = false;
		}
	}

	private async Task OnClosedAsync()
	{
		if (!_scrollLocked)
		{
			return;
		}

		_scrollLocked = false;

		if (_jsModule is null)
		{
			return;
		}

		try
		{
			await _jsModule.InvokeVoidAsync("unlockBodyScroll");
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async Task HandleBackdropClick()
	{
		if (CloseOnBackdrop)
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
		// A second Escape or click can arrive before the render that removes the sheet.
		if (!_open)
		{
			return;
		}

		_open = false;
		await OnClosedAsync();

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}
	}

	// OnParametersSetAsync and OnAfterRenderAsync can both ask for the module while an import is
	// still in flight, so they share one import instead of racing two.
	private async ValueTask EnsureJsModuleAsync()
	{
		if (_jsModule is not null)
		{
			return;
		}

		_jsModuleImport ??= ImportJsModuleAsync();
		_jsModule = await _jsModuleImport;

		if (_jsModule is null)
		{
			// Prerendering or a lost circuit: let a later call try again.
			_jsModuleImport = null;
		}
	}

	private async Task<IJSObjectReference?> ImportJsModuleAsync()
	{
		try
		{
			return await JsRuntime.InvokeAsync<IJSObjectReference>(
				"import", "./_content/Moka.Red.Feedback/moka-dialog.js");
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected during init
			return null;
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering
			return null;
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// An import still in flight is awaited, so its module is disposed below instead of leaking.
		if (_jsModule is null && _jsModuleImport is not null)
		{
			_jsModule = await _jsModuleImport;
		}

		await _focusTrap.ReleaseAsync();
		await OnClosedAsync();

		if (_jsModule is not null)
		{
			try
			{
				await _jsModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
			}

			_jsModule = null;
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
