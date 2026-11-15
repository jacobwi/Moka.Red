using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     One group of a <see cref="MokaDockLayout" />: its header, tabs and buttons, and the slot its panels are drawn
///     over. Drawn by the layout; apps never write it. It redraws only when its group, its place or what its header
///     shows changed, so a tab switch elsewhere costs it nothing.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class MokaDockGroupView : ComponentBase
{
	private readonly Dictionary<string, Func<Task>> _activate = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Func<Task>> _close = new(StringComparer.Ordinal);
	private bool _menuOpen;
	private bool _allTabsOpen;
	private bool _switcherOpen;
	private object? _renderedKey;
	private bool _initialized;

	/// <summary>The layout the group belongs to.</summary>
	[Parameter]
	[EditorRequired]
	public MokaDockLayout Layout { get; set; } = default!;

	/// <summary>The group.</summary>
	[Parameter]
	[EditorRequired]
	public MokaDockGroupNode Group { get; set; } = default!;

	/// <summary>Where the group is drawn.</summary>
	[Parameter]
	public MokaDockGroupMode Mode { get; set; }

	/// <summary>The number of the anchor its slot carries, which its panels' hosts are drawn over.</summary>
	[Parameter]
	public int Anchor { get; set; }

	/// <summary>Changes whenever something the header shows changed outside the group itself.</summary>
	[Parameter]
	public int Stamp { get; set; }

	/// <summary>The group's place in the order F6 and Tab follow.</summary>
	[Parameter]
	public int Order { get; set; }

	private MokaDockText Words => Layout.Words;

	// What the frame draws from the group. Its size isn't in it: a resize changes the track, which the split draws.
	private object Key => ((Group.Id, Group.Panels, Group.Shown, Group.Collapsed, Group.Content), (Mode, Anchor, Stamp, Order),
		(_menuOpen, _allTabsOpen, _switcherOpen));

	private DockGroupSettings Settings => Layout.SettingsOf(Group.Id);

	private bool Collapsed => Mode == MokaDockGroupMode.Docked && Layout.IsCollapsed(Group);

	private bool StackOpen => Layout.IsStackOpen(Group.Id);

	private bool Tabbed => Mode != MokaDockGroupMode.Stack && Layout.ShowsTabs(Group) && !Collapsed;

	private bool Switcher => Layout.TabStyleOf(Group.Id) == MokaDockTabStyle.Switcher && !Collapsed;

	private bool ShowsTabClose => Layout.TabStyleOf(Group.Id) == MokaDockTabStyle.Editor || Mode == MokaDockGroupMode.Narrow;

	private MokaDockPanel? ShownPanel => Group.Shown is { } shown ? Layout.PanelFor(shown) : null;

	private string? ShownTitle => Group.Shown is { } shown ? Layout.TitleOf(shown) : Group.Content ? null : Settings.Title;

	private string Name => Mode == MokaDockGroupMode.Narrow ? Words.Panels : Layout.GroupName(Group);

	private string? OwnedHost => Group.Shown is { } shown
		? Layout.HostIdOf(shown)
		: Group.Content || Mode == MokaDockGroupMode.Narrow ? Layout.HostIdOf(MokaDockLayout.ContentPanelId) : null;

	// An empty grip starts no drag and shows no grab cursor. A floating window keeps its grip, since moving it docks
	// nothing, so AllowDragging leaves it alone.
	private string GripId => Mode == MokaDockGroupMode.Narrow || (Mode != MokaDockGroupMode.Floating && !Layout.AllowDragging)
		? ""
		: Group.Id;

	private string ModeName => Mode switch
	{
		MokaDockGroupMode.Floating => "floating",
		MokaDockGroupMode.Flyout => "flyout",
		MokaDockGroupMode.Narrow => "narrow",
		MokaDockGroupMode.Stack => "stack",
		MokaDockGroupMode.Main => "main",
		_ => "docked"
	};

	// A group shows a header when it has a panel with something to say, or a button to press. The main area with no
	// panel in it shows none, as before.
	private bool HasHeader => Mode is MokaDockGroupMode.Stack or MokaDockGroupMode.Narrow
	                          || (Group.Panels.Count > 0
	                              && (Tabbed || Switcher || ShownTitle is not null || ShownPanel?.TitleContent is not null
	                                  || ShownPanel?.Actions is not null || Buttons.Count > 0));

	private string FrameClass => new CssBuilder("moka-dock-group moka-dock-panel")
		.AddClass($"moka-dock-panel--{SideClass}", SideClass is not null && Mode == MokaDockGroupMode.Docked)
		.AddClass("moka-dock-panel--collapsed", Collapsed)
		.AddClass("moka-dock-panel--floating", Mode == MokaDockGroupMode.Floating)
		.AddClass("moka-dock-group--content", Group.Content)
		.AddClass("moka-dock-group--active", Layout.ViewState.ActiveGroup == Group.Id)
		.AddClass("moka-dock-group--maximized", Layout.ViewState.Maximized == Group.Id && Mode == MokaDockGroupMode.Docked)
		.AddClass("moka-dock-group--stack-closed", Mode == MokaDockGroupMode.Stack && !StackOpen)
		.AddClass(Settings.Class)
		.Build();

	private string? FrameStyle => new StyleBuilder().AddStyle(Settings.Style).Build();

	private string? SideClass => Settings.Side is { } side ? MokaEnumHelpers.ToCssClass(side) : null;

	private string SlotStyle => $"anchor-name: {MokaDockLayout.AnchorName(Anchor)}";

	private string TabsClass => new CssBuilder("moka-dock-tabs")
		.AddClass($"moka-dock-tabs--{MokaEnumHelpers.ToCssClass(Layout.TabStyleOf(Group.Id))}")
		.Build();

	private IReadOnlyList<HeaderButton> Buttons
	{
		get
		{
			List<HeaderButtonKind> kinds = [];
			MokaDockControls controls = Layout.Controls;
			if (Collapsed)
			{
				return [new HeaderButton(HeaderButtonKind.Collapse, CollapsedTooltip)];
			}

			if (Mode is MokaDockGroupMode.Narrow or MokaDockGroupMode.Stack or MokaDockGroupMode.Main)
			{
				if (Group.Shown is { } panel && Layout.IsClosable(panel) && !ShowsTabClose)
				{
					kinds.Add(HeaderButtonKind.Close);
				}
			}
			else
			{
				if ((controls & MokaDockControls.Maximize) != 0 && Mode != MokaDockGroupMode.Flyout && Group.Panels.Count > 0)
				{
					kinds.Add(HeaderButtonKind.Maximize);
				}

				if (Mode == MokaDockGroupMode.Flyout)
				{
					kinds.Add(HeaderButtonKind.Dock);
				}
				else if (Layout.IsFloatable(Group.Id) && Group.Panels.Count > 0)
				{
					kinds.Add(Mode == MokaDockGroupMode.Floating ? HeaderButtonKind.Dock : HeaderButtonKind.Float);
				}

				if ((controls & MokaDockControls.Close) != 0 && !ShowsTabClose && Group.Shown is { } panel && Layout.IsClosable(panel))
				{
					kinds.Add(HeaderButtonKind.Close);
				}

				if ((controls & MokaDockControls.Menu) != 0 && Group.Shown is not null)
				{
					kinds.Add(HeaderButtonKind.Menu);
				}

				if ((controls & MokaDockControls.Collapse) != 0 && Settings.Collapsible && Mode == MokaDockGroupMode.Docked)
				{
					kinds.Add(HeaderButtonKind.Collapse);
				}
			}

			return [.. kinds.Select((k, i) => new HeaderButton(k, i == kinds.Count - 1 ? EndTooltip : MokaTooltipPosition.Bottom))];
		}
	}

	private static MokaTooltipPosition EndTooltip => MokaTooltipPosition.BottomEnd;

	// A folded side strip is a column: a tooltip below would cover the title running down it, so it opens beside the
	// strip, away from the window's edge; a folded bottom strip has the window's edge below it.
	private MokaTooltipPosition CollapsedTooltip => Settings.Side switch
	{
		MokaDockPosition.Left => MokaTooltipPosition.Right,
		MokaDockPosition.Right => MokaTooltipPosition.Left,
		MokaDockPosition.Bottom => MokaTooltipPosition.TopEnd,
		_ => MokaTooltipPosition.BottomEnd
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (!_initialized)
		{
			_initialized = true;
			_renderedKey = Key;
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender()
	{
		object key = Key;
		if (key.Equals(_renderedKey))
		{
			return false;
		}

		_renderedKey = key;
		return true;
	}

	private Func<Task> ActivateHandler(string panel)
	{
		if (!_activate.TryGetValue(panel, out Func<Task>? handler))
		{
			handler = () => Layout.ActivateFromViewAsync(panel);
			_activate[panel] = handler;
		}

		return handler;
	}

	private Func<Task> CloseHandler(string panel)
	{
		if (!_close.TryGetValue(panel, out Func<Task>? handler))
		{
			handler = () => Layout.CloseFromViewAsync(panel);
			_close[panel] = handler;
		}

		return handler;
	}

	private static string TabWrapClass(bool selected) => new CssBuilder("moka-dock-tab-wrap")
		.AddClass("moka-dock-tab-wrap--selected", selected)
		.Build();

	private static string TabClass(bool selected) => new CssBuilder("moka-dock-tab")
		.AddClass("moka-dock-tab--selected", selected)
		.Build();

	private MokaIconDefinition? IconOf(string panel) =>
		panel == MokaDockLayout.ContentPanelId ? null : Layout.PanelFor(panel)?.Icon;

	private string LabelOf(HeaderButtonKind kind) => kind switch
	{
		HeaderButtonKind.Maximize => IsMaximized ? Words.Restore : Words.Maximize,
		HeaderButtonKind.Float => Words.Undock,
		HeaderButtonKind.Dock => Words.Dock,
		HeaderButtonKind.Close => Words.Close,
		HeaderButtonKind.Collapse => Collapsed ? Words.Expand : Words.Collapse,
		_ => Words.Menu
	};

	private bool IsMaximized => Mode == MokaDockGroupMode.Floating
		? Layout.ViewState.Floating.Any(w => w.Group.Id == Group.Id && w.Maximized)
		: Layout.ViewState.Maximized == Group.Id;

	private string? AriaExpanded(HeaderButtonKind kind) =>
		kind == HeaderButtonKind.Collapse ? Collapsed ? "false" : "true" : null;

	private static string IconSize(HeaderButtonKind kind) => kind == HeaderButtonKind.Collapse ? "12px" : "14px";

	private MokaIconDefinition IconFor(HeaderButtonKind kind) => kind switch
	{
		HeaderButtonKind.Maximize => IsMaximized ? MokaIcons.Navigation.Minimize : MokaIcons.Navigation.Maximize,
		HeaderButtonKind.Float => MokaIcons.Navigation.Undock,
		HeaderButtonKind.Dock => Mode == MokaDockGroupMode.Flyout ? MokaIcons.Toggle.Pin : MokaIcons.Navigation.Dock,
		HeaderButtonKind.Close => MokaIcons.Navigation.Close,
		_ => ChevronIcon
	};

	private MokaIconDefinition ChevronIcon => (Settings.Side ?? MokaDockPosition.Left, Collapsed) switch
	{
		(MokaDockPosition.Left, false) => MokaIcons.Navigation.ChevronLeft,
		(MokaDockPosition.Left, true) => MokaIcons.Navigation.ChevronRight,
		(MokaDockPosition.Right, false) => MokaIcons.Navigation.ChevronRight,
		(MokaDockPosition.Right, true) => MokaIcons.Navigation.ChevronLeft,
		(MokaDockPosition.Top, false) => MokaIcons.Navigation.ChevronUp,
		(MokaDockPosition.Top, true) => MokaIcons.Navigation.ChevronDown,
		(MokaDockPosition.Bottom, false) => MokaIcons.Navigation.ChevronDown,
		_ => MokaIcons.Navigation.ChevronUp
	};

	private Task PressAsync(HeaderButtonKind kind) => kind switch
	{
		HeaderButtonKind.Maximize => Layout.ToggleMaximizedAsync(Group.Id),
		HeaderButtonKind.Float => Layout.FloatGroupAsync(Group.Id),
		HeaderButtonKind.Dock => Layout.DockGroupAsync(Group.Id),
		HeaderButtonKind.Close when Group.Shown is { } panel => Layout.CloseFromViewAsync(panel),
		HeaderButtonKind.Collapse => Layout.ToggleCollapsedAsync(Group.Id),
		_ => Task.CompletedTask
	};

	private List<MenuItem> MenuItems()
	{
		List<MenuItem> items = [];
		if (Group.Shown is not { } panel)
		{
			return items;
		}

		if (Mode == MokaDockGroupMode.Floating || Mode == MokaDockGroupMode.Flyout)
		{
			items.Add(new MenuItem(Words.Dock, MokaIcons.Navigation.Dock, false, () => Layout.DockGroupAsync(Group.Id)));
		}
		else if (Layout.IsFloatable(Group.Id))
		{
			items.Add(new MenuItem(Words.Undock, MokaIcons.Navigation.Undock, false, () => Layout.FloatGroupAsync(Group.Id)));
		}

		if (Mode == MokaDockGroupMode.Docked && !Group.Content)
		{
			MokaDockPosition side = Layout.SideOf(Group.Id);
			items.Add(new MenuItem(Words.AutoHide, MokaIcons.Toggle.PinOff, false, () => Layout.AutoHideGroupAsync(Group.Id, side)));
		}

		if (Layout.CanPopOut(panel))
		{
			items.Add(new MenuItem(Words.PopOut, MokaIcons.Content.ExternalLink, false, () => Layout.PopOutPanelAsync(panel)));
		}

		items.Add(new MenuItem(null, null, true, () => Task.CompletedTask));
		items.Add(new MenuItem(Words.DockLeft, MokaIcons.Navigation.PanelLeft, false,
			() => Layout.MoveFromMenuAsync(panel, new MokaDockTarget(null, MokaDockZone.Left))));
		items.Add(new MenuItem(Words.DockRight, MokaIcons.Navigation.PanelRight, false,
			() => Layout.MoveFromMenuAsync(panel, new MokaDockTarget(null, MokaDockZone.Right))));
		items.Add(new MenuItem(Words.DockTop, MokaIcons.Navigation.PanelTop, false,
			() => Layout.MoveFromMenuAsync(panel, new MokaDockTarget(null, MokaDockZone.Top))));
		items.Add(new MenuItem(Words.DockBottom, MokaIcons.Navigation.PanelBottom, false,
			() => Layout.MoveFromMenuAsync(panel, new MokaDockTarget(null, MokaDockZone.Bottom))));

		foreach (MokaDockGroupNode other in Layout.ViewState.DockedGroups.Where(g => g.Id != Group.Id))
		{
			string target = other.Id;
			items.Add(new MenuItem(Words.MoveTo(Layout.GroupName(other)), MokaIcons.Navigation.ArrowRight, false,
				() => Layout.MoveFromMenuAsync(panel, new MokaDockTarget(target, MokaDockZone.Center))));
		}

		if (Layout.IsClosable(panel))
		{
			items.Add(new MenuItem(null, null, true, () => Task.CompletedTask));
			items.Add(new MenuItem(Words.Close, MokaIcons.Navigation.Close, false, () => Layout.CloseFromViewAsync(panel)));
		}

		return items;
	}

	private enum HeaderButtonKind
	{
		Maximize,
		Float,
		Dock,
		Close,
		Menu,
		Collapse
	}

	private readonly record struct HeaderButton(HeaderButtonKind Kind, MokaTooltipPosition Position);

	private sealed record MenuItem(string? Text, MokaIconDefinition? Icon, bool Divider, Func<Task> Run);
}

/// <summary>Where a <see cref="MokaDockGroupView" /> is drawn.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum MokaDockGroupMode
{
	/// <summary>In the docked tree.</summary>
	Docked,

	/// <summary>In a floating window.</summary>
	Floating,

	/// <summary>Open over the content from a side bar.</summary>
	Flyout,

	/// <summary>The one group of a narrow layout's tabs.</summary>
	Narrow,

	/// <summary>One of a narrow layout's stacked groups.</summary>
	Stack,

	/// <summary>The main area of a narrow layout with sheets.</summary>
	Main
}
