using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>The shape a legend entry's key takes.</summary>
public enum MokaChartLegendMark
{
	/// <summary>A dot.</summary>
	Dot,

	/// <summary>A short line, for a line series.</summary>
	Line,

	/// <summary>A small box, for bars and areas.</summary>
	Box
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
}
