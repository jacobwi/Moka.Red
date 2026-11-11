namespace Moka.Red.Data.Chart;

/// <summary>A flow in a <see cref="MokaSankey" />: how much went from one node to another.</summary>
/// <param name="From">The key of the node it leaves.</param>
/// <param name="To">The key of the node it reaches.</param>
/// <param name="Value">How much flowed. Zero, less, or not a number draws nothing.</param>
public sealed record MokaSankeyLink(string From, string To, double Value);
