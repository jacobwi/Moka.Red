using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Chart;

/// <summary>
///     A donut chart: shares of a whole as arcs of one ring, with a legend and an optional center
///     label through <see cref="ChildContent" />. Pure SVG, so it themes and scales with the page.
/// </summary>
public partial class MokaDonutChart : MokaVisualComponentBase
{
	private const double ViewSize = 200;

	/// <summary>The shares to draw. Null or empty renders the empty ring.</summary>
	[Parameter]
	public IReadOnlyList<MokaDonutSegment>? Segments { get; set; }

	/// <summary>Ring thickness, in viewBox units out of 200. Defaults to 20.</summary>
	[Parameter]
	public double Thickness { get; set; } = 20;

	/// <summary>Whether the shares are listed beside the chart with their percentages. Defaults to true.</summary>
	[Parameter]
	public bool ShowLegend { get; set; } = true;

	/// <summary>Content centered in the ring, such as the total or a label.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>What the chart shows, for assistive technology.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Writes a segment's value as text, such as <c>v =&gt; $"{v / 1073741824:0.#} GiB"</c>. With it the tooltips
	///     carry the written value and the legend lists it beside the percentage. Null leaves the legend to percentages
	///     and writes tooltip values as plain numbers.
	/// </summary>
	[Parameter]
	public Func<double, string>? Formatter { get; set; }

	/// <summary>
	///     The whole the segments are shares of, such as a quota. Each segment then takes its share of this rather than
	///     of the segments' sum, and what's left of the ring shows as the track. When the segments add up to more, the
	///     ring is full and the arcs that run past its end are cut off, while the percentages stay shares of this.
	///     Null, zero or less uses the segments' sum.
	/// </summary>
	[Parameter]
	public double? Total { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-donutchart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-donutchart--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	/// <summary>A segment's arc, or null when it starts past the end of a full ring, and its legend and tooltip text.</summary>
	private sealed record SegmentGeometry(MokaDonutSegment Segment, string ColorClass, Arc? Arc, string LegendText, string Title);

	private sealed record Arc(string Dash, string Offset);

	private IReadOnlyList<MokaDonutSegment> Drawn => [.. (Segments ?? []).Where(s => s.Value > 0)];

	private double Sum
	{
		get
		{
			double sum = 0;
			foreach (MokaDonutSegment segment in Drawn)
			{
				sum += segment.Value;
			}

			return sum;
		}
	}

	private double Whole => Total is { } total && double.IsFinite(total) && total > 0 ? total : Sum;

	private double Radius => (ViewSize - Math.Max(1, Thickness)) / 2;

	private double Circumference => 2 * Math.PI * Radius;

	private IReadOnlyList<SegmentGeometry> Geometry
	{
		get
		{
			List<SegmentGeometry> geometry = [];
			double whole = Whole;
			if (whole <= 0 || !double.IsFinite(whole))
			{
				return geometry;
			}

			double circumference = Circumference;
			double consumed = 0;
			for (int i = 0; i < Drawn.Count; i++)
			{
				MokaDonutSegment segment = Drawn[i];
				double length = Math.Min(segment.Value / whole * circumference, circumference - consumed);
				string percent = ((int)Math.Round(segment.Value / whole * 100, MidpointRounding.AwayFromZero))
					.ToString(CultureInfo.CurrentCulture);
				string? value = Formatter?.Invoke(segment.Value);

				geometry.Add(new SegmentGeometry(
					segment,
					MokaChartPalette.ClassFor(segment.Color, i),
					length > 0 ? new Arc($"{Num(length)} {Num(circumference - length)}", Num(-consumed)) : null,
					value is null ? $"{segment.Name} · {percent}%" : $"{segment.Name} · {value} · {percent}%",
					$"{segment.Name}: {value ?? segment.Value.ToString("0.##", CultureInfo.CurrentCulture)} ({percent}%)"));

				consumed += Math.Max(0, length);
			}

			return geometry;
		}
	}

	private string RadiusText => Num(Radius);
	private string ThicknessText => Num(Math.Max(1, Thickness));

	// Geometry leaves the component as strings, so an SVG attribute never carries a culture's
	// decimal comma.
	private static string Num(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
}
