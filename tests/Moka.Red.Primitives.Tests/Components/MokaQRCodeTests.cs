using System.Xml.Linq;
using Bunit;
using Moka.Red.Primitives.QRCode;

namespace Moka.Red.Primitives.Tests.Components;

// Both colors went into the SVG unescaped, and the SVG is rendered raw.
public class MokaQRCodeTests : BunitContext
{
	public static TheoryData<string> HostileValues => GeneratedSvg.HostileValues;

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void HostileColors_LeaveTheSvgWellFormedAndInert(string hostile)
	{
		IRenderedComponent<MokaQRCode> cut = Render<MokaQRCode>(p => p
			.Add(x => x.Value, "https://example.com/orders/10442")
			.Add(x => x.ForegroundColor, hostile)
			.Add(x => x.BackgroundColor, hostile)
			.Add(x => x.RoundedModules, true));

		XDocument svg = GeneratedSvg.Parse(cut.Markup);

		XElement[] rects = GeneratedSvg.Rects(svg).ToArray();
		Assert.Equal("#ffffff", GeneratedSvg.Fill(rects[0]));
		Assert.All(rects.Skip(1), module => Assert.Equal("#000000", GeneratedSvg.Fill(module)));
	}

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void HostileColors_LeaveTheTooLongSvgWellFormedAndInert(string hostile)
	{
		IRenderedComponent<MokaQRCode> cut = Render<MokaQRCode>(p => p
			.Add(x => x.Value, new string('x', 400))
			.Add(x => x.ForegroundColor, hostile)
			.Add(x => x.BackgroundColor, hostile));

		XDocument svg = GeneratedSvg.Parse(cut.Markup);

		Assert.Equal("#ffffff", GeneratedSvg.Fill(GeneratedSvg.Rects(svg).Single()));
		XElement text = svg.Descendants(GeneratedSvg.Ns + "text").Single();
		Assert.Equal("#000000", GeneratedSvg.Fill(text));
		Assert.Equal("Data too long", text.Value);
	}

	[Fact]
	public void ValidColors_AreKept()
	{
		IRenderedComponent<MokaQRCode> cut = Render<MokaQRCode>(p => p
			.Add(x => x.Value, "https://example.com/menu")
			.Add(x => x.ForegroundColor, "#c62828")
			.Add(x => x.BackgroundColor, "hsl(45 100% 96%)"));

		XDocument svg = GeneratedSvg.Parse(cut.Markup);

		XElement[] rects = GeneratedSvg.Rects(svg).ToArray();
		Assert.Equal("hsl(45 100% 96%)", GeneratedSvg.Fill(rects[0]));
		Assert.All(rects.Skip(1), module => Assert.Equal("#c62828", GeneratedSvg.Fill(module)));
	}
}
