using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Watermark;

/// <summary>
///     Subtle text or image watermark overlay on content.
///     Uses CSS with SVG data URIs for tiled text, no JavaScript required.
/// </summary>
public partial class MokaWatermark : MokaComponentBase
{
	private const double DefaultOpacity = 0.08;

	private static readonly string DefaultFontFamily = MokaTypography.Default.FontFamily;

	/// <summary>The content to watermark.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Watermark text (e.g., "CONFIDENTIAL", "DRAFT").</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Watermark image URL. Overrides <see cref="Text" /> when set.</summary>
	[Parameter]
	public string? ImageSrc { get; set; }

	/// <summary>Opacity of the watermark, from 0 to 1. Values outside that range are clamped. Default 0.08.</summary>
	[Parameter]
	public double Opacity { get; set; } = DefaultOpacity;

	/// <summary>Rotation angle in degrees. Default -30.</summary>
	[Parameter]
	public int Rotation { get; set; } = -30;

	/// <summary>
	///     Font size for text watermarks: a number with px, pt, pc, in, cm, mm, q, em or rem (em and rem
	///     count 16px). Anything else uses the default. Default "48px".
	/// </summary>
	[Parameter]
	public string FontSize { get; set; } = "48px";

	/// <summary>
	///     Space between repeated text watermarks, with the same units as <see cref="FontSize" />. Each tile
	///     is sized to hold its text plus this gap. Default "100px".
	/// </summary>
	[Parameter]
	public string Gap { get; set; } = "100px";

	/// <summary>
	///     Color for the watermark text: a hex value, a color keyword or a color function such as
	///     <c>rgb()</c> or <c>var(--moka-color-primary)</c>. Unset, or not a color, uses the theme's
	///     on-surface color.
	/// </summary>
	[Parameter]
	public string? Color { get; set; }

	/// <summary>Whether to repeat the watermark across the content. Default true.</summary>
	[Parameter]
	public bool Repeat { get; set; } = true;

	/// <summary>Watermark positioning mode. Default <see cref="MokaWatermarkPosition.Tiled" />.</summary>
	[Parameter]
	public MokaWatermarkPosition Position { get; set; } = MokaWatermarkPosition.Tiled;

	/// <inheritdoc />
	protected override string RootClass => "moka-watermark";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private bool HasImage => !string.IsNullOrEmpty(ImageSrc);

	private bool HasText => !HasImage && !string.IsNullOrEmpty(Text);

	private string OpacityValue =>
		(double.IsFinite(Opacity) ? Math.Clamp(Opacity, 0, 1) : DefaultOpacity)
		.ToString("0.###", CultureInfo.InvariantCulture);

	private string OverlayClass => new CssBuilder("moka-watermark__overlay")
		.AddClass("moka-watermark__overlay--text", HasText)
		.AddClass("moka-watermark__overlay--center", HasText && Position == MokaWatermarkPosition.Center)
		.Build();

	private string? OverlayStyle
	{
		get
		{
			if (HasImage)
			{
				return new StyleBuilder()
					.AddStyle("background-image", CssValues.Url(ImageSrc!))
					.AddStyle("background-repeat", Repeat ? "repeat" : "no-repeat")
					.AddStyle("background-position", "center")
					.AddStyle("background-size", Position == MokaWatermarkPosition.Center ? "contain" : "auto")
					.AddStyle("opacity", OpacityValue)
					.Build();
			}

			if (HasText)
			{
				// The text is drawn into an image used as a mask over a filled layer, so its color is a plain
				// CSS value: a token or var() resolves against the page, which it cannot inside the image.
				return new StyleBuilder()
					.AddStyle("--_watermark-tile", TextTile())
					.AddStyle("background-color", CssValues.IsColor(Color) ? Color.Trim() : null)
					.AddStyle("opacity", OpacityValue)
					.Build();
			}

			return null;
		}
	}

	private string TextTile()
	{
		double fontSize = CssValues.TryParsePixels(FontSize, out double size) && size > 0
			? size
			: WatermarkTile.DefaultFontSizePx;
		double gap = CssValues.TryParsePixels(Gap, out double gapPx) ? gapPx : WatermarkTile.DefaultGapPx;

		// The image cannot load the page's web fonts, but it names the theme's stack, so an installed
		// copy or the system sans-serif draws the text instead of the default serif.
		string fontFamily = Theme?.Typography.FontFamily ?? DefaultFontFamily;

		return WatermarkTile.Build(Text!, fontSize, gap, Rotation, fontFamily);
	}
}

/// <summary>
///     Positioning mode for the watermark overlay.
/// </summary>
public enum MokaWatermarkPosition
{
	/// <summary>Single centered watermark.</summary>
	Center,

	/// <summary>Repeated grid of watermarks.</summary>
	Tiled
}
