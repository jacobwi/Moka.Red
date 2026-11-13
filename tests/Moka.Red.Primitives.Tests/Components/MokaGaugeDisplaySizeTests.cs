using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.Gauge;

namespace Moka.Red.Primitives.Tests.Components;

// A Display's size followed the ring's width, so a large grade needed a large ring: in a 64 px ring a letter was 18 px.
// DisplaySize sets it apart from the ring.
public class MokaGaugeDisplaySizeTests : BunitContext
{
	private IElement Display(string? size, string? display = "D") =>
		Render<MokaGauge>(p => p
				.Add(x => x.Value, 41d)
				.Add(x => x.Display, display)
				.Add(x => x.DisplaySize, size)
				.Add(x => x.SizeValue, "64px"))
			.Find(".moka-gauge-center").Children[0];

	[Fact]
	public void ItSizesTheDisplay()
	{
		IElement display = Display("1.75rem");

		Assert.Equal("moka-gauge-value moka-gauge-display", display.ClassName);
		Assert.Equal("font-size: 1.75rem", display.GetAttribute("style"));
	}

	[Fact]
	public void ItTakesALengthExpression() =>
		Assert.Equal("font-size: clamp(1rem, 40cqi, 2rem)", Display("clamp(1rem, 40cqi, 2rem)").GetAttribute("style"));

	// Unset, the display keeps the ring's proportion, as before.
	[Fact]
	public void TheDefault_WritesNoStyle() => Assert.Null(Display(null).GetAttribute("style"));

	[Theory]
	[InlineData("big")]
	[InlineData("2rem; color: red")]
	[InlineData("")]
	public void AValueThatIsntALength_IsIgnored(string size) => Assert.Null(Display(size).GetAttribute("style"));

	// It sizes the display only; the number keeps its place and size.
	[Fact]
	public void WithoutADisplay_TheNumberIsLeftAlone()
	{
		IElement number = Display("1.75rem", display: null);

		Assert.Equal("moka-gauge-value", number.ClassName);
		Assert.Null(number.GetAttribute("style"));
	}

	[Fact]
	public void TheFigureUnderIt_KeepsItsOwnSize()
	{
		IRenderedComponent<MokaGauge> cut = Render<MokaGauge>(p => p.Add(x => x.Value, 41d).Add(x => x.Display, "D").Add(x => x.DisplaySize, "1.75rem"));

		Assert.Null(cut.Find(".moka-gauge-figure").GetAttribute("style"));
	}
}
