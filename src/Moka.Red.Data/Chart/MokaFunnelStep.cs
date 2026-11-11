namespace Moka.Red.Data.Chart;

/// <summary>One step of a <see cref="MokaFunnel" />: what it is and how many reached it.</summary>
/// <param name="Name">The step, such as "Signed in".</param>
/// <param name="Value">How many reached it.</param>
public sealed record MokaFunnelStep(string Name, double Value)
{
	/// <summary>A line about the step for the tooltip.</summary>
	public string? Detail { get; init; }
}
