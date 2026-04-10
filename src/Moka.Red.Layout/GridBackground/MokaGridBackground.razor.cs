using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.GridBackground;

/// <summary>
///     A decorative container with configurable grid background patterns.
///     Supports lines, dots, dashed, cross, diagonal, and honeycomb patterns
///     with optional edge fading, center glow highlight, and extensive customization.
/// </summary>
public partial class MokaGridBackground : MokaVisualComponentBase
{
	/// <summary>Content rendered inside the grid background container.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Which grid pattern to render. Default <see cref="MokaGridPattern.Lines" />.</summary>
	[Parameter]
	public MokaGridPattern Pattern { get; set; } = MokaGridPattern.Lines;

	/// <summary>Grid cell size in pixels. Default 40.</summary>
	[Parameter]
	public int CellSize { get; set; } = 40;

	/// <summary>Grid line / stroke thickness in pixels. Default 1.</summary>
	[Parameter]
	public double StrokeWidth { get; set; } = 1;

	/// <summary>Dot radius for <see cref="MokaGridPattern.Dots" /> pattern. Default 1.</summary>
	[Parameter]
	public double DotRadius { get; set; } = 1;

	/// <summary>Cross arm length for <see cref="MokaGridPattern.Cross" /> pattern. Default 3.</summary>
	[Parameter]
	public int CrossArm { get; set; } = 3;

	/// <summary>Dash length and gap for <see cref="MokaGridPattern.Dashed" /> pattern (e.g., "4 4"). Default "4 4".</summary>
	[Parameter]
	public string DashArray { get; set; } = "4 4";

	/// <summary>Diagonal line angle in degrees. Default 45.</summary>
	[Parameter]
	public int DiagonalAngle { get; set; } = 45;

	/// <summary>Override the grid line/dot color. Defaults to the theme's primary-border token (CSS patterns) or rgba(239, 83, 80, 0.25) (SVG patterns).</summary>
	[Parameter]
	public string? PatternColor { get; set; }

	/// <summary>Opacity of the grid pattern overlay. Range 0–1, default 0.7.</summary>
	[Parameter]
	public double PatternOpacity { get; set; } = 0.7;

	/// <summary>When true, applies a radial fade mask so the grid fades toward the edges. Default true.</summary>
	[Parameter]
	public bool FadeEdges { get; set; } = true;

	/// <summary>Controls how far the fade extends. 0 = fade starts immediately, 100 = no fade. Default 30.</summary>
	[Parameter]
	public int FadeStart { get; set; } = 30;

	/// <summary>Controls the outer edge of the fade. Default 80.</summary>
	[Parameter]
	public int FadeEnd { get; set; } = 80;

	/// <summary>Custom CSS mask-image value. Overrides the default radial fade when set.</summary>
	[Parameter]
	public string? FadeMask { get; set; }

	/// <summary>When true, renders a subtle radial glow in the center of the container.</summary>
	[Parameter]
	public bool Highlighted { get; set; }

	/// <summary>Override the highlight glow color. Defaults to the theme's primary-glow-strong token.</summary>
	[Parameter]
	public string? HighlightColor { get; set; }

	/// <summary>Controls how far the highlight glow extends (0–100%). Default 60.</summary>
	[Parameter]
	public int HighlightRadius { get; set; } = 60;

	/// <summary>Background color of the container. Defaults to theme background.</summary>
	[Parameter]
	public string? BackgroundColor { get; set; }

	/// <summary>Minimum height of the container (e.g., "400px", "50vh").</summary>
	[Parameter]
	public string? MinHeight { get; set; }

	/// <summary>When true, sets min-height to 100vh.</summary>
	[Parameter]
	public bool FullScreen { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-grid-bg";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-grid-bg--fullscreen", FullScreen)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("min-height", MinHeight, !string.IsNullOrEmpty(MinHeight) && !FullScreen)
		.AddStyle("background-color", BackgroundColor, !string.IsNullOrEmpty(BackgroundColor))
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>
	///     Whether the current pattern requires an SVG data URI (which cannot use CSS variables).
	/// </summary>
	private bool IsSvgPattern => Pattern is MokaGridPattern.Dashed or MokaGridPattern.Cross or MokaGridPattern.Honeycomb;

	/// <summary>Default SVG-safe color when no custom PatternColor is set.</summary>
	private const string DefaultSvgColor = "rgba(239, 83, 80, 0.25)";

	/// <summary>Inline style for the pattern overlay div.</summary>
	private string PatternStyle
	{
		get
		{
			// SVG data URIs cannot resolve CSS variables — use a raw color fallback
			var color = PatternColor
				?? (IsSvgPattern ? DefaultSvgColor : "var(--moka-color-primary-border)");
			var opacity = PatternOpacity.ToString("F2", CultureInfo.InvariantCulture);
			var bg = GeneratePattern(color);
			var mask = FadeMask
				?? (FadeEdges
					? $"radial-gradient(ellipse at center, black {FadeStart}%, transparent {FadeEnd}%)"
					: null);

			return new StyleBuilder()
				.AddStyle("background-image", bg)
				.AddStyle("background-size", GetBackgroundSize())
				.AddStyle("opacity", opacity)
				.AddStyle("-webkit-mask-image", mask, mask is not null)
				.AddStyle("mask-image", mask, mask is not null)
				.Build()!;
		}
	}

	/// <summary>Inline style for the optional highlight glow div.</summary>
	private string? HighlightStyle
	{
		get
		{
			if (!Highlighted) return null;
			var glowColor = HighlightColor ?? "var(--moka-color-primary-glow-strong)";
			return $"background: radial-gradient(ellipse at center, {glowColor} 0%, transparent {HighlightRadius}%);";
		}
	}

	private string GeneratePattern(string color)
	{
		return Pattern switch
		{
			MokaGridPattern.Lines => GenerateLines(color),
			MokaGridPattern.Dots => GenerateDots(color),
			MokaGridPattern.Dashed => GenerateDashedSvg(color),
			MokaGridPattern.Cross => GenerateCrossSvg(color),
			MokaGridPattern.DiagonalLines => GenerateDiagonalLines(color),
			MokaGridPattern.Honeycomb => GenerateHoneycombSvg(color),
			_ => GenerateLines(color)
		};
	}

	private string? GetBackgroundSize()
	{
		return Pattern switch
		{
			MokaGridPattern.Lines => $"{CellSize}px {CellSize}px",
			MokaGridPattern.Dots => $"{CellSize}px {CellSize}px",
			MokaGridPattern.DiagonalLines => null,
			MokaGridPattern.Dashed => null,
			MokaGridPattern.Cross => null,
			MokaGridPattern.Honeycomb => null,
			_ => $"{CellSize}px {CellSize}px"
		};
	}

	private static string F(double v) => v.ToString("F1", CultureInfo.InvariantCulture);

	// ── Pattern generators ─────────────────────────────────────

	private string GenerateLines(string color)
	{
		var sw = F(StrokeWidth);
		return $"linear-gradient(to right, {color} {sw}px, transparent {sw}px), "
			+ $"linear-gradient(to bottom, {color} {sw}px, transparent {sw}px)";
	}

	private string GenerateDots(string color)
	{
		var r = F(DotRadius);
		return $"radial-gradient(circle, {color} {r}px, transparent {r}px)";
	}

	private string GenerateDiagonalLines(string color)
	{
		var half = CellSize / 2;
		var sw = F(StrokeWidth);
		return $"repeating-linear-gradient({DiagonalAngle}deg, transparent, transparent {half}px, {color} {half}px, {color} {half + StrokeWidth}px)";
	}

	private string GenerateDashedSvg(string color)
	{
		var sw = F(StrokeWidth);
		var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='{CellSize}' height='{CellSize}'>"
			+ $"<line x1='0' y1='0' x2='{CellSize}' y2='0' stroke='{color}' stroke-width='{sw}' stroke-dasharray='{DashArray}'/>"
			+ $"<line x1='0' y1='0' x2='0' y2='{CellSize}' stroke='{color}' stroke-width='{sw}' stroke-dasharray='{DashArray}'/>"
			+ "</svg>";
		return $"url(\"data:image/svg+xml,{Uri.EscapeDataString(svg)}\")";
	}

	private string GenerateCrossSvg(string color)
	{
		var cx = CellSize / 2;
		var cy = CellSize / 2;
		var sw = F(StrokeWidth);
		var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='{CellSize}' height='{CellSize}'>"
			+ $"<line x1='{cx - CrossArm}' y1='{cy}' x2='{cx + CrossArm}' y2='{cy}' stroke='{color}' stroke-width='{sw}'/>"
			+ $"<line x1='{cx}' y1='{cy - CrossArm}' x2='{cx}' y2='{cy + CrossArm}' stroke='{color}' stroke-width='{sw}'/>"
			+ "</svg>";
		return $"url(\"data:image/svg+xml,{Uri.EscapeDataString(svg)}\")";
	}

	private string GenerateHoneycombSvg(string color)
	{
		// Proper flat-topped hexagonal tessellation.
		// A single tile contains two offset hexagons that tile seamlessly.
		var s = CellSize / 2.0; // hexagon "radius" (center to vertex)
		var h = s * Math.Sqrt(3); // hex height (flat-topped)
		var tileW = s * 3.0; // tile width: 1.5 hex widths for offset
		var tileH = h; // tile height: one hex height

		var sw = F(StrokeWidth);

		// Flat-topped hexagon: vertices at angles 0°, 60°, 120°, 180°, 240°, 300°
		static string HexPoints(double cx, double cy, double radius)
		{
			var pts = new string[6];
			for (var i = 0; i < 6; i++)
			{
				var angle = Math.PI / 3.0 * i;
				pts[i] = $"{F(cx + radius * Math.Cos(angle))},{F(cy + radius * Math.Sin(angle))}";
			}

			return string.Join(" ", pts);
		}

		// Row 1 hex: centered at (s, h/2)
		var hex1 = HexPoints(s, tileH / 2.0, s);
		// Row 2 hex (offset): centered at (2.5s, 0) — wraps top and bottom for tiling
		var hex2Top = HexPoints(s * 2.5, 0, s);
		var hex2Bot = HexPoints(s * 2.5, tileH, s);

		var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='{F(tileW)}' height='{F(tileH)}'>"
			+ $"<polygon points='{hex1}' fill='none' stroke='{color}' stroke-width='{sw}'/>"
			+ $"<polygon points='{hex2Top}' fill='none' stroke='{color}' stroke-width='{sw}'/>"
			+ $"<polygon points='{hex2Bot}' fill='none' stroke='{color}' stroke-width='{sw}'/>"
			+ "</svg>";
		return $"url(\"data:image/svg+xml,{Uri.EscapeDataString(svg)}\")";
	}
}
