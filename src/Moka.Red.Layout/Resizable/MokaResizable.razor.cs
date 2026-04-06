using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Resizable;

/// <summary>
///     A wrapper component that makes any child content resizable by dragging edge handles.
///     Supports horizontal, vertical, or both directions with two-way bindable size values.
/// </summary>
public partial class MokaResizable : MokaComponentBase
{
	private bool _bottomAttached;
	private ElementReference _bottomHandleRef;
	private ElementReference _containerRef;
	private bool _cornerAttached;
	private ElementReference _cornerHandleRef;
	private DotNetObjectReference<MokaResizable>? _dotNetRef;
	private IJSObjectReference? _jsModule;
	private bool _rightAttached;
	private ElementReference _rightHandleRef;

	/// <summary>The content to make resizable.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Which directions are resizable. Default Horizontal.</summary>
	[Parameter]
	public MokaResizeDirection Direction { get; set; } = MokaResizeDirection.Horizontal;

	/// <summary>Current width. Two-way bindable.</summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>Callback when <see cref="Width" /> changes.</summary>
	[Parameter]
	public EventCallback<string> WidthChanged { get; set; }

	/// <summary>Current height. Two-way bindable.</summary>
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

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", Width, !string.IsNullOrEmpty(Width))
		.AddStyle("height", Height, !string.IsNullOrEmpty(Height))
		.AddStyle("min-width", MinWidth, !string.IsNullOrEmpty(MinWidth))
		.AddStyle("max-width", MaxWidth, !string.IsNullOrEmpty(MaxWidth))
		.AddStyle("min-height", MinHeight, !string.IsNullOrEmpty(MinHeight))
		.AddStyle("max-height", MaxHeight, !string.IsNullOrEmpty(MaxHeight))
		.AddStyle(Style)
		.Build();

	/// <summary>MokaResizable has internal resize state that requires re-rendering.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		try
		{
			_jsModule ??= await GetJsModuleAsync("./_content/Moka.Red.Core/moka-drag.js");
			_dotNetRef ??= DotNetObjectReference.Create(this);

			if (Direction is MokaResizeDirection.Horizontal or MokaResizeDirection.Both && !_rightAttached)
			{
				await _jsModule.InvokeVoidAsync("makeResizable", _dotNetRef, _containerRef, _rightHandleRef,
					new
					{
						direction = "horizontal", min = MinWidth, max = MaxWidth, callbackMethod = "OnWidthResized"
					});
				_rightAttached = true;
			}

			if (Direction is MokaResizeDirection.Vertical or MokaResizeDirection.Both && !_bottomAttached)
			{
				await _jsModule.InvokeVoidAsync("makeResizable", _dotNetRef, _containerRef, _bottomHandleRef,
					new
					{
						direction = "vertical", min = MinHeight, max = MaxHeight, callbackMethod = "OnHeightResized"
					});
				_bottomAttached = true;
			}

			if (Direction == MokaResizeDirection.Both && !_cornerAttached)
			{
				// Corner handle uses the right handle's horizontal resize behavior
				// The visual corner combines both axes via pointer capture
				_cornerAttached = true;
			}
		}
		catch (JSDisconnectedException)
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}

	/// <summary>Called from JS when horizontal resize completes.</summary>
	[JSInvokable]
	public async Task OnWidthResized(double newSizePx)
	{
		Width = $"{newSizePx}px";
		await WidthChanged.InvokeAsync(Width);
		if (OnResized.HasDelegate)
		{
			await OnResized.InvokeAsync(new MokaResizeResult(newSizePx, 0));
		}
	}

	/// <summary>Called from JS when vertical resize completes.</summary>
	[JSInvokable]
	public async Task OnHeightResized(double newSizePx)
	{
		Height = $"{newSizePx}px";
		await HeightChanged.InvokeAsync(Height);
		if (OnResized.HasDelegate)
		{
			await OnResized.InvokeAsync(new MokaResizeResult(0, newSizePx));
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_jsModule is not null)
		{
			try
			{
				if (_rightAttached)
				{
					await _jsModule.InvokeVoidAsync("removeResizable", _rightHandleRef);
				}

				if (_bottomAttached)
				{
					await _jsModule.InvokeVoidAsync("removeResizable", _bottomHandleRef);
				}
			}
			catch (JSDisconnectedException)
			{
			}
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
