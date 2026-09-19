using System.Xml.Linq;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Barcode;

namespace Moka.Red.Primitives.Tests.Components;

// The colors and the text size went into the SVG unescaped, and the SVG is rendered raw: a quote
// ended the attribute and whatever followed became live markup.
public class MokaBarcodeTests : BunitContext
{
	public static TheoryData<string> HostileValues => GeneratedSvg.HostileValues;

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void HostileColorsAndTextSize_LeaveTheSvgWellFormedAndInert(string hostile)
	{
		IRenderedComponent<MokaBarcode> cut = Render<MokaBarcode>(p => p
			.Add(x => x.Value, "INV-0142")
			.Add(x => x.ForegroundColor, hostile)
			.Add(x => x.BackgroundColor, hostile)
			.Add(x => x.TextSize, hostile));

		XDocument svg = GeneratedSvg.Parse(cut.Markup);

		XElement[] rects = GeneratedSvg.Rects(svg).ToArray();
		Assert.Equal("#ffffff", GeneratedSvg.Fill(rects[0]));
		Assert.All(rects.Skip(1), bar => Assert.Equal("#000000", GeneratedSvg.Fill(bar)));
		XElement text = svg.Descendants(GeneratedSvg.Ns + "text").Single();
		Assert.Equal("12px", text.Attribute("font-size")?.Value);
		Assert.Equal("INV-0142", text.Value);
	}

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void HostileColors_LeaveTheErrorSvgWellFormedAndInert(string hostile)
	{
		IRenderedComponent<MokaBarcode> cut = Render<MokaBarcode>(p => p
			.Add(x => x.Value, "4006381333932")
			.Add(x => x.BarcodeFormat, MokaBarcodeFormat.EAN13)
			.Add(x => x.ForegroundColor, hostile)
			.Add(x => x.BackgroundColor, hostile));

		XDocument svg = GeneratedSvg.Parse(cut.Markup);

		Assert.Equal("#ffffff", GeneratedSvg.Fill(GeneratedSvg.Rects(svg).Single()));
		XElement text = svg.Descendants(GeneratedSvg.Ns + "text").Single();
		Assert.Equal("#000000", GeneratedSvg.Fill(text));
		Assert.Contains("check digit", text.Value, StringComparison.Ordinal);
	}

	[Fact]
	public void AnUnsupportedCharacter_IsQuotedAsTextInTheError()
	{
		IRenderedComponent<MokaBarcode> cut = Render<MokaBarcode>(p => p
			.Add(x => x.Value, "A<B")
			.Add(x => x.BarcodeFormat, MokaBarcodeFormat.Code39));

		XDocument svg = GeneratedSvg.Parse(cut.Markup);

		Assert.Contains("'<'", svg.Descendants(GeneratedSvg.Ns + "text").Single().Value, StringComparison.Ordinal);
	}

	[Fact]
	public void ColorFunctionsAndLengths_AreKept()
	{
		IRenderedComponent<MokaBarcode> cut = Render<MokaBarcode>(p => p
			.Add(x => x.Value, "PKG-88213")
			.Add(x => x.ForegroundColor, "rgb(26 26 34)")
			.Add(x => x.BackgroundColor, "var(--moka-color-surface, #f5f5f7)")
			.Add(x => x.TextSize, "0.8rem"));

		XDocument svg = GeneratedSvg.Parse(cut.Markup);

		XElement[] rects = GeneratedSvg.Rects(svg).ToArray();
		Assert.Equal("var(--moka-color-surface, #f5f5f7)", GeneratedSvg.Fill(rects[0]));
		Assert.Equal("rgb(26 26 34)", GeneratedSvg.Fill(rects[1]));
		Assert.Equal("0.8rem", svg.Descendants(GeneratedSvg.Ns + "text").Single().Attribute("font-size")?.Value);
	}
}
