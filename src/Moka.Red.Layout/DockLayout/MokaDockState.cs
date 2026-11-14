using System.Text.Json.Serialization;
using Moka.Red.Core.Enums;

namespace Moka.Red.Layout.DockLayout;

/// <summary>
///     Everything a <see cref="MokaDockLayout" /> shows: the docked tree of splits and groups, floating windows,
///     groups pinned to a side bar, panels closed or opened in another window, and which group is maximized. A
///     state is immutable, every operation returns a new one, and parts an operation doesn't touch keep the same
///     instances, so the layout redraws only what changed. <see cref="ToJson" /> and <see cref="FromJson" /> save
///     and restore it as versioned JSON.
/// </summary>
public sealed partial record MokaDockState
{
	/// <summary>The JSON format this version writes. <see cref="FromJson" /> reads this one and older ones.</summary>
	public const int FormatVersion = 1;

	/// <summary>An empty layout: no groups, nothing floating, nothing hidden.</summary>
	public static MokaDockState Empty { get; } = new();

	/// <summary>The JSON format of this state, <see cref="FormatVersion" /> unless it came from an older one.</summary>
	public int Version { get; init; } = FormatVersion;

	/// <summary>
	///     The app's own version of its layout, from <see cref="MokaDockLayout.StateVersion" />, so an app that
	///     renames or removes panels can tell an old saved layout apart (<see cref="MokaDockLayout.MigrateState" />).
	/// </summary>
	public int AppVersion { get; init; }

	/// <summary>The docked tree: a split or a single group. Null when nothing is docked.</summary>
	public MokaDockNode? Root { get; init; }

	/// <summary>Groups in windows of their own over the layout, the last one in front.</summary>
	public IReadOnlyList<MokaDockFloatingWindow> Floating { get; init; } = [];

	/// <summary>Groups pinned to a side bar, shown over the content while open.</summary>
	public IReadOnlyList<MokaDockAutoHidden> AutoHidden { get; init; } = [];

	/// <summary>Panels that were closed and can be shown again where they were.</summary>
	public IReadOnlyList<MokaDockParkedPanel> Hidden { get; init; } = [];

	/// <summary>Panels shown in another browser window, with where they go back to.</summary>
	public IReadOnlyList<MokaDockParkedPanel> PoppedOut { get; init; } = [];

	/// <summary>The id of the docked group filling the layout, or null.</summary>
	public string? Maximized { get; init; }

	/// <summary>The id of the group that was used last, or null.</summary>
	public string? ActiveGroup { get; init; }
}

/// <summary>A part of the docked tree: a <see cref="MokaDockSplitNode" /> or a <see cref="MokaDockGroupNode" />.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(MokaDockSplitNode), "split")]
[JsonDerivedType(typeof(MokaDockGroupNode), "group")]
public abstract record MokaDockNode
{
	/// <summary>The node's id, unique in the state.</summary>
	public required string Id { get; init; }

	/// <summary>
	///     The node's size along its parent split: a CSS length such as <c>250px</c> or <c>25%</c>, or a share such
	///     as <c>1.5fr</c>. Null is one share (<c>1fr</c>). A split's children that are shares divide what the
	///     lengths leave over, in proportion.
	/// </summary>
	public string? Size { get; init; }
}

/// <summary>Children side by side (<see cref="MokaDockSplitDirection.Row" />) or one above the other.</summary>
public sealed record MokaDockSplitNode : MokaDockNode
{
	/// <summary>Which way the children are laid out.</summary>
	public MokaDockSplitDirection Direction { get; init; }

	/// <summary>The children, in order: left to right in a row, top to bottom in a column.</summary>
	public IReadOnlyList<MokaDockNode> Children { get; init; } = [];
}

/// <summary>Panels shown as tabs in one place, one of them at a time.</summary>
public sealed record MokaDockGroupNode : MokaDockNode
{
	/// <summary>The ids of the panels in the group, in tab order.</summary>
	public IReadOnlyList<string> Panels { get; init; } = [];

	/// <summary>The id of the panel showing, or null for the first.</summary>
	public string? Active { get; init; }

	/// <summary>Whether the group is folded to a strip.</summary>
	public bool Collapsed { get; init; }

	/// <summary>
	///     Whether this is the layout's main area, from <see cref="MokaDockContent" />. It stays when its last panel
	///     leaves, showing the content's own child content.
	/// </summary>
	public bool Content { get; init; }

	/// <summary>The panel showing: <see cref="Active" /> when it is in the group, else the first.</summary>
	[JsonIgnore]
	public string? Shown => Active is { } active && Panels.Contains(active) ? active : Panels.Count > 0 ? Panels[0] : null;
}

/// <summary>A group in a window of its own over the layout.</summary>
public sealed record MokaDockFloatingWindow
{
	/// <summary>The group the window shows.</summary>
	public required MokaDockGroupNode Group { get; init; }

	/// <summary>The window's left edge, in pixels from the layout's left edge.</summary>
	public double X { get; init; }

	/// <summary>The window's top edge, in pixels from the layout's top edge.</summary>
	public double Y { get; init; }

	/// <summary>The window's width in pixels.</summary>
	public double Width { get; init; } = 320;

	/// <summary>The window's height in pixels.</summary>
	public double Height { get; init; } = 240;

	/// <summary>Whether the window fills the layout.</summary>
	public bool Maximized { get; init; }

	/// <summary>Where the group docks when the window's Dock button is pressed.</summary>
	public MokaDockTarget? Return { get; init; }
}

/// <summary>A group pinned to a side bar, shown over the content while open.</summary>
public sealed record MokaDockAutoHidden
{
	/// <summary>The group.</summary>
	public required MokaDockGroupNode Group { get; init; }

	/// <summary>The side bar the group's tabs sit in.</summary>
	public MokaDockPosition Side { get; init; }

	/// <summary>Where the group docks again when it is unpinned.</summary>
	public MokaDockTarget? Return { get; init; }
}

/// <summary>A panel that isn't in the layout for now: closed, or shown in another window.</summary>
public sealed record MokaDockParkedPanel
{
	/// <summary>The panel's id.</summary>
	public required string Panel { get; init; }

	/// <summary>Where the panel goes back to.</summary>
	public MokaDockTarget? Return { get; init; }

	/// <summary>The size its group had, when the panel was the group's last, so the group comes back at that size.</summary>
	public string? Size { get; init; }
}

/// <summary>Where a panel or group lands.</summary>
/// <param name="Node">
///     The id of the node to land beside or in, a group or a split. Null is the layout's own edge.
/// </param>
/// <param name="Zone">
///     <see cref="MokaDockZone.Center" /> joins the group as a tab; an edge puts a new group on that side of the node.
/// </param>
/// <param name="Index">For <see cref="MokaDockZone.Center" />, the tab position; null is after the last tab.</param>
public sealed record MokaDockTarget(string? Node, MokaDockZone Zone, int? Index = null);

/// <summary>Which way a split lays out its children.</summary>
public enum MokaDockSplitDirection
{
	/// <summary>Side by side, left to right.</summary>
	Row,

	/// <summary>One above the other, top to bottom.</summary>
	Column
}

/// <summary>Where on a node something lands.</summary>
public enum MokaDockZone
{
	/// <summary>Into the group, as a tab.</summary>
	Center,

	/// <summary>A new group on the left.</summary>
	Left,

	/// <summary>A new group on the right.</summary>
	Right,

	/// <summary>A new group above.</summary>
	Top,

	/// <summary>A new group below.</summary>
	Bottom
}

/// <summary>Where a panel is in a <see cref="MokaDockState" />.</summary>
public enum MokaDockPlace
{
	/// <summary>Not in the state.</summary>
	None,

	/// <summary>In a group of the docked tree.</summary>
	Docked,

	/// <summary>In a floating window.</summary>
	Floating,

	/// <summary>In a group pinned to a side bar.</summary>
	AutoHidden,

	/// <summary>Closed, and can be shown again.</summary>
	Hidden,

	/// <summary>Shown in another browser window.</summary>
	PoppedOut
}

/// <summary>Where a panel is: the kind of place and, for a group, the group's id.</summary>
/// <param name="Place">The kind of place.</param>
/// <param name="Group">The group holding the panel, for the places that have one.</param>
public readonly record struct MokaDockPanelLocation(MokaDockPlace Place, string? Group);
