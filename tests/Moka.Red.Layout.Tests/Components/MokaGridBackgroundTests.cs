using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.GridBackground;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Layout.Tests.Components;

// PatternColor and DashArray went into the SVG patterns' data URIs unchecked, so a quote ended the SVG
// attribute and added attributes or elements to the image. PatternColor, HighlightColor and
// BackgroundColor also went into gradients and declarations as they were, where a parenthesis or a
// semicolon added layers or declarations of its own.
public partial class MokaGridBackgroundTests : BunitContext
{
	private const string DefaultSvgColor = "rgba(239, 83, 80, 0.25)";

	private static readonly string[] TileElements = ["svg", "line", "polygon"];

	private static readonly string[] TileAttributes =
		["width", "height", "x1", "y1", "x2", "y2", "points", "fill", "stroke", "stroke-width", "stroke-dasharray"];

	private static readonly string[] UnsizedPatternStyle = ["background-image", "opacity", "-webkit-mask-image", "mask-image"];

	private static readonly string[] SizedPatternStyle =
		["background-image", "background-size", "opacity", "-webkit-mask-image", "mask-image"];

	public static TheoryData<string> HostileColors =>
	[
		"red' onload='alert(1)",
		"red\" onload=\"alert(1)",
		"red'/><image href='https://example.com/x.png'/><line stroke='red",
		"red) 1px, url(https://example.com/track.png",
		"red; position: fixed; inset: 0",
		"rgb(0 0 0)), url(https://example.com/track.png"
	];

	public static TheoryData<MokaGridPattern, string> HostileColorsForSvgPatterns
	{
		get
		{
			TheoryData<MokaGridPattern, string> data = [];
			foreach (MokaGridPattern pattern in new[]
			         {
				         MokaGridPattern.Dashed, MokaGridPattern.Cross, MokaGridPattern.Honeycomb
			         })
			{
				foreach (string color in HostileColors)
				{
					data.Add(pattern, color);
				}
			}

			return data;
		}
	}

	private static IElement PatternLayer(IRenderedComponent<MokaGridBackground> cut) =>
		cut.Find(".moka-grid-bg__pattern");

	private static string PatternStyle(IRenderedComponent<MokaGridBackground> cut) =>
		PatternLayer(cut).GetAttribute("style") ?? "";

	// The SVG sits percent-encoded in a url("data:...") in the pattern layer's style. Parsing it proves it
	// is well formed; the element and attribute checks prove a value added nothing to it.
	private static XDocument Tile(IRenderedComponent<MokaGridBackground> cut)
	{
		Match match = TileUrl().Match(PatternStyle(cut));
		Assert.True(match.Success, "The pattern style holds no SVG tile.");

		XDocument svg = XDocument.Parse(Uri.UnescapeDataString(match.Groups["svg"].Value));
		foreach (XElement element in svg.Descendants())
		{
			Assert.Contains(element.Name.LocalName, TileElements);
			foreach (XAttribute attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
			{
				Assert.Contains(attribute.Name.LocalName, TileAttributes);
			}
		}

		return svg;
	}

	private static IEnumerable<XElement> Strokes(XDocument svg) =>
		svg.Descendants().Where(e => e.Name.LocalName is "line" or "polygon");

	[Theory]
	[MemberData(nameof(HostileColorsForSvgPatterns))]
	public void AHostileColor_LeavesTheSvgPatternWellFormed_AndDrawsTheDefault(MokaGridPattern pattern, string color)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.PatternColor, color));

		XDocument svg = Tile(cut);

		Assert.NotEmpty(Strokes(svg));
		Assert.All(Strokes(svg), e => Assert.Equal(DefaultSvgColor, e.Attribute("stroke")?.Value));
		Assert.Equal(UnsizedPatternStyle, CssDeclarations.PropertyNames(PatternStyle(cut)));
	}

	[Theory]
	[MemberData(nameof(HostileColors))]
	public void AHostileColor_StaysOutOfTheCssPatterns(string color)
	{
		foreach (MokaGridPattern pattern in new[] { MokaGridPattern.Lines, MokaGridPattern.Dots })
		{
			IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
				.Add(x => x.Pattern, pattern)
				.Add(x => x.PatternColor, color));

			string style = PatternStyle(cut);
			Assert.Equal(SizedPatternStyle, CssDeclarations.PropertyNames(style));
			Assert.DoesNotContain("example.com", style, StringComparison.Ordinal);
			Assert.Contains("var(--moka-color-primary-border)", style, StringComparison.Ordinal);
		}
	}

	[Theory]
	[MemberData(nameof(HostileColors))]
	public void AHostileColor_StaysOutOfTheDiagonalPattern(string color)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, MokaGridPattern.DiagonalLines)
			.Add(x => x.PatternColor, color));

		string style = PatternStyle(cut);
		Assert.Equal(UnsizedPatternStyle, CssDeclarations.PropertyNames(style));
		Assert.DoesNotContain("example.com", style, StringComparison.Ordinal);
		Assert.Contains("var(--moka-color-primary-border)", style, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("4 4' onload='alert(1)")]
	[InlineData("4 4'/><image href='https://example.com/x.png'/><line x1='0")]
	[InlineData("4 4\" x=\"1")]
	[InlineData("4; 4")]
	[InlineData("-4 4")]
	[InlineData("calc(4px) 4")]
	public void AHostileDashArray_FallsBackToTheDefault(string dashArray)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, MokaGridPattern.Dashed)
			.Add(x => x.DashArray, dashArray));

		XDocument svg = Tile(cut);

		Assert.All(Strokes(svg), e => Assert.Equal("4 4", e.Attribute("stroke-dasharray")?.Value));
	}

	[Theory]
	[InlineData("6, 3", "6, 3")]
	[InlineData(" 2 1 ", "2 1")]
	[InlineData("8px 4px", "8px 4px")]
	public void AValidDashArray_IsDrawn(string dashArray, string expected)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, MokaGridPattern.Dashed)
			.Add(x => x.DashArray, dashArray));

		Assert.All(Strokes(Tile(cut)), e => Assert.Equal(expected, e.Attribute("stroke-dasharray")?.Value));
	}

	[Theory]
	[InlineData(MokaGridPattern.Dashed)]
	[InlineData(MokaGridPattern.Cross)]
	[InlineData(MokaGridPattern.Honeycomb)]
	public void AValidColor_IsDrawnInTheSvgPatterns(MokaGridPattern pattern)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, pattern)
			.Add(x => x.PatternColor, "#d32f2f"));

		Assert.All(Strokes(Tile(cut)), e => Assert.Equal("#d32f2f", e.Attribute("stroke")?.Value));
	}

	[Fact]
	public void AValidColor_IsDrawnInTheCssPatterns()
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, MokaGridPattern.Lines)
			.Add(x => x.PatternColor, "var(--moka-color-success)"));

		Assert.StartsWith("background-image: linear-gradient(to right, var(--moka-color-success) 1.0px, transparent 1.0px)",
			PatternStyle(cut), StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(HostileColors))]
	public void AHostileHighlightColor_FallsBackToTheDefault(string color)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Highlighted, true)
			.Add(x => x.HighlightColor, color));

		string style = cut.Find(".moka-grid-bg__highlight").GetAttribute("style") ?? "";
		Assert.Equal(["background"], CssDeclarations.PropertyNames(style));
		Assert.Equal("background: radial-gradient(ellipse at center, var(--moka-color-primary-glow-strong) 0%, transparent 60%)",
			style);
	}

	[Fact]
	public void AValidHighlightColor_IsUsed()
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.Highlighted, true)
			.Add(x => x.HighlightColor, "rgba(66, 165, 245, 0.3)"));

		Assert.Contains("radial-gradient(ellipse at center, rgba(66, 165, 245, 0.3) 0%",
			cut.Find(".moka-grid-bg__highlight").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(HostileColors))]
	public void AHostileBackgroundColor_IsDropped(string color)
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.BackgroundColor, color)
			.Add(x => x.MinHeight, "200px"));

		Assert.Equal(["min-height"], CssDeclarations.PropertyNames(cut.Find(".moka-grid-bg").GetAttribute("style") ?? ""));
	}

	[Fact]
	public void AValidBackgroundColor_IsUsed()
	{
		IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
			.Add(x => x.BackgroundColor, "#101015"));

		Assert.Equal("background-color: #101015", cut.Find(".moka-grid-bg").GetAttribute("style"));
	}

	// NaN and infinity went into the pattern as the words "NaN" and "Infinity", and a cell of zero or
	// less as a size CSS rejects, so the whole pattern disappeared.
	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void ANonFiniteNumber_FallsBackToItsDefault(double number)
	{
		IRenderedComponent<MokaGridBackground> lines = Render<MokaGridBackground>(p => p
			.Add(x => x.StrokeWidth, number)
			.Add(x => x.PatternOpacity, number));
		IRenderedComponent<MokaGridBackground> dots = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, MokaGridPattern.Dots)
			.Add(x => x.DotRadius, number));
		IRenderedComponent<MokaGridBackground> cross = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, MokaGridPattern.Cross)
			.Add(x => x.StrokeWidth, number));

		Assert.Equal(
			"background-image: linear-gradient(to right, var(--moka-color-primary-border) 1.0px, transparent 1.0px), "
			+ "linear-gradient(to bottom, var(--moka-color-primary-border) 1.0px, transparent 1.0px); "
			+ "background-size: 40px 40px; opacity: 0.70",
			PatternStyle(lines).Split("; -webkit-mask-image", StringSplitOptions.None)[0]);
		Assert.StartsWith("background-image: radial-gradient(circle, var(--moka-color-primary-border) 1.0px, transparent 1.0px)",
			PatternStyle(dots), StringComparison.Ordinal);
		Assert.All(Strokes(Tile(cross)), line => Assert.Equal("1.0", line.Attribute("stroke-width")?.Value));
		Assert.DoesNotContain("NaN", PatternStyle(lines) + PatternStyle(dots), StringComparison.Ordinal);
		Assert.DoesNotContain("Infinity", PatternStyle(lines) + PatternStyle(dots), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-24)]
	public void ACellSizeOfZeroOrLess_FallsBackToTheDefault(int cellSize)
	{
		IRenderedComponent<MokaGridBackground> lines = Render<MokaGridBackground>(p => p.Add(x => x.CellSize, cellSize));
		IRenderedComponent<MokaGridBackground> dashed = Render<MokaGridBackground>(p => p
			.Add(x => x.Pattern, MokaGridPattern.Dashed)
			.Add(x => x.CellSize, cellSize));

		Assert.Contains("background-size: 40px 40px", PatternStyle(lines), StringComparison.Ordinal);
		Assert.Equal("40", Tile(dashed).Root!.Attribute("width")?.Value);
	}

	// Numbers went in through the current culture, and some cultures write a minus sign CSS does not
	// read (U+2212 in Swedish), so a negative angle broke the diagonal pattern.
	[Fact]
	public void ANegativeAngle_IsWrittenWithAnAsciiMinusUnderAnyCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			IRenderedComponent<MokaGridBackground> cut = Render<MokaGridBackground>(p => p
				.Add(x => x.Pattern, MokaGridPattern.DiagonalLines)
				.Add(x => x.DiagonalAngle, -45)
				.Add(x => x.FadeStart, -10)
				.Add(x => x.Highlighted, true)
				.Add(x => x.HighlightRadius, -5));

			Assert.StartsWith("background-image: repeating-linear-gradient(-45deg,", PatternStyle(cut),
				StringComparison.Ordinal);
			Assert.Contains("black -10%", PatternStyle(cut), StringComparison.Ordinal);
			Assert.Contains("transparent -5%", cut.Find(".moka-grid-bg__highlight").GetAttribute("style"),
				StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[GeneratedRegex("url\\(\"data:image/svg\\+xml,(?<svg>[^\"]*)\"\\)")]
	private static partial Regex TileUrl();
}
