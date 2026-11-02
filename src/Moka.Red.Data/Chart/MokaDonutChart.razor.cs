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

	/// <inheritdoc />
	protected override string RootClass => "moka-donutchart";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-donutchart--{SizeToKebab(Size)}")
		.AddClass(Class)
		.Build();

	private sealed record SegmentGeometry(MokaDonutSegment Segment, string ColorClass, string Dash, string Offset,
		string LegendText, string Title);

	private IReadOnlyList<MokaDonutSegment> Drawn => [.. (Segments ?? []).Where(s => s.Value > 0)];

	private double Total
	{
		get
		{
			double total = 0;
			foreach (MokaDonutSegment segment in Drawn)
			{
				total += segment.Value;
			}

			return total;
		}
	}

	private double Radius => (ViewSize - Math.Max(1, Thickness)) / 2;

	private double Circumference => 2 * Math.PI * Radius;

	private IReadOnlyList<SegmentGeometry> Geometry
	{
		get
		{
			List<SegmentGeometry> geometry = [];
			double total = Total;
			if (total <= 0)
			{
				return geometry;
			}

			double consumed = 0;
			for (int i = 0; i < Drawn.Count; i++)
			{
				MokaDonutSegment segment = Drawn[i];
				double length = segment.Value / total * Circumference;
				int percent = (int)Math.Round(segment.Value / total * 100, MidpointRounding.AwayFromZero);

				geometry.Add(new SegmentGeometry(
					segment,
					MokaChartPalette.ClassFor(segment.Color, i),
					$"{Num(length)} {Num(Circumference - length)}",
					Num(-consumed),
					$"{segment.Name} · {percent.ToString(CultureInfo.CurrentCulture)}%",
					$"{segment.Name}: {segment.Value.ToString("0.##", CultureInfo.CurrentCulture)} " +
					$"({percent.ToString(CultureInfo.CurrentCulture)}%)"));

				consumed += length;
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
