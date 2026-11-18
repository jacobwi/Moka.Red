using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One shape of a <see cref="MokaRadarChart" />: an item's values, one per axis, in the axes' order.</summary>
/// <param name="Name">The series' name, in the legend and the tooltip.</param>
/// <param name="Values">One value per axis. <see cref="double.NaN" /> is a value nobody has, drawn as a break in the outline.</param>
public sealed record MokaRadarSeries(string Name, IReadOnlyList<double> Values)
{
	/// <summary>Its identity for hiding and highlighting. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }

	/// <summary>The palette slot it takes, counting from 1, so it keeps its color across charts. Null takes its position.</summary>
	public int? Slot { get; init; }

	/// <summary>A theme color to draw it in. Null takes the palette.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>A CSS color or token to draw it in, such as <c>var(--moka-color-info)</c>. Wins over the palette.</summary>
	public string? ColorValue { get; init; }
}
