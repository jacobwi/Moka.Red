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
	// The Dashed, Cross and Honeycomb patterns are SVG images, which cannot read the page's CSS
	// variables, so they default to a literal color.
	private const string DefaultSvgColor = "rgba(239, 83, 80, 0.25)";
	private const string DefaultCssColor = "var(--moka-color-primary-border)";
	private const string DefaultDashArray = "4 4";
	private const string DefaultHighlightColor = "var(--moka-color-primary-glow-strong)";
	private const int DefaultCellSize = 40;
	private const double DefaultStrokeWidth = 1;
	private const double DefaultDotRadius = 1;
	private const double DefaultPatternOpacity = 0.7;

	/// <summary>Content rendered inside the grid background container.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Which grid pattern to render. Default <see cref="MokaGridPattern.Lines" />.</summary>
	[Parameter]
	public MokaGridPattern Pattern { get; set; } = MokaGridPattern.Lines;

	/// <summary>Grid cell size in pixels. Zero or less uses the default. Default 40.</summary>
	[Parameter]
	public int CellSize { get; set; } = DefaultCellSize;

	/// <summary>Grid line / stroke thickness in pixels. NaN or infinity uses the default. Default 1.</summary>
	[Parameter]
	public double StrokeWidth { get; set; } = DefaultStrokeWidth;

	/// <summary>
	///     Dot radius for <see cref="MokaGridPattern.Dots" /> pattern. NaN or infinity uses the default.
	///     Default 1.
	/// </summary>
	[Parameter]
	public double DotRadius { get; set; } = DefaultDotRadius;

	/// <summary>Cross arm length for <see cref="MokaGridPattern.Cross" /> pattern. Default 3.</summary>
	[Parameter]
	public int CrossArm { get; set; } = 3;

	/// <summary>
	///     Dash and gap lengths for the <see cref="MokaGridPattern.Dashed" /> pattern: non-negative numbers with
	///     optional units, separated by spaces or commas (e.g., "4 4" or "6, 3"). Anything else uses the
	///     default. Default "4 4".
	/// </summary>
	[Parameter]
	public string DashArray { get; set; } = DefaultDashArray;

	/// <summary>Diagonal line angle in degrees. Default 45.</summary>
	[Parameter]
	public int DiagonalAngle { get; set; } = 45;

	/// <summary>
	///     Grid line and dot color: a hex value, a color keyword or a color function such as <c>rgb()</c> or
	///     <c>var()</c>. Unset, or not a color, uses the theme's primary-border token, or
	///     rgba(239, 83, 80, 0.25) for the Dashed, Cross and Honeycomb patterns. Those three are drawn as SVG
	///     images, which cannot read CSS variables, so give them a literal color.
	/// </summary>
	[Parameter]
	public string? PatternColor { get; set; }

	/// <summary>Opacity of the grid pattern overlay. Range 0-1, NaN or infinity uses the default. Default 0.7.</summary>
	[Parameter]
	public double PatternOpacity { get; set; } = DefaultPatternOpacity;

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

	/// <summary>
	///     Highlight glow color, with the same rules as <see cref="PatternColor" />. Unset, or not a color,
	///     uses the theme's primary-glow-strong token.
	/// </summary>
	[Parameter]
	public string? HighlightColor { get; set; }

	/// <summary>Controls how far the highlight glow extends (0-100%). Default 60.</summary>
	[Parameter]
	public int HighlightRadius { get; set; } = 60;

	/// <summary>
	///     Background color of the container, with the same rules as <see cref="PatternColor" />. Unset, or
	///     not a color, keeps the theme background.
	/// </summary>
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
		.AddStyle("background-color", CssValues.IsColor(BackgroundColor) ? BackgroundColor.Trim() : null)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>
	///     Whether the current pattern requires an SVG data URI (which cannot use CSS variables).
	/// </summary>
	private bool IsSvgPattern => Pattern is MokaGridPattern.Dashed or MokaGridPattern.Cross or MokaGridPattern.Honeycomb;

	// NaN and infinity print as words no CSS or SVG parser reads, and a cell of zero or less draws
	// nothing, so each falls back to its default instead of dropping the whole pattern.
	private int Cell => CellSize > 0 ? CellSize : DefaultCellSize;

	private double Stroke => double.IsFinite(StrokeWidth) ? StrokeWidth : DefaultStrokeWidth;

	private double Radius => double.IsFinite(DotRadius) ? DotRadius : DefaultDotRadius;

	private double Opacity => double.IsFinite(PatternOpacity) ? PatternOpacity : DefaultPatternOpacity;

	/// <summary>Inline style for the pattern overlay div.</summary>
	private string PatternStyle
	{
		get
		{
			// Checked, because the color goes inside a gradient function or an SVG attribute, where a
			// parenthesis or a quote would end it.
			var color = CssValues.ColorOrDefault(PatternColor, IsSvgPattern ? DefaultSvgColor : DefaultCssColor);
			var opacity = Opacity.ToString("F2", CultureInfo.InvariantCulture);
			var bg = GeneratePattern(color);
			var mask = FadeMask
				?? (FadeEdges
					? Css($"radial-gradient(ellipse at center, black {FadeStart}%, transparent {FadeEnd}%)")
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
			var glowColor = CssValues.ColorOrDefault(HighlightColor, DefaultHighlightColor);
			return new StyleBuilder()
				.AddStyle("background",
					Css($"radial-gradient(ellipse at center, {glowColor} 0%, transparent {HighlightRadius}%)"))
				.Build();
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
			MokaGridPattern.Lines => Css($"{Cell}px {Cell}px"),
			MokaGridPattern.Dots => Css($"{Cell}px {Cell}px"),
			MokaGridPattern.DiagonalLines => null,
			MokaGridPattern.Dashed => null,
			MokaGridPattern.Cross => null,
			MokaGridPattern.Honeycomb => null,
			_ => Css($"{Cell}px {Cell}px")
		};
	}

	private static string F(double v) => v.ToString("F1", CultureInfo.InvariantCulture);

	// Every number goes in with a decimal point and an ASCII minus sign, whatever the culture: some
	// cultures write a negative number with U+2212, which CSS does not read.
	private static string Css(FormattableString text) => FormattableString.Invariant(text);

	// ── Pattern generators ─────────────────────────────────────

	private string GenerateLines(string color)
	{
		var sw = F(Stroke);
		return $"linear-gradient(to right, {color} {sw}px, transparent {sw}px), "
			+ $"linear-gradient(to bottom, {color} {sw}px, transparent {sw}px)";
	}

	private string GenerateDots(string color)
	{
		var r = F(Radius);
		return $"radial-gradient(circle, {color} {r}px, transparent {r}px)";
	}

	private string GenerateDiagonalLines(string color)
	{
		var half = Cell / 2;
		var lineEnd = F(half + Stroke);
		return Css($"repeating-linear-gradient({DiagonalAngle}deg, transparent, transparent {half}px, {color} {half}px, {color} {lineEnd}px)");
	}

	private string GenerateDashedSvg(string color)
	{
		var sw = F(Stroke);
		var stroke = CssValues.EscapeXml(color);
		var dashes = CssValues.EscapeXml(CssValues.IsLengthList(DashArray) ? DashArray.Trim() : DefaultDashArray);
		var svg = Css($"<svg xmlns='http://www.w3.org/2000/svg' width='{Cell}' height='{Cell}'>")
			+ Css($"<line x1='0' y1='0' x2='{Cell}' y2='0' stroke='{stroke}' stroke-width='{sw}' stroke-dasharray='{dashes}'/>")
			+ Css($"<line x1='0' y1='0' x2='0' y2='{Cell}' stroke='{stroke}' stroke-width='{sw}' stroke-dasharray='{dashes}'/>")
			+ "</svg>";
		return CssValues.SvgDataUrl(svg);
	}

	private string GenerateCrossSvg(string color)
	{
		var cx = Cell / 2;
		var cy = Cell / 2;
		var sw = F(Stroke);
		var stroke = CssValues.EscapeXml(color);
		var svg = Css($"<svg xmlns='http://www.w3.org/2000/svg' width='{Cell}' height='{Cell}'>")
			+ Css($"<line x1='{cx - CrossArm}' y1='{cy}' x2='{cx + CrossArm}' y2='{cy}' stroke='{stroke}' stroke-width='{sw}'/>")
			+ Css($"<line x1='{cx}' y1='{cy - CrossArm}' x2='{cx}' y2='{cy + CrossArm}' stroke='{stroke}' stroke-width='{sw}'/>")
			+ "</svg>";
		return CssValues.SvgDataUrl(svg);
	}

	private string GenerateHoneycombSvg(string color)
	{
		// Proper flat-topped hexagonal tessellation.
		// A single tile contains two offset hexagons that tile seamlessly.
		var s = Cell / 2.0; // hexagon "radius" (center to vertex)
		var h = s * Math.Sqrt(3); // hex height (flat-topped)
		var tileW = s * 3.0; // tile width: 1.5 hex widths for offset
		var tileH = h; // tile height: one hex height

		var sw = F(Stroke);
		var stroke = CssValues.EscapeXml(color);

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
		// Row 2 hex (offset): centered at (2.5s, 0) - wraps top and bottom for tiling
		var hex2Top = HexPoints(s * 2.5, 0, s);
		var hex2Bot = HexPoints(s * 2.5, tileH, s);

		var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='{F(tileW)}' height='{F(tileH)}'>"
			+ $"<polygon points='{hex1}' fill='none' stroke='{stroke}' stroke-width='{sw}'/>"
			+ $"<polygon points='{hex2Top}' fill='none' stroke='{stroke}' stroke-width='{sw}'/>"
			+ $"<polygon points='{hex2Bot}' fill='none' stroke='{stroke}' stroke-width='{sw}'/>"
			+ "</svg>";
		return CssValues.SvgDataUrl(svg);
	}
}
