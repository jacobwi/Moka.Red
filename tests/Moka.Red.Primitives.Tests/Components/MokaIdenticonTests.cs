using System.Xml.Linq;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Identicon;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaIdenticonTests : BunitContext
{
	public static TheoryData<string> HostileValues => GeneratedSvg.HostileValues;

	private static XElement[] Cells(IRenderedComponent<MokaIdenticon> cut) =>
		GeneratedSvg.Rects(GeneratedSvg.Parse(cut.Markup)).Where(r => r.Attribute("width")?.Value == "10").ToArray();

	private static XElement? Background(IRenderedComponent<MokaIdenticon> cut) =>
		GeneratedSvg.Rects(GeneratedSvg.Parse(cut.Markup)).FirstOrDefault(r => r.Attribute("width")?.Value != "10");

	// The palette and background went into the SVG unescaped, and the SVG is rendered raw.
	[Theory]
	[MemberData(nameof(HostileValues))]
	public void HostilePaletteAndBackground_LeaveTheSvgWellFormedAndInert(string hostile)
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.Palette, [hostile])
			.Add(x => x.Background, hostile));

		XElement[] cells = Cells(cut);

		Assert.NotEmpty(cells);
		Assert.Null(Background(cut));
		Assert.All(cells, cell => Assert.Matches("^#[0-9a-f]{6}$", GeneratedSvg.Fill(cell)));
	}

	// The SVG was cached on Value alone, so the other drawing parameters changed nothing on screen.
	[Fact]
	public void ANewGridSize_Redraws()
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.IdenticonSize, 5));

		cut.Render(p => p.Add(x => x.IdenticonSize, 8));

		Assert.Equal("0 0 80 80", cut.Find("svg").GetAttribute("viewBox"));
	}

	[Fact]
	public void ANewPalette_Redraws()
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.Palette, ["#111111"]));

		cut.Render(p => p.Add(x => x.Palette, ["#222222"]));

		Assert.All(Cells(cut), cell => Assert.Equal("#222222", GeneratedSvg.Fill(cell)));
	}

	[Fact]
	public void APaletteChangedInPlace_Redraws()
	{
		List<string> palette = ["#111111"];
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.Palette, palette));

		palette[0] = "#333333";
		cut.Render(p => p.Add(x => x.Palette, palette));

		Assert.All(Cells(cut), cell => Assert.Equal("#333333", GeneratedSvg.Fill(cell)));
	}

	[Fact]
	public void ANewBackground_Redraws()
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.Background, "#eceff1"));

		cut.Render(p => p.Add(x => x.Background, "#101015"));

		Assert.Equal("#101015", GeneratedSvg.Fill(Background(cut)!));
	}

	[Fact]
	public void ANullBackground_DrawsNone()
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.Background, (string?)null));

		Assert.Null(Background(cut));
	}

	// Only Full clipped the image. Every other radius went on the wrapper, and the SVG's corner cells
	// covered it.
	[Theory]
	[InlineData(MokaRounding.Sm)]
	[InlineData(MokaRounding.Md)]
	[InlineData(MokaRounding.Full)]
	public void AnyRounding_ClipsTheImage(MokaRounding rounding)
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.Rounded, rounding));

		IElement root = cut.Find(".moka-identicon");
		Assert.Contains("moka-identicon--rounded", root.ClassName, StringComparison.Ordinal);
		Assert.Contains("border-radius:", root.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void ACustomRadius_ClipsTheImage()
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p
			.Add(x => x.Value, "mara.okafor")
			.Add(x => x.RoundedValue, "6px"));

		Assert.Contains("moka-identicon--rounded", cut.Find(".moka-identicon").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void NoRounding_DoesNotClip()
	{
		IRenderedComponent<MokaIdenticon> cut = Render<MokaIdenticon>(p => p.Add(x => x.Value, "mara.okafor"));

		Assert.DoesNotContain("moka-identicon--rounded", cut.Find(".moka-identicon").ClassName,
			StringComparison.Ordinal);
	}
}
