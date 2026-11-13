using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
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

	// The exit animation: _closing keeps the markup mounted while it runs, and the generation
	// stamps each pending removal so a reopen cancels the one it superseded.
	private bool _closing;
	private int _closeGeneration;
	private Task _exitTask = Task.CompletedTask;
	private MokaDialogPosition _positionParameter;
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

	/// <summary>
	///     Where the dialog appears. <see cref="MokaDialogPosition.Center" /> is the classic centered
	///     modal; the edges anchor a panel that fills them and slides in - full height from the left
	///     or right, full width from the top or bottom. Defaults to Center.
	/// </summary>
	[Parameter]
	public MokaDialogPosition Position { get; set; } = MokaDialogPosition.Center;

	/// <summary>
	///     The effect painted over the page behind the dialog. Defaults to
	///     <see cref="MokaDialogBackdrop.Default" />, the dimmed page with the standard blur.
	/// </summary>
	[Parameter]
	public MokaDialogBackdrop Backdrop { get; set; } = MokaDialogBackdrop.Default;

	/// <summary>Whether to prevent body scrolling when open. Defaults to true.</summary>
	[Parameter]
	public bool PreventScroll { get; set; } = true;

	/// <summary>Fires when the dialog is closed, as its exit animation starts.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <summary>
	///     Fires when the dialog's exit animation has finished and its markup is gone. A dialog
	///     disposed without exiting (the circuit ending) never raises it.
	/// </summary>
	[Parameter]
	public EventCallback OnExited { get; set; }

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

	/// <summary>Icon shown before the title in the header.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Header title markup, in place of the plain <see cref="Title" /> text. It still goes in the
	///     heading the dialog is named by, so put only the title in it.
	/// </summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Controls in the header, between the title and the close button.</summary>
	[Parameter]
	public RenderFragment? HeaderActions { get; set; }

	/// <summary>Whether the body has padding of its own. Default true. Turn it off for content that owns its edges.</summary>
	[Parameter]
	public bool BodyPadding { get; set; } = true;

	/// <summary>
	///     Whether the body scrolls. Default true. With it off the body clips and a single child fills it,
	///     so a view that scrolls itself shows the only scrollbar, as <c>MokaDockContent</c> does.
	/// </summary>
	[Parameter]
	public bool ScrollBody { get; set; } = true;

	/// <summary>Width of the dialog, overriding <see cref="DialogSize" />. A CSS length such as "60rem".</summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>Maximum width of the dialog. A CSS length such as "90vw".</summary>
	[Parameter]
	public string? MaxWidth { get; set; }

	/// <summary>Height of the dialog. A CSS length; the dialog never grows past the window either way.</summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>
	///     How this dialog is drawn. Null takes the theme's choice (<see cref="MokaTheme.WithDialogs" />), which is
	///     <see cref="MokaDialogLook.Standard" /> unless the app set one. Only the new design draws the looks: under
	///     <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> the dialog stays as it is, whatever this says.
	/// </summary>
	[Parameter]
	public MokaDialogLook? Look { get; set; }

	/// <summary>
	///     The tint of the tile the <see cref="Icon" /> sits in under the <see cref="MokaDialogLook.IconLead" /> and
	///     <see cref="MokaDialogLook.Centered" /> looks. Error, Warning, Success and Info take that meaning's tint;
	///     anything else, and null, the accent's. The other looks draw the icon without a tile and ignore it.
	/// </summary>
	[Parameter]
	public MokaColor? IconColor { get; set; }

	/// <summary>
	///     Raised for each key pressed while focus is in the dialog, Escape included, before the dialog acts on it, for a
	///     viewer that steps through pictures with the arrows. A key an open popup in the dialog keeps to itself, such as an
	///     arrow in an open select, doesn't reach it. The arguments carry no target, so a key typed in a field in the dialog
	///     arrives too. With <see cref="ScrollBody" /> on, an arrow also scrolls the body; a dialog that steps with the
	///     arrows turns it off, so the body clips and nothing scrolls (the page behind is locked while the dialog is open).
	/// </summary>
	[Parameter]
	public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

	/// <summary>
	///     Whether the dialog's action is running. While it is, the dialog can't close itself: Escape and a click on the
	///     backdrop do nothing, the close button is disabled, and the box carries <c>aria-busy</c>. A parent setting
	///     <see cref="Open" /> to false still closes it. Default false.
	/// </summary>
	[Parameter]
	public bool Busy { get; set; }

	/// <summary>
	///     Where focus goes when the dialog opens. Defaults to <see cref="MokaDialogInitialFocus.Content" />: the first
	///     control in the body, else the first action button, never the close button. An element marked <c>autofocus</c>
	///     or <c>data-autofocus</c> wins whatever this says.
	/// </summary>
	[Parameter]
	public MokaDialogInitialFocus InitialFocus { get; set; }

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	[Inject] private IJSRuntime JsRuntime { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-dialog";

	// The original design has no looks, so its markup stays the same whatever Look says.
	private MokaDialogLook ResolvedLook => CascadedTheme?.Look == MokaLook.Matrix
		? MokaDialogLook.Standard
		: Look ?? CascadedTheme?.Dialogs ?? MokaDialogLook.Standard;

	private string? LookClass => ResolvedLook switch
	{
		MokaDialogLook.Bands => "moka-dialog--bands",
		MokaDialogLook.IconLead => "moka-dialog--icon-lead",
		MokaDialogLook.Centered => "moka-dialog--centered",
		MokaDialogLook.Terminal => "moka-dialog--terminal",
		_ => null
	};

	private bool IconInTile => ResolvedLook is MokaDialogLook.IconLead or MokaDialogLook.Centered;

	private string TileIconSize => ResolvedLook == MokaDialogLook.Centered ? "22px" : "20px";

	private string IconTileCss => new CssBuilder("moka-dialog-icon-tile")
		.AddClass(IconColor switch
		{
			MokaColor.Error => "moka-dialog-icon-tile--error",
			MokaColor.Warning => "moka-dialog-icon-tile--warning",
			MokaColor.Success => "moka-dialog-icon-tile--success",
			MokaColor.Info => "moka-dialog-icon-tile--info",
			_ => null
		})
		.Build();

	private bool HasTitle => !string.IsNullOrEmpty(Title) || TitleContent is not null;

	private bool HasHeader => HasTitle || Icon.HasValue || HeaderActions is not null || ShowCloseButton;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-dialog--{SizeToKebab(DialogSize)}")
		.AddClass($"moka-dialog--{PositionToKebab(Position)}", Position != MokaDialogPosition.Center)
		.AddClass("moka-dialog--draggable", Draggable && Position == MokaDialogPosition.Center)
		.AddClass("moka-dialog--resizable", Resizable && Position == MokaDialogPosition.Center)
		.AddClass("moka-dialog--moved", _hasBeenMoved)
		.AddClass(LookClass)
		.AddClass(Class)
		.Build();

	private string WrapperCss => new CssBuilder("moka-dialog-wrapper")
		.AddClass($"moka-dialog-wrapper--{PositionToKebab(Position)}", Position != MokaDialogPosition.Center)
		.AddClass("moka-dialog-wrapper--closing", _closing)
		.Build();

	private string BackdropCss => new CssBuilder("moka-dialog-backdrop")
		.AddClass($"moka-dialog-backdrop--{BackdropToKebab(Backdrop)}", Backdrop != MokaDialogBackdrop.Default)
		.Build();

	private string HeaderCss => new CssBuilder("moka-dialog-header")
		.AddClass("moka-dialog-header--draggable", Draggable && Position == MokaDialogPosition.Center)
		.Build();

	private string BodyCss => new CssBuilder("moka-dialog-body")
		.AddClass("moka-dialog-body--flush", !BodyPadding)
		.AddClass("moka-dialog-body--no-scroll", !ScrollBody)
		.Build();

	// Invariant: under a culture with a decimal comma, "412,5px" is not CSS, and the dialog dropped
	// back to its centering left and top after the drag.
	private string? DialogBoxStyle => new StyleBuilder()
		.AddStyle("left", $"{_posX.ToString(CultureInfo.InvariantCulture)}px", _hasBeenMoved)
		.AddStyle("top", $"{_posY.ToString(CultureInfo.InvariantCulture)}px", _hasBeenMoved)
		.AddStyle("min-width", MinWidth, Resizable)
		.AddStyle("min-height", MinHeight, Resizable)
		.AddStyle("width", Width)
		.AddStyle("max-width", MaxWidth)
		.AddStyle("height", Height)
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

		if (Position != _positionParameter)
		{
			_positionParameter = Position;

			// A panel pinned to an edge has nowhere to be dragged to, so a dialog that moves off
			// Center while dragging takes its header handle off again.
			if (_dragAttached && Position != MokaDialogPosition.Center)
			{
				await DetachDragAsync();
			}
		}

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
			// A reopen while a previous close was still animating out cancels that removal; on the
			// still-mounted box the entrance animation replaces the exit one where it stood.
			_closing = false;
			_closeGeneration++;
			await OnOpenedAsync();
		}
		else
		{
			await OnClosedAsync();
			BeginExit();
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

		if (Draggable && Position == MokaDialogPosition.Center && !_dragAttached)
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
				new Dictionary<string, object?> { ["callbackMethod"] = "OnDragMoved", ["bounds"] = true });
			_dragAttached = true;
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
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

	// The WAI-ARIA dialog pattern starts focus on the first control, and on the least destructive action where a step
	// can't be undone, which the library's convention puts first among the actions. The header comes last: its close
	// button is marked data-moka-focus-last and is never chosen while the dialog itself can take focus.
	private Dictionary<string, object?> FocusOptions() => new()
	{
		["initial"] = InitialFocus switch
		{
			MokaDialogInitialFocus.FirstControl => "first",
			MokaDialogInitialFocus.Dialog => "self",
			_ => "content"
		},
		["order"] = ContentFocusOrder
	};

	private static readonly string[] ContentFocusOrder =
		[":scope > .moka-dialog-body", ":scope > .moka-dialog-actions", ":scope > .moka-dialog-header"];

	private async Task<int> TrapFocusAsync(ElementReference box)
	{
		await EnsureJsModuleAsync();

		if (_jsModule is null)
		{
			return 0;
		}

		try
		{
			return await _jsModule.InvokeAsync<int>("trapFocus", box, FocusOptions());
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
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

		if (handle is null)
		{
			return;
		}

		await _jsModule.SafeInvokeVoidAsync("releaseFocus", handle.Value);
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

		if (!await _jsModule.SafeInvokeVoidAsync("lockBodyScroll"))
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
		await _dragModule.SafeInvokeVoidAsync("removeDraggable", _headerRef);
	}

	private async Task ReleaseScrollLockAsync()
	{
		if (!_scrollLocked)
		{
			return;
		}

		_scrollLocked = false;
		await _jsModule.SafeInvokeVoidAsync("unlockBodyScroll");
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
		if (_open && OnKeyDown.HasDelegate)
		{
			await OnKeyDown.InvokeAsync(e);
		}

		if (CloseOnEscape && e.Key == "Escape")
		{
			await CloseAsync();
		}
	}

	// Every way the dialog closes itself: Escape, the backdrop and the close button. None of them while Busy.
	private async Task CloseAsync()
	{
		// A second Escape or click can arrive before the render that removes the dialog.
		if (!_open || Busy)
		{
			return;
		}

		_open = false;
		await OnClosedAsync();
		BeginExit();

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}

		if (OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}

	// The close is decided the moment it starts - the trap, the scroll lock and the callbacks have
	// all gone already - but the markup stays mounted for the exit animation, then leaves with a
	// render of its own. The generation stamps the pending removal: a reopen bumps it, and the
	// stale removal stands down without touching the reopened dialog.
	private void BeginExit()
	{
		_closing = true;
		_exitTask = CompleteExitAsync(_closeGeneration);
	}

	private async Task CompleteExitAsync(int generation)
	{
		try
		{
			await Task.Delay(ExitDurationMs);
			await InvokeAsync(() =>
			{
				if (_closeGeneration != generation || _open || !_closing)
				{
					return;
				}

				_closing = false;
				StateHasChanged();
			});

			if (_closeGeneration == generation && !_open)
			{
				await OnExited.InvokeAsync();
			}
		}
		catch (ObjectDisposedException)
		{
		}
		catch (OperationCanceledException)
		{
		}
	}

	// The durations the exit keyframes run for, after which the markup is dropped: the centered
	// dialog fades, an edge panel slides home.
	private int ExitDurationMs => Position == MokaDialogPosition.Center ? 200 : 250;

	/// <summary>Completes when the most recent close's exit animation has finished and the markup is gone.</summary>
	internal Task ExitCompletion => _exitTask;

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
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			// The circuit ended before the import came back. Disposal awaits this task, so it must not
			// throw: a closed tab otherwise logged a TaskCanceledException for every dialog on the page.
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
		MokaDialogSize.ExtraLarge => "xl",
		_ => "md"
	};

	private static string PositionToKebab(MokaDialogPosition position) => position switch
	{
		MokaDialogPosition.Center => "center",
		MokaDialogPosition.Left => "left",
		MokaDialogPosition.Right => "right",
		MokaDialogPosition.Top => "top",
		MokaDialogPosition.Bottom => "bottom",
		_ => "center"
	};

	private static string BackdropToKebab(MokaDialogBackdrop backdrop) => backdrop switch
	{
		MokaDialogBackdrop.Default => "default",
		MokaDialogBackdrop.Dim => "dim",
		MokaDialogBackdrop.Frosted => "frosted",
		MokaDialogBackdrop.Dark => "dark",
		MokaDialogBackdrop.None => "none",
		_ => "default"
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
		// itself, while the module is still there to do it. The generation bump stands down a
		// pending exit removal: there is no markup left to remove and no renderer to remove it with.
		_open = false;
		_closeGeneration++;
		await _focusTrapSetup;
		await OnClosedAsync();

		_dotNetRef?.Dispose();

		await _dragModule.SafeDisposeAsync();
		_dragModule = null;

		await _jsModule.SafeDisposeAsync();
		_jsModule = null;

		await base.DisposeAsyncCore();
	}
}
