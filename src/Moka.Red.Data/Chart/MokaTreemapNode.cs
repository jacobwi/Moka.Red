using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>One part of a <see cref="MokaTreemap" />, such as a folder, with the parts inside it.</summary>
/// <param name="Name">What it is called.</param>
/// <param name="Value">How big it is. Ignored when it has <see cref="Children" />, whose values add up to it.</param>
public sealed record MokaTreemapNode(string Name, double Value)
{
	/// <summary>The parts inside it. When set, its value is their sum.</summary>
	public IReadOnlyList<MokaTreemapNode>? Children { get; init; }

	/// <summary>The color its tiles are tinted with. Only read on the top level; the rest take their top node's.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>A CSS color or token to tint its tiles with, such as <c>var(--moka-color-info)</c>. Top level only.</summary>
	public string? ColorValue { get; init; }

	/// <summary>Its identity for hiding and highlighting. Null uses <see cref="Name" />.</summary>
	public string? Key { get; init; }

	/// <summary>A line about it for the tooltip, such as "3,412 files".</summary>
	public string? Detail { get; init; }
}
