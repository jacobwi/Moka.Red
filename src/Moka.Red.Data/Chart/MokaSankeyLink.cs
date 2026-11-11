using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>A flow in a <see cref="MokaSankey" />: how much went from one node to another.</summary>
/// <param name="From">The key of the node it leaves.</param>
/// <param name="To">The key of the node it reaches.</param>
/// <param name="Value">How much flowed. Zero, less, or not a number draws nothing.</param>
public sealed record MokaSankeyLink(string From, string To, double Value)
{
	/// <summary>
	///     The color it is drawn in, such as <see cref="MokaColor.Error" /> for a flow that needs a look. Null takes the
	///     color of the node it leaves. A highlight that mutes that node mutes it too.
	/// </summary>
	public MokaColor? Color { get; init; }

	/// <summary>
	///     A CSS color or token to draw it in, such as <c>var(--moka-color-warning)</c>. Wins over <see cref="Color" />;
	///     anything that isn't a color is ignored.
	/// </summary>
	/// <remarks>
	///     Flows between the same two nodes are added together, as are flows folded into "Other"; the one drawn keeps a
	///     color only when every flow in it gave the same one.
	/// </remarks>
	public string? ColorValue { get; init; }
}
