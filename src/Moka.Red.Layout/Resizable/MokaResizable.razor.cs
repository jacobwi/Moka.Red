using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Resizable;

/// <summary>
///     A wrapper component that makes any child content resizable by dragging edge handles.
///     Supports horizontal, vertical, or both directions with two-way bindable size values.
///     The edge handles are focusable separators: the arrow keys resize by 10px (50px with Shift),
///     Home and End go to the minimum and maximum size.
/// </summary>
public partial class MokaResizable : MokaComponentBase
{
	private const string JsModulePath = "./_content/Moka.Red.Layout/Resizable/MokaResizable.razor.js";

	// What each handle's listener was set up with. Direction adds and removes handle elements, and
	// the listener keeps the limits it was given, so a new element or new limits need a new listener.
	// A flag per handle used to say "attached" for good: a handle Direction brought back stayed dead,
	// and MinWidth and the other limits never reached the drag after the first render.
	private Attachment? _bottom;
	private ElementReference _bottomHandleRef;
	private ElementReference _containerRef;
	private Attachment? _corner;
	private ElementReference _cornerHandleRef;
	private DotNetObjectReference<MokaResizable>? _dotNetRef;
	private Task _handleSync = Task.CompletedTask;

	// The rendered size. A drag changes it, and Width/Height only replace it when the parent passes
	// a new value, so a parent render that repeats the old value cannot undo a drag.
	private string? _height;
	private string? _heightParameter;
	private double _heightPx;
	private IJSObjectReference? _jsModule;
	private Attachment? _right;
	private ElementReference _rightHandleRef;
	private string? _width;
	private string? _widthParameter;
	private double _widthPx;

	/// <summary>The content to make resizable.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Which directions are resizable. Default Horizontal.</summary>
	[Parameter]
	public MokaResizeDirection Direction { get; set; } = MokaResizeDirection.Horizontal;

	/// <summary>
	///     Width as a CSS value. Two-way bindable. A drag keeps its size across parent renders, and a new
	///     value from the parent replaces it.
	/// </summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>Callback when <see cref="Width" /> changes.</summary>
	[Parameter]
	public EventCallback<string> WidthChanged { get; set; }

	/// <summary>
	///     Height as a CSS value. Two-way bindable. A drag keeps its size across parent renders, and a new
	///     value from the parent replaces it.
	/// </summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>Callback when <see cref="Height" /> changes.</summary>
	[Parameter]
	public EventCallback<string> HeightChanged { get; set; }

	/// <summary>Minimum width constraint.</summary>
	[Parameter]
	public string? MinWidth { get; set; }

	/// <summary>Maximum width constraint.</summary>
	[Parameter]
	public string? MaxWidth { get; set; }

	/// <summary>Minimum height constraint.</summary>
	[Parameter]
	public string? MinHeight { get; set; }

	/// <summary>Maximum height constraint.</summary>
	[Parameter]
	public string? MaxHeight { get; set; }

	/// <summary>Whether to show a visible grip indicator on handles. Default true.</summary>
	[Parameter]
	public bool ShowHandle { get; set; } = true;

	/// <summary>Callback when resize completes with new size in pixels.</summary>
	[Parameter]
	public EventCallback<MokaResizeResult> OnResized { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-resizable";

	private bool HasRightHandle => Direction is MokaResizeDirection.Horizontal or MokaResizeDirection.Both;

	private bool HasBottomHandle => Direction is MokaResizeDirection.Vertical or MokaResizeDirection.Both;

	private bool HasCornerHandle => Direction == MokaResizeDirection.Both;

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", _width, !string.IsNullOrEmpty(_width))
		.AddStyle("height", _height, !string.IsNullOrEmpty(_height))
		.AddStyle("min-width", MinWidth, !string.IsNullOrEmpty(MinWidth))
		.AddStyle("max-width", MaxWidth, !string.IsNullOrEmpty(MaxWidth))
		.AddStyle("min-height", MinHeight, !string.IsNullOrEmpty(MinHeight))
		.AddStyle("max-height", MaxHeight, !string.IsNullOrEmpty(MaxHeight))
		.AddStyle(Style)
		.Build();

	/// <summary>MokaResizable has internal resize state that requires re-rendering.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Track both axes so OnResized can report a complete size even when only
		// one axis was dragged. Non-px values (%, vh, auto) leave the axis unknown.
		if (!string.Equals(Width, _widthParameter, StringComparison.Ordinal))
		{
			_widthParameter = Width;
			_width = Width;
			if (TryParsePx(Width, out double widthPx))
			{
				_widthPx = widthPx;
			}
		}

		if (!string.Equals(Height, _heightParameter, StringComparison.Ordinal))
		{
			_heightParameter = Height;
			_height = Height;
			if (TryParsePx(Height, out double heightPx))
			{
				_heightPx = heightPx;
			}
		}
	}

	/// <inheritdoc />
	protected override Task OnAfterRenderAsync(bool firstRender)
	{
		// One sync at a time: each compares against what the one before it set up, so a render that
		// lands while listeners are still being set up cannot attach a second, stale one.
		_handleSync = SyncHandlesAsync(_handleSync);
		return _handleSync;
	}

	private async Task SyncHandlesAsync(Task previous)
	{
		await previous;

		try
		{
			_jsModule ??= await GetJsModuleAsync(JsModulePath);
			_dotNetRef ??= DotNetObjectReference.Create(this);

			// target: 'element' sizes this element. The default rewrites the track of a grid the
			// element sits in, which is right for a dock panel and wrong here. keyboard: the arrow
			// keys, Home and End resize through the same callback as a drag.
			_right = await SyncHandleAsync(_right, HasRightHandle, _rightHandleRef,
				"makeResizable", "removeResizable",
				new
				{
					direction = "horizontal",
					target = "element",
					min = MinWidth,
					max = MaxWidth,
					keyboard = true,
					callbackMethod = nameof(OnWidthResized)
				});

			_bottom = await SyncHandleAsync(_bottom, HasBottomHandle, _bottomHandleRef,
				"makeResizable", "removeResizable",
				new
				{
					direction = "vertical",
					target = "element",
					min = MinHeight,
					max = MaxHeight,
					keyboard = true,
					callbackMethod = nameof(OnHeightResized)
				});

			_corner = await SyncHandleAsync(_corner, HasCornerHandle, _cornerHandleRef,
				"makeCornerResizable", "removeCornerResizable",
				new
				{
					minWidth = MinWidth,
					maxWidth = MaxWidth,
					minHeight = MinHeight,
					maxHeight = MaxHeight,
					callbackMethod = nameof(OnCornerResized)
				});
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}

	// Sets up, replaces or removes one handle's listener so it matches this render. The options are
	// anonymous objects, which compare by value.
	private async Task<Attachment?> SyncHandleAsync(Attachment? attached, bool wanted, ElementReference handle,
		string attach, string detach, object options)
	{
		if (wanted && attached is not null
		           && string.Equals(attached.Handle.Id, handle.Id, StringComparison.Ordinal)
		           && attached.Options.Equals(options))
		{
			return attached;
		}

		if (attached is not null)
		{
			// A handle Direction removed is no longer in the page, and removing its listener does
			// nothing, which is fine.
			await _jsModule!.InvokeVoidAsync(detach, attached.Handle);
		}

		if (!wanted)
		{
			return null;
		}

		await _jsModule!.InvokeVoidAsync(attach, _dotNetRef, _containerRef, handle, options);
		return new Attachment(handle, options);
	}

	private string HandleClass(string edge) => new CssBuilder("moka-resizable__handle")
		.AddClass($"moka-resizable__handle--{edge}")
		.AddClass("moka-resizable__handle--hidden", !ShowHandle)
		.Build();

	/// <summary>Called from JS when horizontal resize completes.</summary>
	[JSInvokable]
	public async Task OnWidthResized(double newSizePx)
	{
		_widthPx = newSizePx;
		_width = ToPx(newSizePx);

		// A call from JS is not a UI event, so nothing re-renders on its own, and an unbound
		// WidthChanged re-renders nobody.
		StateHasChanged();
		await WidthChanged.InvokeAsync(_width);
		await NotifyResizedAsync();
	}

	/// <summary>Called from JS when vertical resize completes.</summary>
	[JSInvokable]
	public async Task OnHeightResized(double newSizePx)
	{
		_heightPx = newSizePx;
		_height = ToPx(newSizePx);
		StateHasChanged();
		await HeightChanged.InvokeAsync(_height);
		await NotifyResizedAsync();
	}

	/// <summary>Called from JS when a corner (two-axis) resize completes.</summary>
	[JSInvokable]
	public async Task OnCornerResized(double newWidthPx, double newHeightPx)
	{
		_widthPx = newWidthPx;
		_heightPx = newHeightPx;
		_width = ToPx(newWidthPx);
		_height = ToPx(newHeightPx);
		StateHasChanged();
		await WidthChanged.InvokeAsync(_width);
		await HeightChanged.InvokeAsync(_height);
		await NotifyResizedAsync();
	}

	private static string ToPx(double px) => $"{px.ToString(CultureInfo.InvariantCulture)}px";

	private async Task NotifyResizedAsync()
	{
		if (OnResized.HasDelegate)
		{
			await OnResized.InvokeAsync(new MokaResizeResult(_widthPx, _heightPx));
		}
	}

	private static bool TryParsePx(string? value, out double px)
	{
		px = 0;
		if (value is null || !value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return double.TryParse(value[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out px);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// A sync still running would set listeners up after the ones below are removed.
		await _handleSync;

		if (_jsModule is not null)
		{
			try
			{
				if (_right is not null)
				{
					await _jsModule.InvokeVoidAsync("removeResizable", _right.Handle);
				}

				if (_bottom is not null)
				{
					await _jsModule.InvokeVoidAsync("removeResizable", _bottom.Handle);
				}

				if (_corner is not null)
				{
					await _jsModule.InvokeVoidAsync("removeCornerResizable", _corner.Handle);
				}
			}
			catch (JSDisconnectedException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private sealed record Attachment(ElementReference Handle, object Options);
}
