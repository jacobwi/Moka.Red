using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     A panel that docks to an edge of a <see cref="MokaDockLayout" />.
///     Supports resizable splitter handles, collapsing, floating/undocking,
///     and header with title/actions.
/// </summary>
public partial class MokaDockPanel : MokaComponentBase
{
	private DotNetObjectReference<MokaDockPanel>? _dotNetRef;
	private bool _draggableAttached;
	private ElementReference _headerRef;
	private ElementReference _panelRef;
	private bool _splitterAttached;
	private ElementReference _splitterRef;

	/// <summary>The edge to dock this panel to.</summary>
	[Parameter]
	public MokaDockPosition Dock { get; set; } = MokaDockPosition.Left;

	/// <summary>The panel body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Initial size of the panel (CSS value, e.g. "250px", "20%").</summary>
	[Parameter]
	public string Size { get; set; } = "250px";

	/// <summary>Minimum size constraint for resizing (CSS value).</summary>
	[Parameter]
	public string? MinSize { get; set; }

	/// <summary>Maximum size constraint for resizing (CSS value).</summary>
	[Parameter]
	public string? MaxSize { get; set; }

	/// <summary>Whether the panel can be resized via a splitter handle. Default true.</summary>
	[Parameter]
	public bool Resizable { get; set; } = true;

	/// <summary>Whether the panel shows a collapse toggle. Default false.</summary>
	[Parameter]
	public bool Collapsible { get; set; }

	/// <summary>Whether the panel is collapsed. Two-way bindable.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Callback when <see cref="Collapsed" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> CollapsedChanged { get; set; }

	/// <summary>Size of the panel when collapsed. Default "0px".</summary>
	[Parameter]
	public string? CollapsedSize { get; set; } = "0px";

	/// <summary>Whether this panel is currently floating (undocked). Two-way bindable.</summary>
	[Parameter]
	public bool Floating { get; set; }

	/// <summary>Callback when <see cref="Floating" /> state changes.</summary>
	[Parameter]
	public EventCallback<bool> FloatingChanged { get; set; }

	/// <summary>Floating panel position X (pixels from left). Default 100.</summary>
	[Parameter]
	public double FloatingX { get; set; } = 100;

	/// <summary>Floating panel position Y (pixels from top). Default 100.</summary>
	[Parameter]
	public double FloatingY { get; set; } = 100;

	/// <summary>Floating panel width. Default "300px".</summary>
	[Parameter]
	public string FloatingWidth { get; set; } = "300px";

	/// <summary>Floating panel height. Default "400px".</summary>
	[Parameter]
	public string FloatingHeight { get; set; } = "400px";

	/// <summary>Simple text title for the panel header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Custom title content. Overrides <see cref="Title" />.</summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Actions rendered in the panel header, aligned to the right.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Callback when the panel is resized. Receives the new size in pixels.</summary>
	[Parameter]
	public EventCallback<double> SizeChanged { get; set; }

	[CascadingParameter] private MokaDockLayout? ParentLayout { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-panel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-dock-panel--{MokaEnumHelpers.ToCssClass(Dock)}", !Floating)
		.AddClass("moka-dock-panel--collapsed", IsCollapsed && !Floating)
		.AddClass("moka-dock-panel--resizable", Resizable && !Floating)
		.AddClass("moka-dock-panel--floating", Floating)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => Floating ? FloatingCssStyle : DockedCssStyle;

	private string? DockedCssStyle => new StyleBuilder()
		.AddStyle("grid-area", MokaEnumHelpers.ToCssClass(Dock))
		.AddStyle("min-width", MinSize, IsHorizontal && MinSize is not null && !IsCollapsed)
		.AddStyle("max-width", MaxSize, IsHorizontal && MaxSize is not null && !IsCollapsed)
		.AddStyle("min-height", MinSize, !IsHorizontal && MinSize is not null && !IsCollapsed)
		.AddStyle("max-height", MaxSize, !IsHorizontal && MaxSize is not null && !IsCollapsed)
		.AddStyle(Style)
		.Build();

	private string? FloatingCssStyle => new StyleBuilder()
		.AddStyle("left", $"{FloatingX.ToString(CultureInfo.InvariantCulture)}px")
		.AddStyle("top", $"{FloatingY.ToString(CultureInfo.InvariantCulture)}px")
		.AddStyle("width", FloatingWidth)
		.AddStyle("height", FloatingHeight)
		.AddStyle(Style)
		.Build();

	internal string CurrentSize { get; private set; } = "250px";

	internal bool IsCollapsed => Collapsed && Collapsible && !Floating;
	internal bool IsFloating => Floating;

	private bool IsHorizontal => Dock is MokaDockPosition.Left or MokaDockPosition.Right;

	private bool HasHeader =>
		Title is not null || TitleContent is not null || Actions is not null || Collapsible || Floating;

	private string SplitterPosition => Dock switch
	{
		MokaDockPosition.Left => "right",
		MokaDockPosition.Right => "left",
		MokaDockPosition.Top => "bottom",
		MokaDockPosition.Bottom => "top",
		_ => "right"
	};

	private string ChevronDirection => Dock switch
	{
		MokaDockPosition.Left => IsCollapsed ? "right" : "left",
		MokaDockPosition.Right => IsCollapsed ? "left" : "right",
		MokaDockPosition.Top => IsCollapsed ? "down" : "up",
		MokaDockPosition.Bottom => IsCollapsed ? "up" : "down",
		_ => "left"
	};

	private MokaIconDefinition ChevronIcon => ChevronDirection switch
	{
		"left" => MokaIcons.Navigation.ChevronLeft,
		"right" => MokaIcons.Navigation.ChevronRight,
		"up" => MokaIcons.Navigation.ChevronUp,
		"down" => MokaIcons.Navigation.ChevronDown,
		_ => MokaIcons.Navigation.ChevronLeft
	};

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		CurrentSize = ClampSize(Size);
		ParentLayout?.RegisterPanel(this);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (CurrentSize == Size || CurrentSize == "250px")
		{
			CurrentSize = ClampSize(Size);
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (Floating)
		{
			await AttachDraggableIfNeededAsync();
		}
		else
		{
			await AttachSplitterIfNeededAsync();
		}
	}

	/// <summary>Dock panels have internal collapse/resize/floating state.</summary>
	protected override bool ShouldRender() => true;

	private async ValueTask AttachSplitterIfNeededAsync()
	{
		if (!Resizable || IsCollapsed || ParentLayout is null || _splitterAttached)
		{
			return;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);

		try
		{
			IJSObjectReference jsModule = await ParentLayout.EnsureJsModuleAsync();
			await jsModule.InvokeVoidAsync("makeResizable", _dotNetRef, _panelRef, _splitterRef,
				new
				{
					direction = IsHorizontal ? "horizontal" : "vertical",
					reverse = Dock is MokaDockPosition.Right or MokaDockPosition.Bottom,
					min = MinSize,
					max = MaxSize,
					callbackMethod = "OnResized"
				});
			_splitterAttached = true;
		}
		catch (JSDisconnectedException)
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}

	private async ValueTask AttachDraggableIfNeededAsync()
	{
		if (!Floating || ParentLayout is null || _draggableAttached)
		{
			return;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);

		try
		{
			IJSObjectReference jsModule = await ParentLayout.EnsureJsModuleAsync();
			await jsModule.InvokeVoidAsync("makeDraggable", _dotNetRef, _panelRef, _headerRef,
				new { callbackMethod = "OnFloatingMoved" });
			_draggableAttached = true;
		}
		catch (JSDisconnectedException)
		{
		}
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}

	private async ValueTask DetachDraggableAsync()
	{
		if (ParentLayout is null)
		{
			return;
		}

		try
		{
			IJSObjectReference jsModule = await ParentLayout.EnsureJsModuleAsync();
			await jsModule.InvokeVoidAsync("removeDraggable", _headerRef);
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private async Task ToggleCollapse()
	{
		if (_splitterAttached)
		{
			await DetachSplitterAsync();
		}

		Collapsed = !Collapsed;
		_splitterAttached = false;
		await CollapsedChanged.InvokeAsync(Collapsed);
		ParentLayout?.NotifyPanelResized();
	}

	private async Task ToggleFloating()
	{
		if (Floating && _draggableAttached)
		{
			await DetachDraggableAsync();
			_draggableAttached = false;
		}
		else if (!Floating && _splitterAttached)
		{
			await DetachSplitterAsync();
		}

		Floating = !Floating;
		_splitterAttached = false;
		_draggableAttached = false;

		await FloatingChanged.InvokeAsync(Floating);

		if (!Floating)
		{
			CurrentSize = ClampSize(Size);
		}

		ParentLayout?.NotifyPanelResized();
	}

	private async ValueTask DetachSplitterAsync()
	{
		if (ParentLayout is null)
		{
			return;
		}

		try
		{
			IJSObjectReference jsModule = await ParentLayout.EnsureJsModuleAsync();
			await jsModule.InvokeVoidAsync("removeResizable", _splitterRef);
		}
		catch (JSDisconnectedException)
		{
		}
		catch (ObjectDisposedException)
		{
		}
	}

	/// <summary>Called from JS when resize completes.</summary>
	[JSInvokable]
	public async Task OnResized(double newSizePx)
	{
		CurrentSize = $"{newSizePx}px";
		ParentLayout?.NotifyPanelResized();

		if (SizeChanged.HasDelegate)
		{
			await SizeChanged.InvokeAsync(newSizePx);
		}
	}

	/// <summary>Called from JS when a floating panel is dragged to a new position.</summary>
	[JSInvokable]
	public void OnFloatingMoved(double x, double y)
	{
		FloatingX = x;
		FloatingY = y;
	}

	private string ClampSize(string size)
	{
		if (TryParsePx(size, out double sizePx))
		{
			if (MinSize is not null && TryParsePx(MinSize, out double minPx) && sizePx < minPx)
			{
				return MinSize;
			}

			if (MaxSize is not null && TryParsePx(MaxSize, out double maxPx) && sizePx > maxPx)
			{
				return MaxSize;
			}
		}

		return size;
	}

	private static bool TryParsePx(string value, out double px)
	{
		px = 0;
		if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
		{
			return double.TryParse(value[..^2], NumberStyles.Float,
				CultureInfo.InvariantCulture, out px);
		}

		return false;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		ParentLayout?.UnregisterPanel(this);

		if (_splitterAttached)
		{
			await DetachSplitterAsync();
		}

		if (_draggableAttached)
		{
			await DetachDraggableAsync();
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
