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
	private static readonly string[] LengthFunctions = ["calc(", "min(", "max(", "clamp("];

	private ElementReference _attachedHeader;
	private ElementReference _attachedSplitter;
	private SplitterOptions? _attachedSplitterOptions;

	// Collapsed, Floating and the floating position also change from inside the panel: its header
	// buttons and the header drag. The rendered state lives in these fields, and a parameter only
	// replaces it when the parent passes a new value, so a parent render that repeats the old value
	// (a one-way Collapsed="@x") cannot undo what the user just did.
	private bool _collapsed;
	private bool _collapsedParameter;
	private DotNetObjectReference<MokaDockPanel>? _dotNetRef;
	private bool _draggableAttached;
	private bool _floating;
	private bool _floatingParameter;
	private double _floatingX;
	private double? _floatingXParameter;
	private double _floatingY;
	private double? _floatingYParameter;
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
	///     it mounted, so child components keep their state. The header button collapses and expands
	///     the panel whether or not this is bound; a new value from the parent replaces that state.
	/// </summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Callback when <see cref="Collapsed" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> CollapsedChanged { get; set; }

	/// <summary>
	///     Size of the panel when collapsed. Default "0px". At a zero size the collapsed panel is inert,
	///     so its hidden header buttons are out of the tab order and the accessibility tree. A size
	///     leaves a strip that shows the title and the expand button, laid out down the strip for a
	///     left or right panel; <see cref="Actions" /> and the undock button come back on expand.
	/// </summary>
	[Parameter]
	public string? CollapsedSize { get; set; } = "0px";

	/// <summary>
	///     Whether this panel is currently floating (undocked). Two-way bindable. The header button
	///     undocks and docks the panel whether or not this is bound; a new value from the parent
	///     replaces that state. Docking again keeps the size the panel had before it floated.
	/// </summary>
	[Parameter]
	public bool Floating { get; set; }

	/// <summary>Callback when <see cref="Floating" /> state changes.</summary>
	[Parameter]
	public EventCallback<bool> FloatingChanged { get; set; }

	/// <summary>
	///     Floating panel position X (pixels from left). Default 100. Dragging the header moves the panel,
	///     and a new value from the parent moves it again.
	/// </summary>
	[Parameter]
	public double FloatingX { get; set; } = 100;

	/// <summary>
	///     Floating panel position Y (pixels from top). Default 100. Dragging the header moves the panel,
	///     and a new value from the parent moves it again.
	/// </summary>
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
		.AddClass($"moka-dock-panel--{MokaEnumHelpers.ToCssClass(Dock)}", !_floating)
		.AddClass("moka-dock-panel--collapsed", IsCollapsed)
		.AddClass("moka-dock-panel--resizable", Resizable && !_floating)
		.AddClass("moka-dock-panel--floating", _floating)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => _floating ? FloatingCssStyle : DockedCssStyle;

	// MinSize and MaxSize bound the track (TrackSize), not this element. On the element a percentage
	// resolves against the panel's own grid area, and any bound the track breaks leaves a gap
	// beside the panel or pushes it over the content.
	private string? DockedCssStyle => new StyleBuilder()
		.AddStyle("grid-area", MokaEnumHelpers.ToCssClass(Dock))
		.AddStyle(Style)
		.Build();

	private string? FloatingCssStyle => new StyleBuilder()
		.AddStyle("left", $"{_floatingX.ToString(CultureInfo.InvariantCulture)}px")
		.AddStyle("top", $"{_floatingY.ToString(CultureInfo.InvariantCulture)}px")
		.AddStyle("width", FloatingWidth)
		.AddStyle("height", FloatingHeight)
		.AddStyle(Style)
		.Build();

	private string BodyClass => new CssBuilder("moka-dock-panel-body")
		.AddClass("moka-dock-panel-body--no-scroll", !Scrollable)
		.Build();

	internal string CurrentSize { get; private set; } = "250px";

	internal bool IsCollapsed => _collapsed && Collapsible && !_floating;
	internal bool IsFloating => _floating;

	// An empty CollapsedSize would drop the track from the grid and misalign every area after it.
	internal string CollapsedTrackSize => string.IsNullOrWhiteSpace(CollapsedSize) ? "0px" : CollapsedSize;

	// The panel's grid track. ClampSize has already applied px bounds to a px size; bounds in any
	// other unit go to CSS, which resolves rem, viewport units and % of the layout the way the
	// splitter drag does.
	internal string TrackSize => IsCollapsed ? CollapsedTrackSize : BoundTrack(CurrentSize);

	// A panel collapsed to nothing still has a header, clipped out of sight, whose buttons would take
	// focus and be read out. Inert removes the whole panel from the tab order and the accessibility
	// tree. A collapsed strip with a size stays usable: its header lays out to keep the expand button
	// on screen.
	private bool IsInert => IsCollapsed && IsZeroLength(CollapsedTrackSize);

	private bool IsHorizontal => Dock is MokaDockPosition.Left or MokaDockPosition.Right;

	private bool HasHeader =>
		Title is not null || TitleContent is not null || Actions is not null || Collapsible || _floating;

	private bool HasSplitter => Resizable && !IsCollapsed && !_floating;

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

		// The layout builds its grid from this panel as soon as it registers.
		SyncStateFromParameters();
		ParentLayout?.RegisterPanel(this);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		SyncStateFromParameters();

		// The layout rendered its grid before this panel received these parameters.
		ParentLayout?.NotifyPanelChanged();
	}

	private void SyncStateFromParameters()
	{
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

		if (Collapsed != _collapsedParameter)
		{
			_collapsedParameter = Collapsed;
			_collapsed = Collapsed;
		}

		if (Floating != _floatingParameter)
		{
			_floatingParameter = Floating;
			_floating = Floating;
		}

		if (FloatingX != _floatingXParameter)
		{
			_floatingXParameter = FloatingX;
			_floatingX = FloatingX;
		}

		if (FloatingY != _floatingYParameter)
		{
			_floatingYParameter = FloatingY;
			_floatingY = FloatingY;
		}
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
					target = "track",
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
		bool wanted = _floating && ParentLayout is not null;

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
		_collapsed = !_collapsed;

		// Update the grid before the callback, which may await before the parent re-renders.
		ParentLayout?.NotifyPanelChanged();
		await CollapsedChanged.InvokeAsync(_collapsed);
	}

	// Docking again keeps CurrentSize, the size the panel had before it floated, the same as when
	// the parent docks it. Only a new Size from the parent replaces a size the user dragged to.
	private async Task ToggleFloating()
	{
		_floating = !_floating;
		ParentLayout?.NotifyPanelChanged();
		await FloatingChanged.InvokeAsync(_floating);
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
		// No render needed: the drag already moved the element, and the next render writes the
		// same position.
		_floatingX = x;
		_floatingY = y;
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

	private string BoundTrack(string size)
	{
		// min(), max() and clamp() only take lengths: wrapping a keyword, an fr or a minmax() would
		// make the whole grid template invalid, so those tracks keep their size unbounded.
		if (!IsLength(size))
		{
			return size;
		}

		string? min = CssBound(MinSize, size);
		string? max = CssBound(MaxSize, size);
		return (min, max) switch
		{
			(null, null) => size,
			(null, _) => $"min({size}, {max})",
			(_, null) => $"max({min}, {size})",
			_ => $"clamp({min}, {size}, {max})"
		};
	}

	// Null when there is no bound for CSS to apply: none given, one ClampSize already applied (px on
	// a px size), or one that is not a length.
	private static string? CssBound(string? bound, string size) =>
		bound is null || !IsLength(bound) || (TryParsePx(bound, out _) && TryParsePx(size, out _))
			? null
			: bound;

	// "0", "0px", "0.0rem" and the like. Anything this cannot read, such as calc(), counts as a
	// size, which keeps the panel usable rather than making a visible strip inert.
	private static bool IsZeroLength(string value) =>
		TryParseLength(value, out double amount, out _) && amount == 0;

	private static bool IsLength(string value)
	{
		string text = value.Trim();
		if (LengthFunctions.Any(function => text.StartsWith(function, StringComparison.OrdinalIgnoreCase)))
		{
			return true;
		}

		return TryParseLength(text, out _, out string unit) && !unit.Equals("fr", StringComparison.OrdinalIgnoreCase);
	}

	// "240px", "20%", "12rem" or "0" as a number and its unit. False for anything else.
	private static bool TryParseLength(string value, out double amount, out string unit)
	{
		ReadOnlySpan<char> text = value.AsSpan().Trim();
		int unitStart = text.Length;
		while (unitStart > 0 && (char.IsAsciiLetter(text[unitStart - 1]) || text[unitStart - 1] == '%'))
		{
			unitStart--;
		}

		unit = text[unitStart..].ToString();
		return double.TryParse(text[..unitStart], NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
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
