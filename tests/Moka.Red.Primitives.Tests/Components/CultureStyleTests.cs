using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Meteors;
using Moka.Red.Primitives.NumberTicker;
using Moka.Red.Primitives.SwipeActions;

namespace Moka.Red.Primitives.Tests.Components;

// These wrote numbers into styles in the current culture. Swedish writes a decimal comma and the
// minus sign U+2212, and CSS reads neither, so the browser dropped the declaration.
public class CultureStyleTests : BunitContext
{
	private static void InSwedish(Action test)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		try
		{
			test();
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[Fact]
	public void NumberTicker_MovesItsDigitColumns() => InSwedish(() =>
	{
		IRenderedComponent<MokaNumberTicker> cut = Render<MokaNumberTicker>(p => p.Add(x => x.Value, 123d));

		Assert.Equal(
			["transform: translateY(-10%)", "transform: translateY(-20%)", "transform: translateY(-30%)"],
			cut.FindAll(".moka-number-ticker-column").Select(column => column.GetAttribute("style")));
	});

	[Fact]
	public void SwipeActions_FollowsThePointer() => InSwedish(() =>
	{
		IRenderedComponent<MokaSwipeActions> cut = Render<MokaSwipeActions>(p => p
			.Add(x => x.RightActions, "<button>Delete</button>")
			.AddChildContent("Row"));

		cut.Find(".moka-swipe-actions__content").PointerDown(new PointerEventArgs { ClientX = 200.5 });
		cut.Find(".moka-swipe-actions__content").PointerMove(new PointerEventArgs { ClientX = 150.25 });

		Assert.Equal("transform: translateX(-50.25px)",
			cut.Find(".moka-swipe-actions__content").GetAttribute("style"));
	});

	[Fact]
	public void Meteors_TakeANegativeAngle() => InSwedish(() =>
	{
		IRenderedComponent<MokaMeteors> cut = Render<MokaMeteors>(p => p.Add(x => x.Angle, -35));

		Assert.Contains("--meteor-angle: -35deg", cut.Find(".moka-meteors").GetAttribute("style"),
			StringComparison.Ordinal);
	});
}
