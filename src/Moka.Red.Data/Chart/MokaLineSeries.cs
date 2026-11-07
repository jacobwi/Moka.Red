using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One named line of a <see cref="MokaLineChart" />.</summary>
public sealed record MokaLineSeries
{
	/// <summary>The series name, shown in the legend and the point tooltips.</summary>
	public required string Name { get; init; }

	/// <summary>The values to draw, one per category. A <c>double.NaN</c> is a gap.</summary>
	public required IReadOnlyList<double> Values { get; init; }

	/// <summary>The color to draw the line in. Null = the palette, in rotation.</summary>
	public MokaColor? Color { get; init; }
}
