using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One named set of bars of a <see cref="MokaBarChart" />.</summary>
public sealed record MokaBarSeries
{
	/// <summary>The series name, shown in the legend and the bar tooltips.</summary>
	public required string Name { get; init; }

	/// <summary>The values to draw, one per category.</summary>
	public required IReadOnlyList<double> Values { get; init; }

	/// <summary>The color to draw the bars in. Null = the palette, in rotation.</summary>
	public MokaColor? Color { get; init; }
}
