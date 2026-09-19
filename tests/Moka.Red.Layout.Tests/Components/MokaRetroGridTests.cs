using System.Globalization;
using Bunit;
using Moka.Red.Layout.RetroGrid;

namespace Moka.Red.Layout.Tests.Components;

public class MokaRetroGridTests : BunitContext
{
	// The angle went in through the current culture, and Swedish writes the minus sign as U+2212,
	// which CSS does not read, so a negative angle was dropped.
	[Fact]
	public void ANegativeAngle_IsWrittenWithAnAsciiMinusUnderAnyCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			IRenderedComponent<MokaRetroGrid> cut = Render<MokaRetroGrid>(p => p.Add(x => x.Angle, -30));

			Assert.Contains("--retro-angle: -30deg", cut.Find(".moka-retro-grid__plane").GetAttribute("style"),
				StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}
}
