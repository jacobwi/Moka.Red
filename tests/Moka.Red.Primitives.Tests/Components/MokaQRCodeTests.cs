using System.Xml.Linq;
using Bunit;
using Moka.Red.Core.Enums;
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
			.Add(x => x.Value, new string('x', 2960))
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

	// The component is only as scannable as the SVG it draws: every dark rect must land on the
	// generator's dark module, at its quiet-zone offset, and nowhere else.
	[Fact]
	public void TheRenderedSvg_PaintsExactlyTheGeneratedGrid_AndScans()
	{
		const string value = "https://example.com/orders/10442";
		const int qrSize = 200;
		const int quietZone = 4;

		IRenderedComponent<MokaQRCode> cut = Render<MokaQRCode>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.QRSize, qrSize)
			.Add(x => x.QuietZone, quietZone));

		bool[][] expected = QRCodeGenerator.Generate(value, MokaQRErrorCorrection.Medium);
		int gridSize = expected.Length;
		double moduleSize = (double)qrSize / (gridSize + quietZone * 2);

		bool[][] painted = new bool[gridSize][];
		for (int i = 0; i < gridSize; i++)
		{
			painted[i] = new bool[gridSize];
		}

		XDocument svg = GeneratedSvg.Parse(cut.Markup);
		foreach (XElement rect in GeneratedSvg.Rects(svg).Skip(1))
		{
			double x = double.Parse(rect.Attribute("x")!.Value, System.Globalization.CultureInfo.InvariantCulture);
			double y = double.Parse(rect.Attribute("y")!.Value, System.Globalization.CultureInfo.InvariantCulture);
			int col = (int)Math.Round(x / moduleSize) - quietZone;
			int row = (int)Math.Round(y / moduleSize) - quietZone;

			Assert.InRange(row, 0, gridSize - 1);
			Assert.InRange(col, 0, gridSize - 1);
			painted[row][col] = true;
		}

		Assert.Equal(expected, painted);
		Assert.Equal(value, QRCodeDecodeTests.Decode(expected));
	}
}
