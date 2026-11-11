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

	/// <summary>Its identity for hiding and highlighting. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }

	/// <summary>The palette slot it takes, counting from 1, so it keeps its color across charts. Null takes its position.</summary>
	public int? Slot { get; init; }

	/// <summary>A CSS color or token to draw it in, such as <c>var(--moka-color-info)</c>. Wins over the palette.</summary>
	public string? ColorValue { get; init; }
}
