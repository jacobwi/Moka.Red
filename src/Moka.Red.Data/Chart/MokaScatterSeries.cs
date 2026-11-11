using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One group of points in a <see cref="MokaScatterChart" />, drawn in one color.</summary>
/// <param name="Name">What the group is, in the legend and the tooltip.</param>
/// <param name="Points">Its points.</param>
public sealed record MokaScatterSeries(string Name, IReadOnlyList<MokaScatterPoint> Points)
{
	/// <summary>The color to draw it in. Null takes the palette, in turn.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>A CSS color or token to draw it in, such as <c>var(--moka-color-info)</c>. Wins over the palette.</summary>
	public string? ColorValue { get; init; }

	/// <summary>Its identity for hiding and highlighting. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }

	/// <summary>The palette slot it takes, counting from 1, so it keeps its color across charts. Null takes its position.</summary>
	public int? Slot { get; init; }
}
