using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.GridBackground;

/// <summary>
///     A decorative container with a grid pattern behind its content: eighteen patterns, a choice of where the
///     pattern fades, a glow, slow motion and a choice of theme colors.
/// </summary>
public partial class MokaGridBackground : MokaVisualComponentBase
{
	private const string JsModulePath = "./_content/Moka.Red.Layout/GridBackground/MokaGridBackground.razor.js";

	// Dashed, Cross and Honeycomb were SVG images, which can't read CSS variables, so they drew this literal
	// color. They are masks over a color now. The original design keeps the color, and the new design's
	// stylesheet sets --_gb-ink to the theme's token.
	private const string DefaultSvgColor = "rgba(239, 83, 80, 0.25)";
	private const string DefaultSvgInk = "var(--_gb-ink, " + DefaultSvgColor + ")";
	private const string DefaultCssColor = "var(--moka-color-primary-border)";
	private const string Outline = "var(--moka-color-outline)";
	private const string OutlineStrong = "var(--moka-color-outline-strong)";
	private const string SurfaceVariant = "var(--moka-color-surface-variant)";
	private const string Primary = "var(--moka-color-primary)";
	private const string PrimaryLight = "var(--moka-color-primary-light)";
	private const string PrimaryGlow = "var(--moka-color-primary-glow)";
	private const string Info = "var(--moka-color-info)";

	// A mask reads only alpha, so any opaque color draws it.
	private const string MaskInk = "black";

	private const string DefaultDashArray = "4 4";
	private const string DefaultHighlightColor = "var(--moka-color-primary-glow-strong)";
	private const int DefaultCellSize = 40;
	private const int DefaultMajorEvery = 5;
	private const double DefaultStrokeWidth = 1;
	private const double DefaultDotRadius = 1;
	private const double DefaultPatternOpacity = 0.7;
	private const double DesignedHighlightRadius = 60;
	private const double DriftPixelsPerSecond = 2.5;

	private static readonly string TopographicPath = BuildContours();

	private static readonly (string X, string Y)[] CircuitPads =
	[
		("62.5", "36.5"), ("78.5", "24.5"), ("32.5", "50.5"), ("20.5", "10.5"),
		("8.5", "72.5"), ("74.5", "8.5"), ("46.5", "60.5"), ("66.5", "52.5")
	];

	private ElementReference _fade;
	private bool _spotlightBound;

	/// <summary>Content rendered inside the grid background container.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Which grid pattern to render. Default <see cref="MokaGridPattern.Lines" />.</summary>
	[Parameter]
	public MokaGridPattern Pattern { get; set; } = MokaGridPattern.Lines;

	/// <summary>
	///     Grid cell size in pixels. Zero or less, the default, uses the pattern's own size: 40 for Lines, Dots,
	///     Dashed, Cross, DiagonalLines and Honeycomb, 16 for GraphPaper's fine cell, 24 for a Checker square,
	///     28 for Rows and Radar, 32 for a Triangles side, 48 for Isometric, Diamonds, Columns and CornerMarks, 96
	///     for Circuit and 160 for Grain. Topographic covers the box and ignores it.
	/// </summary>
	[Parameter]
	public int CellSize { get; set; }

	/// <summary>
	///     How many fine cells sit between the stronger lines of <see cref="MokaGridPattern.GraphPaper" />. Less
	///     than 2 uses the default. Default 5.
	/// </summary>
	[Parameter]
	public int MajorEvery { get; set; } = DefaultMajorEvery;

	/// <summary>Grid line / stroke thickness in pixels. NaN or infinity uses the default. Default 1.</summary>
	[Parameter]
	public double StrokeWidth { get; set; } = DefaultStrokeWidth;

	/// <summary>
	///     Dot radius for the <see cref="MokaGridPattern.Dots" /> pattern. It also scales the dots of Diamonds and
	///     the pads of Circuit. NaN or infinity uses the default. Default 1.
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
	///     Grid line and dot color: a hex value, a color keyword or a color function such as <c>rgb()</c>,
	///     <c>color-mix()</c> or <c>var()</c>. It wins over <see cref="Tone" />. Unset, or not a color, draws the
	///     tone's colors.
	/// </summary>
	[Parameter]
	public string? PatternColor { get; set; }

	/// <summary>
	///     Which theme colors draw the pattern when <see cref="PatternColor" /> isn't set. Default
	///     <see cref="MokaGridTone.Accent" />, the original look.
	/// </summary>
	[Parameter]
	public MokaGridTone Tone { get; set; } = MokaGridTone.Accent;

	/// <summary>
	///     Opacity of the grid pattern, from 0 to 1. Unset, NaN or infinity uses the pattern's own: 0.7, or 1 for
	///     Checker and for any <see cref="Tone" /> but Accent, 0.9 for CornerMarks and 0.75 for Grain.
	/// </summary>
	[Parameter]
	public double PatternOpacity { get; set; } = double.NaN;

	/// <summary>
	///     When true, applies a radial fade mask so the grid fades toward the edges. <see cref="Fade" /> wins when
	///     it is set. Default true.
	/// </summary>
	[Parameter]
	public bool FadeEdges { get; set; } = true;

	/// <summary>
	///     Where the pattern fades. Unset keeps <see cref="FadeEdges" />: <see cref="MokaGridFade.Center" />, or
	///     <see cref="MokaGridFade.None" /> when it is false. <see cref="MokaGridFade.Spotlight" /> loads a small
	///     script that follows the pointer.
	/// </summary>
	[Parameter]
	public MokaGridFade? Fade { get; set; }

	/// <summary>
	///     Controls how far the centre fade extends. 0 = fade starts immediately, 100 = no fade. Default 30.
	///     Shapes <see cref="MokaGridFade.Center" /> only.
	/// </summary>
	[Parameter]
	public int FadeStart { get; set; } = 30;

	/// <summary>Controls the outer edge of the centre fade. Default 80. Shapes <see cref="MokaGridFade.Center" /> only.</summary>
	[Parameter]
	public int FadeEnd { get; set; } = 80;

	/// <summary>Custom CSS mask-image value. Overrides every <see cref="Fade" /> when set.</summary>
	[Parameter]
	public string? FadeMask { get; set; }

	/// <summary>When true, renders a subtle radial glow in the center of the container. <see cref="Glow" /> wins.</summary>
	[Parameter]
	public bool Highlighted { get; set; }

	/// <summary>
	///     Which glow to draw over the pattern. Unset keeps <see cref="Highlighted" />: the centre glow when it is
	///     true, none when false.
	/// </summary>
	[Parameter]
	public MokaGridGlow? Glow { get; set; }

	/// <summary>
	///     Glow color, with the same rules as <see cref="PatternColor" />. It replaces the accent in every glow;
	///     TwoTone keeps its info half. Unset, or not a color, uses the theme's accent tokens.
	/// </summary>
	[Parameter]
	public string? HighlightColor { get; set; }

	/// <summary>
	///     How far the glow reaches, as a percentage of the box. 60 is each glow's designed size, and other values
	///     scale it. Default 60.
	/// </summary>
	[Parameter]
	public int HighlightRadius { get; set; } = 60;

	/// <summary>
	///     How the background moves. Default <see cref="MokaGridMotion.None" />. Every motion stops when the
	///     system asks for reduced motion.
	/// </summary>
	[Parameter]
	public MokaGridMotion Motion { get; set; } = MokaGridMotion.None;

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
		.AddClass("moka-grid-bg--spotlight", Spotlit)
		.AddClass("moka-grid-bg--drift", Drifting)
		.AddClass("moka-grid-bg--pulse", Motion == MokaGridMotion.Pulse)
		.AddClass("moka-grid-bg--sweep", Motion == MokaGridMotion.Sweep)
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

	// Lines, Dots and DiagonalLines keep their original single layer, a gradient drawn in the color, unless
	// a motion has to move the pattern apart from the fade or the spotlight needs the wrapper to find the box.
	// Every other pattern is a mask over a layer filled with the color, inside a wrapper that carries the fade.
	private bool Layered =>
		Pattern is not (MokaGridPattern.Lines or MokaGridPattern.Dots or MokaGridPattern.DiagonalLines)
		|| Drifting || Motion == MokaGridMotion.Sweep || Spotlit;

	private bool Drifting =>
		Motion == MokaGridMotion.Drift && Pattern is not (MokaGridPattern.Topographic or MokaGridPattern.Radar);

	private bool HasMajorLines => Pattern == MokaGridPattern.GraphPaper;

	private MokaGridFade ResolvedFade => Fade ?? (FadeEdges ? MokaGridFade.Center : MokaGridFade.None);

	private bool Spotlit => FadeMask is null && ResolvedFade == MokaGridFade.Spotlight;

	private MokaGridGlow? ResolvedGlow => Glow ?? (Highlighted ? MokaGridGlow.Center : null);

	// Checked, because the color goes inside a gradient function or a declaration, where a parenthesis or a
	// semicolon would end it.
	private bool HasPatternColor => CssValues.IsColor(PatternColor);

	private MokaGridTone EffectiveTone =>
		HasPatternColor || (Tone == MokaGridTone.AccentMajor && !HasMajorLines) ? MokaGridTone.Accent : Tone;

	// NaN and infinity print as words no CSS or SVG parser reads, and a cell of zero or less draws
	// nothing, so each falls back to its default instead of dropping the whole pattern.
	private int Cell => CellSize > 0 ? CellSize : OwnCellSize(Pattern);

	private int Major => MajorEvery >= 2 ? MajorEvery : DefaultMajorEvery;

	private double Stroke => double.IsFinite(StrokeWidth) ? StrokeWidth : DefaultStrokeWidth;

	private double Radius => double.IsFinite(DotRadius) ? DotRadius : DefaultDotRadius;

	private double Opacity => double.IsFinite(PatternOpacity) ? PatternOpacity : OwnOpacity();

	private string Ink => HasPatternColor ? PatternColor!.Trim() : ToneInk(false);

	private string MajorInk => HasPatternColor ? PatternColor!.Trim() : ToneInk(true);

	private string OpacityText => Opacity.ToString("F2", CultureInfo.InvariantCulture);

	private string? FadeImage => FadeMask ?? ResolvedFade switch
	{
		MokaGridFade.Center => Css($"radial-gradient(ellipse at center, black {FadeStart}%, transparent {FadeEnd}%)"),
		MokaGridFade.Bottom => "linear-gradient(to bottom, black 5%, transparent 92%)",
		MokaGridFade.Top => "linear-gradient(to top, black 5%, transparent 92%)",
		MokaGridFade.Frame => "radial-gradient(ellipse at center, transparent 32%, black 72%)",
		MokaGridFade.Spotlight =>
			"radial-gradient(circle 150px at var(--_gb-x, 50%) var(--_gb-y, 50%), black 0 35%, rgb(0 0 0 / 0.12) 100%)",
		_ => null
	};

	/// <summary>Inline style for the pattern layer.</summary>
	private string PatternStyle => Layered ? MaskedLayerStyle(BuildMask(MaskInk), Ink) : GradientLayerStyle();

	/// <summary>Inline style for GraphPaper's major lines.</summary>
	private string MajorStyle => MaskedLayerStyle(
		new PatternMask(GenerateLines(MaskInk), TileSize(Cell * Major, Cell * Major), Cell * Major, Cell * Major), MajorInk);

	/// <summary>Inline style for the band that sweeps across the pattern.</summary>
	private string? SweepStyle => MaskStyle(new StyleBuilder(), BuildMask(MaskInk)).Build();

	/// <summary>Inline style for the wrapper that fades the layered pattern.</summary>
	private string? FadeStyle
	{
		get
		{
			string? fade = FadeImage;
			StyleBuilder style = new StyleBuilder()
				.AddStyle("-webkit-mask-image", fade, fade is not null)
				.AddStyle("mask-image", fade, fade is not null);
			if (Drifting)
			{
				(double x, double y) = DriftDistance();
				style.AddStyle("--_gb-drift-x", Px(x))
					.AddStyle("--_gb-drift-y", Px(y))
					.AddStyle("--_gb-drift-time", Number(Math.Max(x, y) / DriftPixelsPerSecond) + "s");
			}

			return style.Build();
		}
	}

	/// <summary>Inline style for the optional highlight glow div.</summary>
	private string? HighlightStyle
	{
		get
		{
			if (ResolvedGlow is not { } glow)
			{
				return null;
			}

			return new StyleBuilder().AddStyle("background", GlowImage(glow)).Build();
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		bool wanted = Spotlit;
		if (wanted == _spotlightBound)
		{
			return;
		}

		_spotlightBound = wanted;
		await SafeModuleInvokeVoidAsync(JsModulePath, wanted ? "bindSpotlight" : "unbindSpotlight", _fade);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_spotlightBound)
		{
			_spotlightBound = false;
			await SafeModuleInvokeVoidAsync(JsModulePath, "unbindSpotlight", _fade);
		}

		await base.DisposeAsyncCore();
	}

	private static int OwnCellSize(MokaGridPattern pattern) => pattern switch
	{
		MokaGridPattern.GraphPaper => 16,
		MokaGridPattern.Checker => 24,
		MokaGridPattern.Rows or MokaGridPattern.Radar => 28,
		MokaGridPattern.Triangles => 32,
		MokaGridPattern.Isometric or MokaGridPattern.Diamonds or MokaGridPattern.Columns
			or MokaGridPattern.CornerMarks => 48,
		MokaGridPattern.Circuit => 96,
		MokaGridPattern.Grain => 160,
		_ => DefaultCellSize
	};

	private double OwnOpacity()
	{
		if (EffectiveTone != MokaGridTone.Accent)
		{
			return 1;
		}

		return Pattern switch
		{
			MokaGridPattern.Checker => 1,
			MokaGridPattern.CornerMarks => 0.9,
			MokaGridPattern.Grain => 0.75,
			_ => DefaultPatternOpacity
		};
	}

	private string ToneInk(bool major) => EffectiveTone switch
	{
		MokaGridTone.Neutral => Pattern switch
		{
			MokaGridPattern.GraphPaper => major ? OutlineStrong : Outline,
			MokaGridPattern.Checker => SurfaceVariant,
			_ => OutlineStrong
		},
		MokaGridTone.Text => Pattern switch
		{
			MokaGridPattern.GraphPaper => TextMix(major ? 16 : 8),
			MokaGridPattern.Checker => TextMix(5),
			_ => TextMix(14)
		},
		MokaGridTone.AccentMajor => major ? Mix(Primary, 42) : Outline,
		_ => Pattern switch
		{
			MokaGridPattern.Dashed or MokaGridPattern.Cross or MokaGridPattern.Honeycomb => DefaultSvgInk,
			MokaGridPattern.GraphPaper => Mix(Primary, major ? 46 : 22),
			MokaGridPattern.Checker => Mix(Primary, 7),
			_ => DefaultCssColor
		}
	};

	private static string TextMix(int percent) => Mix("var(--moka-color-on-background)", percent);

	private static string Mix(string color, double percent) =>
		$"color-mix(in srgb, {color} {Number(percent)}%, transparent)";

	private string GradientLayerStyle()
	{
		string? fade = FadeImage;
		return new StyleBuilder()
			.AddStyle("background-image", Gradient(Ink))
			.AddStyle("background-size", Pattern == MokaGridPattern.DiagonalLines ? null : TileSize(Cell, Cell))
			.AddStyle("opacity", OpacityText)
			.AddStyle("-webkit-mask-image", fade, fade is not null)
			.AddStyle("mask-image", fade, fade is not null)
			.Build()!;
	}

	private string MaskedLayerStyle(PatternMask mask, string ink)
	{
		StyleBuilder style = new StyleBuilder().AddStyle("background-color", ink);
		return MaskStyle(style, mask).AddStyle("opacity", OpacityText).Build()!;
	}

	private static StyleBuilder MaskStyle(StyleBuilder style, PatternMask mask) => style
		.AddStyle("-webkit-mask-image", mask.Image)
		.AddStyle("mask-image", mask.Image)
		.AddStyle("-webkit-mask-size", mask.Size)
		.AddStyle("mask-size", mask.Size)
		.AddStyle("-webkit-mask-repeat", mask.Repeat)
		.AddStyle("mask-repeat", mask.Repeat)
		.AddStyle("-webkit-mask-position", mask.Position)
		.AddStyle("mask-position", mask.Position);

	private string Gradient(string ink) => Pattern switch
	{
		MokaGridPattern.Dots => GenerateDots(ink),
		MokaGridPattern.DiagonalLines => GenerateDiagonalLines(ink),
		_ => GenerateLines(ink)
	};

	private PatternMask BuildMask(string ink)
	{
		int cell = Cell;
		return Pattern switch
		{
			MokaGridPattern.Lines or MokaGridPattern.GraphPaper =>
				new PatternMask(GenerateLines(ink), TileSize(cell, cell), cell, cell),
			MokaGridPattern.Dots => new PatternMask(GenerateDots(ink), TileSize(cell, cell), cell, cell),
			MokaGridPattern.DiagonalLines => new PatternMask(GenerateDiagonalLines(ink), null, 0, 0),
			MokaGridPattern.Dashed => new PatternMask(GenerateDashedSvg(), null, cell, cell),
			MokaGridPattern.Cross => new PatternMask(GenerateCrossSvg(), null, cell, cell),
			MokaGridPattern.Honeycomb => GenerateHoneycombSvg(),
			MokaGridPattern.Isometric => GenerateIsometricSvg(),
			MokaGridPattern.Triangles => GenerateTrianglesSvg(),
			MokaGridPattern.Diamonds => GenerateDiamondsSvg(),
			MokaGridPattern.Checker => new PatternMask(
				$"repeating-conic-gradient({ink} 0 25%, transparent 0 50%)", TileSize(cell * 2, cell * 2), cell * 2, cell * 2),
			MokaGridPattern.Rows => new PatternMask(
				Css($"linear-gradient(to bottom, {ink} {F(Stroke)}px, transparent {F(Stroke)}px)"), Css($"100% {cell}px"), 0, cell),
			MokaGridPattern.Columns => new PatternMask(
				Css($"linear-gradient(to right, {ink} {F(Stroke)}px, transparent {F(Stroke)}px)"), Css($"{cell}px 100%"), cell, 0),
			MokaGridPattern.Circuit => GenerateCircuitSvg(),
			MokaGridPattern.Topographic => GenerateTopographicSvg(),
			MokaGridPattern.Radar => new PatternMask(GenerateRadar(ink), null, 0, 0),
			MokaGridPattern.CornerMarks => GenerateCornerMarksSvg(),
			MokaGridPattern.Grain => GenerateGrainSvg(),
			_ => new PatternMask(GenerateLines(ink), TileSize(cell, cell), cell, cell)
		};
	}

	// How far one loop of the drift moves the pattern: one tile, so the end looks like the start.
	private (double X, double Y) DriftDistance()
	{
		if (HasMajorLines)
		{
			return (Cell * Major, Cell * Major);
		}

		if (Pattern == MokaGridPattern.DiagonalLines)
		{
			// The stripes repeat every period across their own direction, so a shift along one axis that
			// crosses exactly one period looks the same. The axis the stripes cross more steeply needs less.
			double period = Cell / 2 + Stroke;
			double angle = DiagonalAngle * Math.PI / 180;
			double across = Math.Abs(Math.Sin(angle));
			double down = Math.Abs(Math.Cos(angle));
			return across >= down ? (Round(period / across), 0) : (0, Round(period / down));
		}

		PatternMask mask = BuildMask(MaskInk);
		return (mask.TileWidth, mask.TileHeight);
	}

	private string GlowImage(MokaGridGlow glow)
	{
		if (glow == MokaGridGlow.Center)
		{
			string glowColor = CssValues.ColorOrDefault(HighlightColor, DefaultHighlightColor);
			return Css($"radial-gradient(ellipse at center, {glowColor} 0%, transparent {HighlightRadius}%)");
		}

		string? custom = CssValues.IsColor(HighlightColor) ? HighlightColor.Trim() : null;
		string accent = custom ?? Primary;
		string soft = custom is null ? PrimaryGlow : Mix(custom, 12);
		double reach = HighlightRadius / DesignedHighlightRadius;
		string To(double stop) => $"transparent {Number(stop * reach)}%";

		return glow switch
		{
			MokaGridGlow.Aurora =>
				$"radial-gradient(55% 85% at 28% -18%, {Mix(accent, 30)}, {To(72)}), "
				+ $"radial-gradient(48% 75% at 72% -28%, {Mix(custom ?? PrimaryLight, 24)}, {To(70)}), "
				+ $"radial-gradient(90% 55% at 50% -10%, {soft}, {To(80)})",
			MokaGridGlow.Corner => $"radial-gradient(58% 105% at 100% 0%, {Mix(accent, 30)}, {To(72)})",
			MokaGridGlow.Beam =>
				$"radial-gradient(44% 24% at 50% 44%, {Mix(accent, 30)}, {To(100)}), "
				+ $"radial-gradient(70% 40% at 50% 46%, {soft}, {To(100)})",
			MokaGridGlow.TwoTone =>
				$"radial-gradient(42% 75% at 34% 42%, {Mix(accent, 26)}, {To(72)}), "
				+ $"radial-gradient(42% 75% at 66% 58%, {Mix(Info, 26)}, {To(72)})",
			_ =>
				$"radial-gradient(70% 80% at 50% 122%, {Mix(accent, 42)}, {To(70)}), "
				+ $"linear-gradient(to top, {soft}, {To(45)})"
		};
	}

	private static string F(double v) => v.ToString("F1", CultureInfo.InvariantCulture);

	private static string Number(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

	private static string Px(double v) => Number(v) + "px";

	private static string TileSize(double width, double height) => $"{Px(width)} {Px(height)}";

	private static double Round(double v) => Math.Round(v, 2);

	// Every number goes in with a decimal point and an ASCII minus sign, whatever the culture: some
	// cultures write a negative number with U+2212, which CSS does not read.
	private static string Css(FormattableString text) => FormattableString.Invariant(text);

	private static string Svg(double width, double height, string body, string? viewBox = null) =>
		CssValues.SvgDataUrl(
			$"<svg xmlns='http://www.w3.org/2000/svg' width='{Number(width)}' height='{Number(height)}'"
			+ (viewBox is null ? "" : $" viewBox='{viewBox}'") + ">" + body + "</svg>");

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

	private string GenerateRadar(string color)
	{
		double ring = Cell;
		double half = Stroke / 2;
		string inner = Px(Math.Max(0, ring - Stroke));
		string Across(string direction) =>
			$"linear-gradient(to {direction}, transparent calc(50% - {Px(half)}), {color} calc(50% - {Px(half)}) "
			+ $"calc(50% + {Px(half)}), transparent calc(50% + {Px(half)}))";
		return $"repeating-radial-gradient(circle at 50% 50%, transparent 0 {inner}, {color} {inner} {Px(ring)}), "
			+ Across("right") + ", " + Across("bottom");
	}

	private string GenerateDashedSvg()
	{
		var sw = F(Stroke);
		var dashes = CssValues.EscapeXml(CssValues.IsLengthList(DashArray) ? DashArray.Trim() : DefaultDashArray);
		var svg = Css($"<svg xmlns='http://www.w3.org/2000/svg' width='{Cell}' height='{Cell}'>")
			+ Css($"<line x1='0' y1='0' x2='{Cell}' y2='0' stroke='{MaskInk}' stroke-width='{sw}' stroke-dasharray='{dashes}'/>")
			+ Css($"<line x1='0' y1='0' x2='0' y2='{Cell}' stroke='{MaskInk}' stroke-width='{sw}' stroke-dasharray='{dashes}'/>")
			+ "</svg>";
		return CssValues.SvgDataUrl(svg);
	}

	private string GenerateCrossSvg()
	{
		var cx = Cell / 2;
		var cy = Cell / 2;
		var sw = F(Stroke);
		var svg = Css($"<svg xmlns='http://www.w3.org/2000/svg' width='{Cell}' height='{Cell}'>")
			+ Css($"<line x1='{cx - CrossArm}' y1='{cy}' x2='{cx + CrossArm}' y2='{cy}' stroke='{MaskInk}' stroke-width='{sw}'/>")
			+ Css($"<line x1='{cx}' y1='{cy - CrossArm}' x2='{cx}' y2='{cy + CrossArm}' stroke='{MaskInk}' stroke-width='{sw}'/>")
			+ "</svg>";
		return CssValues.SvgDataUrl(svg);
	}

	private PatternMask GenerateHoneycombSvg()
	{
		// Proper flat-topped hexagonal tessellation.
		// A single tile contains two offset hexagons that tile seamlessly.
		var s = Cell / 2.0; // hexagon "radius" (center to vertex)
		var h = s * Math.Sqrt(3); // hex height (flat-topped)
		var tileW = s * 3.0; // tile width: 1.5 hex widths for offset
		var tileH = h; // tile height: one hex height

		var sw = F(Stroke);

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
			+ $"<polygon points='{hex1}' fill='none' stroke='{MaskInk}' stroke-width='{sw}'/>"
			+ $"<polygon points='{hex2Top}' fill='none' stroke='{MaskInk}' stroke-width='{sw}'/>"
			+ $"<polygon points='{hex2Bot}' fill='none' stroke='{MaskInk}' stroke-width='{sw}'/>"
			+ "</svg>";
		return new PatternMask(CssValues.SvgDataUrl(svg), null,
			double.Parse(F(tileW), CultureInfo.InvariantCulture), double.Parse(F(tileH), CultureInfo.InvariantCulture));
	}

	private PatternMask GenerateIsometricSvg()
	{
		double width = Cell;
		double height = Round(width / Math.Sqrt(3));
		string w = Number(width), h = Number(height), mid = Number(width / 2);
		string body = $"<path d='M0 0V{h}M{w} 0V{h}M{mid} 0V{h}M0 0L{w} {h}M0 {h}L{w} 0' "
			+ $"stroke='{MaskInk}' stroke-width='{F(Stroke)}' fill='none'/>";
		return new PatternMask(Svg(width, height, body), null, width, height);
	}

	private PatternMask GenerateTrianglesSvg()
	{
		double side = Cell;
		double row = Round(side * Math.Sqrt(3) / 2);
		string w = Number(side), mid = Number(side / 2), h = Number(row), h2 = Number(row * 2);
		string body =
			$"<path d='M0 0L{mid} {h}L{w} 0ZM0 {h}H{mid}L0 {h2}ZM{mid} {h}H{w}V{h2}Z' fill='{MaskInk}' fill-opacity='.3'/>"
			+ $"<path d='M0 0H{w}M0 {h}H{w}M0 {h2}H{w}M0 0L{w} {h2}M{w} 0L0 {h2}' "
			+ $"stroke='{MaskInk}' stroke-width='{F(Stroke)}' fill='none'/>";
		return new PatternMask(Svg(side, row * 2, body), null, side, row * 2);
	}

	private PatternMask GenerateDiamondsSvg()
	{
		double width = Cell;
		double height = Round(width * 7 / 12);
		string w = Number(width), h = Number(height), midX = Number(width / 2), midY = Number(height / 2);
		string r = Number(Radius * 1.3);
		var body = new StringBuilder()
			.Append(CultureInfo.InvariantCulture, $"<path d='M{midX} 0L{w} {midY}L{midX} {h}L0 {midY}Z' stroke='{MaskInk}' stroke-width='{F(Stroke)}' fill='none'/>");
		foreach ((string x, string y) in new[] { (midX, midY), ("0", "0"), (w, "0"), ("0", h), (w, h) })
		{
			body.Append(CultureInfo.InvariantCulture, $"<circle cx='{x}' cy='{y}' r='{r}' fill='{MaskInk}'/>");
		}

		return new PatternMask(Svg(width, height, body.ToString()), null, width, height);
	}

	private PatternMask GenerateCircuitSvg()
	{
		const double Design = 96;
		double cell = Cell;
		string stroke = Number(Stroke * Design / cell);
		string r = Number(Radius * 2.2);
		var body = new StringBuilder()
			.Append("<path d='M0 24.5H28.5L40.5 36.5H62.5M78.5 24.5H96M20.5 96V74.5L32.5 62.5V50.5M20.5 0V10.5")
			.Append("M96 72.5H86.5L74.5 84.5V96M0 72.5H8.5M74.5 0V8.5M46.5 60.5H58.5L66.5 52.5' ")
			.Append(CultureInfo.InvariantCulture, $"stroke='{MaskInk}' stroke-width='{stroke}' fill='none'/>");
		foreach ((string x, string y) in CircuitPads)
		{
			body.Append(CultureInfo.InvariantCulture, $"<circle cx='{x}' cy='{y}' r='{r}' fill='{MaskInk}'/>");
		}

		return new PatternMask(Svg(cell, cell, body.ToString(), "0 0 96 96"), null, cell, cell);
	}

	private PatternMask GenerateCornerMarksSvg()
	{
		const double Design = 48;
		double cell = Cell;
		string body = "<path d='M5.5 12V5.5H12M36 5.5H42.5V12M42.5 36V42.5H36M12 42.5H5.5V36' "
			+ $"stroke='{MaskInk}' stroke-width='{Number(Stroke * Design / cell)}' fill='none'/>";
		return new PatternMask(Svg(cell, cell, body, "0 0 48 48"), null, cell, cell);
	}

	private PatternMask GenerateGrainSvg()
	{
		double cell = Cell;
		string size = Number(cell);
		string body = "<filter id='g' x='0' y='0' width='100%' height='100%'>"
			+ "<feTurbulence type='fractalNoise' baseFrequency='.8' numOctaves='3' stitchTiles='stitch'/>"
			+ "<feColorMatrix values='0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 3.2 0 0 0 -1.25'/>"
			+ $"</filter><rect width='{size}' height='{size}' filter='url(#g)'/>";
		return new PatternMask(Svg(cell, cell, body), null, cell, cell);
	}

	// The contours scale with the box, so the stroke keeps its width in screen pixels rather than growing
	// with them.
	private PatternMask GenerateTopographicSvg()
	{
		string body = $"<path d='{TopographicPath}' stroke='{MaskInk}' stroke-width='{F(Stroke)}' "
			+ "vector-effect='non-scaling-stroke' fill='none'/>";
		return new PatternMask(Svg(400, 170, body, "0 0 400 170"), "cover", 0, 0, "no-repeat", "center");
	}

	private static string BuildContours()
	{
		var path = new StringBuilder();
		AddHill(path, 92, 58, 10, 18.4, 0.4);
		AddHill(path, 322, 128, 7, 17, 2.1);
		return path.ToString();
	}

	// Each ring is a closed path of short relative steps in tenths of a unit, because the whole image goes
	// into the style twice, percent-encoded. Steps are taken between rounded points, so nothing drifts.
	private static void AddHill(StringBuilder path, double centerX, double centerY, int rings, double step, double phase)
	{
		const int Points = 48;
		for (int ring = 0; ring < rings; ring++)
		{
			double radius = 13 + step * ring;
			int lastX = 0, lastY = 0;
			for (int i = 0; i < Points; i++)
			{
				double angle = 2 * Math.PI * i / Points;
				double wobble = 1 + 0.07 * Math.Sin(3 * angle + phase + 0.35 * ring) + 0.05 * Math.Cos(2 * angle - phase);
				int x = (int)Math.Round((centerX + radius * wobble * Math.Cos(angle)) * 10);
				int y = (int)Math.Round((centerY + 0.64 * radius * wobble * Math.Sin(angle)) * 10);
				if (i == 0)
				{
					path.Append('M').Append(Tenths(x)).Append(' ').Append(Tenths(y)).Append('l');
				}
				else
				{
					AppendStep(path, Tenths(x - lastX), i > 1);
					AppendStep(path, Tenths(y - lastY), true);
				}

				lastX = x;
				lastY = y;
			}

			path.Append('z');
		}
	}

	private static void AppendStep(StringBuilder path, string number, bool separate)
	{
		if (separate && number[0] != '-')
		{
			path.Append(' ');
		}

		path.Append(number);
	}

	private static string Tenths(int tenths) =>
		(tenths / 10.0).ToString("0.#", CultureInfo.InvariantCulture);

	private readonly record struct PatternMask(
		string Image,
		string? Size,
		double TileWidth,
		double TileHeight,
		string? Repeat = null,
		string? Position = null);
}
