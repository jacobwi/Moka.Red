using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A treemap: a hierarchy of parts as nested rectangles sized by value, such as disk use by folder. Tiles are
///     tinted rather than solid, colored by their top-level part, and each tile shows its name only where it fits, with
///     the full name in the tooltip and the table.
/// </summary>
/// <remarks>
///     The layout is squarified in C# into percentages, so it needs no script and costs one element per tile. Past
///     <see cref="MaxTiles" /> the smallest parts of a parent fold into one "Other" tile.
/// </remarks>
public partial class MokaTreemap : MokaChartBase
{
	// The box the layout is computed in; the drawing keeps this proportion unless a height is set.
	private const double BoxWidth = 600;
	private const double BoxHeight = 400;
	private const double GroupInset = 2;

	private static readonly int[] NameWidths = [3, 4, 5, 6, 8, 10, 12, 15, 19, 24, 30];

	private MokaTreemapNode[]? _snapshot;
	private List<Tile> _tiles = [];
	private List<ChartSeriesInfo> _seriesInfo = [];
	private double _total;

	/// <summary>The top level of the hierarchy.</summary>
	[Parameter]
	public IReadOnlyList<MokaTreemapNode>? Nodes { get; set; }

	/// <summary>How many levels are drawn as tiles; deeper parts add up into their parent's tile. Defaults to 2.</summary>
	[Parameter]
	public int Depth { get; set; } = 2;

	/// <summary>
	///     The most tiles drawn. Past it the smallest parts of a parent fold into one "Other" tile, smallest first.
	///     Defaults to 150.
	/// </summary>
	[Parameter]
	public int MaxTiles { get; set; } = 150;

	/// <summary>
	///     Raised with the part a tile stands for when it is clicked, so the app can drill in by passing that part's
	///     children as <see cref="Nodes" />. With a listener every tile is a button.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTreemapNode> OnNodeClick { get; set; }

	/// <summary>Writes a share for the tooltip, given the percent and what it is a share of. Defaults to "18% of Photos".</summary>
	[Parameter]
	public Func<string, string, string>? ShareText { get; set; }

	/// <summary>What the whole is called in the tooltip's last share, "6% of all". Defaults to "all".</summary>
	[Parameter]
	public string AllText { get; set; } = "all";

	/// <inheritdoc />
	protected override string RootClass => "moka-treemap";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-treemap--{SizeToKebab(Size)}")
		.AddClass(ModernRootClasses)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--moka-treemap-height", CssValues.IsLength(Height) ? CssValues.WithPixels(Height.Trim()) : null)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => _tiles.Count == 0
		? "Treemap"
		: $"Treemap: {string.Join(", ", _seriesInfo.Where(s => !s.Hidden).Select(s => s.Name))}";

	/// <inheritdoc />
	internal override bool HasData => _tiles.Count > 0;

	/// <inheritdoc />
	internal override bool HitsMarks => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _tiles.Count;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _seriesInfo;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => new([Words.Name, Words.Value, Words.Share], _tiles.Count,
		(row, column) => column switch
		{
			0 => _tiles[row].Path,
			1 => _tiles[row].ValueText,
			_ => ChartWholes.Percent(_tiles[row].ShareOfAll)
		},
		(row, column) => column switch
		{
			0 => _tiles[row].Path,
			1 => _tiles[row].Value.ToString(CultureInfo.InvariantCulture),
			_ => _tiles[row].ShareOfAll.ToString(CultureInfo.InvariantCulture)
		});

	/// <summary>A drawn tile, placed as percentages of the box, with its texts.</summary>
	private sealed record Tile(
		int Drawn,
		string Name,
		string Path,
		double Value,
		int Depth,
		int Top,
		string Class,
		string? Style,
		string KeyClass,
		string? KeyStyle,
		string LabelClass,
		string ValueText,
		string Title,
		string Note,
		double ShareOfAll,
		MokaTreemapNode? Node,
		EventCallback Click);

	/// <summary>A part while the tiles are worked out: its value, its parts, and the ones folded into "Other".</summary>
	private sealed class Branch
	{
		public required string Name { get; init; }
		public double Value { get; set; }
		public MokaTreemapNode? Node { get; init; }
		public Branch? Parent { get; init; }
		public int Top { get; init; }
		public int Level { get; init; }
		public bool IsOther { get; init; }
		public List<Branch> Kids { get; } = [];
		public Branch? Other { get; set; }
		public Branch? Waiting { get; set; }
		public int Folded { get; set; }

		public string Path => Parent is null || Parent.Node is null ? Name : $"{Parent.Path} / {Name}";
	}

	/// <inheritdoc />
	internal override string RootRole => base.RootRole == "figure" || OnNodeClick.HasDelegate ? "figure" : "img";

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Nodes)
		? SameTree(after as IReadOnlyList<MokaTreemapNode>, _snapshot)
		: Equals(before, after);

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _tiles.Count)
		{
			return null;
		}

		Tile tile = _tiles[drawn];
		ChartSeriesInfo top = _seriesInfo[tile.Top];
		MokaChartPoint point = new(top.Key, top.Name, tile.Top, drawn, tile.Value, tile.ValueText)
		{
			Category = tile.Path,
			Total = _total,
			Share = tile.ShareOfAll
		};
		return ChartWholes.Describe(drawn, tile.Path, tile.KeyClass, tile.KeyStyle, tile.ValueText, tile.Note, point);
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_snapshot = DataVersion is null ? DeepCopy(Nodes) : null;

		List<ChartSeriesInfo> info = [];
		Branch root = new() { Name = string.Empty, Level = 0 };
		IReadOnlyList<MokaTreemapNode> nodes = Nodes ?? [];
		for (int i = 0; i < nodes.Count; i++)
		{
			MokaTreemapNode node = nodes[i];
			string key = node.Key ?? node.Name;
			int slot = SlotFor(i, null);
			bool muted = IsMuted(key);
			bool hidden = IsHidden(key);
			info.Add(new ChartSeriesInfo(key, node.Name, slot, SeriesClass(slot, node.Color, muted),
				SeriesStyle(slot, node.ColorValue), ChartMark.Box, hidden, muted, null));
			double value = ValueOf(node);
			if (!hidden && value > 0)
			{
				root.Kids.Add(Expand(node, root, i, 1, value));
			}
		}

		_seriesInfo = info;
		root.Value = root.Kids.Sum(kid => kid.Value);
		_total = root.Value;
		Fold(root);

		List<Tile> tiles = [];
		if (_total > 0)
		{
			Place(root, new TreemapRect(0, 0, BoxWidth, BoxHeight), tiles);
		}

		_tiles = tiles;
	}

	private Branch Expand(MokaTreemapNode node, Branch parent, int top, int level, double value)
	{
		Branch branch = new() { Name = node.Name, Value = value, Node = node, Parent = parent, Top = top, Level = level };
		if (level < Depth && node.Children is { Count: > 0 } children)
		{
			foreach (MokaTreemapNode child in children)
			{
				double childValue = ValueOf(child);
				if (childValue > 0)
				{
					branch.Kids.Add(Expand(child, branch, top, level + 1, childValue));
				}
			}
		}

		return branch;
	}

	private static double ValueOf(MokaTreemapNode node) => node.Children is { Count: > 0 } children
		? children.Sum(ValueOf)
		: double.IsFinite(node.Value) ? Math.Max(0, node.Value) : 0;

	// Smallest tiles first, whichever parent they sit in: a parent's first two folds make its "Other" tile, every later
	// one takes a tile away, until the count is under the limit.
	private void Fold(Branch root)
	{
		List<Branch> leaves = [];
		Collect(root, leaves);
		int count = leaves.Count;
		int limit = Math.Max(1, MaxTiles);
		if (count <= limit)
		{
			return;
		}

		foreach (Branch leaf in leaves.OrderBy(leaf => leaf.Value))
		{
			if (count <= limit)
			{
				break;
			}

			Branch parent = leaf.Parent!;
			if (parent.Other is { } other)
			{
				other.Value += leaf.Value;
				other.Folded++;
				parent.Kids.Remove(leaf);
				count--;
			}
			else if (parent.Waiting is { } waiting)
			{
				Branch created = new()
				{
					Name = Words.Other,
					Value = waiting.Value + leaf.Value,
					Parent = parent,
					Top = parent.Node is null ? -1 : parent.Top,
					Level = leaf.Level,
					IsOther = true,
					Folded = 2
				};
				parent.Kids.Remove(waiting);
				parent.Kids.Remove(leaf);
				parent.Kids.Add(created);
				parent.Other = created;
				parent.Waiting = null;
				count--;
			}
			else
			{
				parent.Waiting = leaf;
			}
		}
	}

	private static void Collect(Branch branch, List<Branch> leaves)
	{
		foreach (Branch kid in branch.Kids)
		{
			if (kid.Kids.Count == 0)
			{
				leaves.Add(kid);
			}
			else
			{
				Collect(kid, leaves);
			}
		}
	}

	private void Place(Branch branch, TreemapRect box, List<Tile> tiles)
	{
		List<Branch> kids = [.. branch.Kids.OrderBy(kid => kid.IsOther).ThenByDescending(kid => kid.Value)];
		TreemapRect[] rects = TreemapLayout.Squarify([.. kids.Select(kid => kid.Value)], box);
		for (int i = 0; i < kids.Count; i++)
		{
			Branch kid = kids[i];
			if (rects[i].Width <= 0 || rects[i].Height <= 0)
			{
				continue;
			}

			if (kid.Kids.Count > 0)
			{
				Place(kid, rects[i].Inset(GroupInset), tiles);
			}
			else
			{
				tiles.Add(TileFor(kid, rects[i], tiles.Count));
			}
		}
	}

	private Tile TileFor(Branch leaf, TreemapRect rect, int drawn)
	{
		ChartSeriesInfo? top = leaf.Top >= 0 ? _seriesInfo[leaf.Top] : null;
		string valueText = Write(leaf.Value);
		double shareOfAll = _total > 0 ? leaf.Value / _total : 0;
		Branch? parent = leaf.Parent?.Node is null ? null : leaf.Parent;
		string name = leaf.IsOther ? $"{leaf.Name} ({leaf.Folded.ToString(CultureInfo.CurrentCulture)})" : leaf.Name;
		string path = parent is null ? name : $"{parent.Path} / {name}";

		Func<string, string, string> share = ShareText ?? ((percent, whole) => $"{percent} of {whole}");
		List<string> parts = [];
		if (parent is not null && parent.Value > 0)
		{
			parts.Add(share(ChartWholes.Percent(leaf.Value / parent.Value), parent.Name));
		}

		parts.Add(share(ChartWholes.Percent(shareOfAll), AllText));
		if (leaf.Node?.Detail is { Length: > 0 } detail)
		{
			parts.Add(detail);
		}

		string keyClass = leaf.IsOther || top is null ? "moka-chart-muted" : top.ColorClass;
		string? keyStyle = leaf.IsOther ? null : top?.ColorStyle;
		string tileClass = new CssBuilder("moka-treemap__tile")
			.AddClass($"moka-treemap__tile--d{Math.Clamp(leaf.Level, 1, 3).ToString(CultureInfo.InvariantCulture)}")
			.AddClass(keyClass)
			.AddClass("moka-treemap__tile--other", leaf.IsOther)
			.Build();
		string? style = new StyleBuilder()
			.AddStyle("left", ChartWholes.Css(rect.X / BoxWidth))
			.AddStyle("top", ChartWholes.Css(rect.Y / BoxHeight))
			.AddStyle("width", ChartWholes.Css(rect.Width / BoxWidth))
			.AddStyle("height", ChartWholes.Css(rect.Height / BoxHeight))
			.AddStyle(keyStyle)
			.Build();

		MokaTreemapNode? node = leaf.IsOther ? null : leaf.Node;
		EventCallback click = node is not null && OnNodeClick.HasDelegate
			? EventCallback.Factory.Create(this, () => OnNodeClick.InvokeAsync(node))
			: default;
		string note = string.Join(", ", parts);
		return new Tile(drawn, name, path, leaf.Value, leaf.Level, Math.Max(0, leaf.Top), tileClass, style, keyClass, keyStyle,
			$"moka-treemap__label moka-treemap__label--w{NameWidth(name).ToString(CultureInfo.InvariantCulture)}", valueText,
			$"{path}: {valueText}, {note}", note, shareOfAll, node, click);
	}

	// The narrowest tile content, in the tile's own em, that holds the name: 0.62em a character on average, which the
	// semibold label rarely passes, snapped up to the widths the stylesheet knows.
	private static int NameWidth(string name)
	{
		double need = name.Length * 0.62 + 0.3;
		foreach (int width in NameWidths)
		{
			if (need <= width)
			{
				return width;
			}
		}

		return NameWidths[^1];
	}

	private string Write(double value) => Formatter?.Invoke(value) ?? value.ToString("#,0.##", CultureInfo.CurrentCulture);

	// A copy of the whole tree, children included, so a part changed in place deep down still counts as a change.
	private static MokaTreemapNode[]? DeepCopy(IReadOnlyList<MokaTreemapNode>? nodes) =>
		nodes is null ? null : [.. nodes.Select(node => node with { Children = DeepCopy(node.Children) })];

	private static bool SameTree(IReadOnlyList<MokaTreemapNode>? nodes, IReadOnlyList<MokaTreemapNode>? snapshot)
	{
		if (nodes is null || snapshot is null)
		{
			return nodes is null && snapshot is null;
		}

		if (nodes.Count != snapshot.Count)
		{
			return false;
		}

		for (int i = 0; i < nodes.Count; i++)
		{
			MokaTreemapNode node = nodes[i];
			MokaTreemapNode copy = snapshot[i];
			if (node.Name != copy.Name || !node.Value.Equals(copy.Value) || node.Color != copy.Color
			    || node.ColorValue != copy.ColorValue || node.Key != copy.Key || node.Detail != copy.Detail
			    || !SameTree(node.Children, copy.Children))
			{
				return false;
			}
		}

		return true;
	}
}
