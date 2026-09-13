using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Identicon;

/// <summary>
///     Generates a unique, deterministic visual identifier from a string input.
///     Produces a GitHub-style horizontally symmetric grid rendered as inline SVG.
///     Pure C#, no JS, no external dependencies.
/// </summary>
public partial class MokaIdenticon
{
	private const uint FnvPrime = 16777619;
	private const uint FnvOffsetBasis = 2166136261;

	// Second basis for the high word of the bit supply. An 8x8 grid needs 32 half-cell bits,
	// which one 32-bit hash cannot supply without repeating itself.
	private const uint FnvAltOffsetBasis = 0x9E3779B9;

	private static readonly string[] DefaultPalette =
	[
		"#d32f2f", "#c2185b", "#7b1fa2", "#512da8",
		"#303f9f", "#1976d2", "#0288d1", "#00796b",
		"#388e3c", "#689f38", "#f57c00", "#e64a19"
	];

	private string? _cachedValue;
	private string _svg = string.Empty;

	/// <summary>The input string to hash (name, email, ID, etc.). Required.</summary>
	[Parameter]
	public string Value { get; set; } = string.Empty;

	/// <summary>Grid size (e.g., 5 produces a 5x5 GitHub-style identicon). Range 3-8. Default 5.</summary>
	[Parameter]
	public int IdenticonSize { get; set; } = 5;

	/// <summary>Custom color palette. Default uses 12 bright, distinguishable colors.</summary>
	[Parameter]
	public IReadOnlyList<string>? Palette { get; set; }

	/// <summary>Background color for the identicon. Default "transparent".</summary>
	[Parameter]
	public string? Background { get; set; } = "transparent";

	/// <inheritdoc />
	protected override string RootClass => "moka-identicon";

	private bool IsCircular => Rounded == MokaRounding.Full;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-identicon--rounded", IsCircular)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", ResolvedSize)
		.AddStyle("height", ResolvedSize)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("overflow", "hidden", IsCircular)
		.AddStyle("display", "inline-block")
		.AddStyle("line-height", "0")
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_cachedValue != Value)
		{
			_cachedValue = Value;
			_svg = GenerateSvg();
		}
	}

	private string GenerateSvg()
	{
		if (string.IsNullOrEmpty(Value))
		{
			return string.Empty;
		}

		ulong bitSupply = ComputeBitSupply(Value);

		// Low word drives the colour, so the palette choice is unchanged from the single-hash version.
		int hash = unchecked((int)(uint)bitSupply);
		IReadOnlyList<string> palette = Palette is { Count: > 0 } ? Palette : DefaultPalette;

		// Take the modulo first: Math.Abs(int.MinValue) overflows.
		int colorIndex = Math.Abs(hash % palette.Count);
		string color = palette[colorIndex];

		int gridSize = Math.Clamp(IdenticonSize, 3, 8);
		int halfWidth = (gridSize + 1) / 2;
		int cellSize = 10;
		int svgSize = gridSize * cellSize;

		var sb = new StringBuilder();
		sb.Append(CultureInfo.InvariantCulture,
			$"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {svgSize} {svgSize}' width='100%' height='100%'>");

		if (Background is not null)
		{
			sb.Append(CultureInfo.InvariantCulture,
				$"<rect width='{svgSize}' height='{svgSize}' fill='{Background}'/>");
		}

		int bitIndex = 0;
		for (int row = 0; row < gridSize; row++)
		{
			for (int col = 0; col < halfWidth; col++)
			{
				bool filled = ((bitSupply >> (bitIndex & 63)) & 1UL) == 1UL;
				bitIndex++;

				if (filled)
				{
					sb.Append(CultureInfo.InvariantCulture,
						$"<rect x='{col * cellSize}' y='{row * cellSize}' width='{cellSize}' height='{cellSize}' fill='{color}'/>");
					int mirrorCol = gridSize - 1 - col;
					if (mirrorCol != col)
					{
						sb.Append(CultureInfo.InvariantCulture,
							$"<rect x='{mirrorCol * cellSize}' y='{row * cellSize}' width='{cellSize}' height='{cellSize}' fill='{color}'/>");
					}
				}
			}
		}

		sb.Append("</svg>");
		return sb.ToString();
	}

	/// <summary>
	///     Builds the 64-bit supply the grid reads one bit per half-cell from. The low word is the
	///     original FNV-1a hash, so grids of 31 half-cells or fewer (every size below 8x8) keep the
	///     pattern they had when the supply was a single 32-bit hash.
	/// </summary>
	private static ulong ComputeBitSupply(string input)
	{
		uint low = ComputeHash(input, FnvOffsetBasis);
		uint high = ComputeHash(input, FnvAltOffsetBasis);
		return ((ulong)high << 32) | low;
	}

	/// <summary>FNV-1a 32-bit hash. Deterministic and stable across processes.</summary>
	private static uint ComputeHash(string input, uint offsetBasis)
	{
		unchecked
		{
			uint hash = offsetBasis;
			foreach (char c in input)
			{
				hash ^= c;
				hash *= FnvPrime;
			}

			return hash;
		}
	}
}
