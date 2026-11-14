using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Primitives.Badge;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     A panel of a <see cref="MokaDockLayout" />: a tab in a group, which can be dragged to another group or edge,
///     floated, pinned to a side bar, maximized, closed and opened in another window. In a <see cref="MokaDockGroup" />
///     it starts as one of the group's tabs; directly in the layout, it docks to <see cref="Dock" /> around the main
///     area, joining the other panels docked to that edge as tabs. Its content renders once, where it is declared, and
///     is drawn over whichever group holds it, so it keeps its component state when it moves.
/// </summary>
public partial class MokaDockPanel : MokaDockPart
{
	private DockPlacement _placement = DockPlacement.Nowhere;
	private int _contentVersion;
	private bool _shownOnce;
	private bool _synced;
	private bool _lastCollapsed;
	private bool _lastFloating;
	private double _lastFloatingX;
	private double _lastFloatingY;
	private string? _lastFloatingWidth;
	private string? _lastFloatingHeight;
	private string? _lastSize;
	private object? _lastMeta;

	/// <summary>For a panel directly in the layout: the edge it docks to, around the main area. Default left.</summary>
	[Parameter]
	public MokaDockPosition Dock { get; set; } = MokaDockPosition.Left;

	/// <summary>The panel's content. <see cref="Component" /> takes its place when set.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     A component to show as the content, made with <see cref="MokaDockComponent.Create{TComponent}" />. Only a panel
	///     shown this way can open in another browser window, since the window renders it in a circuit of its own.
	/// </summary>
	[Parameter]
	public MokaDockComponent? Component { get; set; }

	/// <summary>
	///     For a panel directly in the layout or in a row or column: its size along the split it is in, as a CSS length,
	///     or a share such as <c>2fr</c>. A panel directly in the layout starts at <c>250px</c>. A new value from the parent
	///     replaces a size the person dragged the splitter to. Two-way bindable.
	/// </summary>
	[Parameter]
	public string? Size { get; set; }

	/// <summary>Raised with the new size, in pixels as a CSS length such as <c>320px</c>, when a splitter drag resizes the panel's group.</summary>
	[Parameter]
	public EventCallback<string> SizeChanged { get; set; }

	/// <summary>The smallest the panel's group can be dragged to, as a CSS length.</summary>
	[Parameter]
	public string? MinSize { get; set; }

	/// <summary>The largest the panel's group can be dragged to, as a CSS length.</summary>
	[Parameter]
	public string? MaxSize { get; set; }

	/// <summary>Whether the splitter beside the panel's group moves. Default true.</summary>
	[Parameter]
	public bool Resizable { get; set; } = true;

	/// <summary>Whether the header shows a button that folds the panel's group to a strip.</summary>
	[Parameter]
	public bool Collapsible { get; set; }

	/// <summary>
	///     Whether the panel's group is folded. Two-way bindable. A folded group keeps its panels mounted. The header
	///     button folds and opens it whether or not this is bound; a new value from the parent replaces that.
	/// </summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Raised when the panel's group is folded or opened.</summary>
	[Parameter]
	public EventCallback<bool> CollapsedChanged { get; set; }

	/// <summary>
	///     The folded strip's size. Default <c>0px</c>, which takes the group off screen and out of the tab order. A size
	///     leaves a strip showing the title and the button that opens it.
	/// </summary>
	[Parameter]
	public string? CollapsedSize { get; set; } = "0px";

	/// <summary>
	///     Whether the panel floats in a window of its own. Two-way bindable. The header button floats and docks it
	///     whether or not this is bound; a new value from the parent replaces that. Docking again puts it back at the
	///     size it had.
	/// </summary>
	[Parameter]
	public bool Floating { get; set; }

	/// <summary>Raised when the panel floats or docks.</summary>
	[Parameter]
	public EventCallback<bool> FloatingChanged { get; set; }

	/// <summary>The floating window's left edge, in pixels from the layout's. Default 100. Two-way bindable.</summary>
	[Parameter]
	public double FloatingX { get; set; } = 100;

	/// <summary>Raised when the floating window is moved.</summary>
	[Parameter]
	public EventCallback<double> FloatingXChanged { get; set; }

	/// <summary>The floating window's top edge, in pixels from the layout's. Default 100. Two-way bindable.</summary>
	[Parameter]
	public double FloatingY { get; set; } = 100;

	/// <summary>Raised when the floating window is moved.</summary>
	[Parameter]
	public EventCallback<double> FloatingYChanged { get; set; }

	/// <summary>The floating window's width, in pixels (<c>300px</c>). Two-way bindable.</summary>
	[Parameter]
	public string FloatingWidth { get; set; } = "300px";

	/// <summary>Raised when the floating window is resized.</summary>
	[Parameter]
	public EventCallback<string> FloatingWidthChanged { get; set; }

	/// <summary>The floating window's height, in pixels (<c>400px</c>). Two-way bindable.</summary>
	[Parameter]
	public string FloatingHeight { get; set; } = "400px";

	/// <summary>Raised when the floating window is resized.</summary>
	[Parameter]
	public EventCallback<string> FloatingHeightChanged { get; set; }

	/// <summary>Whether the panel can float at all. Default true; false hides the buttons and refuses a drag out.</summary>
	[Parameter]
	public bool Floatable { get; set; } = true;

	/// <summary>
	///     Whether the content scrolls when it overflows. Default true. Set false for content that scrolls itself, such as
	///     a terminal or an editor: the content then clips instead, and a single child fills it.
	/// </summary>
	[Parameter]
	public bool Scrollable { get; set; } = true;

	/// <summary>The panel's title, on its tab and in its header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Custom title content, in place of <see cref="Title" /> in the header when the panel's group shows no tabs.</summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Buttons in the header while the panel shows, before the layout's own.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>The panel's icon, on its tab, in side bars and in the narrow layout's bar.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>A count on the panel's tab.</summary>
	[Parameter]
	public int? Badge { get; set; }

	/// <summary>The count's tone.</summary>
	[Parameter]
	public MokaBadgeTone BadgeTone { get; set; } = MokaBadgeTone.Count;

	/// <summary>Whether the panel can be closed. Unset follows whether the layout's <c>Controls</c> include Close.</summary>
	[Parameter]
	public bool? Closable { get; set; }

	/// <summary>Asked before the panel closes; false keeps it open.</summary>
	[Parameter]
	public Func<Task<bool>>? CanClose { get; set; }

	/// <summary>
	///     Whether the content first renders when the panel first shows. Default true. False renders it straight away,
	///     hidden while another tab shows.
	/// </summary>
	[Parameter]
	public bool Lazy { get; set; } = true;

	/// <summary>
	///     Whether the content stays mounted, with its state, while another tab shows. Default true. False removes it
	///     while hidden and renders it afresh each time it shows.
	/// </summary>
	[Parameter]
	public bool KeepAlive { get; set; } = true;

	/// <summary>Whether the panel starts in the layout. False starts it closed, to be shown from code.</summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>Whether the panel starts as the one showing in its group, rather than the first.</summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>Starts the panel pinned to this side's bar instead of docked.</summary>
	[Parameter]
	public MokaDockPosition? AutoHide { get; set; }

	internal int MetaVersion { get; private set; }

	internal override object Declaration =>
		(Id, Dock, Visible, AutoHide, (Size, MinSize, MaxSize, Resizable, Collapsible, CollapsedSize), (Floatable, Closable, Active));

	internal RenderFragment? Body => Component?.Render() ?? ChildContent;

	/// <inheritdoc />
	protected override string RootClass => "moka-dock-panel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass).AddClass(Class).Build();

	private string PanelId => Layout?.IdOf(this) ?? Id ?? "";

	private string HostId => Layout?.HostIdOf(PanelId) ?? PanelId;

	private string HostClass => new CssBuilder("moka-dock-host moka-dock-panel-body")
		.AddClass("moka-dock-panel-body--no-scroll", !Scrollable)
		.AddClass(Layout is null ? null : Class)
		.Build();

	private string? HostStyle => new StyleBuilder()
		.AddStyle("position-anchor", _placement.Anchor > 0 ? MokaDockLayout.AnchorName(_placement.Anchor) : null)
		.AddStyle("z-index", _placement.Z > 0 ? _placement.Z.ToString(CultureInfo.InvariantCulture) : null)
		.AddStyle(Style)
		.Build();

	private string? AnchorAttribute => _placement.Anchor > 0 ? _placement.Anchor.ToString(CultureInfo.InvariantCulture) : null;

	private bool RendersContent => !_placement.InSheet && (_placement.Visible || !Lazy || (KeepAlive && _shownOnce));

	/// <summary>Dock panels redraw when the layout moves them, outside the parameters their parent passes.</summary>
	/// <returns>Always true.</returns>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_contentVersion++;

		object meta = (Title, Icon, Badge, BadgeTone, Closable, Floatable);
		if (!meta.Equals(_lastMeta) || TitleContent is not null || Actions is not null)
		{
			_lastMeta = meta;
			MetaVersion++;
			Layout?.PanelMetaChanged();
		}

		Layout?.UpdateWindowSession(this);
		if (Layout is not null && _synced)
		{
			SyncFromParameters();
		}

		_synced = true;
		_lastCollapsed = Collapsed;
		_lastFloating = Floating;
		_lastFloatingX = FloatingX;
		_lastFloatingY = FloatingY;
		_lastFloatingWidth = FloatingWidth;
		_lastFloatingHeight = FloatingHeight;
		_lastSize = Size;
		_placement = Layout?.PlacementOf(this) ?? DockPlacement.Nowhere;
		_shownOnce |= _placement.Visible;
	}

	// A parameter the parent changed replaces what the person did; the same value passed again leaves it alone, so a
	// one-way Collapsed="@x" never undoes a click.
	private void SyncFromParameters()
	{
		MokaDockLayout layout = Layout!;
		if (Collapsed != _lastCollapsed)
		{
			RunDetached(() => layout.SetPanelCollapsedAsync(this, Collapsed));
		}

		if (Floating != _lastFloating)
		{
			RunDetached(() => layout.SetPanelFloatingAsync(this, Floating));
		}
		else if (FloatingX != _lastFloatingX || FloatingY != _lastFloatingY
		                                     || FloatingWidth != _lastFloatingWidth || FloatingHeight != _lastFloatingHeight)
		{
			RunDetached(() => layout.SetPanelWindowAsync(this));
		}

		if (Size != _lastSize)
		{
			RunDetached(() => layout.SetPanelSizeAsync(this, Size));
		}
	}

	internal void PlacementChanged()
	{
		DockPlacement placement = Layout?.PlacementOf(this) ?? DockPlacement.Nowhere;
		if (placement == _placement)
		{
			return;
		}

		_placement = placement;
		_shownOnce |= placement.Visible;
		StateHasChanged();
	}

	// After the layout's state changed: raises this panel's own callbacks for what changed about it.
	internal async Task StateChangedAsync(MokaDockState before, MokaDockState after)
	{
		string id = PanelId;
		MokaDockPanelLocation was = before.Locate(id);
		MokaDockPanelLocation now = after.Locate(id);

		if (CollapsedChanged.HasDelegate && GroupOf(before, was) is { } oldGroup && GroupOf(after, now) is { } newGroup
		    && oldGroup.Collapsed != newGroup.Collapsed)
		{
			await CollapsedChanged.InvokeAsync(newGroup.Collapsed);
		}

		bool wasFloating = was.Place == MokaDockPlace.Floating;
		bool isFloating = now.Place == MokaDockPlace.Floating;
		if (wasFloating != isFloating)
		{
			await FloatingChanged.InvokeAsync(isFloating);
		}

		if (isFloating && after.Floating.FirstOrDefault(w => w.Group.Id == now.Group) is { } window)
		{
			MokaDockFloatingWindow? old = wasFloating ? before.Floating.FirstOrDefault(w => w.Group.Id == was.Group) : null;
			if (old?.X != window.X)
			{
				await FloatingXChanged.InvokeAsync(window.X);
			}

			if (old?.Y != window.Y)
			{
				await FloatingYChanged.InvokeAsync(window.Y);
			}

			if (old?.Width != window.Width)
			{
				await FloatingWidthChanged.InvokeAsync(Pixels(window.Width));
			}

			if (old?.Height != window.Height)
			{
				await FloatingHeightChanged.InvokeAsync(Pixels(window.Height));
			}
		}

		if (SizeChanged.HasDelegate && was.Place == MokaDockPlace.Docked && now.Place == MokaDockPlace.Docked
		    && GroupOf(before, was)?.Size is var oldSize && GroupOf(after, now)?.Size is { } newSize && oldSize != newSize
		    && !MokaDockState.TryShare(newSize, out _))
		{
			await SizeChanged.InvokeAsync(newSize);
		}
	}

	private static MokaDockGroupNode? GroupOf(MokaDockState state, MokaDockPanelLocation location) =>
		location.Group is { } group ? state.FindGroup(group) : null;

	private static string Pixels(double value) => string.Create(CultureInfo.InvariantCulture, $"{value}px");
}
