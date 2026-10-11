using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.SplitPane;

/// <summary>
///     A resizable two-panel layout (similar to VS Code's editor split).
///     Supports horizontal and vertical orientations with a draggable handle.
/// </summary>
public partial class MokaSplitPane : MokaComponentBase
{
	private const string JsModulePath = "./_content/Moka.Red.Layout/SplitPane/MokaSplitPane.razor.js";

	private double _currentSizePx;
	private bool _dragging;
	private DotNetObjectReference<MokaSplitPane>? _dotNetRef;
	private bool _jsDrag;
	private bool _observing;
	private ElementReference _rootRef;
	private double _startPointer;
	private double _startSize;

	/// <summary>Content rendered in the first (left or top) pane.</summary>
	[Parameter]
	public RenderFragment? FirstContent { get; set; }

	/// <summary>Content rendered in the second (right or bottom) pane.</summary>
	[Parameter]
	public RenderFragment? SecondContent { get; set; }

	/// <summary>Split orientation. Defaults to <see cref="MokaSplitOrientation.Horizontal" /> (side by side).</summary>
	[Parameter]
	public MokaSplitOrientation Orientation { get; set; } = MokaSplitOrientation.Horizontal;

	/// <summary>Initial size of the first pane. Accepts any CSS length (e.g., "50%", "300px"). Defaults to "50%".</summary>
	[Parameter]
	public string InitialSize { get; set; } = "50%";

	/// <summary>Minimum size for each pane. Accepts any CSS length. Defaults to "100px".</summary>
	[Parameter]
	public string MinSize { get; set; } = "100px";

	/// <summary>Whether the second pane is collapsed. Defaults to false.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Whether to show the drag handle between panes. Defaults to true.</summary>
	[Parameter]
	public bool ShowHandle { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-split-pane";

	private bool IsHorizontal => Orientation == MokaSplitOrientation.Horizontal;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-split-pane--horizontal", IsHorizontal)
		.AddClass("moka-split-pane--vertical", !IsHorizontal)
		.AddClass("moka-split-pane--collapsed", Collapsed)
		.AddClass("moka-split-pane--dragging", _dragging)
		.AddClass("moka-split-pane--no-handle", !ShowHandle)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-split-size",
			_currentSizePx > 0 ? $"{_currentSizePx.ToString(CultureInfo.InvariantCulture)}px" : InitialSize)
		.AddStyle("--moka-split-min", MinSize)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	// Attached only while .NET is the one dragging: a default EventCallback renders no listener at
	// all. An event handler on an element re-renders the component whatever the handler does, so a
	// permanent pointermove listener re-rendered both panes and everything in them on every move
	// across the split, dragging or not.
	private EventCallback<PointerEventArgs> PointerMoveHandler => DotNetDragging
		? EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerMove)
		: default;

	private EventCallback<PointerEventArgs> PointerUpHandler => DotNetDragging
		? EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerUp)
		: default;

	private bool DotNetDragging => _dragging && !_jsDrag;

	private async Task HandlePointerDown(PointerEventArgs e)
	{
		_dragging = true;
		_startPointer = IsHorizontal ? e.ClientX : e.ClientY;
		_startSize = _currentSizePx;

		// The browser runs the drag from here on, so a pointer move no longer renders both panes and
		// everything in them. HandlePointerMove stays as the fallback for the moves that arrive before
		// this resolves, and for a page where the module never loads.
		_jsDrag = await TryBeginJsDragAsync();
	}

	private async Task HandlePointerMove(PointerEventArgs e)
	{
		if (!_dragging || _jsDrag)
		{
			return;
		}

		double current = IsHorizontal ? e.ClientX : e.ClientY;
		double delta = current - _startPointer;
		double newSize = _startSize + delta;

		if (newSize > 0)
		{
			_currentSizePx = newSize;
		}

		await Task.CompletedTask;
	}

	private void HandlePointerUp(PointerEventArgs e)
	{
		// A browser drag ends through OnDragFinished, which carries the size it settled on.
		if (!_jsDrag)
		{
			_dragging = false;
		}
	}

	/// <summary>Called from JS when a handle drag ends, with the size the first pane settled on.</summary>
	/// <param name="sizePx">Size of the first pane in pixels.</param>
	[JSInvokable]
	public void OnDragFinished(double sizePx)
	{
		_jsDrag = false;
		_dragging = false;

		if (sizePx > 0)
		{
			_currentSizePx = sizePx;
		}

		StateHasChanged();
	}

	/// <summary>
	///     Called from JS when the root changes size. The snapshot every drag starts from is only right
	///     while the layout it was taken in holds, so a split that was hidden, or in a container that has
	///     since resized, measures again.
	/// </summary>
	[JSInvokable]
	public async Task OnRootResized()
	{
		if (_dragging)
		{
			return;
		}

		await MeasureFirstPaneAsync();
	}

	/// <summary>
	///     Measures the first pane again and sizes the split to what it finds. Call it after showing a
	///     split that was hidden while it first rendered, if the automatic watch cannot see the change.
	/// </summary>
	public Task MeasureAsync() => MeasureFirstPaneAsync();

	private async Task<bool> TryBeginJsDragAsync()
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(JsModulePath);
			_dotNetRef ??= DotNetObjectReference.Create(this);
			return await module.InvokeAsync<bool>(
				"beginDrag", _rootRef, IsHorizontal, _startPointer, _startSize, _dotNetRef);
		}
		catch (JSDisconnectedException)
		{
			return false;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
			return false;
		}
	}

	/// <summary>
	///     Captures the initial pixel size of the first pane on first render
	///     so that drag deltas can be applied correctly. Measured once: the drag
	///     handlers work off this snapshot and never call back into JS.
	/// </summary>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			if (_currentSizePx <= 0)
			{
				await MeasureFirstPaneAsync();
			}

			await StartObservingAsync();
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	private async Task StartObservingAsync()
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(JsModulePath);
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await module.InvokeVoidAsync("observeSize", _rootRef, _dotNetRef);
			_observing = true;
		}
		catch (JSDisconnectedException)
		{
		}
		catch (OperationCanceledException)
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_observing)
		{
			try
			{
				IJSObjectReference module = await GetJsModuleAsync(JsModulePath);
				await module.InvokeVoidAsync("stopObserving", _rootRef);
				await module.InvokeVoidAsync("endDrag", _rootRef);
			}
			catch (JSDisconnectedException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			catch (InvalidOperationException)
			{
			}
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;

		await base.DisposeAsyncCore();
	}

	private async Task MeasureFirstPaneAsync()
	{
		try
		{
			IJSObjectReference module = await GetJsModuleAsync(JsModulePath);
			double size = await module.InvokeAsync<double>("measureFirstPane", _rootRef, IsHorizontal);

			// A zero means the pane is not laid out yet - fall back to the CSS initial size. The pixel
			// of slack keeps the ResizeObserver from reacting to the size it just caused.
			if (size > 0 && Math.Abs(size - _currentSizePx) > 1)
			{
				_currentSizePx = size;
				StateHasChanged();
			}
		}
		catch (JSDisconnectedException)
		{
		}
		catch (OperationCanceledException)
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}
}
