using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One share of a <see cref="MokaDonutChart" />.</summary>
public sealed record MokaDonutSegment
{
	/// <summary>The segment name, shown in the legend and the tooltip.</summary>
	public required string Name { get; init; }

	/// <summary>The segment's value. Negative values are drawn as zero.</summary>
	public double Value { get; init; }

	/// <summary>The color to draw the segment in. Null = the palette, in rotation.</summary>
	public MokaColor? Color { get; init; }
}
