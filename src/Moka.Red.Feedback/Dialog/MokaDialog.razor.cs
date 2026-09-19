using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Dialog;

/// <summary>
///     A declarative modal dialog component with backdrop, focus trap, and body scroll lock.
///     Supports two-way binding on <see cref="Open" />.
/// </summary>
public partial class MokaDialog : MokaComponentBase
{
	private readonly string _titleId = $"moka-dialog-title-{Guid.NewGuid():N}";
	private ElementReference _dialogBoxRef;
	private ElementReference _dialogElement;
	private DotNetObjectReference<MokaDialog>? _dotNetRef;
	private bool _dragAttached;
	private IJSObjectReference? _dragModule;
	private int? _focusTrapHandle;
	private Task _focusTrapSetup = Task.CompletedTask;
	private bool _hasBeenMoved;
	private ElementReference _headerRef;
	private IJSObjectReference? _jsModule;
	private Task<IJSObjectReference?>? _jsModuleImport;

	// What is rendered. The dialog closes itself (Escape, the backdrop, the close button), and Open
	// only replaces this when the parent passes a new value, so a parent render that repeats the
	// old value cannot reopen a dialog the user closed.
	private bool _open;
	private bool _openParameter;
	private double _posX;
	private double _posY;
	private bool _scrollLocked;

	/// <summary>
	///     Whether the dialog is currently visible. Two-way bindable. The dialog can also close itself,
	///     for example on Escape, and reports that through <see cref="OpenChanged" />; a new value from
	///     the parent replaces that state.
	/// </summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Dialog title displayed in the header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Dialog body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Footer actions slot for custom buttons.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Whether to show the close (X) button. Defaults to true.</summary>
	[Parameter]
	public bool ShowCloseButton { get; set; } = true;

	/// <summary>
	///     Whether a click on the backdrop, the area around the box, closes the dialog. Defaults to true.
	///     The press and the release must both land on the backdrop, so selecting text in the dialog
	///     and letting go outside it does not close it.
	/// </summary>
	[Parameter]
	public bool CloseOnBackdropClick { get; set; } = true;

	/// <summary>Whether pressing Escape closes the dialog. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Dialog width size. Defaults to <see cref="MokaDialogSize.Medium" />.</summary>
	[Parameter]
	public MokaDialogSize DialogSize { get; set; } = MokaDialogSize.Medium;

	/// <summary>Whether to prevent body scrolling when open. Defaults to true.</summary>
	[Parameter]
	public bool PreventScroll { get; set; } = true;

	/// <summary>Fires when the dialog is closed.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <summary>Whether the dialog can be dragged by its header. Default false.</summary>
	[Parameter]
	public bool Draggable { get; set; }

	/// <summary>Whether the dialog can be resized. Default false.</summary>
	[Parameter]
	public bool Resizable { get; set; }

	/// <summary>Minimum width when resizable. Default "200px".</summary>
	[Parameter]
	public string MinWidth { get; set; } = "200px";

	/// <summary>Minimum height when resizable. Default "100px".</summary>
	[Parameter]
	public string MinHeight { get; set; } = "100px";

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-dialog";

	private bool HasTitle => !string.IsNullOrEmpty(Title);

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-dialog--{SizeToKebab(DialogSize)}")
		.AddClass("moka-dialog--draggable", Draggable)
		.AddClass("moka-dialog--resizable", Resizable)
		.AddClass("moka-dialog--moved", _hasBeenMoved)
		.AddClass(Class)
		.Build();

	private string HeaderCss => new CssBuilder("moka-dialog-header")
		.AddClass("moka-dialog-header--draggable", Draggable)
		.Build();

	// Invariant: under a culture with a decimal comma, "412,5px" is not CSS, and the dialog dropped
	// back to its centering left and top after the drag.
	private string? DialogBoxStyle => new StyleBuilder()
		.AddStyle("left", $"{_posX.ToString(CultureInfo.InvariantCulture)}px", _hasBeenMoved)
		.AddStyle("top", $"{_posY.ToString(CultureInfo.InvariantCulture)}px", _hasBeenMoved)
		.AddStyle("min-width", MinWidth, Resizable)
		.AddStyle("min-height", MinHeight, Resizable)
		.AddStyle(Style)
		.Build();

	/// <summary>Dialog has internal drag/position state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <summary>Called from JS when the dialog is dragged to a new position.</summary>
	/// <param name="x">The CSS <c>left</c> the drag moved the dialog to, in pixels.</param>
	/// <param name="y">The CSS <c>top</c> the drag moved the dialog to, in pixels.</param>
	[JSInvokable]
	public void OnDragMoved(double x, double y)
	{
		_posX = x;
		_posY = y;
		_hasBeenMoved = true;
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();

		// Start loading the script before the first render, so a dialog created open traps focus as
		// soon as it has rendered instead of waiting on the import afterwards. While prerendering
		// there is no JS: the import fails quietly and the interactive render starts it again.
		// (Not gated on RendererInfo, which bUnit makes every consumer's test set up.)
		_jsModuleImport ??= ImportJsModuleAsync();
	}

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
		// Load the module while the dialog is still closed. Otherwise the first open waits on
		// an import before the trap can move focus, and keyboard input keeps landing on the
		// page behind the dialog until it does.
		if (firstRender)
		{
			await EnsureJsModuleAsync();
		}

		if (!_open)
		{
			return;
		}

		// The dialog box only exists in the DOM once Open has rendered, so the trap is attached
		// here rather than in OnOpenedAsync, and before any other interop so nothing delays it.
		if (_focusTrapHandle is null)
		{
			await AttachFocusTrapAsync();
		}

		if (firstRender)
		{
			await OnOpenedAsync();
		}

		if (Draggable && !_dragAttached)
		{
			await AttachDragAsync();
		}
	}

	private async Task AttachDragAsync()
	{
		try
		{
			_dragModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>(
				"import", "./_content/Moka.Red.Core/moka-drag.js");
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await _dragModule.InvokeVoidAsync("makeDraggable", _dotNetRef, _dialogBoxRef, _headerRef,
				new { callbackMethod = "OnDragMoved", bounds = true });
			_dragAttached = true;
		}
		catch (JSDisconnectedException)
		{
		}
		catch (InvalidOperationException)
		{
		}
	}

	// A render that finishes while trapFocus is still out must not set up a second trap. Its handle
	// replaced the first one, which was then never released.
	private Task AttachFocusTrapAsync()
	{
		if (!_focusTrapSetup.IsCompleted)
		{
			return Task.CompletedTask;
		}

		_focusTrapSetup = SetUpFocusTrapAsync();
		return _focusTrapSetup;
	}

	private async Task SetUpFocusTrapAsync()
	{
		while (_open && _focusTrapHandle is null)
		{
			ElementReference box = _dialogBoxRef;
			int handle = await TrapFocusAsync(box);
			if (handle <= 0)
			{
				return;
			}

			_focusTrapHandle = handle;
			if (_open && string.Equals(box.Id, _dialogBoxRef.Id, StringComparison.Ordinal))
			{
				return;
			}

			// Closed while trapFocus was out, so the close found no trap to release. Released here
			// instead, and set up again on the new box if the dialog opened again meanwhile.
			await ReleaseFocusTrapAsync();
		}
	}

	private async Task<int> TrapFocusAsync(ElementReference box)
	{
		await EnsureJsModuleAsync();

		if (_jsModule is null)
		{
			return 0;
		}

		try
		{
			return await _jsModule.InvokeAsync<int>("trapFocus", box);
		}
		catch (JSDisconnectedException)
		{
			return 0;
		}
		catch (OperationCanceledException)
		{
			return 0;
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering
			return 0;
		}
	}

	private async Task ReleaseFocusTrapAsync()
	{
		int? handle = _focusTrapHandle;
		_focusTrapHandle = null;

		if (handle is null || _jsModule is null)
		{
			return;
		}

		try
		{
			await _jsModule.InvokeVoidAsync("releaseFocus", handle.Value);
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
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

		// Claimed before the await: a dialog created open reaches here from OnParametersSetAsync and
		// again after its first render, and the lock is counted, so taking it twice would leave the
		// page locked after the dialog closes.
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

	// Every close ends here: the dialog's own controls, a parent setting Open to false, and disposal.
	// Closing from the parent used to skip the drag reset, so a reopened dialog kept its old position
	// and its new header was never made draggable.
	private async Task OnClosedAsync()
	{
		_hasBeenMoved = false;
		await ReleaseFocusTrapAsync();
		await ReleaseScrollLockAsync();
		await DetachDragAsync();
	}

	private async Task DetachDragAsync()
	{
		if (!_dragAttached)
		{
			return;
		}

		_dragAttached = false;

		if (_dragModule is null)
		{
			return;
		}

		try
		{
			await _dragModule.InvokeVoidAsync("removeDraggable", _headerRef);
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async Task ReleaseScrollLockAsync()
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
		// A second Escape or click can arrive before the render that removes the dialog.
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

		if (OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}

	// OnParametersSetAsync (scroll lock) and OnAfterRenderAsync (focus trap) can both ask for the
	// module while an import is still in flight, so they share one import instead of racing two.
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

	private static string SizeToKebab(MokaDialogSize size) => size switch
	{
		MokaDialogSize.Small => "sm",
		MokaDialogSize.Medium => "md",
		MokaDialogSize.Large => "lg",
		MokaDialogSize.FullScreen => "fullscreen",
		_ => "md"
	};

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// The import started in OnInitialized may still be running. Wait for it so the trap and the
		// scroll lock are released and the module is disposed below instead of leaking.
		if (_jsModule is null && _jsModuleImport is not null)
		{
			_jsModule = await _jsModuleImport;
		}

		// A trap still being set up sees the dialog closed when trapFocus comes back and releases
		// itself, while the module is still there to do it.
		_open = false;
		await _focusTrapSetup;
		await OnClosedAsync();

		_dotNetRef?.Dispose();

		if (_dragModule is not null)
		{
			try
			{
				await _dragModule.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
			}

			_dragModule = null;
		}

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

		await base.DisposeAsyncCore();
	}
}
