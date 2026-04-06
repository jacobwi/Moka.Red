using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Identicon;

/// <summary>
///     Generates a unique, deterministic visual identifier from a string input.
///     Produces a GitHub-style horizontally symmetric grid rendered as inline SVG.
///     Pure C# — no JS, no external dependencies.
/// </summary>
public partial class MokaIdenticon
{
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

		int hash = ComputeHash(Value);
		IReadOnlyList<string> palette = Palette ?? DefaultPalette;
		int colorIndex = Math.Abs(hash) % palette.Count;
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
				bool filled = ((hash >> (bitIndex % 31)) & 1) == 1;
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

	private static int ComputeHash(string input)
	{
		// FNV-1a 32-bit hash — deterministic, stable across processes
		unchecked
		{
			int hash = (int)2166136261;
			foreach (char c in input)
			{
				hash ^= c;
				hash *= 16777619;
			}

			return hash;
		}
	}
}
