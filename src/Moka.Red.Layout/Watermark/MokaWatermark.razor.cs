using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Watermark;

/// <summary>
///     Subtle text or image watermark overlay on content.
///     Uses CSS with SVG data URIs for tiled text — no JavaScript required.
/// </summary>
public partial class MokaWatermark : MokaComponentBase
{
	/// <summary>The content to watermark.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Watermark text (e.g., "CONFIDENTIAL", "DRAFT").</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Watermark image URL. Overrides <see cref="Text" /> when set.</summary>
	[Parameter]
	public string? ImageSrc { get; set; }

	/// <summary>Opacity of the watermark. Default 0.08.</summary>
	[Parameter]
	public double Opacity { get; set; } = 0.08;

	/// <summary>Rotation angle in degrees. Default -30.</summary>
	[Parameter]
	public int Rotation { get; set; } = -30;

	/// <summary>Font size for text watermarks. Default "48px".</summary>
	[Parameter]
	public string FontSize { get; set; } = "48px";

	/// <summary>Gap between repeated watermarks. Default "100px".</summary>
	[Parameter]
	public string Gap { get; set; } = "100px";

	/// <summary>Color for the watermark text. Defaults to on-surface color.</summary>
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

	private string? OverlayStyle
	{
		get
		{
			if (!string.IsNullOrEmpty(ImageSrc))
			{
				return new StyleBuilder()
					.AddStyle("background-image", $"url('{ImageSrc}')")
					.AddStyle("background-repeat", Repeat ? "repeat" : "no-repeat")
					.AddStyle("background-position", "center")
					.AddStyle("background-size", Position == MokaWatermarkPosition.Center ? "contain" : "auto")
					.AddStyle("opacity", Opacity.ToString("F2", CultureInfo.InvariantCulture))
					.Build();
			}

			if (!string.IsNullOrEmpty(Text))
			{
				if (Position == MokaWatermarkPosition.Center)
				{
					return new StyleBuilder()
						.AddStyle("background-image", GenerateWatermarkBackground())
						.AddStyle("background-repeat", "no-repeat")
						.AddStyle("background-position", "center")
						.Build();
				}

				return new StyleBuilder()
					.AddStyle("background-image", GenerateWatermarkBackground())
					.AddStyle("background-repeat", "repeat")
					.Build();
			}

			return null;
		}
	}

	private string GenerateWatermarkBackground()
	{
		string svgText = Text ?? "";
		string colorCss = Color ?? "rgba(0,0,0,1)";
		string svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='300' height='200'>" +
		             $"<text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' " +
		             $"transform='rotate({Rotation} 150 100)' fill='{colorCss}' " +
		             $"font-size='{FontSize}' opacity='{Opacity.ToString("F2", CultureInfo.InvariantCulture)}'>{svgText}</text></svg>";
		return $"url(\"data:image/svg+xml,{Uri.EscapeDataString(svg)}\")";
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
