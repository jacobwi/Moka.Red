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
	private ElementReference _dialogBoxRef;
	private ElementReference _dialogElement;
	private DotNetObjectReference<MokaDialog>? _dotNetRef;
	private bool _dragAttached;
	private IJSObjectReference? _dragModule;
	private int? _focusTrapHandle;
	private bool _hasBeenMoved;
	private ElementReference _headerRef;
	private IJSObjectReference? _jsModule;
	private double _posX;
	private double _posY;
	private bool _previousOpen;
	private bool _scrollLocked;

	/// <summary>Whether the dialog is currently visible. Two-way bindable.</summary>
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

	/// <summary>Whether clicking the backdrop closes the dialog. Defaults to true.</summary>
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

	private string? DialogBoxStyle => new StyleBuilder()
		.AddStyle("left", $"{_posX}px", _hasBeenMoved)
		.AddStyle("top", $"{_posY}px", _hasBeenMoved)
		.AddStyle("min-width", MinWidth, Resizable)
		.AddStyle("min-height", MinHeight, Resizable)
		.AddStyle(Style)
		.Build();

	/// <summary>Dialog has internal drag/position state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <summary>Called from JS when dialog is dragged to a new position.</summary>
	[JSInvokable]
	public void OnDragMoved(double x, double y)
	{
		_posX = x;
		_posY = y;
		_hasBeenMoved = true;
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();

		if (Open != _previousOpen)
		{
			_previousOpen = Open;
			if (Open)
			{
				await OnOpenedAsync();
			}
			else
			{
				await OnClosedAsync();
			}
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && Open)
		{
			await OnOpenedAsync();
		}

		if (!Open)
		{
			return;
		}

		if (Draggable && !_dragAttached)
		{
			await AttachDragAsync();
		}

		// The dialog box only exists in the DOM once Open has rendered, so the trap
		// is attached here rather than in OnOpenedAsync.
		if (_focusTrapHandle is null)
		{
			await AttachFocusTrapAsync();
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

	private async Task AttachFocusTrapAsync()
	{
		await EnsureJsModuleAsync();

		if (_jsModule is null)
		{
			return;
		}

		try
		{
			int handle = await _jsModule.InvokeAsync<int>("trapFocus", _dialogBoxRef);
			if (handle > 0)
			{
				_focusTrapHandle = handle;
			}
		}
		catch (JSDisconnectedException)
		{
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering
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

		await EnsureJsModuleAsync();

		if (_jsModule is null)
		{
			return;
		}

		try
		{
			await _jsModule.InvokeVoidAsync("lockBodyScroll");
			_scrollLocked = true;
		}
		catch (JSDisconnectedException)
		{
		}
	}

	private async Task OnClosedAsync()
	{
		await ReleaseFocusTrapAsync();
		await ReleaseScrollLockAsync();
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
		Open = false;
		_previousOpen = false;
		_hasBeenMoved = false;
		_dragAttached = false;

		if (_dragModule is not null)
		{
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

	private async ValueTask EnsureJsModuleAsync()
	{
		if (_jsModule is not null)
		{
			return;
		}

		try
		{
			_jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
				"import", "./_content/Moka.Red.Feedback/moka-dialog.js");
		}
		catch (JSDisconnectedException)
		{
			// Circuit disconnected during init
		}
		catch (InvalidOperationException)
		{
			// JS interop attempted during prerendering
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
		if (_dragAttached && _dragModule is not null)
		{
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

		await base.DisposeAsyncCore();
	}
}
