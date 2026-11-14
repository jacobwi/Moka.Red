using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.DockLayout;

public partial class MokaDockLayout
{
	internal const string NarrowGroupId = "moka-dock-narrow";
	internal const string ContentPanelId = "moka-dock-content";
	private const string Gutter = "var(--_dock-gutter)";
	private const string LegacySize = "250px";

	private static readonly MokaDockPosition[] Sides =
		[MokaDockPosition.Left, MokaDockPosition.Right, MokaDockPosition.Top, MokaDockPosition.Bottom];

	private Dictionary<string, DockPlacement> _placements = new(StringComparer.Ordinal);
	private DockPlacement _contentPlacement = DockPlacement.Nowhere;
	private bool _narrow;
	private string? _flyoutGroup;
	private string? _sheetPanel;
	private string? _narrowShown;
	private readonly HashSet<string> _stackClosed = new(StringComparer.Ordinal);

	internal MokaDockState ViewState => _state;

	internal bool IsNarrow => _narrow && NarrowBelow is not null;

	internal string? FlyoutGroup => _flyoutGroup;

	internal string? SheetPanel => _sheetPanel;

	internal string? NarrowShown => _narrowShown;

	internal MokaDockContent? Content => FindContent(_parts);

	internal bool IsStackOpen(string group) => !_stackClosed.Contains(group);

	internal DockPlacement PlacementOf(MokaDockPanel panel) =>
		_placements.TryGetValue(IdOf(panel), out DockPlacement placement) ? placement : DockPlacement.Nowhere;

	internal DockPlacement ContentPlacement => _contentPlacement;

	internal int AnchorOf(string group)
	{
		if (!_anchors.TryGetValue(group, out int anchor))
		{
			anchor = _anchors.Count + 1;
			_anchors[group] = anchor;
		}

		return anchor;
	}

	internal static string AnchorName(int anchor) => string.Create(CultureInfo.InvariantCulture, $"--moka-dock-{anchor}");

	internal static string FlyoutKey => "moka-dock-flyout";

	internal string TitleOf(string panel)
	{
		if (panel == ContentPanelId)
		{
			return Content?.Title ?? Words.Content;
		}

		if (PanelFor(panel) is { } declared)
		{
			return declared.Title ?? panel;
		}

		return _opened.TryGetValue(panel, out MokaDockPanelOptions? options) ? options.Title ?? panel : panel;
	}

	internal string GroupName(MokaDockGroupNode group)
	{
		if (SettingsOf(group.Id).Title is { } title)
		{
			return title;
		}

		if (group.Content && group.Shown is null)
		{
			return Content?.Title ?? Words.Content;
		}

		return group.Shown is { } shown ? TitleOf(shown) : Words.Group;
	}

	internal MokaDockTabStyle TabStyleOf(string group) => SettingsOf(group).TabStyle ?? TabStyle;

	internal bool ShowsTabs(MokaDockGroupNode group) =>
		TabStyleOf(group.Id) != MokaDockTabStyle.Switcher
		&& (group.Panels.Count > 1 || TabStyleOf(group.Id) == MokaDockTabStyle.Editor || ResolvedLook == MokaDockLook.Ide
		    || (group.Content && group.Panels.Count > 0));

	internal DockGroupSettings SettingsOf(string group)
	{
		if (FindDeclaredGroup(_parts, group) is { } declared)
		{
			return new DockGroupSettings(declared.MinSize, declared.MaxSize, declared.Resizable, declared.Collapsible,
				declared.CollapsedSize, declared.TabStyle, declared.Title, declared.Class, declared.Style, declared.Dock, true);
		}

		foreach (MokaDockPosition side in Sides)
		{
			if (group == SideGroupId(side) && LegacyPanels(side).FirstOrDefault() is { } first)
			{
				return PanelSettings(first, side);
			}
		}

		if (group.StartsWith("g-", StringComparison.Ordinal) && PanelFor(group[2..]) is { } panel)
		{
			return PanelSettings(panel, panel.Container is null ? panel.Dock : null);
		}

		return DockGroupSettings.Default;
	}

	private static DockGroupSettings PanelSettings(MokaDockPanel panel, MokaDockPosition? side) =>
		new(panel.MinSize, panel.MaxSize, panel.Resizable, panel.Collapsible, panel.CollapsedSize, null, null, null, null,
			side, panel.Floatable);

	internal bool IsClosable(string panel)
	{
		if (PanelFor(panel) is { } declared)
		{
			return declared.Closable ?? (Controls & MokaDockControls.Close) != 0;
		}

		return _opened.TryGetValue(panel, out MokaDockPanelOptions? options) && options.Closable;
	}

	internal bool IsFloatable(string group) =>
		(Controls & MokaDockControls.Floating) != 0
		&& SettingsOf(group).Floatable
		&& _state.FindGroup(group) is { Content: false } node
		&& node.Panels.All(p => PanelFor(p)?.Floatable ?? true);

	internal bool CanPopOut(string panel) =>
		_popOutSupported && (PanelFor(panel)?.Component ?? (_opened.TryGetValue(panel, out MokaDockPanelOptions? o) ? o.Component : null)) is not null;

	// The CSS track list of a split: each child's track, with a gutter track between neighbours. A share is
	// minmax(min, Nfr), so a group can shrink past its content; a length is bounded by min() / max() / clamp() as the
	// group's limits ask. A folded group takes its strip's size.
	internal string TrackList(MokaDockSplitNode split)
	{
		List<string> tracks = new(split.Children.Count * 2);
		foreach (MokaDockNode child in split.Children)
		{
			if (tracks.Count > 0)
			{
				tracks.Add(Gutter);
			}

			tracks.Add(Track(child));
		}

		return string.Join(' ', tracks);
	}

	internal string Track(MokaDockNode child)
	{
		DockGroupSettings settings = child is MokaDockGroupNode ? SettingsOf(child.Id) : DockGroupSettings.Default;
		if (child is MokaDockGroupNode { Collapsed: true } && settings.Collapsible)
		{
			return string.IsNullOrWhiteSpace(settings.CollapsedSize) ? "0px" : settings.CollapsedSize;
		}

		if (MokaDockState.TryShare(child.Size, out double weight))
		{
			string min = settings.MinSize is { } m && CssValues.IsLengthExpression(m) ? m : "0px";
			return string.Create(CultureInfo.InvariantCulture, $"minmax({min}, {Math.Round(weight, 4)}fr)");
		}

		return BoundTrack(child.Size!, settings.MinSize, settings.MaxSize);
	}

	internal bool IsResizable(MokaDockNode node) =>
		node is not MokaDockGroupNode group || (SettingsOf(group.Id).Resizable && !(group.Collapsed && SettingsOf(group.Id).Collapsible));

	internal static bool IsShare(MokaDockNode node) => MokaDockState.TryShare(node.Size, out _);

	internal bool IsCollapsed(MokaDockGroupNode group) => group.Collapsed && SettingsOf(group.Id).Collapsible;

	// The panel each group shows and where every host is drawn: over its group's slot, in front for a window in
	// front, nowhere for a closed panel. Hosts whose place changed are told, so only they redraw.
	private void Recompute()
	{
		Dictionary<string, DockPlacement> placements = new(StringComparer.Ordinal);
		DockPlacement content = DockPlacement.Nowhere;

		if (IsNarrow)
		{
			content = RecomputeNarrow(placements);
		}
		else
		{
			foreach (MokaDockGroupNode group in _state.DockedGroups)
			{
				int z = group.Id == _state.Maximized ? 2 : 0;
				Place(placements, group, AnchorOf(group.Id), !IsCollapsed(group), z);
				if (group.Content)
				{
					content = new DockPlacement(group.Id, AnchorOf(group.Id), group.Shown is null, z, false, false, null);
				}
			}

			for (int i = 0; i < _state.Floating.Count; i++)
			{
				MokaDockGroupNode group = _state.Floating[i].Group;
				Place(placements, group, AnchorOf(group.Id), true, 3 + i);
			}

			foreach (MokaDockAutoHidden pinned in _state.AutoHidden)
			{
				bool open = pinned.Group.Id == _flyoutGroup;
				Place(placements, pinned.Group, AnchorOf(FlyoutKey), open, 2);
			}
		}

		Dictionary<string, DockPlacement> old = _placements;
		_placements = placements;
		foreach ((string id, MokaDockPanel panel) in _panels)
		{
			DockPlacement now = placements.GetValueOrDefault(id, DockPlacement.Nowhere);
			if (!old.TryGetValue(id, out DockPlacement before) || before != now)
			{
				panel.PlacementChanged();
			}
		}

		if (_contentPlacement != content)
		{
			_contentPlacement = content;
			Content?.PlacementChanged();
		}
	}

	private DockPlacement RecomputeNarrow(Dictionary<string, DockPlacement> placements)
	{
		DockPlacement content = DockPlacement.Nowhere;
		switch (NarrowMode)
		{
			case MokaDockNarrowMode.Tabs:
				MokaDockGroupNode narrow = NarrowGroup();
				int anchor = AnchorOf(NarrowGroupId);
				foreach (string panel in narrow.Panels)
				{
					bool shown = panel == narrow.Shown;
					if (panel == ContentPanelId)
					{
						content = new DockPlacement(NarrowGroupId, anchor, shown, 0, true, false, TabIdOf(ContentPanelId));
					}
					else
					{
						placements[panel] = new DockPlacement(NarrowGroupId, anchor, shown, 0, true, false, TabIdOf(panel));
					}
				}

				break;
			case MokaDockNarrowMode.Stack:
				foreach (MokaDockGroupNode group in StackGroups())
				{
					bool open = IsStackOpen(group.Id);
					Place(placements, group, AnchorOf(group.Id), open, 0);
					if (group.Content)
					{
						content = new DockPlacement(group.Id, AnchorOf(group.Id), open && group.Shown is null, 0, false, false, null);
					}
				}

				break;
			default:
				MokaDockGroupNode? main = MainGroup();
				foreach (MokaDockGroupNode group in _state.AllGroups)
				{
					if (main is not null && group.Id == main.Id)
					{
						Place(placements, group, AnchorOf(group.Id), true, 0);
						if (group.Content)
						{
							content = new DockPlacement(group.Id, AnchorOf(group.Id), group.Shown is null, 0, false, false, null);
						}

						continue;
					}

					foreach (string panel in group.Panels)
					{
						placements[panel] = new DockPlacement(group.Id, 0, false, 0, false, true, null);
					}
				}

				break;
		}

		return content;
	}

	private void Place(Dictionary<string, DockPlacement> placements, MokaDockGroupNode group, int anchor, bool open, int z)
	{
		bool tabbed = ShowsTabs(group);
		string? shown = group.Shown;
		foreach (string panel in group.Panels)
		{
			placements[panel] = new DockPlacement(group.Id, anchor, open && panel == shown, z, tabbed, false,
				tabbed ? TabIdOf(panel) : null);
		}
	}

	internal string HostIdOf(string panel) =>
		panel == ContentPanelId
			? Content?.Id ?? string.Create(CultureInfo.InvariantCulture, $"moka-dock-{AnchorOf("#content")}-content")
			: PanelFor(panel)?.Id ?? (_opened.ContainsKey(panel) ? panel : string.Create(CultureInfo.InvariantCulture, $"moka-dock-host-{AnchorOf("#" + panel)}"));

	internal string TabIdOf(string panel) => HostIdOf(panel) + "-tab";

	internal MokaDockGroupNode NarrowGroup()
	{
		List<string> panels = [];
		if (Content is not null)
		{
			panels.Add(ContentPanelId);
		}

		panels.AddRange(_state.AllGroups.SelectMany(g => g.Panels));
		string? active = _narrowShown is not null && panels.Contains(_narrowShown)
			? _narrowShown
			: _state.ActiveGroup is { } g && _state.FindGroup(g)?.Shown is { } shown ? shown : null;
		return new MokaDockGroupNode { Id = NarrowGroupId, Panels = panels, Active = active };
	}

	internal IEnumerable<MokaDockGroupNode> StackGroups() => _state.AllGroups;

	internal RenderFragment? BodyOf(string panel)
	{
		if (PanelFor(panel) is { } declared)
		{
			return declared.Body;
		}

		return null;
	}

	// Changes whenever something a group's header shows changed outside its own node: a panel's title or badge, the
	// layout's look or words, which group is active or maximized, an open flyout.
	internal int StampOf(MokaDockGroupNode group, MokaDockGroupMode mode)
	{
		HashCode hash = new();
		hash.Add(ViewVersion);
		hash.Add(mode);
		foreach (string panel in group.Panels)
		{
			hash.Add(PanelFor(panel)?.MetaVersion ?? 0);
			hash.Add(IsClosable(panel));
		}

		hash.Add(_state.ActiveGroup == group.Id);
		hash.Add(_state.Maximized == group.Id);
		hash.Add(_state.Floating.Any(w => w.Group.Id == group.Id && w.Maximized));
		hash.Add(_flyoutGroup == group.Id);
		hash.Add(IsStackOpen(group.Id));
		hash.Add(_popOutSupported);
		if (mode == MokaDockGroupMode.Docked && _state.Root is not null)
		{
			hash.Add(_state.DockedGroups.Count());
		}

		return hash.ToHashCode();
	}

	// The side bar a group pins to: the edge its declaration names, else the edge of the layout it sits nearest.
	internal MokaDockPosition SideOf(string group)
	{
		if (SettingsOf(group).Side is { } side)
		{
			return side;
		}

		MokaDockPosition found = MokaDockPosition.Right;
		MokaDockNode? node = _state.Root;
		while (node is MokaDockSplitNode split)
		{
			int index = -1;
			for (int i = 0; i < split.Children.Count; i++)
			{
				if (MokaDockState.Groups(split.Children[i]).Any(g => g.Id == group))
				{
					index = i;
					break;
				}
			}

			if (index < 0)
			{
				break;
			}

			bool row = split.Direction == MokaDockSplitDirection.Row;
			if (index == 0)
			{
				found = row ? MokaDockPosition.Left : MokaDockPosition.Top;
			}
			else if (index == split.Children.Count - 1)
			{
				found = row ? MokaDockPosition.Right : MokaDockPosition.Bottom;
			}

			node = split.Children[index];
		}

		return found;
	}

	internal MokaDockGroupNode? MainGroup() =>
		_state.DockedGroups.FirstOrDefault(g => g.Content) ?? _state.DockedGroups.FirstOrDefault();

	internal IEnumerable<string> SheetPanels()
	{
		MokaDockGroupNode? main = MainGroup();
		return _state.AllGroups.Where(g => main is null || g.Id != main.Id).SelectMany(g => g.Panels);
	}

	private MokaDockState BuildDefaults()
	{
		_splitCount = 0;
		_groupCount = 0;
		List<MokaDockFloatingWindow> floating = [];
		List<MokaDockAutoHidden> pinned = [];
		List<MokaDockParkedPanel> hidden = [];
		Dictionary<MokaDockPosition, List<MokaDockNode>> sides = Sides.ToDictionary(s => s, _ => new List<MokaDockNode>());
		List<MokaDockNode> centers = [];
		MokaDockNode? content = null;

		foreach (MokaDockPart part in _parts)
		{
			switch (part)
			{
				case MokaDockContent declared:
					content = ContentNode(declared);
					break;
				case MokaDockGroup group when group.AutoHide is { } side:
					if (Convert(group, floating, pinned, hidden) is MokaDockGroupNode node)
					{
						pinned.Add(new MokaDockAutoHidden { Group = node, Side = side, Return = new MokaDockTarget(null, ZoneOf(side)) });
					}

					break;
				case MokaDockGroup group when group.Dock is { } side:
					if (Convert(group, floating, pinned, hidden) is { } docked)
					{
						sides[side].Add(docked.Size is null ? docked with { Size = LegacySize } : docked);
					}

					break;
				case MokaDockContainer container:
					if (Convert(container, floating, pinned, hidden) is { } center)
					{
						centers.Add(center);
					}

					break;
				case MokaDockPanel panel when _opened.ContainsKey(IdOf(panel)):
					break;
				case MokaDockPanel panel:
					if (Park(panel, floating, pinned, hidden))
					{
						break;
					}

					List<MokaDockNode> list = sides[panel.Dock];
					string id = SideGroupId(panel.Dock);
					if (list.FindIndex(n => n.Id == id) is var at and >= 0)
					{
						MokaDockGroupNode side = (MokaDockGroupNode)list[at];
						list[at] = side with
						{
							Panels = [.. side.Panels, IdOf(panel)],
							Active = panel.Active ? IdOf(panel) : side.Active
						};
					}
					else
					{
						list.Add(new MokaDockGroupNode
						{
							Id = id,
							Panels = [IdOf(panel)],
							Active = IdOf(panel),
							Size = panel.Size ?? LegacySize,
							Collapsed = panel.Collapsed && panel.Collapsible
						});
					}

					break;
			}
		}

		if (content is not null)
		{
			centers.Add(content);
		}

		MokaDockNode? middle = centers.Count switch
		{
			0 => null,
			1 => centers[0],
			_ => new MokaDockSplitNode { Id = "split-center", Direction = MokaDockSplitDirection.Row, Children = centers }
		};

		List<MokaDockNode> column = [.. sides[MokaDockPosition.Top]];
		if (middle is not null)
		{
			column.Add(middle with { Size = null });
		}

		column.AddRange(sides[MokaDockPosition.Bottom]);
		MokaDockNode? columnNode = column.Count switch
		{
			0 => null,
			1 => column[0] with { Size = null },
			_ => new MokaDockSplitNode { Id = "split-column", Direction = MokaDockSplitDirection.Column, Children = column }
		};

		List<MokaDockNode> row = [.. sides[MokaDockPosition.Left]];
		if (columnNode is not null)
		{
			row.Add(columnNode);
		}

		row.AddRange(sides[MokaDockPosition.Right]);
		MokaDockNode? root = row.Count switch
		{
			0 => null,
			1 => row[0] with { Size = null },
			_ => new MokaDockSplitNode { Id = "split-row", Direction = MokaDockSplitDirection.Row, Children = row }
		};

		return new MokaDockState
		{
			AppVersion = StateVersion,
			Root = root,
			Floating = floating,
			AutoHidden = pinned,
			Hidden = hidden
		}.Normalize();
	}

	private int _splitCount;
	private int _groupCount;

	private MokaDockNode? Convert(MokaDockContainer container, List<MokaDockFloatingWindow> floating,
		List<MokaDockAutoHidden> pinned, List<MokaDockParkedPanel> hidden)
	{
		if (container is MokaDockGroup group)
		{
			string id = group.Id ?? string.Create(CultureInfo.InvariantCulture, $"group-{++_groupCount}");
			List<string> panels = [];
			string? active = null;
			foreach (MokaDockPanel panel in group.Parts.OfType<MokaDockPanel>())
			{
				if (Park(panel, floating, pinned, hidden))
				{
					continue;
				}

				panels.Add(IdOf(panel));
				if (panel.Active)
				{
					active = IdOf(panel);
				}
			}

			MokaDockGroupNode node = new()
			{
				Id = id,
				Size = group.Size,
				Panels = panels,
				Active = active,
				Collapsed = group.Collapsed && group.Collapsible
			};
			if (group.AutoHide is { } side && container.Container is not null && panels.Count > 0)
			{
				pinned.Add(new MokaDockAutoHidden { Group = node, Side = side, Return = new MokaDockTarget(null, ZoneOf(side)) });
				return null;
			}

			return panels.Count > 0 ? node : null;
		}

		string splitId = container.Id ?? string.Create(CultureInfo.InvariantCulture, $"split-{++_splitCount}");
		List<MokaDockNode> children = [];
		foreach (MokaDockPart part in container.Parts)
		{
			MokaDockNode? child = part switch
			{
				MokaDockContainer inner => Convert(inner, floating, pinned, hidden),
				MokaDockContent declared => ContentNode(declared),
				MokaDockPanel panel when !Park(panel, floating, pinned, hidden) => new MokaDockGroupNode
				{
					Id = "g-" + IdOf(panel),
					Size = panel.Size,
					Panels = [IdOf(panel)],
					Active = IdOf(panel),
					Collapsed = panel.Collapsed && panel.Collapsible
				},
				_ => null
			};
			if (child is not null)
			{
				children.Add(child);
			}
		}

		return new MokaDockSplitNode
		{
			Id = splitId,
			Size = container.Size,
			Direction = container is MokaDockColumn ? MokaDockSplitDirection.Column : MokaDockSplitDirection.Row,
			Children = children
		};
	}

	private static MokaDockGroupNode ContentNode(MokaDockContent content) =>
		new() { Id = ContentGroupId, Content = true, Size = content.Size };

	// A panel that starts somewhere other than its group: floating, pinned or closed. Returns whether it did.
	private bool Park(MokaDockPanel panel, List<MokaDockFloatingWindow> floating, List<MokaDockAutoHidden> pinned,
		List<MokaDockParkedPanel> hidden)
	{
		string id = IdOf(panel);
		MokaDockTarget back = new(null, ZoneOf(panel.Dock));
		if (!panel.Visible)
		{
			hidden.Add(new MokaDockParkedPanel { Panel = id, Return = back, Size = panel.Size });
			return true;
		}

		if (panel.Floating && panel.Floatable)
		{
			floating.Add(new MokaDockFloatingWindow
			{
				Group = new MokaDockGroupNode { Id = "g-" + id, Panels = [id], Active = id, Size = panel.Size ?? LegacySize },
				X = panel.FloatingX,
				Y = panel.FloatingY,
				Width = Pixels(panel.FloatingWidth, 300),
				Height = Pixels(panel.FloatingHeight, 400),
				Return = back
			});
			return true;
		}

		if (panel.AutoHide is { } side)
		{
			pinned.Add(new MokaDockAutoHidden
			{
				Group = new MokaDockGroupNode { Id = "g-" + id, Panels = [id], Active = id, Size = panel.Size ?? LegacySize },
				Side = side,
				Return = new MokaDockTarget(null, ZoneOf(side))
			});
			return true;
		}

		return false;
	}

	internal static double Pixels(string? value, double fallback) =>
		CssValues.TryParsePixels(value, out double pixels) ? pixels : fallback;

	internal static MokaDockZone ZoneOf(MokaDockPosition side) => side switch
	{
		MokaDockPosition.Left => MokaDockZone.Left,
		MokaDockPosition.Right => MokaDockZone.Right,
		MokaDockPosition.Top => MokaDockZone.Top,
		_ => MokaDockZone.Bottom
	};

	internal static string SideGroupId(MokaDockPosition side) => MokaEnumHelpers.ToCssClass(side);

	private IEnumerable<MokaDockPanel> LegacyPanels(MokaDockPosition side) =>
		_parts.OfType<MokaDockPanel>()
			.Where(p => p.Dock == side && !p.Floating && p.Visible && p.AutoHide is null && !_opened.ContainsKey(IdOf(p)));

	private static MokaDockGroup? FindDeclaredGroup(IEnumerable<MokaDockPart> parts, string id)
	{
		foreach (MokaDockPart part in parts)
		{
			if (part is MokaDockGroup group && group.Id == id)
			{
				return group;
			}

			if (part is MokaDockContainer container && FindDeclaredGroup(container.Parts, id) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private static MokaDockContent? FindContent(IEnumerable<MokaDockPart> parts)
	{
		foreach (MokaDockPart part in parts)
		{
			if (part is MokaDockContent content)
			{
				return content;
			}

			if (part is MokaDockContainer container && FindContent(container.Parts) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	// min(), max() and clamp() only take lengths: wrapping a keyword or an fr would make the whole template invalid,
	// so those keep their size unbounded. A px bound on a px size is applied here.
	private static string BoundTrack(string size, string? min, string? max)
	{
		if (!CssValues.IsLengthExpression(size))
		{
			return CssValues.IsSafe(size) ? size : "auto";
		}

		if (TryPx(size, out double px))
		{
			if (TryPx(min, out double minPx) && px < minPx)
			{
				return min!;
			}

			if (TryPx(max, out double maxPx) && px > maxPx)
			{
				return max!;
			}
		}

		string? low = Bound(min, size);
		string? high = Bound(max, size);
		return (low, high) switch
		{
			(null, null) => size,
			(null, _) => $"min({size}, {high})",
			(_, null) => $"max({low}, {size})",
			_ => $"clamp({low}, {size}, {high})"
		};
	}

	private static string? Bound(string? bound, string size) =>
		bound is null || !CssValues.IsLengthExpression(bound) || (TryPx(bound, out _) && TryPx(size, out _))
			? null
			: bound;

	// A length written in px, which CSS and this agree on; rem and the rest are left to CSS.
	private static bool TryPx(string? value, out double px)
	{
		px = 0;
		return value is not null && value.Trim().EndsWith("px", StringComparison.OrdinalIgnoreCase)
		                         && CssValues.TryParsePixels(value, out px);
	}
}

/// <summary>Where a panel's host is drawn.</summary>
internal readonly record struct DockPlacement(string? Group, int Anchor, bool Visible, int Z, bool Tabbed, bool InSheet, string? TabId)
{
	public static DockPlacement Nowhere => new(null, 0, false, 0, false, false, null);
}

/// <summary>A group's settings from its declaration: the <see cref="MokaDockGroup" />, or the panel it was made for.</summary>
internal sealed record DockGroupSettings(
	string? MinSize,
	string? MaxSize,
	bool Resizable,
	bool Collapsible,
	string? CollapsedSize,
	MokaDockTabStyle? TabStyle,
	string? Title,
	string? Class,
	string? Style,
	MokaDockPosition? Side,
	bool Floatable)
{
	public static DockGroupSettings Default { get; } = new(null, null, true, false, "0px", null, null, null, null, null, true);
}
