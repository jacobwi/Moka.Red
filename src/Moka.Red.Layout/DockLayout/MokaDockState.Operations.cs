using System.Globalization;
using Moka.Red.Core.Enums;

namespace Moka.Red.Layout.DockLayout;

public sealed partial record MokaDockState
{
	private const string EdgeSize = "25%";

	/// <summary>The groups of the docked tree, left to right and top to bottom.</summary>
	public IEnumerable<MokaDockGroupNode> DockedGroups => Groups(Root);

	/// <summary>Every group: the docked ones, then the floating ones back to front, then the pinned ones.</summary>
	public IEnumerable<MokaDockGroupNode> AllGroups =>
		DockedGroups.Concat(Floating.Select(w => w.Group)).Concat(AutoHidden.Select(a => a.Group));

	/// <summary>The id of every panel the state holds, wherever it is.</summary>
	public IEnumerable<string> Panels =>
		AllGroups.SelectMany(g => g.Panels).Concat(Hidden.Select(p => p.Panel)).Concat(PoppedOut.Select(p => p.Panel));

	/// <summary>Finds where a panel is.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>The place, and the group for a place that has one.</returns>
	public MokaDockPanelLocation Locate(string panel)
	{
		ArgumentNullException.ThrowIfNull(panel);
		if (DockedGroups.FirstOrDefault(g => g.Panels.Contains(panel)) is { } docked)
		{
			return new MokaDockPanelLocation(MokaDockPlace.Docked, docked.Id);
		}

		if (Floating.FirstOrDefault(w => w.Group.Panels.Contains(panel)) is { } window)
		{
			return new MokaDockPanelLocation(MokaDockPlace.Floating, window.Group.Id);
		}

		if (AutoHidden.FirstOrDefault(a => a.Group.Panels.Contains(panel)) is { } pinned)
		{
			return new MokaDockPanelLocation(MokaDockPlace.AutoHidden, pinned.Group.Id);
		}

		if (Hidden.Any(p => p.Panel == panel))
		{
			return new MokaDockPanelLocation(MokaDockPlace.Hidden, null);
		}

		return PoppedOut.Any(p => p.Panel == panel)
			? new MokaDockPanelLocation(MokaDockPlace.PoppedOut, null)
			: default;
	}

	/// <summary>Finds a group by id, docked, floating or pinned.</summary>
	/// <param name="group">The group's id.</param>
	/// <returns>The group, or null.</returns>
	public MokaDockGroupNode? FindGroup(string group) => AllGroups.FirstOrDefault(g => g.Id == group);

	/// <summary>Finds a node of the docked tree by id.</summary>
	/// <param name="node">The node's id.</param>
	/// <returns>The node, or null.</returns>
	public MokaDockNode? FindNode(string node) => Find(Root, node);

	/// <summary>Shows a panel in its group and makes that group the active one.</summary>
	/// <param name="panel">The panel's id. A hidden panel comes back where it was.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Activate(string panel)
	{
		MokaDockPanelLocation location = Locate(panel);
		if (location.Place == MokaDockPlace.Hidden)
		{
			return Show(panel);
		}

		if (location.Group is not { } group)
		{
			return this;
		}

		MokaDockState next = UpdateGroup(group, g => g.Active == panel ? g : g with { Active = panel });
		return next.ActiveGroup == group ? next : next with { ActiveGroup = group };
	}

	/// <summary>Adds a panel the state doesn't hold yet, such as a document opened from code, and shows it.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="target">Where it lands. Null is the layout's right edge.</param>
	/// <returns>The new state, or the panel shown where it is when the state holds it already.</returns>
	public MokaDockState Add(string panel, MokaDockTarget? target)
	{
		ArgumentNullException.ThrowIfNull(panel);
		return Locate(panel).Place != MokaDockPlace.None
			? Show(panel)
			: InsertGroup(NewGroup(panel), target).Normalize().Activate(panel);
	}

	/// <summary>Brings a closed panel back where it was, else shows it where it is.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Show(string panel)
	{
		if (Hidden.FirstOrDefault(p => p.Panel == panel) is not { } parked)
		{
			return Locate(panel).Place == MokaDockPlace.None ? this : Activate(panel);
		}

		MokaDockState without = this with { Hidden = [.. Hidden.Where(p => p.Panel != panel)] };
		return without.InsertGroup(NewGroup(panel) with { Size = parked.Size }, parked.Return).Normalize().Activate(panel);
	}

	/// <summary>Takes a panel out of the layout.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="remember">
	///     True keeps it in <see cref="Hidden" /> so <see cref="Show" /> brings it back where it was; false forgets it.
	/// </param>
	/// <returns>The new state.</returns>
	public MokaDockState Close(string panel, bool remember = true)
	{
		(MokaDockState without, MokaDockTarget? back, string? size) = Extract(panel);
		if (ReferenceEquals(without, this))
		{
			return this;
		}

		return remember
			? without with { Hidden = [.. without.Hidden, new MokaDockParkedPanel { Panel = panel, Return = back, Size = size }] }
			: without;
	}

	/// <summary>Moves a panel: into a group as a tab, or into a new group beside a node or the layout's edge.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="target">Where it lands.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Move(string panel, MokaDockTarget target)
	{
		ArgumentNullException.ThrowIfNull(target);
		MokaDockPanelLocation location = Locate(panel);
		if (location.Place == MokaDockPlace.None)
		{
			return this;
		}

		if (location.Group is { } from && target.Node == from)
		{
			MokaDockGroupNode source = FindGroup(from)!;
			if (target.Zone == MokaDockZone.Center)
			{
				int at = target.Index ?? source.Panels.Count;
				return Reorder(panel, at > IndexOf(source.Panels, panel) ? at - 1 : at);
			}

			if (source.Panels.Count == 1)
			{
				return this;
			}
		}

		(MokaDockState without, _, _) = Extract(panel);
		return without.InsertGroup(NewGroup(panel), target).Normalize().Activate(panel);
	}

	/// <summary>Moves a whole group, its tabs in order, beside a node or into another group.</summary>
	/// <param name="group">The group's id.</param>
	/// <param name="target">Where it lands.</param>
	/// <returns>The new state.</returns>
	public MokaDockState MoveGroup(string group, MokaDockTarget target)
	{
		ArgumentNullException.ThrowIfNull(target);
		if (target.Node == group)
		{
			return this;
		}

		(MokaDockState without, MokaDockGroupNode? node, _) = ExtractGroup(group);
		if (node is null)
		{
			return this;
		}

		return without.InsertGroup(node with { Collapsed = false }, target).Normalize() with { ActiveGroup = group };
	}

	/// <summary>Moves a panel to another place among its group's tabs.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="index">The new position, clamped to the group.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Reorder(string panel, int index)
	{
		if (Locate(panel).Group is not { } group)
		{
			return this;
		}

		return UpdateGroup(group, g =>
		{
			List<string> panels = [.. g.Panels];
			int from = panels.IndexOf(panel);
			int to = Math.Clamp(index, 0, panels.Count - 1);
			if (from == to)
			{
				return g;
			}

			panels.RemoveAt(from);
			panels.Insert(to, panel);
			return g with { Panels = panels, Active = panel };
		});
	}

	/// <summary>Takes a panel out into a floating window of its own.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <param name="x">The window's left edge in pixels.</param>
	/// <param name="y">The window's top edge in pixels.</param>
	/// <param name="width">The window's width in pixels.</param>
	/// <param name="height">The window's height in pixels.</param>
	/// <returns>The new state.</returns>
	public MokaDockState FloatPanel(string panel, double x, double y, double width, double height)
	{
		MokaDockPanelLocation location = Locate(panel);
		if (location.Place == MokaDockPlace.Floating && FindGroup(location.Group!)!.Panels.Count == 1)
		{
			return MoveFloating(location.Group!, x, y, width, height);
		}

		if (location.Place == MokaDockPlace.Docked && FindGroup(location.Group!) is { Panels.Count: 1, Content: false })
		{
			return FloatGroup(location.Group!, x, y, width, height);
		}

		(MokaDockState without, MokaDockTarget? back, _) = Extract(panel);
		if (ReferenceEquals(without, this))
		{
			return this;
		}

		MokaDockFloatingWindow window = new()
		{
			Group = NewGroup(panel),
			X = x,
			Y = y,
			Width = width,
			Height = height,
			Return = back
		};
		return without with { Floating = [.. without.Floating, window], ActiveGroup = window.Group.Id };
	}

	/// <summary>Takes a whole group out into a floating window, or moves one that already floats.</summary>
	/// <param name="group">The group's id.</param>
	/// <param name="x">The window's left edge in pixels.</param>
	/// <param name="y">The window's top edge in pixels.</param>
	/// <param name="width">The window's width in pixels.</param>
	/// <param name="height">The window's height in pixels.</param>
	/// <returns>The new state.</returns>
	public MokaDockState FloatGroup(string group, double x, double y, double width, double height)
	{
		if (Floating.Any(w => w.Group.Id == group))
		{
			return MoveFloating(group, x, y, width, height);
		}

		(MokaDockState without, MokaDockGroupNode? node, MokaDockTarget? back) = ExtractGroup(group);
		if (node is null || node.Content)
		{
			return this;
		}

		MokaDockFloatingWindow window = new()
		{
			Group = node with { Collapsed = false },
			X = x,
			Y = y,
			Width = width,
			Height = height,
			Return = back
		};
		return without with { Floating = [.. without.Floating, window], ActiveGroup = group };
	}

	/// <summary>Docks a floating or pinned group where it came from.</summary>
	/// <param name="group">The group's id.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Dock(string group)
	{
		MokaDockTarget? back;
		if (Floating.FirstOrDefault(w => w.Group.Id == group) is { } window)
		{
			back = window.Return;
		}
		else if (AutoHidden.FirstOrDefault(a => a.Group.Id == group) is { } pinned)
		{
			back = pinned.Return ?? new MokaDockTarget(null, ZoneOf(pinned.Side));
		}
		else
		{
			return this;
		}

		(MokaDockState without, MokaDockGroupNode? node, _) = ExtractGroup(group);
		return without.InsertGroup(node!, back ?? new MokaDockTarget(null, MokaDockZone.Right)).Normalize() with { ActiveGroup = group };
	}

	/// <summary>Pins a group to a side bar: its tabs sit in the bar, and it opens over the content.</summary>
	/// <param name="group">The group's id.</param>
	/// <param name="side">The side bar.</param>
	/// <returns>The new state.</returns>
	public MokaDockState AutoHide(string group, MokaDockPosition side)
	{
		if (AutoHidden.Any(a => a.Group.Id == group))
		{
			return this;
		}

		MokaDockTarget? floatingReturn = Floating.FirstOrDefault(w => w.Group.Id == group)?.Return;
		(MokaDockState without, MokaDockGroupNode? node, MokaDockTarget? back) = ExtractGroup(group);
		if (node is null || node.Content)
		{
			return this;
		}

		MokaDockAutoHidden pinned = new()
		{
			Group = node with { Collapsed = false },
			Side = side,
			Return = floatingReturn ?? back
		};
		return without with { AutoHidden = [.. without.AutoHidden, pinned] };
	}

	/// <summary>Folds a group to a strip, or opens it again.</summary>
	/// <param name="group">The group's id.</param>
	/// <param name="collapsed">Whether the group is folded.</param>
	/// <returns>The new state.</returns>
	public MokaDockState SetCollapsed(string group, bool collapsed) =>
		UpdateGroup(group, g => g.Collapsed == collapsed ? g : g with { Collapsed = collapsed });

	/// <summary>Sets the sizes of a split's children.</summary>
	/// <param name="split">The split's id.</param>
	/// <param name="sizes">One size per child, as <see cref="MokaDockNode.Size" /> takes it.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Resize(string split, IReadOnlyList<string?> sizes)
	{
		ArgumentNullException.ThrowIfNull(sizes);
		if (Root is null)
		{
			return this;
		}

		MokaDockNode? root = Replace(Root, split, node =>
		{
			if (node is not MokaDockSplitNode s || s.Children.Count != sizes.Count)
			{
				return node;
			}

			bool same = true;
			MokaDockNode[] children = new MokaDockNode[s.Children.Count];
			for (int i = 0; i < children.Length; i++)
			{
				children[i] = s.Children[i].Size == sizes[i] ? s.Children[i] : s.Children[i] with { Size = sizes[i] };
				same &= ReferenceEquals(children[i], s.Children[i]);
			}

			return same ? s : s with { Children = children };
		});
		return ReferenceEquals(root, Root) ? this : this with { Root = root };
	}

	/// <summary>Moves and sizes a floating window.</summary>
	/// <param name="group">The group the window shows.</param>
	/// <param name="x">The left edge in pixels.</param>
	/// <param name="y">The top edge in pixels.</param>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	/// <returns>The new state.</returns>
	public MokaDockState MoveFloating(string group, double x, double y, double width, double height) =>
		UpdateWindow(group, w => w.X == x && w.Y == y && w.Width == width && w.Height == height
			? w
			: w with { X = x, Y = y, Width = width, Height = height });

	/// <summary>Makes a floating window fill the layout, or brings it back to its own size.</summary>
	/// <param name="group">The group the window shows.</param>
	/// <param name="maximized">Whether the window fills the layout.</param>
	/// <returns>The new state.</returns>
	public MokaDockState SetFloatingMaximized(string group, bool maximized) =>
		UpdateWindow(group, w => w.Maximized == maximized ? w : w with { Maximized = maximized });

	/// <summary>Puts a floating window in front of the others.</summary>
	/// <param name="group">The group the window shows.</param>
	/// <returns>The new state.</returns>
	public MokaDockState BringToFront(string group)
	{
		int index = IndexOfWindow(group);
		if (index < 0 || index == Floating.Count - 1)
		{
			return this;
		}

		List<MokaDockFloatingWindow> windows = [.. Floating];
		MokaDockFloatingWindow window = windows[index];
		windows.RemoveAt(index);
		windows.Add(window);
		return this with { Floating = windows, ActiveGroup = group };
	}

	/// <summary>Makes a docked group fill the layout, or none.</summary>
	/// <param name="group">The group's id, or null to bring every group back.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Maximize(string? group)
	{
		string? maximized = group is not null && DockedGroups.Any(g => g.Id == group) ? group : null;
		return maximized == Maximized ? this : this with { Maximized = maximized };
	}

	/// <summary>Takes a panel out to show it in another browser window.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>The new state.</returns>
	public MokaDockState PopOut(string panel)
	{
		(MokaDockState without, MokaDockTarget? back, string? size) = Extract(panel);
		return ReferenceEquals(without, this)
			? this
			: without with { PoppedOut = [.. without.PoppedOut, new MokaDockParkedPanel { Panel = panel, Return = back, Size = size }] };
	}

	/// <summary>Brings a panel shown in another window back where it was.</summary>
	/// <param name="panel">The panel's id.</param>
	/// <returns>The new state.</returns>
	public MokaDockState ReturnPoppedOut(string panel)
	{
		if (PoppedOut.FirstOrDefault(p => p.Panel == panel) is not { } parked)
		{
			return this;
		}

		MokaDockState without = this with { PoppedOut = [.. PoppedOut.Where(p => p.Panel != panel)] };
		return without.InsertGroup(NewGroup(panel) with { Size = parked.Size }, parked.Return).Normalize().Activate(panel);
	}

	/// <summary>
	///     Fits a saved state to the panels there are now: panels no longer there are dropped, and panels the saved
	///     state doesn't know join it where <paramref name="defaults" /> has them.
	/// </summary>
	/// <param name="defaults">The layout's starting state, which knows where every panel starts.</param>
	/// <param name="panels">The ids of the panels the layout has.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Reconcile(MokaDockState defaults, IReadOnlyCollection<string> panels)
	{
		ArgumentNullException.ThrowIfNull(defaults);
		ArgumentNullException.ThrowIfNull(panels);

		HashSet<string> known = [.. panels];
		MokaDockState state = KeepOnly(known);
		HashSet<string> present = [.. state.Panels];

		foreach (MokaDockGroupNode group in defaults.AllGroups)
		{
			List<string> missing = [.. group.Panels.Where(p => known.Contains(p) && !present.Contains(p))];
			if (missing.Count == 0)
			{
				continue;
			}

			present.UnionWith(missing);
			if (state.FindGroup(group.Id) is not null)
			{
				state = state.UpdateGroup(group.Id, g => g with { Panels = [.. g.Panels, .. missing] });
				continue;
			}

			if (defaults.Floating.FirstOrDefault(w => w.Group.Id == group.Id) is { } window)
			{
				state = state with { Floating = [.. state.Floating, window with { Group = group with { Panels = missing } }] };
				continue;
			}

			if (defaults.AutoHidden.FirstOrDefault(a => a.Group.Id == group.Id) is { } pinned)
			{
				state = state with { AutoHidden = [.. state.AutoHidden, pinned with { Group = group with { Panels = missing } }] };
				continue;
			}

			MokaDockTarget? target = NeighborTarget(defaults.Root, group.Id);
			if (target?.Node is { } node && state.FindNode(node) is null)
			{
				target = new MokaDockTarget(null, MokaDockZone.Right);
			}

			state = state.InsertGroup(group with { Panels = missing }, target);
		}

		if (defaults.DockedGroups.FirstOrDefault(g => g.Content) is { } content && !state.DockedGroups.Any(g => g.Content))
		{
			state = state.InsertGroup(content with { Panels = [] }, NeighborTarget(defaults.Root, content.Id));
		}

		List<MokaDockParkedPanel> hidden = [.. state.Hidden];
		foreach (MokaDockParkedPanel parked in defaults.Hidden.Where(p => known.Contains(p.Panel) && !present.Contains(p.Panel)))
		{
			hidden.Add(parked);
			present.Add(parked.Panel);
		}

		return (state with { Hidden = hidden }).Normalize();
	}

	/// <summary>
	///     Fits a state from elsewhere, such as a preset or one the app saved, to the layout as it is now. It does what
	///     <see cref="Reconcile(MokaDockState, IReadOnlyCollection{string})" /> does, and carries over what is open in
	///     <paramref name="current" />: a panel <paramref name="defaults" /> has no place for, such as a document opened
	///     from code, keeps its group when this state has a group with that id, else lands where an opened panel lands
	///     (the main area, else the active group, else the layout's right edge). It takes over what a group shows only
	///     when it showed in that same group before, or the group shows nothing. The panels shown in another window are
	///     the ones <paramref name="current" /> shows there, since only a live layout has windows open. The active group
	///     stays this state's.
	/// </summary>
	/// <param name="defaults">The layout's starting state, which knows where every declared panel starts.</param>
	/// <param name="panels">The ids of the panels the layout has.</param>
	/// <param name="current">The state this one replaces.</param>
	/// <returns>The new state.</returns>
	public MokaDockState Reconcile(MokaDockState defaults, IReadOnlyCollection<string> panels, MokaDockState current)
	{
		ArgumentNullException.ThrowIfNull(current);
		MokaDockState state = Reconcile(defaults, panels);
		string? active = state.ActiveGroup;
		HashSet<string> known = [.. panels];

		foreach (MokaDockParkedPanel parked in state.PoppedOut.Where(p => current.Locate(p.Panel).Place != MokaDockPlace.PoppedOut).ToList())
		{
			state = state.ReturnPoppedOut(parked.Panel);
		}

		foreach (MokaDockParkedPanel parked in current.PoppedOut.Where(p => known.Contains(p.Panel)))
		{
			if (state.Locate(parked.Panel).Place == MokaDockPlace.PoppedOut)
			{
				continue;
			}

			(MokaDockState without, MokaDockTarget? back, string? size) = state.Extract(parked.Panel);
			MokaDockParkedPanel kept = ReferenceEquals(without, state) ? parked : parked with { Return = back, Size = size };
			state = without with { PoppedOut = [.. without.PoppedOut, kept] };
		}

		foreach (string panel in current.Panels.Where(known.Contains).ToList())
		{
			if (state.Locate(panel).Place == MokaDockPlace.None)
			{
				state = state.Carry(panel, current);
			}
		}

		return (state.ActiveGroup == active ? state : state with { ActiveGroup = active }).Normalize();
	}

	// Where a panel opened from code lands when no target is named: the main area as a tab, else the active group,
	// else the layout's right edge.
	internal MokaDockTarget? OpenTarget() =>
		DockedGroups.FirstOrDefault(g => g.Content) is { } content
			? new MokaDockTarget(content.Id, MokaDockZone.Center)
			: ActiveGroup is { } active && FindGroup(active) is not null
				? new MokaDockTarget(active, MokaDockZone.Center)
				: null;

	// Puts a panel this state has no place for where current has it. Joining a group that shows a panel already
	// doesn't change that, unless this panel showed in that same group before.
	private MokaDockState Carry(string panel, MokaDockState current)
	{
		MokaDockPanelLocation was = current.Locate(panel);
		if (was.Place == MokaDockPlace.Hidden)
		{
			return this with { Hidden = [.. Hidden, current.Hidden.First(p => p.Panel == panel)] };
		}

		MokaDockGroupNode from = current.FindGroup(was.Group!)!;
		bool rejoins = FindGroup(from.Id) is not null;
		MokaDockTarget? target = rejoins ? new MokaDockTarget(from.Id, MokaDockZone.Center) : OpenTarget();
		string? shown = target?.Node is { } node ? FindGroup(node)?.Shown : null;
		MokaDockState next = InsertGroup(NewGroup(panel), target).Normalize();
		if (shown is not null && !(rejoins && from.Shown == panel) && next.Locate(panel).Group is { } group)
		{
			next = next.UpdateGroup(group, g => g with { Active = shown });
		}

		return next;
	}

	/// <summary>
	///     Tidies the tree: drops empty groups (but the main area) and empty splits, puts a split's only child in its
	///     place, and makes every group's active panel one it holds. The operations call it themselves.
	/// </summary>
	/// <returns>The tidy state, this one when nothing needed doing.</returns>
	public MokaDockState Normalize()
	{
		MokaDockNode? root = Root is null ? null : NormalizeNode(Root);
		if (root is { Size: not null })
		{
			root = root with { Size = null };
		}

		List<MokaDockFloatingWindow> floating = [.. Floating.Where(w => w.Group.Panels.Count > 0)
			.Select(w => NormalizeNode(w.Group) is MokaDockGroupNode g && !ReferenceEquals(g, w.Group) ? w with { Group = g } : w)];
		List<MokaDockAutoHidden> pinned = [.. AutoHidden.Where(a => a.Group.Panels.Count > 0)
			.Select(a => NormalizeNode(a.Group) is MokaDockGroupNode g && !ReferenceEquals(g, a.Group) ? a with { Group = g } : a)];

		MokaDockState state = this;
		if (!ReferenceEquals(root, Root))
		{
			state = state with { Root = root };
		}

		if (!floating.SequenceEqual(Floating))
		{
			state = state with { Floating = floating };
		}

		if (!pinned.SequenceEqual(AutoHidden))
		{
			state = state with { AutoHidden = pinned };
		}

		if (state.Maximized is { } maximized && !state.DockedGroups.Any(g => g.Id == maximized))
		{
			state = state with { Maximized = null };
		}

		if (state.ActiveGroup is { } active && state.FindGroup(active) is null)
		{
			state = state with { ActiveGroup = null };
		}

		return state;
	}

	internal static MokaDockGroupNode NewGroup(string panel) => new()
	{
		Id = NewId("g"),
		Panels = [panel],
		Active = panel
	};

	internal static string NewId(string prefix) =>
		string.Create(CultureInfo.InvariantCulture, $"{prefix}-{Guid.NewGuid():N}")[..(prefix.Length + 9)];

	internal static bool TryShare(string? size, out double weight)
	{
		weight = 1;
		if (size is null)
		{
			return true;
		}

		string text = size.Trim();
		return text.EndsWith("fr", StringComparison.OrdinalIgnoreCase)
		       && double.TryParse(text.AsSpan(0, text.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out weight)
		       && weight > 0;
	}

	internal static string Share(double weight) =>
		string.Create(CultureInfo.InvariantCulture, $"{Math.Round(weight, 4)}fr");

	internal static MokaDockTarget? NeighborTarget(MokaDockNode? root, string node)
	{
		if (root is null || FindParent(root, node) is not var (parent, index))
		{
			return null;
		}

		bool row = parent.Direction == MokaDockSplitDirection.Row;
		if (index > 0)
		{
			return new MokaDockTarget(parent.Children[index - 1].Id, row ? MokaDockZone.Right : MokaDockZone.Bottom);
		}

		return parent.Children.Count > 1
			? new MokaDockTarget(parent.Children[1].Id, row ? MokaDockZone.Left : MokaDockZone.Top)
			: null;
	}

	internal static IEnumerable<MokaDockGroupNode> Groups(MokaDockNode? node)
	{
		switch (node)
		{
			case MokaDockGroupNode group:
				yield return group;
				break;
			case MokaDockSplitNode split:
				foreach (MokaDockNode child in split.Children)
				{
					foreach (MokaDockGroupNode group in Groups(child))
					{
						yield return group;
					}
				}

				break;
		}
	}

	internal static (MokaDockSplitNode Parent, int Index)? FindParent(MokaDockNode node, string id)
	{
		if (node is not MokaDockSplitNode split)
		{
			return null;
		}

		for (int i = 0; i < split.Children.Count; i++)
		{
			if (split.Children[i].Id == id)
			{
				return (split, i);
			}

			if (FindParent(split.Children[i], id) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private static MokaDockNode? Find(MokaDockNode? node, string id) => node switch
	{
		null => null,
		_ when node.Id == id => node,
		MokaDockSplitNode split => split.Children.Select(c => Find(c, id)).FirstOrDefault(c => c is not null),
		_ => null
	};

	// The tree with the node replaced by what replace gives back, or removed when that is null. Branches that don't
	// hold the node keep their instances.
	private static MokaDockNode? Replace(MokaDockNode node, string id, Func<MokaDockNode, MokaDockNode?> replace)
	{
		if (node.Id == id)
		{
			return replace(node);
		}

		if (node is not MokaDockSplitNode split)
		{
			return node;
		}

		bool changed = false;
		List<MokaDockNode> children = new(split.Children.Count);
		foreach (MokaDockNode child in split.Children)
		{
			MokaDockNode? next = Replace(child, id, replace);
			changed |= !ReferenceEquals(next, child);
			if (next is not null)
			{
				children.Add(next);
			}
		}

		return changed ? split with { Children = children } : split;
	}

	private static MokaDockNode? NormalizeNode(MokaDockNode node)
	{
		if (node is MokaDockGroupNode group)
		{
			if (group.Panels.Count == 0 && !group.Content)
			{
				return null;
			}

			return group.Active is { } active && !group.Panels.Contains(active)
				? group with { Active = group.Panels.Count > 0 ? group.Panels[0] : null }
				: group;
		}

		MokaDockSplitNode split = (MokaDockSplitNode)node;
		bool changed = false;
		List<MokaDockNode> children = new(split.Children.Count);
		foreach (MokaDockNode child in split.Children)
		{
			MokaDockNode? next = NormalizeNode(child);
			changed |= !ReferenceEquals(next, child);
			if (next is MokaDockSplitNode inner && inner.Direction == split.Direction && CanMerge(inner))
			{
				TryShare(inner.Size, out double weight);
				double total = inner.Children.Sum(c => TryShare(c.Size, out double w) ? w : 0);
				children.AddRange(inner.Children.Select(c =>
				{
					TryShare(c.Size, out double w);
					return c with { Size = Share(w / total * weight) };
				}));
				changed = true;
			}
			else if (next is not null)
			{
				children.Add(next);
			}
		}

		return children.Count switch
		{
			0 => null,
			1 => children[0].Size == split.Size ? children[0] : children[0] with { Size = split.Size },
			_ => changed ? split with { Children = children } : split
		};
	}

	private static bool CanMerge(MokaDockSplitNode inner) =>
		TryShare(inner.Size, out _) && inner.Children.All(c => TryShare(c.Size, out _));

	private static MokaDockZone ZoneOf(MokaDockPosition side) => side switch
	{
		MokaDockPosition.Left => MokaDockZone.Left,
		MokaDockPosition.Right => MokaDockZone.Right,
		MokaDockPosition.Top => MokaDockZone.Top,
		_ => MokaDockZone.Bottom
	};

	private MokaDockState KeepOnly(HashSet<string> known)
	{
		MokaDockState state = this;
		foreach (string panel in Panels.Where(p => !known.Contains(p)).ToList())
		{
			state = state.Close(panel, remember: false);
		}

		return state with
		{
			Hidden = [.. state.Hidden.Where(p => known.Contains(p.Panel))],
			PoppedOut = [.. state.PoppedOut.Where(p => known.Contains(p.Panel))]
		};
	}

	private int IndexOfWindow(string group)
	{
		for (int i = 0; i < Floating.Count; i++)
		{
			if (Floating[i].Group.Id == group)
			{
				return i;
			}
		}

		return -1;
	}

	private MokaDockState UpdateWindow(string group, Func<MokaDockFloatingWindow, MokaDockFloatingWindow> update)
	{
		int index = IndexOfWindow(group);
		if (index < 0)
		{
			return this;
		}

		MokaDockFloatingWindow next = update(Floating[index]);
		if (ReferenceEquals(next, Floating[index]))
		{
			return this;
		}

		List<MokaDockFloatingWindow> windows = [.. Floating];
		windows[index] = next;
		return this with { Floating = windows };
	}

	private MokaDockState UpdateGroup(string group, Func<MokaDockGroupNode, MokaDockGroupNode?> update)
	{
		if (Root is not null && Find(Root, group) is MokaDockGroupNode)
		{
			MokaDockNode? root = Replace(Root, group, node => update((MokaDockGroupNode)node));
			return ReferenceEquals(root, Root) ? this : (this with { Root = root }).Normalize();
		}

		int window = IndexOfWindow(group);
		if (window >= 0)
		{
			MokaDockGroupNode? next = update(Floating[window].Group);
			if (ReferenceEquals(next, Floating[window].Group))
			{
				return this;
			}

			List<MokaDockFloatingWindow> windows = [.. Floating];
			if (next is null || next.Panels.Count == 0)
			{
				windows.RemoveAt(window);
			}
			else
			{
				windows[window] = windows[window] with { Group = next };
			}

			return (this with { Floating = windows }).Normalize();
		}

		for (int i = 0; i < AutoHidden.Count; i++)
		{
			if (AutoHidden[i].Group.Id != group)
			{
				continue;
			}

			MokaDockGroupNode? next = update(AutoHidden[i].Group);
			if (ReferenceEquals(next, AutoHidden[i].Group))
			{
				return this;
			}

			List<MokaDockAutoHidden> pinned = [.. AutoHidden];
			if (next is null || next.Panels.Count == 0)
			{
				pinned.RemoveAt(i);
			}
			else
			{
				pinned[i] = pinned[i] with { Group = next };
			}

			return (this with { AutoHidden = pinned }).Normalize();
		}

		return this;
	}

	// The state without the panel, and where it would go back to: its group as a tab when the group stays, else the
	// spot beside its group's neighbor. A panel in no group comes out of the hidden and popped-out lists.
	private (MokaDockState Without, MokaDockTarget? Return, string? Size) Extract(string panel)
	{
		MokaDockPanelLocation location = Locate(panel);
		switch (location.Place)
		{
			case MokaDockPlace.Hidden:
				MokaDockParkedPanel hidden = Hidden.First(p => p.Panel == panel);
				return (this with { Hidden = [.. Hidden.Where(p => p.Panel != panel)] }, hidden.Return, hidden.Size);
			case MokaDockPlace.PoppedOut:
				MokaDockParkedPanel popped = PoppedOut.First(p => p.Panel == panel);
				return (this with { PoppedOut = [.. PoppedOut.Where(p => p.Panel != panel)] }, popped.Return, popped.Size);
			case MokaDockPlace.None:
				return (this, null, null);
		}

		MokaDockGroupNode group = FindGroup(location.Group!)!;
		MokaDockTarget? back;
		string? size = null;
		if (group.Panels.Count > 1 || group.Content)
		{
			back = new MokaDockTarget(group.Id, MokaDockZone.Center, IndexOf(group.Panels, panel));
		}
		else
		{
			back = location.Place switch
			{
				MokaDockPlace.Floating => Floating.First(w => w.Group.Id == group.Id).Return,
				MokaDockPlace.AutoHidden => AutoHidden.First(a => a.Group.Id == group.Id).Return,
				_ => NeighborTarget(Root, group.Id)
			};
			size = group.Size;
		}

		MokaDockState without = UpdateGroup(group.Id, g => g with { Panels = [.. g.Panels.Where(p => p != panel)] });
		return (without, back, size);
	}

	// The state without the group, the group itself, and where it would go back to.
	private (MokaDockState Without, MokaDockGroupNode? Group, MokaDockTarget? Return) ExtractGroup(string group)
	{
		int window = IndexOfWindow(group);
		if (window >= 0)
		{
			MokaDockFloatingWindow w = Floating[window];
			return (this with { Floating = [.. Floating.Where((_, i) => i != window)] }, w.Group, w.Return);
		}

		if (AutoHidden.FirstOrDefault(a => a.Group.Id == group) is { } pinned)
		{
			return (this with { AutoHidden = [.. AutoHidden.Where(a => a.Group.Id != group)] }, pinned.Group, pinned.Return);
		}

		if (Root is null || Find(Root, group) is not MokaDockGroupNode node)
		{
			return (this, null, null);
		}

		MokaDockTarget? back = NeighborTarget(Root, group);
		MokaDockNode? root = Replace(Root, group, _ => null);
		return ((this with { Root = root }).Normalize(), node, back);
	}

	private MokaDockState InsertGroup(MokaDockGroupNode group, MokaDockTarget? target)
	{
		target ??= new MokaDockTarget(null, MokaDockZone.Right);
		if (target.Zone == MokaDockZone.Center)
		{
			if (target.Node is { } into && FindGroup(into) is { } existing)
			{
				return UpdateGroup(existing.Id, g =>
				{
					List<string> panels = [.. g.Panels];
					int at = Math.Clamp(target.Index ?? panels.Count, 0, panels.Count);
					panels.InsertRange(at, group.Panels.Where(p => !panels.Contains(p)));
					return g with { Panels = panels, Active = group.Shown ?? g.Active, Collapsed = false };
				}) with { ActiveGroup = existing.Id };
			}

			target = target with { Zone = MokaDockZone.Right };
		}

		bool row = target.Zone is MokaDockZone.Left or MokaDockZone.Right;
		bool before = target.Zone is MokaDockZone.Left or MokaDockZone.Top;
		MokaDockSplitDirection direction = row ? MokaDockSplitDirection.Row : MokaDockSplitDirection.Column;

		if (Root is null)
		{
			return this with { Root = group with { Size = null }, ActiveGroup = group.Id };
		}

		MokaDockNode? node = target.Node is null ? null : Find(Root, target.Node);
		if (node is null || FindParent(Root, node.Id) is not var (parent, index))
		{
			return this with { Root = WrapEdge(Root, group, direction, before), ActiveGroup = group.Id };
		}

		MokaDockNode root;
		if (parent.Direction == direction)
		{
			string? size = group.Size;
			MokaDockNode sibling = node;
			if (size is null)
			{
				if (TryShare(node.Size, out double weight))
				{
					size = Share(weight / 2);
					sibling = node with { Size = size };
				}
				else
				{
					size = node.Size;
				}
			}

			List<MokaDockNode> children = [.. parent.Children];
			children[index] = sibling;
			children.Insert(before ? index : index + 1, group with { Size = size });
			root = Replace(Root, parent.Id, _ => parent with { Children = children })!;
		}
		else
		{
			MokaDockNode inner = node with { Size = null };
			MokaDockNode added = group;
			MokaDockSplitNode wrap = new()
			{
				Id = NewId("s"),
				Size = node.Size,
				Direction = direction,
				Children = before ? [added, inner] : [inner, added]
			};
			root = Replace(Root, node.Id, _ => wrap)!;
		}

		return this with { Root = root, ActiveGroup = group.Id };
	}

	private static MokaDockSplitNode WrapEdge(MokaDockNode root, MokaDockGroupNode group, MokaDockSplitDirection direction, bool before)
	{
		MokaDockNode added = group with { Size = group.Size ?? EdgeSize };
		if (root is MokaDockSplitNode split && split.Direction == direction)
		{
			return split with { Children = before ? [added, .. split.Children] : [.. split.Children, added] };
		}

		return new MokaDockSplitNode
		{
			Id = NewId("s"),
			Direction = direction,
			Children = before ? [added, root with { Size = null }] : [root with { Size = null }, added]
		};
	}

	private static int IndexOf(IReadOnlyList<string> list, string item)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == item)
			{
				return i;
			}
		}

		return -1;
	}
}
