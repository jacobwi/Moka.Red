using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;

namespace Moka.Red.Data.Chart;

/// <summary>The shape a legend entry's key takes.</summary>
public enum MokaChartLegendMark
{
	/// <summary>A dot.</summary>
	Dot,

	/// <summary>A short line, for a line series.</summary>
	Line,

	/// <summary>A small box, for bars and areas.</summary>
	Box,

	/// <summary>A short dashed line, for a line drawn dashed such as a comparison. Shown whatever the legend's key.</summary>
	Dash
}

/// <summary>One entry of a chart legend.</summary>
/// <param name="Key">The series' key, what hiding and highlighting go by.</param>
/// <param name="Name">What the entry reads.</param>
public sealed record MokaChartLegendItem(string Key, string Name)
{
	/// <summary>The palette slot, counting from 1, so a series keeps its color across charts. Null uses its position.</summary>
	public int? Slot { get; init; }

	/// <summary>A theme color that wins over the slot.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>A CSS color or token that wins over <see cref="Color" />. Anything that isn't a color is ignored.</summary>
	public string? ColorValue { get; init; }

	/// <summary>The latest value, written beside the name.</summary>
	public string? Value { get; init; }

	/// <summary>The key's shape. Defaults to <see cref="MokaChartLegendMark.Dot" />.</summary>
	public MokaChartLegendMark Mark { get; init; } = MokaChartLegendMark.Dot;

	/// <summary>
	///     An icon drawn in the entry's color in place of its key, for an entry that names a state, such as down, so the
	///     state reads without its color. Null draws the key.
	/// </summary>
	public MokaIconDefinition? Icon { get; init; }
}
