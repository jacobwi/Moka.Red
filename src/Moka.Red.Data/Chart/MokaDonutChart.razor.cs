using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A donut chart: shares of a whole as arcs of one ring, with a legend and an optional center
///     label through <see cref="ChildContent" />. Pure SVG, so it themes and scales with the page.
/// </summary>
/// <remarks>
///     <see cref="Shape" /> draws the same segments as one tall bar or as a waffle of a hundred squares instead. With
///     the ring, the Standard look and none of the chart options set, it draws exactly what it always has; any of them
///     moves it onto the shared chart frame, where the look sets the ring's thickness, gaps and ends.
/// </remarks>
public partial class MokaDonutChart : MokaChartBase
{
	private const double ViewSize = 200;
	private const double DefaultThickness = 20;
	private const int WaffleCells = 100;

	private readonly string _uid = Guid.NewGuid().ToString("N")[..12];
	private MokaDonutSegment[]? _snapshot;
	private List<SegmentGeometry> _geometry = [];
	private List<Part> _parts = [];
	private IReadOnlyList<ChartSeriesInfo> _seriesInfo = [];
	private RingArc? _rest;
	private int _emptyCells;
	private bool _classic = true;

	/// <summary>The shares to draw. Null or empty renders the empty ring.</summary>
	[Parameter]
	public IReadOnlyList<MokaDonutSegment>? Segments { get; set; }

	/// <summary>
	///     Ring thickness, in viewBox units out of 200. Defaults to 20. A look other than Standard sets its own unless
	///     this is set to something else.
	/// </summary>
	[Parameter]
	public double Thickness { get; set; } = DefaultThickness;

	/// <summary>
	///     Content centered in the ring, such as the total or a label. With the bar and the waffle it sits above the
	///     drawing as a summary line.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     The whole the segments are shares of, such as a quota. Each segment then takes its share of this rather than
	///     of the segments' sum, and what's left of the ring shows as the track. When the segments add up to more, the
	///     ring is full and the arcs that run past its end are cut off, while the percentages stay shares of this.
	///     Null, zero or less uses the segments' sum.
	/// </summary>
	[Parameter]
	public double? Total { get; set; }

	/// <summary>How the shares are drawn: a ring, one tall bar, or a waffle of a hundred squares. Defaults to the ring.</summary>
	[Parameter]
	public MokaDonutShape Shape { get; set; }

	/// <summary>
	///     Whether each share is printed inside its segment where it fits. Null prints them on the bar and nowhere else;
	///     the waffle lists its shares beside it either way.
	/// </summary>
	[Parameter]
	public bool? ShowShares { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-donutchart";

	/// <inheritdoc />
	protected override string CssClass => _classic
		? new CssBuilder(RootClass)
			.AddClass($"moka-donutchart--{SizeToKebab(Size)}")
			.AddClass(Class)
			.Build()
		: new CssBuilder(RootClass)
			.AddClass($"moka-donutchart--{SizeToKebab(Size)}")
			.AddClass(Shape switch
			{
				MokaDonutShape.Bar => "moka-donutchart--bar",
				MokaDonutShape.Waffle => "moka-donutchart--waffle",
				_ => "moka-donutchart--ring"
			})
			.AddClass(ModernRootClasses)
			.AddClass(Class)
			.Build();

	/// <inheritdoc />
	protected override string? CssStyle => _classic
		? base.CssStyle
		: SpacingStyle()
			.AddStyle("--moka-donut-size", HeightValue)
			.AddStyle(Style)
			.Build();

	/// <inheritdoc />
	internal override bool IsClassic => _classic;

	/// <inheritdoc />
	internal override string DefaultLabel => _classic ? "Donut chart" : Summary;

	/// <inheritdoc />
	internal override bool HasData => _parts.Count > 0 || _geometry.Count > 0;

	/// <inheritdoc />
	internal override bool HitsMarks => true;

	/// <inheritdoc />
	internal override MokaChartTooltip AutoTooltip => MokaChartTooltip.Column;

	/// <inheritdoc />
	internal override int DrawnCount(int panel) => _parts.Count;

	/// <inheritdoc />
	internal override IReadOnlyList<ChartSeriesInfo> SeriesInfo => _seriesInfo;

	/// <inheritdoc />
	/// <remarks>The ring and the waffle list their shares beside them; the bar takes the frame's legend.</remarks>
	internal override bool DrawsOwnLegend => Shape != MokaDonutShape.Bar;

	/// <inheritdoc />
	internal override ChartTableModel? TableModel => _classic
		? null
		: new ChartTableModel([Words.Name, Words.Value, Words.Share], _parts.Count,
			(row, column) => column switch
			{
				0 => _parts[row].Segment.Name,
				1 => _parts[row].ValueText,
				_ => _parts[row].ShareText
			},
			(row, column) => column switch
			{
				0 => _parts[row].Segment.Name,
				1 => _parts[row].Segment.Value.ToString(CultureInfo.InvariantCulture),
				_ => _parts[row].Share.ToString(CultureInfo.InvariantCulture)
			});

	/// <summary>A segment's arc, or null when it starts past the end of a full ring, and its legend and tooltip text.</summary>
	private sealed record SegmentGeometry(MokaDonutSegment Segment, string ColorClass, Arc? Arc, string LegendText, string Title);

	private sealed record Arc(string Dash, string Offset);

	/// <summary>A dash along the ring and where it starts, in viewBox units.</summary>
	private sealed record RingArc(string Dash, string Offset);

	/// <summary>A share label on the ring, at the middle of its arc.</summary>
	private sealed record RingLabel(string X, string Y, string Text);

	/// <summary>One visible segment of the modern shapes, with everything each shape draws from it.</summary>
	private sealed record Part(
		int Drawn,
		MokaDonutSegment Segment,
		string Key,
		int Position,
		int Slot,
		string ColorClass,
		string? ColorStyle,
		bool Muted,
		double Share,
		string ShareText,
		string ValueText,
		string Title,
		RingArc? Ring,
		RingLabel? RingLabel,
		int Cells);

	private IReadOnlyList<SegmentGeometry> Geometry => _geometry;

	private double Whole => WholeOf(Drawn);

	private IReadOnlyList<MokaDonutSegment> Drawn => [.. (Segments ?? []).Where(s => s.Value > 0)];

	private double Radius => (ViewSize - Math.Max(1, Thickness)) / 2;

	private double Circumference => 2 * Math.PI * Radius;

	private string RadiusText => Num(Radius);
	private string ThicknessText => Num(Math.Max(1, Thickness));

	// The modern ring: thickness, gaps and round ends from the look unless the chart set its own thickness.
	private double RingThickness => Math.Max(1, Thickness != DefaultThickness ? Thickness : Spec.RingThickness ?? Thickness);

	private double RingRadius => (ViewSize - RingThickness) / 2;

	private string RingRadiusText => Num(RingRadius);

	private string RingThicknessText => Num(RingThickness);

	private string ShareFontText => Num(Math.Clamp(RingThickness * 0.45, 7, 14));

	private bool StandardTrack => Spec.RingGap <= 0 && !Spec.RoundRingEnds;

	private bool DrawsList => ShowLegend && Shape != MokaDonutShape.Bar && _parts.Count > 0;

	private bool NativeTooltips => ResolvedTooltip == MokaChartTooltip.Native;

	private string? SvgHidden => Interactive ? "true" : null;

	private string Summary => _parts.Count == 0
		? "Donut chart"
		: string.Join(", ", _parts.Select(part => $"{part.Segment.Name} {part.ShareText}"));

	private string PatternId(int drawn) => $"moka-donut-{_uid}-h{drawn.ToString(CultureInfo.InvariantCulture)}";

	// Rebuilding a donut costs microseconds, and whether it draws the classic ring depends on the chrome too, so every
	// parameter but the center content rebuilds it.
	/// <inheritdoc />
	internal override bool AffectsGeometry(string parameterName) => parameterName != nameof(ChildContent);

	// The segments are compared by value with what was last drawn, so a list changed in place still draws again.
	/// <inheritdoc />
	internal override bool SameValue(string name, object? before, object? after) => name == nameof(Segments)
		? ChartSnapshot.SameItems(after as IReadOnlyList<MokaDonutSegment>, _snapshot)
		: Equals(before, after);

	/// <inheritdoc />
	internal override ChartActiveInfo? DescribeActive(int panel, int series, int drawn, int? pinnedDrawn)
	{
		if (drawn < 0 || drawn >= _parts.Count)
		{
			return null;
		}

		Part part = _parts[drawn];
		MokaChartPoint point = new(part.Key, part.Segment.Name, part.Position, drawn, part.Segment.Value, part.ValueText)
		{
			Category = part.Segment.Name,
			Total = WholeOf(_parts.Select(p => p.Segment)),
			Share = part.Share
		};
		return ChartWholes.Describe(drawn, part.Segment.Name, part.ColorClass, part.ColorStyle, part.ValueText, part.ShareText,
			point);
	}

	/// <inheritdoc />
	internal override void Rebuild()
	{
		_snapshot = DataVersion is null ? ChartSnapshot.Copy(Segments) : null;
		_classic = ClassicNow();
		if (_classic)
		{
			_geometry = ClassicGeometry();
			_parts = [];
			_seriesInfo = [];
			_rest = null;
			return;
		}

		_geometry = [];
		BuildParts();
	}

	private bool ClassicNow() =>
		Shape == MokaDonutShape.Ring && ShowShares is null && ResolvedLook == MokaChartLook.Standard && !Textured
		&& Print == MokaChartPrint.AsShown && (ChartTheme is not { Look: not MokaLook.Matrix } theme
		                                       || theme.ChartPalette == MokaChartPalette.Standard)
		&& Title is null && Subtitle is null && TitleContent is null && Actions is null && Height is null && Colors is null
		&& Legend is null && LegendKey is null && LegendActions is null && HiddenSeries is null && Highlight is null
		&& HighlightSeries is null && Tooltip is null && TooltipTemplate is null && Pinnable is null && Keyboard is null
		&& SyncGroup is null && !OnPointClick.HasDelegate && !ActiveChanged.HasDelegate && Controls is null
		&& ToolbarControl is null && View == MokaChartView.Chart && State == MokaChartState.Auto && EmptyText is null
		&& EmptyContent is null && ErrorContent is null && ErrorText is null && !OnRetry.HasDelegate && Text is null
		&& ExportName is null && (Segments ?? []).All(s => s.Key is null && s.Slot is null && s.ColorValue is null);

	private List<SegmentGeometry> ClassicGeometry()
	{
		List<SegmentGeometry> geometry = [];
		double whole = Whole;
		if (whole <= 0 || !double.IsFinite(whole))
		{
			return geometry;
		}

		double circumference = Circumference;
		double consumed = 0;
		IReadOnlyList<MokaDonutSegment> drawn = Drawn;
		for (int i = 0; i < drawn.Count; i++)
		{
			MokaDonutSegment segment = drawn[i];
			double length = Math.Min(segment.Value / whole * circumference, circumference - consumed);
			string percent = ((int)Math.Round(segment.Value / whole * 100, MidpointRounding.AwayFromZero))
				.ToString(CultureInfo.CurrentCulture);
			string? value = Formatter?.Invoke(segment.Value);

			geometry.Add(new SegmentGeometry(
				segment,
				MokaChartRotation.ClassFor(segment.Color, i),
				length > 0 ? new Arc($"{Num(length)} {Num(circumference - length)}", Num(-consumed)) : null,
				value is null ? $"{segment.Name} · {percent}%" : $"{segment.Name} · {value} · {percent}%",
				$"{segment.Name}: {value ?? segment.Value.ToString("0.##", CultureInfo.CurrentCulture)} ({percent}%)"));

			consumed += Math.Max(0, length);
		}

		return geometry;
	}

	private void BuildParts()
	{
		List<(MokaDonutSegment Segment, int Position, string Key)> drawable = [];
		List<ChartSeriesInfo> info = [];
		IReadOnlyList<MokaDonutSegment> positive = Drawn;
		for (int i = 0; i < positive.Count; i++)
		{
			MokaDonutSegment segment = positive[i];
			string key = segment.Key ?? segment.Name;
			int slot = SlotFor(i, segment.Slot);
			bool hidden = IsHidden(key);
			bool muted = IsMuted(key);
			info.Add(new ChartSeriesInfo(key, segment.Name, slot, SeriesClass(slot, segment.Color, muted),
				SeriesStyle(slot, segment.ColorValue), ChartMark.Box, hidden, muted, null));
			if (!hidden)
			{
				drawable.Add((segment, i, key));
			}
		}

		_seriesInfo = info;

		double whole = WholeOf(drawable.Select(d => d.Segment));
		if (whole <= 0 || !double.IsFinite(whole))
		{
			_parts = [];
			_rest = null;
			_emptyCells = WaffleCells;
			return;
		}

		double sum = drawable.Sum(d => d.Segment.Value);
		int[] cells = ChartWholes.LargestRemainder([.. drawable.Select(d => d.Segment.Value)], whole, WaffleCells);
		_emptyCells = WaffleCells - cells.Sum();

		double thickness = RingThickness;
		double radius = (ViewSize - thickness) / 2;
		double circumference = 2 * Math.PI * radius;
		bool round = Spec.RoundRingEnds;
		bool several = drawable.Count + (sum < whole ? 1 : 0) > 1;
		double gap = several ? Spec.RingGap : 0;
		double fontSize = Math.Clamp(thickness * 0.45, 7, 14);
		bool ringShares = ShowShares == true;

		List<Part> parts = [];
		double consumed = 0;
		for (int drawn = 0; drawn < drawable.Count; drawn++)
		{
			(MokaDonutSegment segment, int position, string key) = drawable[drawn];
			ChartSeriesInfo series = info[position];
			double share = segment.Value / whole;
			double length = Math.Min(share * circumference, circumference - consumed);
			string shareText = ChartWholes.Percent(share);
			string valueText = Formatter?.Invoke(segment.Value) ?? segment.Value.ToString("0.##", CultureInfo.CurrentCulture);
			RingLabel? label = null;
			if (ringShares && length > fontSize * (shareText.Length * 0.62 + 0.8))
			{
				double angle = (consumed + length / 2) / circumference * 2 * Math.PI - Math.PI / 2;
				label = new RingLabel(Num(ViewSize / 2 + radius * Math.Cos(angle)), Num(ViewSize / 2 + radius * Math.Sin(angle)),
					shareText);
			}

			parts.Add(new Part(drawn, segment, key, position, series.Slot, series.ColorClass, series.ColorStyle, series.Muted, share,
				shareText,
				valueText, $"{segment.Name}: {valueText} ({shareText})", ArcAlong(consumed, length, gap, thickness, round, circumference),
				label, cells[drawn]));
			consumed += Math.Max(0, length);
		}

		_parts = parts;
		_rest = sum < whole && consumed < circumference
			? ArcAlong(consumed, circumference - consumed, gap, thickness, round, circumference)
			: null;
	}

	// A dash for a stretch of the ring that starts `start` along it and runs `length`. The gap is split between its ends,
	// and round caps reach half the thickness past each end, so a round dash is shorter by the thickness.
	private static RingArc? ArcAlong(double start, double length, double gap, double thickness, bool round, double circumference)
	{
		if (length <= 0)
		{
			return null;
		}

		double inset = gap / 2 + (round ? thickness / 2 : 0);
		double dash = Math.Max(0.01, length - 2 * inset);
		return new RingArc($"{Num(dash)} {Num(circumference - dash)}", Num(-(start + inset)));
	}

	private double WholeOf(IEnumerable<MokaDonutSegment> segments)
	{
		double sum = 0;
		foreach (MokaDonutSegment segment in segments)
		{
			sum += segment.Value;
		}

		return Total is { } total && double.IsFinite(total) && total > 0 ? total : sum;
	}

	private static string ItemClass(Part part) => new CssBuilder("moka-donutchart-item")
		.AddClass(part.ColorClass)
		.AddClass("moka-donutchart-item--muted", part.Muted)
		.Build();

	private string MarkClass(string mark, Part part) => new CssBuilder(mark)
		.AddClass(part.ColorClass)
		.AddClass("moka-chart-hatch", Textured)
		.Build();

	// The key takes its color from the list item, which carries the class and any inline color, so an inline color
	// isn't overridden by a class on the key itself.
	private string KeyClass => new CssBuilder("moka-donutchart-key").AddClass("moka-chart-hatch", Textured).Build();

	private static string ArcGroupClass(Part part) => new CssBuilder("moka-donutchart-arc").AddClass(part.ColorClass).Build();

	private string? ArcStyle(Part part) => Textured
		? new StyleBuilder().AddStyle("stroke", $"url(#{PatternId(part.Drawn)})").Build()
		: null;

	private string CapText => Spec.RoundRingEnds ? "round" : "butt";

	// Hatching runs at 45 degrees for odd slots and 135 for even ones, wider from the third slot on, so neighbours
	// differ in angle and spacing as well as color.
	private static string HatchTransform(Part part) =>
		part.Slot % 2 == 1 ? "rotate(45)" : "rotate(135)";

	private static string HatchSize(Part part) => part.Slot >= 3 ? "9" : "6";

	private bool BarShares => ShowShares ?? true;

	private static string? BarStyle(Part part) => new StyleBuilder()
		.AddStyle("flex-grow", part.Share.ToString("0.#####", CultureInfo.InvariantCulture))
		.AddStyle(part.ColorStyle)
		.Build();

	private string? RestStyle => new StyleBuilder()
		.AddStyle("flex-grow", Math.Max(0, 1 - _parts.Sum(p => p.Share)).ToString("0.#####", CultureInfo.InvariantCulture))
		.Build();

	private static string Index(int drawn) => drawn.ToString(CultureInfo.InvariantCulture);

	// Geometry leaves the component as strings, so an SVG attribute never carries a culture's
	// decimal comma.
	private static string Num(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
