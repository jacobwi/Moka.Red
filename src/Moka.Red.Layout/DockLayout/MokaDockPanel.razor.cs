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
	private ElementReference _attachedHeader;
	private ElementReference _attachedSplitter;
	private SplitterOptions? _attachedSplitterOptions;
	private DotNetObjectReference<MokaDockPanel>? _dotNetRef;
	private bool _draggableAttached;
	private ElementReference _headerRef;
	private Task _jsSync = Task.CompletedTask;
	private string? _maxSize;
	private string? _minSize;
	private ElementReference _panelRef;
	private string? _size;
	private ElementReference _splitterRef;

	/// <summary>The edge to dock this panel to.</summary>
	[Parameter]
	public MokaDockPosition Dock { get; set; } = MokaDockPosition.Left;

	/// <summary>The panel body content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Size of the panel (CSS value, e.g. "250px", "20%"). A new value from the parent replaces
	///     a size the user dragged the splitter to.
	/// </summary>
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

	/// <summary>
	///     Whether the panel is collapsed. Two-way bindable. A collapsed panel hides its body but keeps
	///     it mounted, so child components keep their state.
	/// </summary>
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

	/// <summary>
	///     Whether the body scrolls when its content overflows. Default true. Set false for content that
	///     scrolls itself, such as a terminal or an editor: the body then clips instead, and a single child
	///     fills it, so the child gets a definite size and shows the only scrollbar.
	/// </summary>
	[Parameter]
	public bool Scrollable { get; set; } = true;

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

	private string BodyClass => new CssBuilder("moka-dock-panel-body")
		.AddClass("moka-dock-panel-body--no-scroll", !Scrollable)
		.Build();

	internal string CurrentSize { get; private set; } = "250px";

	internal bool IsCollapsed => Collapsed && Collapsible && !Floating;
	internal bool IsFloating => Floating;

	private bool IsHorizontal => Dock is MokaDockPosition.Left or MokaDockPosition.Right;

	private bool HasHeader =>
		Title is not null || TitleContent is not null || Actions is not null || Collapsible || Floating;

	private bool HasSplitter => Resizable && !IsCollapsed && !Floating;

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

		// Follow a new Size, and re-clamp when the bounds move. Otherwise keep the dragged size.
		if (!string.Equals(Size, _size, StringComparison.Ordinal))
		{
			CurrentSize = ClampSize(Size);
		}
		else if (!string.Equals(MinSize, _minSize, StringComparison.Ordinal)
		         || !string.Equals(MaxSize, _maxSize, StringComparison.Ordinal))
		{
			CurrentSize = ClampSize(CurrentSize);
		}

		_size = Size;
		_minSize = MinSize;
		_maxSize = MaxSize;

		// The layout rendered its grid before this panel received these parameters.
		ParentLayout?.NotifyPanelChanged();
	}

	/// <inheritdoc />
	protected override Task OnAfterRenderAsync(bool firstRender)
	{
		// One sync at a time: each one reads the state it attaches, so a later render cannot be
		// overtaken by the JS calls of an earlier one.
		_jsSync = SyncJsAsync(_jsSync);
		return _jsSync;
	}

	/// <summary>Dock panels have internal collapse/resize/floating state.</summary>
	protected override bool ShouldRender() => true;

	private async Task SyncJsAsync(Task previous)
	{
		await previous;
		await SyncSplitterAsync();
		await SyncDraggableAsync();
	}

	// The splitter element is re-created after a collapse or a float, and Dock, MinSize and
	// MaxSize change its options. Comparing against what is attached covers those changes
	// whether they came from this panel's buttons or from a parent.
	private async ValueTask SyncSplitterAsync()
	{
		SplitterOptions? wanted = HasSplitter && ParentLayout is not null
			? new SplitterOptions(IsHorizontal, Dock is MokaDockPosition.Right or MokaDockPosition.Bottom,
				MinSize, MaxSize)
			: null;

		if (wanted == _attachedSplitterOptions
		    && (wanted is null || string.Equals(_attachedSplitter.Id, _splitterRef.Id, StringComparison.Ordinal)))
		{
			return;
		}

		if (_attachedSplitterOptions is not null)
		{
			_attachedSplitterOptions = null;
			await InvokeDragModuleAsync("removeResizable", _attachedSplitter);
		}

		if (wanted is { } options)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			_attachedSplitter = _splitterRef;
			_attachedSplitterOptions = options;
			await InvokeDragModuleAsync("makeResizable", _dotNetRef, _panelRef, _splitterRef,
				new
				{
					direction = options.Horizontal ? "horizontal" : "vertical",
					reverse = options.Reverse,
					min = options.Min,
					max = options.Max,
					callbackMethod = nameof(OnResized)
				});
		}
	}

	// A docked panel must not keep the floating header drag, or dragging its header would move it.
	private async ValueTask SyncDraggableAsync()
	{
		bool wanted = Floating && ParentLayout is not null;

		if (wanted == _draggableAttached
		    && (!wanted || string.Equals(_attachedHeader.Id, _headerRef.Id, StringComparison.Ordinal)))
		{
			return;
		}

		if (_draggableAttached)
		{
			_draggableAttached = false;
			await InvokeDragModuleAsync("removeDraggable", _attachedHeader);
		}

		if (wanted)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			_attachedHeader = _headerRef;
			_draggableAttached = true;
			await InvokeDragModuleAsync("makeDraggable", _dotNetRef, _panelRef, _headerRef,
				new { callbackMethod = nameof(OnFloatingMoved) });
		}
	}

	// moka-drag.js is imported once by the layout and shared by its panels.
	private async ValueTask InvokeDragModuleAsync(string identifier, params object?[] args)
	{
		if (ParentLayout is null)
		{
			return;
		}

		try
		{
			IJSObjectReference jsModule = await ParentLayout.EnsureJsModuleAsync();
			await jsModule.InvokeVoidAsync(identifier, args);
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
		catch (InvalidOperationException) when (!HasRendered)
		{
		}
	}

	private async Task ToggleCollapse()
	{
		Collapsed = !Collapsed;

		// Update the grid before the callback, which may await before the parent re-renders.
		ParentLayout?.NotifyPanelChanged();
		await CollapsedChanged.InvokeAsync(Collapsed);
	}

	private async Task ToggleFloating()
	{
		Floating = !Floating;

		if (!Floating)
		{
			CurrentSize = ClampSize(Size);
		}

		ParentLayout?.NotifyPanelChanged();
		await FloatingChanged.InvokeAsync(Floating);
	}

	/// <summary>Called from JS when resize completes.</summary>
	[JSInvokable]
	public async Task OnResized(double newSizePx)
	{
		CurrentSize = $"{newSizePx.ToString(CultureInfo.InvariantCulture)}px";
		ParentLayout?.NotifyPanelChanged();

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

		if (_attachedSplitterOptions is not null)
		{
			_attachedSplitterOptions = null;
			await InvokeDragModuleAsync("removeResizable", _attachedSplitter);
		}

		if (_draggableAttached)
		{
			_draggableAttached = false;
			await InvokeDragModuleAsync("removeDraggable", _attachedHeader);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private readonly record struct SplitterOptions(bool Horizontal, bool Reverse, string? Min, string? Max);
}
