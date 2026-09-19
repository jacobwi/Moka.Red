using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Layout.Watermark;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// Text, Color, FontSize and ImageSrc went into the SVG tile or the style unescaped: "R&D" made the
// tile invalid XML, so nothing drew, and a quote let a value add attributes or declarations. Gap was
// never read, the tile was a fixed 300 by 200 that cut long text off, and the text had no font.
public partial class MokaWatermarkTests : BunitContext
{
	private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

	private static readonly string[] TileElements = ["svg", "text"];

	private static IElement Overlay(IRenderedComponent<MokaWatermark> cut) => cut.Find(".moka-watermark__overlay");

	private static string OverlayStyle(IRenderedComponent<MokaWatermark> cut) =>
		Overlay(cut).GetAttribute("style") ?? "";

	// The tile sits percent-encoded in a url("data:...") in the overlay's style. Parsing it proves it
	// is well formed; the element and attribute checks prove a value added nothing to it.
	private static XDocument Tile(IRenderedComponent<MokaWatermark> cut)
	{
		Match match = TileUrl().Match(OverlayStyle(cut));
		Assert.True(match.Success, "The overlay style holds no SVG tile.");

		XDocument svg = XDocument.Parse(Uri.UnescapeDataString(match.Groups["svg"].Value));
		foreach (XElement element in svg.Descendants())
		{
			Assert.Contains(element.Name.LocalName, TileElements);
			Assert.DoesNotContain(element.Attributes(), a => a.Name.LocalName.StartsWith("on", StringComparison.Ordinal));
		}

		return svg;
	}

	private static XElement TileText(IRenderedComponent<MokaWatermark> cut) =>
		Tile(cut).Descendants(Svg + "text").Single();

	private static int TileWidth(IRenderedComponent<MokaWatermark> cut) =>
		int.Parse(Tile(cut).Root!.Attribute("width")!.Value, CultureInfo.InvariantCulture);

	private static int TileHeight(IRenderedComponent<MokaWatermark> cut) =>
		int.Parse(Tile(cut).Root!.Attribute("height")!.Value, CultureInfo.InvariantCulture);

	[Fact]
	public void TextWithMarkupCharacters_IsDrawnAsText()
	{
		const string text = "R&D <b>\"Q3\"</b> 'draft'";
		IRenderedComponent<MokaWatermark> cut = Render<MokaWatermark>(p => p.Add(x => x.Text, text));

		Assert.Equal(text, TileText(cut).Value);
	}

	[Theory]
	[InlineData("red' onload='alert(1)")]
	[InlineData("red\" onload=\"alert(1)")]
	[InlineData("red; position: fixed; inset: 0")]
	[InlineData("url(https://example.com/track.png)")]
	public void AHostileColor_IsDropped(string color)
	{
		IRenderedComponent<MokaWatermark> cut = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "DRAFT")
			.Add(x => x.Color, color));

		Assert.Null(TileText(cut).Attribute("fill"));
		Assert.Equal(["--_watermark-tile", "opacity"], CssDeclarations.PropertyNames(OverlayStyle(cut)));
	}

	[Theory]
	[InlineData("48px' onload='alert(1)")]
	[InlineData("48px\"><script>alert(1)</script>")]
	[InlineData("huge")]
	public void AHostileFontSize_FallsBackToTheDefault(string fontSize)
	{
		IRenderedComponent<MokaWatermark> cut = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "DRAFT")
			.Add(x => x.FontSize, fontSize));

		Assert.Equal("48", TileText(cut).Attribute("font-size")?.Value);
	}

	[Theory]
	[InlineData("x.png'); background-color: red; content: url('y")]
	[InlineData("x.png\"); background-color: red; content: url(\"y")]
	[InlineData("x.png\\\"); background-color: red")]
	[InlineData("x.png\n); background-color: red")]
	public void AHostileImageSrc_StaysInsideTheUrl(string src)
	{
		IRenderedComponent<MokaWatermark> cut = Render<MokaWatermark>(p => p.Add(x => x.ImageSrc, src));

		Assert.Equal(["background-image", "background-repeat", "background-position", "background-size", "opacity"],
			CssDeclarations.PropertyNames(OverlayStyle(cut)));
	}

	[Fact]
	public void Gap_SpacesTheTiles()
	{
		IRenderedComponent<MokaWatermark> tight = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "DRAFT")
			.Add(x => x.Gap, "0px"));
		IRenderedComponent<MokaWatermark> loose = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "DRAFT")
			.Add(x => x.Gap, "200px"));

		Assert.Equal(200, TileWidth(loose) - TileWidth(tight));
		Assert.Equal(200, TileHeight(loose) - TileHeight(tight));
	}

	[Fact]
	public void TheTile_IsWideEnoughForLongText()
	{
		// Twelve capitals at 48px run about 330 to 350px in common sans-serif fonts.
		IRenderedComponent<MokaWatermark> cut = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "CONFIDENTIAL")
			.Add(x => x.Rotation, 0)
			.Add(x => x.Gap, "0px"));

		Assert.InRange(TileWidth(cut), 360, 480);
	}

	[Fact]
	public void TheText_UsesTheThemeFont()
	{
		MokaTheme theme = MokaTheme.Dark with
		{
			Typography = MokaTypography.Default with { FontFamily = "'Fira Sans', sans-serif" }
		};

		IRenderedComponent<MokaWatermark> themed = Render<MokaWatermark>(p => p
			.AddCascadingValue(theme)
			.Add(x => x.Text, "DRAFT"));
		IRenderedComponent<MokaWatermark> plain = Render<MokaWatermark>(p => p.Add(x => x.Text, "DRAFT"));

		Assert.Equal("'Fira Sans', sans-serif", TileText(themed).Attribute("font-family")?.Value);
		Assert.Equal(MokaTypography.Default.FontFamily, TileText(plain).Attribute("font-family")?.Value);
	}

	// Unset, the color comes from the component CSS, which uses --moka-color-on-surface.
	[Fact]
	public void TheColor_IsCssOnTheOverlay_SoTokensWork()
	{
		IRenderedComponent<MokaWatermark> plain = Render<MokaWatermark>(p => p.Add(x => x.Text, "DRAFT"));
		IRenderedComponent<MokaWatermark> tinted = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "DRAFT")
			.Add(x => x.Color, "var(--moka-color-primary)"));

		Assert.Contains("moka-watermark__overlay--text", Overlay(plain).ClassName, StringComparison.Ordinal);
		Assert.DoesNotContain("background-color", OverlayStyle(plain), StringComparison.Ordinal);
		Assert.Contains("background-color: var(--moka-color-primary)", OverlayStyle(tinted), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(5, "1")]
	[InlineData(-1, "0")]
	[InlineData(double.NaN, "0.08")]
	public void Opacity_StaysBetweenZeroAndOne(double opacity, string expected)
	{
		IRenderedComponent<MokaWatermark> cut = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "DRAFT")
			.Add(x => x.Opacity, opacity));

		Assert.EndsWith($"opacity: {expected}", OverlayStyle(cut), StringComparison.Ordinal);
	}

	[Fact]
	public void Center_DrawsTheMarkOnce()
	{
		IRenderedComponent<MokaWatermark> cut = Render<MokaWatermark>(p => p
			.Add(x => x.Text, "DRAFT")
			.Add(x => x.Position, MokaWatermarkPosition.Center));

		Assert.Contains("moka-watermark__overlay--center", Overlay(cut).ClassName, StringComparison.Ordinal);
	}

	[GeneratedRegex("url\\(\"data:image/svg\\+xml,(?<svg>[^\"]*)\"\\)")]
	private static partial Regex TileUrl();
}
