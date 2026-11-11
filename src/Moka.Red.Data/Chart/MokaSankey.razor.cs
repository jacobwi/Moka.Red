using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A sankey diagram: flows from sources to destinations, each as wide as its volume, such as traffic from WAN to the
///     networks behind it. Reads well at card size with three or four nodes a side, down to a phone.
/// </summary>
/// <remarks>
///     Nodes sit in columns by the longest path to them, the largest at the top; a flow that would close a cycle is
///     dropped. Past <see cref="MaxNodesPerSide" /> nodes in a column the smallest fold into one "Other" node, since a
///     sankey with more tangles and its labels collide. The flows are filled paths in a stretched drawing, the nodes and
///     their names are HTML, and no script is needed. A point from a flow, in <c>OnPointClick</c> or
///     <c>ActiveChanged</c>, names the node it leaves in <see cref="MokaChartPoint.SeriesKey" /> and the node it reaches in
///     <see cref="MokaChartPoint.ToKey" />; a point from a node has no <c>ToKey</c>.
/// </remarks>
public partial class MokaSankey : MokaChartBase
{
	private const double Span = 1000;

	private MokaSankeyLink[]? _linkSnapshot;
	private MokaSankeyNode[]? _nodeSnapshot;
	private List<Ribbon> _ribbons = [];
	private List<NodeBox> _nodes = [];
	private List<ChartSeriesInfo> _seriesInfo = [];
	private int _rows;
	private double _startEm;
	private double _endEm;

	/// <summary>The flows to draw.</summary>
	[Parameter]
	public IReadOnlyList<MokaSankeyLink>? Links { get; set; }

	/// <summary>Names, colors and columns for the nodes the links name. A key without one shows as itself.</summary>
	[Parameter]
	public IReadOnlyList<MokaSankeyNode>? Nodes { get; set; }

	/// <summary>
	///     The most nodes a column shows. Past it the smallest fold into one "Other" node. Defaults to 6; four a side
	///     reads best.
	/// </summary>
	[Parameter]
	public int MaxNodesPerSide { get; set; } = 6;

	/// <summary>Writes a share for the tooltip, given the percent and what it is a share of. Defaults to "62% of WAN".</summary>
	[Parameter]
	public Func<string, string, string>? ShareText { get; set; }

	/// <summary>The word between a flow's two ends in its tooltip, "WAN to LAN". Defaults to "to".</summary>
	[Parameter]
	public string ToText { get; set; } = "to";

	/// <inheritdoc />
	protected override string RootClass => "moka-sankey";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-sankey--{SizeToKebab(Size)}")
		.AddClass(ModernRootClasses)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--moka-sankey-height", HeightValue)
		.AddStyle("--_sk-rows", Math.Max(1, _rows).ToString(CultureInfo.InvariantCulture))
		.AddStyle("--_sk-start", Em(_startEm))
		.AddStyle("--_sk-end", Em(_endEm))
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	internal override string DefaultLabel => _ribbons.Count == 0
		? "Sankey diagram"
		: $"Sankey diagram: {string.Join(", ", _ribbons.Select(r => $"{r.FromName} {ToText} {r.ToName} {r.ValueText}"))}";

	/// <inheritdoc />
	internal override bool HasData => _ribbons.Count > 0;

	/// <inheritdoc />
	internal override bool HitsMarks => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _ribbons.Count + _nodes.Count;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _seriesInfo;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => new([Words.From, Words.To, Words.Value], _ribbons.Count,
		(row, column) => column switch
		{
			0 => _ribbons[row].FromName,
			1 => _ribbons[row].ToName,
			_ => _ribbons[row].ValueText
		},
		(row, column) => column switch
		{
			0 => _ribbons[row].FromName,
			1 => _ribbons[row].ToName,
			_ => _ribbons[row].Value.ToString(CultureInfo.InvariantCulture)
		});

	/// <summary>A drawn flow: its path in the stretched drawing and its texts.</summary>
	private sealed record Ribbon(
		int Drawn,
		string From,
		string FromName,
		string To,
		string ToName,
		double Value,
		string Path,
		string Class,
		string? Style,
		string ValueText,
		string Title,
		string Note);

	/// <summary>A drawn node: its bar, placed as percentages, and its label.</summary>
	private sealed record NodeBox(
		int Drawn,
		string Key,
		string Name,
		string Class,
		string? Style,
		string LabelClass,
		string? LabelStyle,
		string ValueText,
		string Title,
		string ColorClass,
		string? ColorStyle);

	/// <summary>Where a node sits while the drawing is worked out.</summary>
	private sealed class Place
	{
		public required string Key { get; init; }
		public required string Name { get; init; }
		public int Column { get; set; }
		public double In { get; set; }
		public double Out { get; set; }
		public double Value => Math.Max(In, Out);
		public bool Other { get; init; }
		public double Top { get; set; }
		public double Height { get; set; }
		public double OutOffset { get; set; }
		public double InOffset { get; set; }
		public string ColorClass { get; set; } = "moka-chart-muted";
		public string? ColorStyle { get; set; }
		public bool Muted { get; set; }
	}

	private string? SvgHidden => Interactive ? "true" : null;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name switch
	{
		nameof(Links) => ChartSnapshot.SameItems(after as IReadOnlyList<MokaSankeyLink>, _linkSnapshot),
		nameof(Nodes) => ChartSnapshot.SameItems(after as IReadOnlyList<MokaSankeyNode>, _nodeSnapshot),
		_ => Equals(before, after)
	};

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn >= 0 && drawn < _ribbons.Count)
		{
			Ribbon ribbon = _ribbons[drawn];
			string heading = $"{ribbon.FromName} {ToText} {ribbon.ToName}";
			MokaChartPoint point = new(ribbon.From, ribbon.FromName, 0, drawn, ribbon.Value, ribbon.ValueText)
			{
				Category = heading,
				ToKey = ribbon.To,
				ToName = ribbon.ToName
			};
			return ChartWholes.Describe(drawn, heading, ribbon.Class, ribbon.Style, ribbon.ValueText, ribbon.Note, point);
		}

		int at = drawn - _ribbons.Count;
		if (at < 0 || at >= _nodes.Count)
		{
			return null;
		}

		NodeBox node = _nodes[at];
		MokaChartPoint nodePoint = new(node.Key, node.Name, 0, drawn, 0, node.ValueText) { Category = node.Name };
		return ChartWholes.Describe(drawn, node.Name, node.ColorClass, node.ColorStyle, node.ValueText, null, nodePoint);
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		bool versioned = DataVersion is not null;
		_linkSnapshot = versioned ? null : ChartSnapshot.Copy(Links);
		_nodeSnapshot = versioned ? null : ChartSnapshot.Copy(Nodes);

		Dictionary<string, MokaSankeyNode> named = new(StringComparer.Ordinal);
		foreach (MokaSankeyNode node in Nodes ?? [])
		{
			named.TryAdd(node.Key, node);
		}

		(List<SankeyFlow> all, List<string> keys, _) =
			SankeyLayout.Acyclic((Links ?? []).Select(link => new SankeyFlow(link.From, link.To, link.Value, TintOf(link))));
		Dictionary<string, int> column = SankeyLayout.Columns(keys, all, key => named.GetValueOrDefault(key)?.Column);
		all = [.. all.Where(flow => column[flow.To] > column[flow.From])];

		List<string> sources = [.. keys.Where(key => column[key] == 0 && all.Any(flow => flow.From == key))];
		_seriesInfo = [.. sources.Select((key, i) => SourceInfo(key, i, named.GetValueOrDefault(key)))];

		List<SankeyFlow> flows = [.. all.Where(flow => !IsHidden(flow.From) && !IsHidden(flow.To))];
		Dictionary<string, double> value = Totals(flows);
		Dictionary<string, int> visibleColumn = value.Keys.ToDictionary(key => key, key => column[key], StringComparer.Ordinal);
		(flows, Dictionary<string, int> folded) =
			SankeyLayout.Fold(flows, visibleColumn, key => value.GetValueOrDefault(key), MaxNodesPerSide);

		Dictionary<string, Place> places = new(StringComparer.Ordinal);
		foreach (SankeyFlow flow in flows)
		{
			PlaceOf(flow.From).Out += flow.Value;
			PlaceOf(flow.To).In += flow.Value;
		}

		Layout(places, flows, named);

		Place PlaceOf(string key)
		{
			if (!places.TryGetValue(key, out Place? place))
			{
				bool other = folded.TryGetValue(key, out int count);
				place = new Place
				{
					Key = key,
					Name = other ? $"{Words.Other} ({count.ToString(CultureInfo.CurrentCulture)})" : named.GetValueOrDefault(key)?.Name ?? key,
					Column = visibleColumn[key],
					Other = other
				};
				places[key] = place;
			}

			return place;
		}
	}

	private ChartSeriesInfo SourceInfo(string key, int index, MokaSankeyNode? node)
	{
		int slot = SlotFor(index, null);
		bool muted = IsMuted(key);
		return new ChartSeriesInfo(key, node?.Name ?? key, slot, SeriesClass(slot, node?.Color, muted),
			SeriesStyle(slot, node?.ColorValue), ChartMark.Box, IsHidden(key), muted, null);
	}

	private static SankeyTint? TintOf(MokaSankeyLink link)
	{
		string? value = CssValues.IsColor(link.ColorValue) ? link.ColorValue.Trim() : null;
		return link.Color is null && value is null ? null : new SankeyTint(link.Color, value);
	}

	// A flow with its own color keeps it unless a highlight mutes the node it leaves; without one it is that node's.
	private static (string Class, string? Style) FlowColor(Place source, SankeyTint? tint)
	{
		if (tint is not { } own || source.Muted)
		{
			return (source.ColorClass, source.ColorStyle);
		}

		string? style = own.Value is null ? null : new StyleBuilder().AddStyle("--moka-chart-color", own.Value).Build();
		return (SeriesClass(1, own.Color, false), style);
	}

	private static Dictionary<string, double> Totals(List<SankeyFlow> flows)
	{
		Dictionary<string, (double In, double Out)> sums = new(StringComparer.Ordinal);
		foreach (SankeyFlow flow in flows)
		{
			(double fromIn, double fromOut) = sums.GetValueOrDefault(flow.From);
			sums[flow.From] = (fromIn, fromOut + flow.Value);
			(double toIn, double toOut) = sums.GetValueOrDefault(flow.To);
			sums[flow.To] = (toIn + flow.Value, toOut);
		}

		return sums.ToDictionary(pair => pair.Key, pair => Math.Max(pair.Value.In, pair.Value.Out), StringComparer.Ordinal);
	}

	private void Layout(Dictionary<string, Place> places, List<SankeyFlow> flows, Dictionary<string, MokaSankeyNode> named)
	{
		_ribbons = [];
		_nodes = [];
		_rows = 0;
		if (flows.Count == 0)
		{
			_startEm = _endEm = 0;
			return;
		}

		// Columns no node sits in close up, so the drawing never has an empty band.
		List<int> used = [.. places.Values.Select(p => p.Column).Distinct().Order()];
		foreach (Place place in places.Values)
		{
			place.Column = used.IndexOf(place.Column);
		}

		int last = used.Count - 1;
		List<List<Place>> columns = [.. Enumerable.Range(0, used.Count).Select(c => places.Values
			.Where(p => p.Column == c)
			.OrderBy(p => p.Other)
			.ThenByDescending(p => p.Value)
			.ToList())];
		_rows = columns.Max(c => c.Count);

		double pad = 0.4 / Math.Max(1, _rows);
		double scale = columns.Min(c => (1 - pad * (c.Count - 1)) / Math.Max(double.Epsilon, c.Sum(p => p.Value)));
		foreach (List<Place> nodes in columns)
		{
			double filled = nodes.Sum(p => p.Value) * scale + pad * (nodes.Count - 1);
			double y = (1 - filled) / 2;
			foreach (Place place in nodes)
			{
				place.Top = y;
				place.Height = place.Value * scale;
				y += place.Height + pad;
			}
		}

		Paint(columns, flows, places, named);

		ILookup<string, SankeyFlow> outgoing = flows.ToLookup(flow => flow.From, StringComparer.Ordinal);
		ILookup<string, SankeyFlow> incoming = flows.ToLookup(flow => flow.To, StringComparer.Ordinal);
		Func<string, string, string> share = ShareText ?? ((percent, whole) => $"{percent} of {whole}");
		Dictionary<(string, string), (double From, double To)> ends = [];
		foreach (Place place in columns.SelectMany(c => c))
		{
			foreach (SankeyFlow flow in outgoing[place.Key].OrderBy(f => places[f.To].Top))
			{
				ends[(flow.From, flow.To)] = (place.Top + place.OutOffset, 0);
				place.OutOffset += flow.Value * scale;
			}
		}

		foreach (Place place in columns.SelectMany(c => c))
		{
			foreach (SankeyFlow flow in incoming[place.Key].OrderBy(f => places[f.From].Top))
			{
				(double from, _) = ends[(flow.From, flow.To)];
				ends[(flow.From, flow.To)] = (from, place.Top + place.InOffset);
				place.InOffset += flow.Value * scale;
			}
		}

		foreach (Place source in columns.SelectMany(c => c))
		{
			foreach (SankeyFlow flow in outgoing[source.Key].OrderBy(f => places[f.To].Top))
			{
				Place target = places[flow.To];
				(double y0, double y1) = ends[(flow.From, flow.To)];
				string valueText = Write(flow.Value);
				string note = share(ChartWholes.Percent(flow.Value / Math.Max(double.Epsilon, source.Value)), source.Name);
				(string colorClass, string? colorStyle) = FlowColor(source, flow.Tint);
				_ribbons.Add(new Ribbon(_ribbons.Count, source.Key, source.Name, target.Key, target.Name, flow.Value,
					RibbonPath(source.Column, target.Column, last, y0, y1, flow.Value * scale),
					new CssBuilder("moka-sankey__ribbon").AddClass(colorClass).Build(), colorStyle, valueText,
					$"{source.Name} {ToText} {target.Name}: {valueText}, {note}", note));
			}
		}

		foreach (Place place in columns.SelectMany(c => c))
		{
			string side = place.Column == 0 ? "start" : place.Column == last ? "end" : "mid";
			double x = last == 0 ? 0 : (double)place.Column / last;
			string valueText = Write(place.Value);
			_nodes.Add(new NodeBox(_ribbons.Count + _nodes.Count, place.Key, place.Name,
				new CssBuilder("moka-sankey__node").AddClass(place.ColorClass).Build(),
				new StyleBuilder()
					.AddStyle("left", ChartWholes.Css(x))
					.AddStyle("top", ChartWholes.Css(place.Top))
					.AddStyle("height", ChartWholes.Css(place.Height))
					.AddStyle(place.ColorStyle)
					.Build(),
				$"moka-sankey__label moka-sankey__label--{side}",
				new StyleBuilder()
					.AddStyle("--_sk-x", ChartWholes.Css(x))
					.AddStyle("top", ChartWholes.Css(place.Top + place.Height / 2))
					.Build(),
				valueText, $"{place.Name}: {valueText}", place.ColorClass, place.ColorStyle));
		}

		_startEm = LabelEm(columns[0]);
		_endEm = last == 0 ? 0 : LabelEm(columns[last]);
	}

	// Sources take the palette in the legend's order; a node further on takes the color of the source behind its
	// largest inflow, and the last column stays muted, so the eye follows each source's flows to where they end.
	private void Paint(List<List<Place>> columns, List<SankeyFlow> flows, Dictionary<string, Place> places,
		Dictionary<string, MokaSankeyNode> named)
	{
		Dictionary<string, ChartSeriesInfo> sources = _seriesInfo.ToDictionary(s => s.Key, StringComparer.Ordinal);
		ILookup<string, SankeyFlow> incoming = flows.ToLookup(flow => flow.To, StringComparer.Ordinal);
		int last = columns.Count - 1;
		for (int c = 0; c < columns.Count; c++)
		{
			foreach (Place place in columns[c])
			{
				if (place.Other)
				{
					continue;
				}

				named.TryGetValue(place.Key, out MokaSankeyNode? node);
				if (sources.TryGetValue(place.Key, out ChartSeriesInfo? source))
				{
					place.ColorClass = source.ColorClass;
					place.ColorStyle = source.ColorStyle;
					place.Muted = source.Muted;
				}
				else if (node is { Color: not null } or { ColorValue: not null })
				{
					bool muted = IsMuted(place.Key);
					place.ColorClass = SeriesClass(1, node.Color, muted);
					place.ColorStyle = SeriesStyle(1, node.ColorValue);
					place.Muted = muted;
				}
				else if (c < last && incoming[place.Key].MaxBy(flow => flow.Value) is { } main)
				{
					place.ColorClass = places[main.From].ColorClass;
					place.ColorStyle = places[main.From].ColorStyle;
					place.Muted = places[main.From].Muted;
				}
			}
		}
	}

	// A filled band between two columns, as wide as the flow at both ends, bending halfway across.
	private static string RibbonPath(int fromColumn, int toColumn, int last, double y0, double y1, double width)
	{
		double x0 = last == 0 ? 0 : fromColumn * Span / last;
		double x1 = last == 0 ? Span : toColumn * Span / last;
		double xm = (x0 + x1) / 2;
		double a = y0 * Span;
		double b = y1 * Span;
		double h = width * Span;
		return string.Create(CultureInfo.InvariantCulture,
			$"M{x0:0.##},{a:0.##}C{xm:0.##},{a:0.##} {xm:0.##},{b:0.##} {x1:0.##},{b:0.##}L{x1:0.##},{b + h:0.##}C{xm:0.##},{b + h:0.##} {xm:0.##},{a + h:0.##} {x0:0.##},{a + h:0.##}Z");
	}

	// The widest label of an outer column, in em, from 0.6em a character: what the box keeps free beside it.
	private double LabelEm(List<Place> nodes) =>
		nodes.Count == 0 ? 0 : nodes.Max(p => (p.Name.Length + Write(p.Value).Length + 1) * 0.6) + 0.5;

	private static string Em(double em) => em.ToString("0.##", CultureInfo.InvariantCulture) + "em";

	private string Write(double value) => Formatter?.Invoke(value) ?? value.ToString("#,0.##", CultureInfo.CurrentCulture);
}
