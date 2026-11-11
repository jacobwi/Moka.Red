using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Chart;

/// <summary>What a <see cref="MokaSankey" /> knows about a node besides its flows: its name, color and column.</summary>
/// <param name="Key">The key its links use.</param>
/// <param name="Name">What it is called.</param>
public sealed record MokaSankeyNode(string Key, string Name)
{
	/// <summary>The color it and the flows leaving it are drawn in. Null: a source takes the palette, the rest follow.</summary>
	public MokaColor? Color { get; init; }

	/// <summary>A CSS color or token to draw it in, such as <c>var(--moka-color-info)</c>. Wins over the palette.</summary>
	public string? ColorValue { get; init; }

	/// <summary>The column it sits in, counting from 0. Null places it by the longest path to it.</summary>
	public int? Column { get; init; }
}
